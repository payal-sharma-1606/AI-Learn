using System.ClientModel;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using SmartNotes.Api.Exceptions;
using SmartNotes.Api.Interfaces;

namespace SmartNotes.Api.Services;

public class AiService : IAiService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Tags are meant to be short labels, not sentences. Anything longer is a sign the model
    /// wrote prose into the array, so we drop it rather than store it.
    /// </summary>
    private const int MaxTagLength = 30;

    private const int MaxTags = 5;

    private const string TagSystemPrompt =
        "You are a tagging assistant for a personal knowledge base. " +
        "Suggest 3-5 short topic tags for the text the user provides. " +
        "Each tag is 1-3 lowercase words, no punctuation, no hashtags. " +
        "Prefer specific technical or subject terms over generic words like 'notes' or 'text'. " +
        "Respond only with JSON in the form {\"tags\": [\"tag one\", \"tag two\"]} " +
        "and nothing else.";

    private readonly Lazy<ChatClient> _chatClient;
    private readonly ILogger<AiService> _logger;

    public AiService(IConfiguration configuration, ILogger<AiService> logger)
    {
        _logger = logger;
        // Built once and reused: constructing a client per request rebuilds the whole HTTP pipeline.
        // Lazy so that a missing key fails the AI call rather than application startup.
        _chatClient = new Lazy<ChatClient>(() => CreateChatClient(configuration));
    }

    public Task<string> SummarizeAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content must not be empty.", nameof(content));
        }

        return CompleteAsync(
            [new UserChatMessage($"Summarize the following text in 2-3 sentences:\n\n{content}")],
            options: null,
            cancellationToken);
    }

    public async Task<List<string>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content must not be empty.", nameof(content));
        }

        // JSON mode guarantees syntactically valid JSON, but not that the shape is the one we
        // asked for - the parsing below still has to be defensive.
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        var raw = await CompleteAsync(
            [new SystemChatMessage(TagSystemPrompt), new UserChatMessage(content)],
            options,
            cancellationToken);

        var tags = ParseTags(raw);
        if (tags.Count == 0)
        {
            _logger.LogWarning("Model returned no usable tags. Raw response: {Response}", raw);
            throw new AiServiceException(
                "The model did not return any usable tags. Please try again.",
                isTransient: true);
        }

        return tags;
    }

    /// <summary>
    /// Pulls tags out of the model's reply. Accepts the requested {"tags": [...]} shape and
    /// falls back to a bare array or any other single array-of-strings property, because a model
    /// asked for JSON does not always return the exact envelope it was asked for.
    /// </summary>
    internal static List<string> ParseTags(string raw)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(raw);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return [];
        }

        var array = root.ValueKind switch
        {
            JsonValueKind.Array => root,
            JsonValueKind.Object => FindTagArray(root),
            _ => (JsonElement?)null,
        };

        if (array is null)
        {
            return [];
        }

        var tags = new List<string>();
        foreach (var element in array.Value.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var tag = Normalize(element.GetString());
            if (tag is null || tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            tags.Add(tag);
            if (tags.Count == MaxTags)
            {
                break;
            }
        }

        return tags;
    }

    private static JsonElement? FindTagArray(JsonElement root)
    {
        if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            return tags;
        }

        // The model wrapped the array under some other key ("suggestions", "result", ...).
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var tag = value.Trim().TrimStart('#').Trim().ToLowerInvariant();

        // Commas would split into two tags once stored in the comma-separated Tags column.
        tag = tag.Replace(',', ' ').Trim();

        return tag.Length is > 0 and <= MaxTagLength ? tag : null;
    }

    private async Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options,
        CancellationToken cancellationToken)
    {
        // Bound the call so a stalled request cannot hold the connection open indefinitely.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            ChatCompletion completion = await _chatClient.Value.CompleteChatAsync(
                messages,
                options,
                timeoutCts.Token);

            // A reasoning model that spends its budget before emitting an answer returns no content.
            if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
            {
                _logger.LogWarning(
                    "Model returned no text (finish reason: {FinishReason}).",
                    completion.FinishReason);
                throw new AiServiceException("The model returned an empty response.", isTransient: true);
            }

            return completion.Content[0].Text;
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            _logger.LogWarning(ex, "Azure OpenAI rate limit hit.");
            throw new AiServiceException(
                "The AI service is rate limited right now. Please try again in a moment.",
                isTransient: true,
                ex);
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "Azure OpenAI request failed with status {Status}.", ex.Status);
            throw new AiServiceException(
                "The AI service could not complete the request.",
                isTransient: ex.Status >= 500,
                ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Our own timeout fired; a caller-initiated cancellation is left to propagate.
            _logger.LogWarning("Azure OpenAI request timed out after {Timeout}.", RequestTimeout);
            throw new AiServiceException(
                "The AI service took too long to respond. Please try again.",
                isTransient: true);
        }
    }

    private static ChatClient CreateChatClient(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deploymentName = configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new AiServiceException(
                "The AI service is not configured. Set AzureOpenAI Endpoint, ApiKey and DeploymentName.",
                isTransient: false);
        }

        return new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
            .GetChatClient(deploymentName);
    }
}

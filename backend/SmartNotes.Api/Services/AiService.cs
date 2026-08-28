using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace SmartNotes.Api.Services;

/// <summary>
/// Raised when a summary could not be produced. <see cref="IsTransient"/> distinguishes
/// "try again in a moment" (rate limits, timeouts) from "this will keep failing".
/// </summary>
public class AiServiceException : Exception
{
    public AiServiceException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    public bool IsTransient { get; }
}

public interface IAiService
{
    Task<string> SummarizeAsync(string content, CancellationToken cancellationToken = default);
}

public class AiService : IAiService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);

    private readonly Lazy<ChatClient> _chatClient;
    private readonly ILogger<AiService> _logger;

    public AiService(IConfiguration configuration, ILogger<AiService> logger)
    {
        _logger = logger;
        // Built once and reused: constructing a client per request rebuilds the whole HTTP pipeline.
        // Lazy so that a missing key fails the summarize call rather than application startup.
        _chatClient = new Lazy<ChatClient>(() => CreateChatClient(configuration));
    }

    public async Task<string> SummarizeAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content must not be empty.", nameof(content));
        }

        // Bound the call so a stalled request cannot hold the connection open indefinitely.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        var prompt = $"Summarize the following text in 2-3 sentences:\n\n{content}";

        try
        {
            ChatCompletion completion = await _chatClient.Value.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                options: null,
                cancellationToken: timeoutCts.Token);

            // A reasoning model that spends its budget before emitting an answer returns no content.
            if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
            {
                _logger.LogWarning(
                    "Model returned no summary text (finish reason: {FinishReason}).",
                    completion.FinishReason);
                throw new AiServiceException("The model returned an empty summary.", isTransient: true);
            }

            return completion.Content[0].Text;
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            _logger.LogWarning(ex, "Azure OpenAI rate limit hit while summarizing.");
            throw new AiServiceException(
                "The AI service is rate limited right now. Please try again in a moment.",
                isTransient: true,
                ex);
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "Azure OpenAI request failed with status {Status}.", ex.Status);
            throw new AiServiceException(
                "The AI service could not generate a summary.",
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

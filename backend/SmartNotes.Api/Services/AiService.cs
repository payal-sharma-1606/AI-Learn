using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace SmartNotes.Api.Services;

public interface IAiService
{
    Task<string> SummarizeAsync(string content);
}

public class AiService : IAiService
{
    private readonly IConfiguration _configuration;

    public AiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SummarizeAsync(string content)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var apiKey = _configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured.");
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        var chatClient = client.GetChatClient(deploymentName);

        var prompt = $"Summarize the following text in 2-3 sentences: {content}";

        ChatCompletion completion = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));

        return completion.Content[0].Text;
    }
}

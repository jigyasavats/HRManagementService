using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace HRManagementService.AIService;

public class AIManager
{
    private readonly ChatClient _chatClient;

    public AIManager(string endpoint, string apiKey, string deploymentName)
    {
        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey)
        );

        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 800
        };

        try
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            return $"[AI Error] {ex.Message}";
        }
    }
}

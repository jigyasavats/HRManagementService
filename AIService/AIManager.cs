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

    public async Task StartHRChatbotAsync()
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("   HR Policy Chatbot");
        Console.WriteLine("========================================");
        Console.WriteLine("  Ask me anything about company policies,");
        Console.WriteLine("  holidays, promotions, salary, etc.");
        Console.WriteLine("  Type 'exit' to go back.\n");

        var systemPrompt = @"You are an HR policy assistant for a company. Answer employee questions about:
- Holidays: Employees get fixed public holidays + a personal holiday bank (casual, sick, earned leave). They can request holidays which need manager approval.
- Promotions: Managers propose employees for promotion based on performance reviews. HR reviews the proposal, decides the new level and salary, checks budget, then approves or rejects.
- Salary: Salary is based on levels (L1-L10). Each level has a min and max range. HR sets salary within the range during promotion or onboarding.
- Performance Reviews: Employees submit a yearly self-assessment (accomplishments, improvements, goals, self-rating 1-5). Manager reviews and gives their rating.
- Termination: HR can terminate employees. It's a soft delete — records are kept but account is deactivated.
- Teams: Each team has a manager and skip manager. Employees belong to teams.
Be concise, professional, and helpful. Keep answers under 100 words.";

        while (true)
        {
            Console.Write("  You: ");
            var question = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(question)) continue;
            if (question.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            Console.WriteLine("\n  Thinking...\n");
            var response = await GetCompletionAsync(systemPrompt, question);
            Console.WriteLine($"  HR Bot: {response}\n");
        }

        Console.WriteLine("  Chatbot closed.");
    }
}

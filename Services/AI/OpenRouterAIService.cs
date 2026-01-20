using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Models;
using System.Net.Http.Headers;
using ChatAISystem.Helper;

public class OpenRouterAIService : IAIService
{
    private readonly ChatAIDBContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const int ContextMessageLimit = 12;
    private const string OpenRouterUrl = "https://openrouter.ai/api/v1/chat/completions";
    private const string DefaultModel = "deepseek/deepseek-chat";

    public OpenRouterAIService(
        ChatAIDBContext dbContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["AI:OpenRouterApiKey"]
            ?? throw new ArgumentNullException("AI:OpenRouterApiKey not configured");
    }

    private static readonly string[] ModelFallbackChain =
{
    "deepseek/deepseek-chat",
    "mistralai/mistral-7b-instruct",
    "meta-llama/llama-3-8b-instruct"
};

    private async Task<(bool success, string result)> TryGenerateWithModelAsync(
    string model,
    List<object> messages)
    {
        var payload = new
        {
            model,
            messages,
            temperature = 0.65,
            top_p = 0.85,
            max_tokens = 150
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterUrl)
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        request.Headers.Add("HTTP-Referer", "https://localhost");
        request.Headers.Add("X-Title", "ChatAISystem");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (false, $"""
        ❌ Model failed: {model}
        Status: {(int)response.StatusCode}
        Response:
        {responseBody}
        """);
        }

        using var doc = JsonDocument.Parse(responseBody);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return (true, text ?? "...");
    }

    private async Task<List<object>> BuildMessagesAsync(
    int userId,
    int characterId)
    {
        var messages = new List<object>();

        var character = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (!string.IsNullOrWhiteSpace(character?.Description))
        {
            messages.Add(new
            {
                role = "system",
                content = $"""
            You are roleplaying as this character:
            {character.Description}

            Rules:
            - Stay in character
            - Speak English
            - Use *actions* naturally
            - Never mention AI
            """
            });
        }

        var history = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderByDescending(c => c.Timestamp)
            .Take(ContextMessageLimit)
            .OrderBy(c => c.Timestamp)
            .ToListAsync();

        foreach (var msg in history)
        {
            messages.Add(new
            {
                role = Utilities.NormalizeRole(msg.Role),
                content = msg.MessageText
            });
        }

        return messages;
    }


    public async Task<string> GenerateResponseAsync(int userId, int characterId)
    {
        try
        {
            var messages = await BuildMessagesAsync(userId, characterId);

            var errors = new StringBuilder();

            foreach (var model in ModelFallbackChain)
            {
                var (success, result) =
                    await TryGenerateWithModelAsync(model, messages);

                if (success)
                    return result;

                errors.AppendLine(result);
            }

            return $"""
        ⚠️ All models failed.

        {errors}
        """;
        }
        catch (Exception ex)
        {
            return $"""
        ❌ Exception thrown
        {ex.Message}
        """;
        }
    }


}

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Models;

public interface IAIService
{
    Task<string> GenerateResponseAsync(int userId, int characterId);
}

public class OllamaAIService : IAIService
{
    private readonly ChatAIDBContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly string _ollamaUrl;

    private const int ContextMessageLimit = 12;

    public OllamaAIService(
        ChatAIDBContext dbContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _httpClient = httpClientFactory.CreateClient();
        _ollamaUrl = configuration["AI:OllamaUrl"]
            ?? throw new ArgumentNullException("AI:OllamaUrl not configured");
    }

    public async Task<string> GenerateResponseAsync(int userId, int characterId)
    {
        try
        {
            var promptParts = new List<string>();

            var character = await _dbContext.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == characterId);

            if (!string.IsNullOrWhiteSpace(character?.Description))
            {
                promptParts.Add($"""
                You are roleplaying as this character:
                {character.Description}

                Rules:
                - Stay in character
                - Speak English
                - Use *actions* naturally
                - Never mention AI
                """);
            }

            var history = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderByDescending(c => c.Timestamp)
                .Take(ContextMessageLimit)
                .OrderBy(c => c.Timestamp)
                .Select(c => $"{c.Role}: {c.MessageText}")
                .ToListAsync();

            promptParts.AddRange(history);

            string prompt = string.Join("\n", promptParts) + "\nAssistant:";

            var payload = new
            {
                model = "llama3.2:3b",
                prompt,
                temperature = 0.65,
                top_p = 0.85,
                num_predict = 100,
                stream = false
            };

            var response = await _httpClient.PostAsync(
                _ollamaUrl,
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            if (!response.IsSuccessStatusCode)
                return "The character hesitates, unsure of what to say.";

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return doc.RootElement.TryGetProperty("response", out var text)
                ? text.GetString() ?? "..."
                : "...";
        }
        catch (Exception ex)
        {
            //Console.WriteLine("OLLAMA ERROR:");
            //Console.WriteLine(ex.ToString());
            return "The character remains silent.";
        }

    }
}

using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using ChatAISystem.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class ChatHub : Hub
{
    private readonly ChatAIDBContext _dbContext;
    private readonly string _apiKey;
    private const int MaxMessagesPerCharacter = 1000; // Limit of messages per character
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHub(ChatAIDBContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = context;
        _apiKey = configuration["ApiKeys:Gemini"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey cannot be null");
        _httpContextAccessor = httpContextAccessor;
    }

    // Loads paginated messages for the chat history
    public async Task LoadChatHistory(int userId, int characterId, int page, int pageSize)
    {
        try
        {
            if (userId <= 0 || characterId <= 0 || page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("Invalid parameters");
            }

            int skipMessages = (page - 1) * pageSize;

            var messages = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderByDescending(c => c.Timestamp)
                .Skip(skipMessages)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Role,
                    c.MessageText,
                    Timestamp = c.Timestamp.ToUniversalTime().ToString("o")
                })
                .ToListAsync();

            var jsonMessages = JsonSerializer.Serialize(messages);
            await Clients.Caller.SendAsync("LoadChatHistory", jsonMessages);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
            await Clients.Caller.SendAsync("LoadChatHistoryError", ex.Message);
        }
    }

    public async Task SendMessage(int userId, int characterId, string message)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Error: User not found.");
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        int? lastCharacterId = httpContext?.Session.GetInt32("LastCharacterId");
        string? characterName = _dbContext.Characters.Find(characterId)?.Name;

        var userMessage = new Conversation
        {
            UserId = userId,
            CharacterId = characterId,
            Role = "user",
            MessageText = message,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(userMessage);
        await _dbContext.SaveChangesAsync();

        await EnsureMessageLimit(userId, characterId);

        await Clients.All.SendAsync("ReceiveMessage", user.Username, message);

        string aiResponse = await GetAIResponse(userId, characterId);

        var aiMessage = new Conversation
        {
            UserId = userId,
            CharacterId = characterId,
            Role = "ai",
            MessageText = aiResponse,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(aiMessage);
        await _dbContext.SaveChangesAsync();

        await EnsureMessageLimit(userId, characterId);

        await Clients.All.SendAsync("ReceiveMessage", "AI", aiResponse);
    }

    /// <summary>
    /// Keeps only the most recent messages in the database.
    /// </summary>
    private async Task EnsureMessageLimit(int userId, int characterId)
    {
        var messagesToDelete = await _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderByDescending(c => c.Timestamp)
            .Skip(MaxMessagesPerCharacter)
            .ToListAsync();

        if (messagesToDelete.Any())
        {
            _dbContext.Conversations.RemoveRange(messagesToDelete);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<string> GetAIResponse(int userId, int characterId)
    {
        try
        {
            var messages = new List<APIMessage.Message>();

            var character = await _dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == characterId);
            if (character?.Description != null)
            {
                string enhancedPrompt = $@"You are roleplaying as the following character: {character.Description}

Instructions:
1. Always respond in **English**.
2. Stay completely in character — use their knowledge, tone, and personality.
3. If there is no prior conversation, **start by briefly introducing yourself** in-character.
4. Include physical expressions or emotions between asterisks. Example: *smiles*, *glances around nervously*.
5. Naturally integrate these actions within dialogue — don't add them all at the end.
6. Never say you're an AI or break character.
7. Be concise and direct like the character would be.";

                messages.Add(new APIMessage.Message { role = "system", content = enhancedPrompt });
            }



            var chatHistory = await _dbContext.Conversations.AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderBy(c => c.Timestamp)
                .Select(c => new APIMessage.Message { role = c.Role, content = c.MessageText })
                .ToListAsync();

            messages.AddRange(chatHistory);

            string combinedPrompt = string.Join("\n", messages.Select(m => $"{m.role}: {m.content}"));

            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            using var client = new RestClient(endpoint);
            var request = new RestRequest("", Method.Post)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            request.AddHeader("Content-Type", "application/json");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = combinedPrompt }
                        }
                    }
                }
            };
            request.AddJsonBody(requestBody);

            var response = await client.ExecuteAsync(request);
            Console.WriteLine("Raw Gemini response: " + response.Content);

            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                var geminiResponse = JsonSerializer.Deserialize<APIMessage.GeminiResponse>(response.Content);
                if (geminiResponse?.candidates != null && geminiResponse.candidates.Any())
                {
                    var candidate = geminiResponse.candidates.FirstOrDefault();
                    if (candidate?.content?.parts != null && candidate.content.parts.Any())
                    {
                        var outputText = candidate.content.parts.First().text;
                        if (!string.IsNullOrEmpty(outputText))
                        {
                            if (outputText.StartsWith("assistant:", StringComparison.OrdinalIgnoreCase))
                            {
                                outputText = outputText.Substring("assistant:".Length).Trim();
                            }

                            return outputText;
                        }
                    }
                }
                return "Error: AI response does not contain valid messages.";
            }
            else
            {
                return $"API Error: {response.StatusCode} - {response.Content}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAIResponse: {ex.Message}");
            return "Error retrieving AI response.";
        }
    }
}

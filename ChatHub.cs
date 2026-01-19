using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using ChatAISystem.Models;
using ChatAISystem.Services.Interfaces;

public class ChatHub : Hub
{
    private readonly ChatAIDBContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private const int MaxMessagesPerCharacter = 1000;
    private const int ContextMessageLimit = 12;
    private readonly IAIService _aiService;
    private readonly IConversationService _conversationService;


    public ChatHub(
        ChatAIDBContext context,
        IHttpContextAccessor httpContextAccessor,
        IAIService aiService,
        IConversationService conversationService)
    {
        _dbContext = context;
        _httpContextAccessor = httpContextAccessor;
        _aiService = aiService;
        _conversationService = conversationService;
    }

    // ============================
    // LOAD CHAT HISTORY
    // ============================
    public async Task LoadChatHistory(int userId, int characterId, int page, int pageSize)
    {
        if (userId <= 0 || characterId <= 0 || page <= 0 || pageSize <= 0)
            return;

        int skip = (page - 1) * pageSize;

        var messages = await _conversationService.GetHistoryAsync(
            userId,
            characterId,
            skip,
            pageSize
        );

        await Clients.Caller.SendAsync(
            "LoadChatHistory",
            messages.Select(m => new
            {
                m.Role,
                m.MessageText,
                Timestamp = m.Timestamp.ToUniversalTime().ToString("o")
            })
        );
    }


    // ============================
    // SEND MESSAGE
    // ============================
    public async Task SendMessage(int userId, int characterId, string message)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return;

        string? characterName = await _dbContext.Characters
            .Where(c => c.Id == characterId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        // Guardar mensaje del usuario
        await _conversationService.SaveUserMessageAsync(
            userId,
            characterId,
            message
        );

        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            user.Username,
            message
        );

        await Clients.Caller.SendAsync(
            "AIWritingStarted",
            characterName ?? "The character"
        );

        try
        {
            var aiResponse = await _aiService.GenerateResponseAsync(
                userId,
                characterId
            );

            await _conversationService.SaveAIMessageAsync(
                userId,
                characterId,
                aiResponse
            );

            await Clients.Caller.SendAsync(
                "ReceiveMessage",
                "AI",
                aiResponse
            );
        }
        finally
        {
            await Clients.Caller.SendAsync("AIWritingFinished");
        }
    }



    // ============================
    // MESSAGE LIMIT
    // ============================
    private async Task EnsureMessageLimit(int userId, int characterId)
    {
        var excessMessages = await _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderByDescending(c => c.Timestamp)
            .Skip(MaxMessagesPerCharacter)
            .ToListAsync();

        if (excessMessages.Any())
        {
            _dbContext.Conversations.RemoveRange(excessMessages);
            await _dbContext.SaveChangesAsync();
        }
    }
}

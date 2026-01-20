using ChatAISystem.Models;
using ChatAISystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatAISystem.Services.ConversationService
{
    public class ConversationService : IConversationService
    {
        private readonly ChatAIDBContext _dbContext;
        private const int MaxMessagesPerCharacter = 1000;

        public ConversationService(ChatAIDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveUserMessageAsync(int userId, int characterId, string message)
        {
            await SaveMessageAsync(userId, characterId, "user", message);
        }

        public async Task SaveAIMessageAsync(int userId, int characterId, string message)
        {
            await SaveMessageAsync(userId, characterId, "ai", message);
        }

        private async Task SaveMessageAsync(
            int userId,
            int characterId,
            string role,
            string message)
        {
            var conversation = new Conversation
            {
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                MessageText = message,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();

            await EnsureMessageLimitAsync(userId, characterId);
        }

        public async Task<IReadOnlyList<Conversation>> GetHistoryAsync(
            int userId,
            int characterId,
            int skip,
            int take)
        {
            return await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderByDescending(c => c.Timestamp)
                .Skip(skip)
                .Take(take)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }

        private async Task EnsureMessageLimitAsync(int userId, int characterId)
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
}

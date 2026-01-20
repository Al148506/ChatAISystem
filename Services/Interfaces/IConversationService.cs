using ChatAISystem.Models;

namespace ChatAISystem.Services.Interfaces
{
    public interface IConversationService
    {
        Task SaveUserMessageAsync(int userId, int characterId, string message);
        Task SaveAIMessageAsync(int userId, int characterId, string message);
        Task<IReadOnlyList<Conversation>> GetHistoryAsync(
            int userId, int characterId, int skip, int take);
    }
}

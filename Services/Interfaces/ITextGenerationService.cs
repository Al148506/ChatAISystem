using static ChatAISystem.Models.APIMessage;

namespace ChatAISystem.Services.Interfaces
{
    public interface ITextGenerationService
    {
        Task<string> GenerateAsync(
            List<ChatMessage> messages,
            string model,
            double temperature = 0.7
        );
    }

}

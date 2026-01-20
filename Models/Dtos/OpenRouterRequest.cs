using static ChatAISystem.Models.APIMessage;

namespace ChatAISystem.Models.Dtos
{
    public class OpenRouterRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = new();
        public double Temperature { get; set; }
    }

}

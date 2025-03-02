namespace ChatAISystem.Models
{
    public class APIMessage
    {
        // Clases para deserializar la respuesta de la API
        public class Message
        {
            public required string role { get; set; }
            public required string content { get; set; }
        }
        public class GeminiResponse
        {
            public Candidate[] candidates { get; set; }
        }

        public class Candidate
        {
            public Content content { get; set; }
            public string finishReason { get; set; }
            public double avgLogprobs { get; set; }
        }

        public class Content
        {
            public Part[] parts { get; set; }
            public string role { get; set; }
        }

        public class Part
        {
            public string text { get; set; }
        }


    }
}

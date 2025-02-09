namespace ChatAISystem.Models
{
    // Clase para mapear la respuesta del reCAPTCHA
    public class CaptchaResult
    {
        public bool success { get; set; }
        public string challenge_ts { get; set; } // Marca de tiempo del desafío
        public string hostname { get; set; } // Nombre del host del cliente
    }
}

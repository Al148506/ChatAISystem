using ChatAISystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatAISystem.Helper
{
    public class Utilities
    {

        public static string ConverterSha256(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                throw new ArgumentNullException(nameof(texto), "El texto no puede ser nulo o vacío.");
            }
            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        // Método para validar formato de correo electrónico
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), "El texto no puede ser nulo o vacío.");
            }
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return emailRegex.IsMatch(email);
        }

        public static string CleanField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException(nameof(field), "El texto no puede ser nulo o vacío.");
            }
            field = field.Trim();
            field = Regex.Replace(field, @"\s+", " ");
            return field;

        }

        public static bool ValidateLinkImage(string url)
        {
            if (string.IsNullOrEmpty(url)) {
                throw new ArgumentNullException(nameof(url), "El texto no puede ser nulo o vacío.");
            }
            var urlRegex = new Regex(@"(http(s?):)([/|.|\w|\s|-])*\.(?:jpg|gif|png)");
            return urlRegex.IsMatch(url);
        }

        public async Task<bool> ValidateCaptcha(string captchaResponse, IConfiguration _configuration)
        {
            var secretKey = _configuration.GetValue<string>("GoogleReCaptcha:SecretKey");
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}",
                    null);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var captchaResult = System.Text.Json.JsonSerializer.Deserialize<CaptchaResult>(jsonResponse);

                return captchaResult != null && captchaResult.success;
            }
        }

    }
}

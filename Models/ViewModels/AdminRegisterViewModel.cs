using System.ComponentModel.DataAnnotations;

namespace ChatAISystem.Models.ViewModels
{
    public class AdminRegisterViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string Username { get; set; } = null!;
        public string? Password { get; set; }

        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

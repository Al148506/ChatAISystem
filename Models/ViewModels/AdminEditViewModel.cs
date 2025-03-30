using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ChatAISystem.Models.ViewModels
{
    public class AdminEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        [Remote(action: "IsEmailAvailable", controller: "User", ErrorMessage = "This email has already been registered.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = null!;
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }  // No es obligatorio en edición

        [Required(ErrorMessage = "Rol is required.")]
        public string Role { get; set; } = "User";
    }
}
using ChatAISystem.Helper;
using ChatAISystem.Models.ViewModels;
using ChatAISystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class UserValidationService : IUserValidationService
{
    private readonly IConfiguration _configuration;
    private readonly Utilities _utilities;

    public UserValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _utilities = new Utilities();
    }

    public async Task<(bool success, string message)> ValidateRegistrationAsync(
        RegisterViewModel model,
        IFormCollection form)
    {
        if (model == null)
        {
            return (false, "Invalid request data.");
        }

        var captchaResponse = form["g-recaptcha-response"];

        if (string.IsNullOrEmpty(captchaResponse))
        {
            return (false, "reCAPTCHA token was not found.");
        }

        var isCaptchaValid = await _utilities.ValidateCaptcha(captchaResponse, _configuration);
        if (!isCaptchaValid)
        {
            return (false, "Please complete the reCAPTCHA to continue.");
        }

        if (string.IsNullOrWhiteSpace(model.Username) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            return (false, "Please fill in all required fields.");
        }

        if (!Utilities.IsValidEmail(model.Email))
        {
            return (false, "Please enter a valid email address.");
        }

        if (model.Password.Length < 6)
        {
            return (false, "Password must be at least 6 characters long.");
        }

        if (model.Password != model.ConfirmPassword)
        {
            return (false, "Passwords do not match.");
        }

        return (true, "Validation successful.");
    }
}

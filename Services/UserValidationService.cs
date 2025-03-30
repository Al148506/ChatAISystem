using ChatAISystem.Helper;
using ChatAISystem.Models.ViewModels;
using ChatAISystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
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

    public async Task<(bool success, string message)> ValidateRegistrationAsync(RegisterViewModel model, IFormCollection form)
    {
        if (model == null)
        {
            return (false, "Datos inválidos.");
        }

        var captchaResponse = form["g-recaptcha-response"];

        if (string.IsNullOrEmpty(captchaResponse))
        {
            return (false, "Captcha no encontrado en el formulario.");
        }

        var isCaptchaValid = await _utilities.ValidateCaptcha(captchaResponse, _configuration);
        if (!isCaptchaValid)
        {
            return (false, "Por favor, resuelva el reCAPTCHA para continuar.");
        }

        if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return (false, "Por favor, ingrese todos los datos.");
        }

        if (!Utilities.IsValidEmail(model.Email))
        {
            return (false, "Ingrese un correo electrónico válido.");
        }

        if (model.Password.Length < 6)
        {
            return (false, "La contraseña debe tener al menos 6 caracteres.");
        }

        if (model.Password != model.ConfirmPassword)
        {
            return (false, "Las contraseñas no coinciden.");
        }

        return (true, "Validación exitosa");
    }
}

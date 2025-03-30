using ChatAISystem.Models.ViewModels;

namespace ChatAISystem.Services.Interfaces
{
    public interface IUserValidationService
    {
        Task<(bool success, string message)> ValidateRegistrationAsync(RegisterViewModel model, IFormCollection form);
    }
}

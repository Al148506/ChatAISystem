using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
namespace ChatAISystem.Permissions
{
    public class RoleValidationAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;
        public RoleValidationAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // Obtener el rol del usuario desde la sesión
            var userRole = session.GetString("Role");

            if (userRole == null || !_allowedRoles.Contains(userRole))
            {
                // Redirigir al usuario si no tiene acceso
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }

            base.OnActionExecuting(context);
        }

    }
}

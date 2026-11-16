using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace QuattroLingo.Tests.Controllers
{
    /// <summary>Shared setup for controller unit tests.</summary>
    internal static class ControllerTestHelpers
    {
        /// <summary>Signs the controller in as a user with the given JWT claims (null claims are left out).</summary>
        public static T WithUser<T>(this T controller, string? userId, string? email = null, string? role = null)
            where T : ControllerBase
        {
            var claims = new List<Claim>();
            if (userId is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId));
            if (email is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
            if (role is not null) claims.Add(new Claim("role", role));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        /// <summary>Leaves the controller with no signed-in user, like an anonymous request.</summary>
        public static T Anonymous<T>(this T controller) where T : ControllerBase
        {
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        /// <summary>
        /// True when the action needs a signed-in user: [Authorize] on the action or its controller,
        /// and no [AllowAnonymous] on the action.
        /// </summary>
        public static bool RequiresAuthorization<TController>(string action)
        {
            var method = FindAction<TController>(action);
            if (method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)) return false;

            return method.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                   || typeof(TController).IsDefined(typeof(AuthorizeAttribute), inherit: true);
        }

        /// <summary>The HTTP verb and full route ("api/rooms/{code}") an action is mapped to.</summary>
        public static (string Verb, string Route) RouteOf<TController>(string action)
        {
            var method = FindAction<TController>(action);
            var http = method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Single();
            var prefix = typeof(TController).GetCustomAttribute<RouteAttribute>()!.Template
                .Replace("[controller]", typeof(TController).Name.Replace("Controller", "").ToLowerInvariant());

            var route = string.IsNullOrEmpty(http.Template) ? prefix : $"{prefix}/{http.Template}";
            return (http.HttpMethods.Single(), route);
        }

        private static MethodInfo FindAction<TController>(string action) =>
            typeof(TController).GetMethod(action, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new ArgumentException($"{typeof(TController).Name} has no action '{action}'.");

        /// <summary>Reads a property of an anonymous-type response body.</summary>
        public static object? Property(this object? value, string name) =>
            value!.GetType().GetProperty(name)?.GetValue(value);
    }
}

using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Middlewares
{
    public class BasicAuthenticationMiddleware
    {
        private const string BasicAuthenticationType = "Basic";

        private readonly RequestDelegate _next;
        private readonly IOptionsSnapshot<BasicAuthenticationOptions> _options;

        public BasicAuthenticationMiddleware(RequestDelegate next, 
            IOptionsSnapshot<BasicAuthenticationOptions> options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            if (_options.Value.ExcludePaths.Contains(context.Request.Path.Value, 
                StringComparer.OrdinalIgnoreCase))
            {
                await _next.Invoke(context);
                return;
            }

            string authHeader = context.Request.Headers["Authorization"];

            if (authHeader != null && authHeader.StartsWith(BasicAuthenticationType))
            {
                var authHeaderSplit = authHeader.Split(new[] {' '},
                    StringSplitOptions.RemoveEmptyEntries);

                if (authHeaderSplit.Length == 2)
                {
                    var encodedUsernamePassword = authHeaderSplit[1];

                    var base64 = GetBase64Bytes(encodedUsernamePassword);
                    if (base64 != null)
                    {
                        var encoding = Encoding.GetEncoding("iso-8859-1");
                        var usernamePassword = encoding.GetString(base64);
                        var seperatorIndex = usernamePassword.IndexOf(':');

                        if (seperatorIndex > 0)
                        {
                            var username = usernamePassword.Substring(0, seperatorIndex);
                            var password = usernamePassword.Substring(seperatorIndex + 1);

                            if (username == _options.Value.TestUsername && 
                                password == _options.Value.TestPassword)
                            {
                                context.User = GetClaimsPrincipal(username);

                                await _next.Invoke(context);
                                return;
                            }
                        }
                    }
                }
            }

            SetResponseUnauthorized(context.Response);
        }

        private static byte[] GetBase64Bytes(string value)
        {
            byte[] base64;

            try
            {
                base64 = Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }

            return base64;
        }

        private static ClaimsPrincipal GetClaimsPrincipal(string username)
        {
            var claims = new[] { new Claim("name", username),
                                new Claim(ClaimTypes.Role, "Admin") };

            var identity = new ClaimsIdentity(claims, BasicAuthenticationType);
            return new ClaimsPrincipal(identity);
        }

        private static void SetResponseUnauthorized(HttpResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.Headers.Append(HeaderNames.WWWAuthenticate, "Basic realm=\"Arbitrary Realm\"");
        }
    }

    public static class BasicAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BasicAuthenticationMiddleware>();
        }
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace kestrelssl
{
    public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public ConfigureJwtBearerOptions(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            Configure(options);
        }

        public void Configure(JwtBearerOptions options)
        {
            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new PreserveOriginalClaimsValidater());

            ConfigureTokenValidationParameters(options.TokenValidationParameters);
        }
        
        private void ConfigureTokenValidationParameters(TokenValidationParameters validationParams)
        {
            // TODO: support validation of issuer
            validationParams.ValidateIssuer = false;
            validationParams.ValidateLifetime = false;
            validationParams.LifetimeValidator =
                (before, expires, token, parameters) => true; // skip validation

            validationParams.ValidateAudience = false;
            validationParams.AudienceValidator = AudienceValidator;

            validationParams.ValidateIssuerSigningKey = false;
            validationParams.IssuerSigningKeyResolver = IssuerSigningKeyResolver;
        }

        private bool AudienceValidator(IEnumerable<string> audiences, SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            return true;
        }

        private IEnumerable<SecurityKey> IssuerSigningKeyResolver(
            string token,
            SecurityToken securityToken,
            string kid,
            TokenValidationParameters validationParameters)
        {
            var arr = new List<SecurityKey>();
            arr.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")));
            return arr;
        }

        private sealed class PreserveOriginalClaimsValidater : JwtSecurityTokenHandler
        {
            // Keep the original claims for https://github.com/Azure/azure-signalr/issues/429
            protected override ClaimsIdentity CreateClaimsIdentity(JwtSecurityToken jwtToken, string issuer, TokenValidationParameters validationParameters)
            {
                var originalClaims = jwtToken.Claims;
                var transformedClaims = base.CreateClaimsIdentity(jwtToken, issuer, validationParameters);
                foreach (var claim in originalClaims)
                {
                    if (transformedClaims.FindFirst(claim.Type) == null)
                    {
                        transformedClaims.AddClaim(claim);
                    }
                }

                return transformedClaims;
            }
        }
    }
    
}

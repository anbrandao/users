using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using users_api.Domain.Entities;

namespace users_api.Identity
{
    public class TokenService
    {
        private readonly byte[] _keyBytes;

        public TokenService(IConfiguration configuration)
        {
            var keyStr =
                configuration["Jwt:SymmetricSecurityKey"] ??
                configuration["Jwt__SymmetricSecurityKey"] ??
                configuration["Jwt:SymmetricKey"] ??
                configuration["Jwt__SymmetricKey"] ??
                configuration["JWT:Secret"] ??
                configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(keyStr))
                throw new InvalidOperationException(
                    "JWT secret não configurada. Defina 'Jwt:SymmetricSecurityKey' (ou Jwt__SymmetricSecurityKey).");

            if (IsBase64(keyStr)) _keyBytes = Convert.FromBase64String(keyStr.Trim());
            else _keyBytes = Encoding.UTF8.GetBytes(keyStr);

            if (_keyBytes.Length < 16)
                throw new ArgumentOutOfRangeException(nameof(keyStr), "JWT secret deve ter pelo menos 128 bits (16 bytes).");
        }

        public string GenerateToken(Usuario usuario)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.UserName ?? usuario.Nome ?? "user"),
                new(ClaimTypes.NameIdentifier, usuario.Id ?? string.Empty),
                new(ClaimTypes.Role, usuario.Role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(_keyBytes), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool IsBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            if (value.Length % 4 != 0) return false;
            foreach (var c in value)
            {
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=';
                if (!ok) return false;
            }
            return true;
        }
    }
}

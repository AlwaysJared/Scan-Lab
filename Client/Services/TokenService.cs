using System;
using System.IdentityModel.Tokens.Jwt;

namespace Client.Services
{
    public class TokenService
    {
        private string? _jwtToken;

        public string? JwtToken
        {
            get => _jwtToken;
            set => _jwtToken = value;
        }

        public bool HasValidToken()
        {
            if (string.IsNullOrEmpty(_jwtToken))
                return false;

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(_jwtToken))
                return false;

            var token = handler.ReadJwtToken(_jwtToken);
            var expiry = token.ValidTo.ToUniversalTime();

            return expiry > DateTime.UtcNow;
        }

        public void ClearToken()
        {
            _jwtToken = null;
        }
    }
}

namespace ProductApi.Application.Interfaces;

public interface ITokenGenerator
{
    (string token, DateTime expiresOn) GenerateAccessToken(string userName, string role);
    string GenerateRefreshToken();
}

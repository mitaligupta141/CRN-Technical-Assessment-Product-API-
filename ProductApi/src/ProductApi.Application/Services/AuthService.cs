using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;

namespace ProductApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthService(IUnitOfWork uow, ITokenGenerator tokenGenerator, ILogger<AuthService> logger)
    {
        _uow = uow;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        var exists = await _uow.Users.AnyAsync(u => u.UserName == dto.UserName, ct);
        if (exists)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["UserName"] = new[] { "A user with this username already exists." }
            });

        var user = new ApplicationUser
        {
            UserName = dto.UserName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role,
            CreatedOn = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("New user registered: {UserName}", user.UserName);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var user = await _uow.Users.Query(asNoTracking: true)
            .FirstOrDefaultAsync(u => u.UserName == dto.UserName, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Invalid username or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken ct = default)
    {
        var storedToken = await _uow.RefreshTokens.Query(asNoTracking: false)
            .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedAppException("Invalid or expired refresh token.");

        var user = await _uow.Users.Query(asNoTracking: true)
            .FirstOrDefaultAsync(u => u.UserName == storedToken.UserName, ct);

        if (user is null)
            throw new UnauthorizedAppException("Invalid or expired refresh token.");

        // Rotate: revoke the old refresh token and issue a brand new pair.
        storedToken.RevokedOn = DateTime.UtcNow;

        var response = await IssueTokensAsync(user, ct);
        storedToken.ReplacedByToken = response.RefreshToken;

        _uow.RefreshTokens.Update(storedToken);
        await _uow.SaveChangesAsync(ct);

        return response;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _uow.RefreshTokens.Query(asNoTracking: false)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            throw new NotFoundException(nameof(RefreshToken), refreshToken);

        storedToken.RevokedOn = DateTime.UtcNow;
        _uow.RefreshTokens.Update(storedToken);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<AuthResponseDto> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var (accessToken, expiresOn) = _tokenGenerator.GenerateAccessToken(user.UserName, user.Role);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            Token = refreshToken,
            UserName = user.UserName,
            ExpiresOn = DateTime.UtcNow.Add(RefreshTokenLifetime)
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresOn = expiresOn
        };
    }
}

using Microsoft.Extensions.Logging;
using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Kayıt isteği alındı: {Email}", request.Email);

        if (await userRepository.ExistsAsync(request.Email, ct))
            throw new ConflictException($"'{request.Email}' adresi zaten kayıtlı.");

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash);

        await userRepository.AddAsync(user, ct);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Giriş isteği alındı: {Email}", request.Email);

        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Email veya şifre hatalı.");

        if (!user.IsActive)
            throw new UnauthorizedException("Hesap aktif değil.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email veya şifre hatalı.");

        await userRepository.RevokeAllUserTokensAsync(user.Id, ct);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var existingToken = await userRepository.GetRefreshTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedException("Refresh token geçersiz.");

        if (!existingToken.IsValid)
            throw new UnauthorizedException("Refresh token süresi dolmuş veya iptal edilmiş.");

        var user = await userRepository.GetByIdAsync(existingToken.UserId, ct)
            ?? throw new UnauthorizedException("Kullanıcı bulunamadı.");

        existingToken.Revoke();

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Kullanıcı bulunamadı.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Mevcut şifre hatalı.");

        user.UpdatePasswordHash(passwordHasher.Hash(request.NewPassword));
        await userRepository.SaveChangesAsync(ct);

        logger.LogInformation("Şifre değiştirildi: {UserId}", userId);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken ct)
    {
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();
        var expiresAt = jwtTokenService.GetAccessTokenExpiry();

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, expirationDays: 7);
        await userRepository.AddRefreshTokenAsync(refreshToken, ct);

        return new AuthResponse(accessToken, refreshTokenValue, expiresAt, user.Email, user.Role);
    }
}

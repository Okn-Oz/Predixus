namespace Predixus.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!; //null ef doldurucak.
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsRevoked && !IsExpired; //iptal edilmemiş ve süresi dolmamış olmalı

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, int expirationDays)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId geçersiz.");
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token boş olamaz.");
        if (expirationDays <= 0) throw new ArgumentException("ExpirationDays sıfırdan büyük olmalıdır.");

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        SetUpdated();
    }
}

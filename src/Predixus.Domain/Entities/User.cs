namespace Predixus.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string Role { get; private set; } = "User";

    public ICollection<Prediction> Predictions { get; private set; } = new List<Prediction>();


    //constructor ile user oluşturmayı kapattık  new user() ile user oluşturamam şu an
    // ef core db den nesne oluşturucak normalde new user() ile emailsiz kullanıcı oluşturulabilirdi şimdi oluşturulamaz.
    private User() { }


    // geçerli nesne oluşturma metodu ( factory pattern ) içinde gerekli validasyonlar var ve onlar geçerli olmadan oluşturamayız
    public static User Create(string email, string passwordHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email boş olamaz.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash boş olamaz.");

        return new User
        {
            //db de çakışma önleme için tüm emailleri lowercase
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            IsActive = true,
            Role = role
        };
    }

    public void SetRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Rol boş olamaz.");
        Role = role;
        SetUpdated();
    }

    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new ArgumentException("PasswordHash boş olamaz.");
        PasswordHash = newPasswordHash;
        SetUpdated(); //baseclass tan password oluşum tarihi 
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdated();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Entities;

namespace TradeEase.Services;

public interface IAuthService
{
    Task<User?> ValidateLoginAsync(string email, string password);
    Task<(User? User, string? Error)> RegisterTraderAsync(
        string fullName, string email, string businessName, string cnic, string ntn, string password);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly TradeEaseDbContext _db;
    private readonly ITaxpayerVerificationService _verification;
    private readonly IWebHostEnvironment _env;
    private readonly IJwtTokenService _jwt;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(
        TradeEaseDbContext db, 
        ITaxpayerVerificationService verification,
        IWebHostEnvironment env,
        IJwtTokenService jwt)
    {
        _db = db;
        _verification = verification;
        _env = env;
        _jwt = jwt;
    }

    public async Task<User?> ValidateLoginAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user is null) return null;

        User? validated = null;
        bool isDemoBypass = user.PasswordHash == "CHANGE_ME_HASH" || string.IsNullOrEmpty(user.PasswordHash);
        
        if (isDemoBypass && !_env.IsProduction())
            validated = user;
        else
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", password);
            validated = result == PasswordVerificationResult.Success ? user : null;
        }

        if (validated is null) return null;

        validated.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return validated;
    }

    public async Task<(User? User, string? Error)> RegisterTraderAsync(
        string fullName,
        string email,
        string businessName,
        string cnic,
        string ntn,
        string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (null, "Email already registered.");

        var traderRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Trader");
        if (traderRole is null)
            return (null, "Trader role missing. Run database setup.");

        var verify = await _verification.VerifyAsync(fullName, businessName, cnic, ntn);
        if (!verify.CnicFormatValid || !verify.NtnFormatValid)
            return (null, verify.Message);

        var cleanCnic = new string(cnic.Where(char.IsDigit).ToArray());
        var cleanNtn = new string(ntn.Where(char.IsDigit).ToArray());

        if (await _db.Users.AnyAsync(u => u.CNIC == cleanCnic))
            return (null, "CNIC already registered.");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            BusinessName = businessName,
            CNIC = cleanCnic,
            NTN = cleanNtn,
            RoleId = traderRole.RoleId,
            IsActive = true,
            IsVerified = verify.IsVerified,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = _hasher.HashPassword(new User(), password),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var saved = await _db.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.UserId == user.UserId);

        return (saved, verify.IsVerified ? null : verify.Message);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found.");

        bool isDemoBypass = user.PasswordHash == "CHANGE_ME_HASH" || string.IsNullOrEmpty(user.PasswordHash);
        
        if (!isDemoBypass || _env.IsProduction())
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", currentPassword);
            if (result != PasswordVerificationResult.Success)
                return (false, "Current password is incorrect.");
        }

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user is null) return null;
        
        return _jwt.CreatePasswordResetToken(user);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        var userId = _jwt.ValidatePasswordResetToken(token);
        if (userId is null)
            return (false, "Invalid or expired reset token.");

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null || !user.IsActive)
            return (false, "User not found or inactive.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}

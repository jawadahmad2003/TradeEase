using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IJwtTokenService _jwt;
    private readonly ITaxpayerVerificationService _verification;
    private readonly ITwoFactorService _twoFactor;
    private readonly IEmailService _email;
    private readonly TradeEaseDbContext _db;

    public AuthController(
        IAuthService auth,
        IJwtTokenService jwt,
        ITaxpayerVerificationService verification,
        ITwoFactorService twoFactor,
        IEmailService email,
        TradeEaseDbContext db)
    {
        _auth = auth;
        _jwt = jwt;
        _verification = verification;
        _twoFactor = twoFactor;
        _email = email;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        string? maintenance = null;
        try
        {
            maintenance = await _db.SystemSettings
                .Where(s => s.SettingKey == "MaintenanceMode")
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync();
        }
        catch { }

        var user = await _auth.ValidateLoginAsync(request.Email.Trim(), request.Password);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        if (maintenance == "true" && user.Role.RoleName != "Admin")
            return StatusCode(503, new { message = "TradeEase is in maintenance mode. Please try again later." });

        // 2FA requirement bypassed per user request

        return Ok(new AuthResponse(
            _jwt.CreateToken(user),
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.IsVerified));
    }



    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var (success, error) = await _auth.ChangePasswordAsync(
            User.GetUserId(), request.CurrentPassword, request.NewPassword);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var token = await _auth.GeneratePasswordResetTokenAsync(request.Email.Trim());
        if (token != null)
        {
            await _email.SendPasswordResetEmailAsync(request.Email.Trim(), token);
        }
        
        // Always return success to prevent email enumeration
        return Ok(new { message = "If the email is registered, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, error) = await _auth.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Password has been successfully reset." });
    }



    [HttpPost("verify-taxpayer")]
    [AllowAnonymous]
    public async Task<ActionResult<TaxpayerVerificationDto>> VerifyTaxpayer([FromBody] VerifyTaxpayerRequest request)
    {
        var result = await _verification.VerifyAsync(
            request.FullName.Trim(),
            request.BusinessName.Trim(),
            request.Cnic,
            request.Ntn);

        return Ok(new TaxpayerVerificationDto(
            result.IsVerified,
            result.CnicFormatValid,
            result.NtnFormatValid,
            result.AtlStatusChecked,
            result.AtlStatus,
            result.Message,
            result.Source));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        string? maintenance = null;
        try
        {
            maintenance = await _db.SystemSettings
                .Where(s => s.SettingKey == "MaintenanceMode")
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync();
        }
        catch { }

        if (maintenance == "true")
            return StatusCode(503, new { message = "Registration is disabled during maintenance." });

        var (user, error) = await _auth.RegisterTraderAsync(
            request.FullName.Trim(),
            request.Email.Trim(),
            request.BusinessName.Trim(),
            request.Cnic,
            request.Ntn,
            request.Password);

        if (user is null)
            return BadRequest(new { message = error ?? "Registration failed." });

        return Ok(new AuthResponse(
            _jwt.CreateToken(user),
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.IsVerified,
            error));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ProfileDto>> Me()
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == User.GetUserId());
        if (user is null) return NotFound();

        return Ok(MapProfile(user));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == User.GetUserId());
        if (user is null) return NotFound();

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone;
        user.BusinessName = request.BusinessName;
        user.City = request.City;
        user.NTN = request.Ntn;
        user.CNIC = string.IsNullOrWhiteSpace(request.Cnic)
            ? user.CNIC
            : new string(request.Cnic.Where(char.IsDigit).ToArray());

        if (request.NotifyShipments.HasValue) user.NotifyShipments = request.NotifyShipments.Value;
        if (request.NotifyForex.HasValue) user.NotifyForex = request.NotifyForex.Value;
        if (request.NotifyCompliance.HasValue) user.NotifyCompliance = request.NotifyCompliance.Value;

        if (!string.IsNullOrWhiteSpace(user.NTN) && !string.IsNullOrWhiteSpace(user.BusinessName) && !string.IsNullOrWhiteSpace(user.CNIC))
        {
            var verify = await _verification.VerifyAsync(user.FullName, user.BusinessName!, user.CNIC, user.NTN!);
            user.IsVerified = verify.IsVerified;
        }

        await _db.SaveChangesAsync();
        return Ok(MapProfile(user));
    }

    private static ProfileDto MapProfile(Models.Entities.User user) => new(
        user.UserId,
        user.FullName,
        user.Email,
        user.Role.RoleName,
        user.BusinessName,
        user.NTN,
        user.CNIC,
        user.Phone,
        user.City,
        user.IsActive,
        user.IsVerified,
        user.NotifyShipments,
        user.NotifyForex,
        user.NotifyCompliance,
        user.TwoFactorEnabled,
        user.LastLogin,
        user.CreatedAt);
}

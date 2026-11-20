using OtpNet;
using QRCoder;
using TradeEase.Models.Entities;

namespace TradeEase.Services;

public interface ITwoFactorService
{
    TwoFactorSetupDto CreateSetup(User user);
    bool Verify(User user, string code);
    bool VerifySecret(string secret, string code);
}

public record TwoFactorSetupDto(string Secret, string OtpAuthUri, string QrCodeBase64);

public class TwoFactorService : ITwoFactorService
{
    public TwoFactorSetupDto CreateSetup(User user)
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        var base32 = Base32Encoding.ToString(secret);
        var issuer = Uri.EscapeDataString("TradeEase");
        var account = Uri.EscapeDataString(user.Email);
        var otpAuthUri = $"otpauth://totp/{issuer}:{account}?secret={base32}&issuer={issuer}&digits=6";

        using var qrGen = new QRCodeGenerator();
        using var data = qrGen.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var qrBytes = png.GetGraphic(8);
        var qrBase64 = Convert.ToBase64String(qrBytes);

        return new TwoFactorSetupDto(base32, otpAuthUri, qrBase64);
    }

    public bool Verify(User user, string code) =>
        !string.IsNullOrEmpty(user.TwoFactorSecret) && VerifySecret(user.TwoFactorSecret, code);

    public bool VerifySecret(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6) return false;
        try
        {
            var bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }
}

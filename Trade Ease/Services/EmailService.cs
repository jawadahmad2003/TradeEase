using Microsoft.Extensions.Logging;

namespace TradeEase.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;

    public EmailService(ILogger<EmailService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        // For development/demo purposes, we just log the token to the console.
        // In a real application, this would send an SMTP email.
        _logger.LogInformation("===============================================");
        _logger.LogInformation("PASSWORD RESET EMAIL INTERCEPTED");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Token: {Token}", resetToken);
        _logger.LogInformation("===============================================");

        if (_env.IsProduction())
        {
            _logger.LogWarning("EmailService is running in stub mode in Production. No real email was sent.");
        }

        return Task.CompletedTask;
    }
}

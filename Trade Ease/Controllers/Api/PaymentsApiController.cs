using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace TradeEase.Controllers.Api;

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
}

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require user to be logged in
public class PaymentsApiController : ControllerBase
{
    [HttpPost("create-intent")]
    public ActionResult CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Stripe expects amounts in cents
                Currency = request.Currency.ToLower(),
                PaymentMethodTypes = new List<string> { "card" },
            };

            var service = new PaymentIntentService();
            var intent = service.Create(options);

            return Ok(new { clientSecret = intent.ClientSecret });
        }
        catch (StripeException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while creating the payment intent." });
        }
    }
}

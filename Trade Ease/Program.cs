using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TradeEase.Data;
using TradeEase.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TradeEaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("ForexApi", client =>
{
    var baseUrl = builder.Configuration["ForexApi:BaseUrl"] ?? "https://api.frankfurter.app/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("FbrAtl", client =>
{
    client.BaseAddress = new Uri("https://iris.fbr.gov.pk/");
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TradeEase/1.0");
});

builder.Services.AddHttpClient("RestCountries", client =>
{
    client.BaseAddress = new Uri("https://restcountries.com/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHttpClient("UkTariff", client =>
{
    client.BaseAddress = new Uri("https://www.trade-tariff.service.gov.uk/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TradeEase/1.0");
});

builder.Services.AddHttpClient("ReportingApi", client =>
{
    var baseUrl = builder.Configuration["ReportingApi:BaseUrl"] ?? "http://localhost:5210/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<ILiveForexService, LiveForexService>();
builder.Services.AddSingleton<ILiveDataSyncState, LiveDataSyncState>();
builder.Services.AddSingleton<IRestCountriesGeoClient, RestCountriesGeoClient>();
builder.Services.AddSingleton<IUkHarmonizedTariffClient, UkHarmonizedTariffClient>();
builder.Services.AddSingleton<IWorldGeoService, WorldGeoService>();
builder.Services.AddSingleton<IWorldHsCatalogService, WorldHsCatalogService>();
builder.Services.AddScoped<ILiveTradeTariffService, LiveTradeTariffService>();
builder.Services.AddScoped<ITaxpayerVerificationService, TaxpayerVerificationService>();

var jwtKey = Environment.GetEnvironmentVariable("TRADEEASE_JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? "TradeEaseDemoSecretKey_ChangeInProduction_Min32Chars!";

Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddRazorPages();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "SmartScheme";
        options.DefaultChallengeScheme = "SmartScheme";
    })
    .AddPolicyScheme("SmartScheme", "JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;

            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TradeEase",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TradeEaseClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Expired/invalid Bearer tokens must not block [AllowAnonymous] login/register.
                context.NoResult();
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5003", "https://localhost:7126" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddScoped<IDocumentPdfService, DocumentPdfService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IDutyCalculatorService, DutyCalculatorService>();
builder.Services.AddScoped<IForexRefreshService, ForexRefreshService>();
builder.Services.AddHostedService<ForexBackgroundService>();
builder.Services.AddHostedService<LiveDataBackgroundService>();
builder.Services.AddHostedService<StartupWarmupService>();

var app = builder.Build();

var spaIndex = Path.Combine(app.Environment.WebRootPath, "spa", "index.html");
var spaReady = File.Exists(spaIndex);

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "documents"));

{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    if (spaReady)
        logger.LogInformation("React UI ready at /spa/index.html");
    else
        logger.LogWarning(
            "React UI not built. Run BUILD-UI.bat or Rebuild Solution before F5. Expected: wwwroot/spa/index.html");

    logger.LogInformation("TradeEase API listening — live data loads in background after startup.");
    
    if (app.Environment.IsProduction() && jwtKey == "TradeEaseDemoSecretKey_ChangeInProduction_Min32Chars!")
    {
        logger.LogWarning("WARNING: USING DEFAULT JWT SECRET KEY IN PRODUCTION!");
    }

    app.Lifetime.ApplicationStarted.Register(() => {
        logger.LogInformation("Application listening on: {Urls}", string.Join(", ", app.Urls));
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(spaReady ? "/spa/index.html" : "/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("ReactDev");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Portal}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (spaReady)
{
    app.MapGet("/", () => Results.Redirect("/spa/index.html"));
    app.MapGet("/spa", () => Results.Redirect("/spa/index.html"));
    app.MapFallbackToFile("spa/{*path:nonfile}", "spa/index.html");
}
else
{
    app.MapGet("/", () => Results.Redirect("/Index"));
}

app.Run();

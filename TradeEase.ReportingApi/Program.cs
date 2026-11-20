using Microsoft.Data.SqlClient;
using System.Data;
using TradeEase.ReportingApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS – allow the main TradeEase app to call this API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://localhost:7001",
            "http://localhost:5001",
            "https://localhost:5173",
            "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Dapper – register IDbConnection as scoped
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is required.");
builder.Services.AddScoped<IDbConnection>(_ => new SqlConnection(connStr));

// Reporting service
builder.Services.AddScoped<IReportingService, ReportingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

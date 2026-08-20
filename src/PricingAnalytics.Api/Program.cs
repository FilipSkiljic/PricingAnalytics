using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PricingAnalytics.Api.OpenApi;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Application.Services;
using PricingAnalytics.Infrastructure.Csv;
using PricingAnalytics.Infrastructure.Persistence;
using PricingAnalytics.Infrastructure.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    // Lets the documentation UI offer a place to paste the token, and marks which
    // endpoints need one.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthorizationOperationTransformer>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IMarketEntryRepository, EfMarketEntryRepository>();
builder.Services.AddScoped<IBusinessRepository, EfBusinessRepository>();
builder.Services.AddScoped<IServiceRepository, EfServiceRepository>();
builder.Services.AddScoped<ICanonicalServiceRepository, EfCanonicalServiceRepository>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IPriceListCsvParser, PriceListCsvParser>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddScoped<PriceAnalysisService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PriceListService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<AdminService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing \"Jwt\" configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            // No leeway on expiry - the default 5 minutes would keep tokens usable past
            // the lifetime the API reports to the client.
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// The Angular dev server runs on its own origin, so the browser needs explicit
// permission to call this API from there.
const string AngularDevCorsPolicy = "AngularDev";
builder.Services.AddCors(options =>
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Dev convenience: apply pending migrations and seed the market corpus from the
    // CSV export on startup so `dotnet run` gives a working /analyze endpoint immediately.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var csvPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, app.Configuration["MarketDataCsvPath"]!));
    if (File.Exists(csvPath))
    {
        await MarketEntrySeeder.SeedFromCsvAsync(dbContext, csvPath);
    }

    // Only fills an empty catalog table, so edits made through the admin screen survive restarts.
    await CanonicalServiceSeeder.SeedAsync(dbContext);
}
else
{
    // Only outside development: in dev the API is served over plain HTTP on :5049 and
    // the Angular dev server calls it there, so a redirect to HTTPS would break those calls.
    app.UseHttpsRedirection();
}

app.UseCors(AngularDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

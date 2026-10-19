using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Binding;
using RadafraApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Outside development (Docker/Render) listen on the host-provided port.
// Render sets PORT; fall back to 8080 for a plain `docker run`.
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Parse numbers/dates with the invariant culture (API contract is culture-independent,
// otherwise a client-sent "19.99" is read as 1999 under a comma-decimal OS culture).
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var invariant = new[] { CultureInfo.InvariantCulture };
    options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
    options.SupportedCultures = invariant;
    options.SupportedUICultures = invariant;
});

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Accept both "." and "," as the decimal separator for model binding.
    options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRequestLocalization();

// The host (Render) terminates TLS, so only redirect in local development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Binding;
using RadafraApp.Data;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

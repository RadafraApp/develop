using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Contracts;
using RadafraApp.Data;

namespace RadafraApp.Controllers
{
    [ApiController]
    [Route("api/bids")]
    public class PhoneBidsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PhoneBidsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhoneBidDto>>> GetByPhone([FromQuery] string? phone)
        {
            if (!TryNormalize(phone, out var digits, out var error))
            {
                ModelState.AddModelError("phone", error!);
                return ValidationProblem(ModelState);
            }

            var bids = await _db.Bids
                .FromSqlInterpolated($@"SELECT * FROM ""Bids"" WHERE regexp_replace(""Phone"", '[^0-9]', '', 'g') = {digits}")
                .AsNoTracking()
                .ToListAsync();

            if (bids.Count == 0)
            {
                return Ok(Array.Empty<PhoneBidDto>());
            }

            var productIds = bids.Select(b => b.ProductId).Distinct().ToList();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, HasImage = p.ImageData != null, p.ImageUpdatedAt, p.AuctionEndTime })
                .AsNoTracking()
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            var result = bids
                .Where(b => productMap.ContainsKey(b.ProductId))
                .GroupBy(b => b.ProductId)
                .Select(g =>
                {
                    var product = productMap[g.Key];
                    return new PhoneBidDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ImagePath = ImageUrl(product.Id, product.HasImage, product.ImageUpdatedAt),
                        Amount = g.Max(b => b.Amount),
                        AuctionEndTime = product.AuctionEndTime
                    };
                })
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.ProductName)
                .ToList();

            return Ok(result);
        }

        private static string ImageUrl(int id, bool hasImage, DateTimeOffset? updatedAt) =>
            hasImage ? $"/api/products/{id}/image?v={updatedAt?.ToUnixTimeMilliseconds() ?? 0}" : string.Empty;

        private static bool TryNormalize(string? phone, out string digits, out string? error)
        {
            var value = phone?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                digits = string.Empty;
                error = "El teléfono es obligatorio.";
                return false;
            }

            if (value.Length > 20)
            {
                digits = string.Empty;
                error = "El teléfono no puede superar los 20 caracteres.";
                return false;
            }

            if (value.Any(c => !char.IsDigit(c) && c is not (' ' or '+' or '-' or '(' or ')' or '.')))
            {
                digits = string.Empty;
                error = "El teléfono contiene caracteres no válidos.";
                return false;
            }

            digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length is < 9 or > 15)
            {
                error = "Introduce un teléfono válido (entre 9 y 15 dígitos).";
                return false;
            }

            error = null;
            return true;
        }
    }
}

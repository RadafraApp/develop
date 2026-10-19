using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Contracts;
using RadafraApp.Data;
using RadafraApp.Entities;

namespace RadafraApp.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/bids")]
    public class BidsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BidsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BidDto>>> GetByProduct(int productId)
        {
            var exists = await _db.Products.AnyAsync(p => p.Id == productId);
            if (!exists)
            {
                return NotFound();
            }

            var bids = await _db.Bids
                .Where(b => b.ProductId == productId)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.CreatedAt)
                .Select(b => new BidDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    Phone = b.Phone,
                    Amount = b.Amount,
                    CreatedAt = b.CreatedAt
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(bids);
        }

        [HttpPost]
        public async Task<ActionResult<BidDto>> Create(int productId, [FromBody] CreateBidDto dto)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product is null)
            {
                return NotFound();
            }

            if (!IsValidPhone(dto.Phone, out var phoneError))
            {
                ModelState.AddModelError(nameof(dto.Phone), phoneError!);
                return ValidationProblem(ModelState);
            }

            if (DateTimeOffset.UtcNow >= product.AuctionEndTime)
            {
                ModelState.AddModelError(nameof(dto.Amount), "La subasta ya ha finalizado.");
                return ValidationProblem(ModelState);
            }

            if (dto.Amount <= 0)
            {
                ModelState.AddModelError(nameof(dto.Amount), "La puja debe ser mayor que cero.");
                return ValidationProblem(ModelState);
            }

            if (dto.Amount % 0.5m != 0m)
            {
                ModelState.AddModelError(nameof(dto.Amount), "La puja debe ser un múltiplo de 0,50 €.");
                return ValidationProblem(ModelState);
            }

            var highest = await _db.Bids
                .Where(b => b.ProductId == productId)
                .MaxAsync(b => (decimal?)b.Amount);

            if (highest is null)
            {
                if (dto.Amount < product.StartingPrice)
                {
                    ModelState.AddModelError(
                        nameof(dto.Amount),
                        $"La puja debe ser al menos el precio inicial ({Money(product.StartingPrice)}).");
                    return ValidationProblem(ModelState);
                }
            }
            else if (dto.Amount <= highest.Value)
            {
                ModelState.AddModelError(
                    nameof(dto.Amount),
                    $"La puja debe superar la puja actual ({Money(highest.Value)}).");
                return ValidationProblem(ModelState);
            }

            var bid = new Bid
            {
                ProductId = productId,
                Phone = dto.Phone.Trim(),
                Amount = dto.Amount,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Bids.Add(bid);
            await _db.SaveChangesAsync();

            var result = new BidDto
            {
                Id = bid.Id,
                ProductId = bid.ProductId,
                Phone = bid.Phone,
                Amount = bid.Amount,
                CreatedAt = bid.CreatedAt
            };

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpDelete("{bidId:int}")]
        public async Task<IActionResult> Delete(int productId, int bidId)
        {
            var bid = await _db.Bids
                .FirstOrDefaultAsync(b => b.Id == bidId && b.ProductId == productId);

            if (bid is null)
            {
                return NotFound();
            }

            _db.Bids.Remove(bid);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static string Money(decimal value) =>
            value.ToString("0.00", CultureInfo.InvariantCulture) + " €";

        private static bool IsValidPhone(string? phone, out string? error)
        {
            var value = phone?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                error = "El teléfono es obligatorio.";
                return false;
            }

            if (value.Length > 20)
            {
                error = "El teléfono no puede superar los 20 caracteres.";
                return false;
            }

            if (value.Any(c => !char.IsDigit(c) && c is not (' ' or '+' or '-' or '(' or ')' or '.')))
            {
                error = "El teléfono contiene caracteres no válidos.";
                return false;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
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

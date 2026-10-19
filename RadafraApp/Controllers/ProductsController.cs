using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Contracts;
using RadafraApp.Data;
using RadafraApp.Entities;

namespace RadafraApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

        private static readonly Expression<Func<Product, ProductView>> Projection = p => new ProductView
        {
            Id = p.Id,
            Name = p.Name,
            HasImage = p.ImageData != null,
            ImageUpdatedAt = p.ImageUpdatedAt,
            StartingPrice = p.StartingPrice,
            CurrentPrice = p.Bids.Any() ? p.Bids.Max(b => b.Amount) : p.StartingPrice,
            BidCount = p.Bids.Count(),
            Publisher = p.Publisher,
            Collection = p.Collection,
            IsNumbered = p.IsNumbered,
            Numbering = p.Numbering,
            AuctionEndTime = p.AuctionEndTime
        };

        private readonly ApplicationDbContext _db;

        public ProductsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _db.Products
                .OrderByDescending(p => p.Id)
                .Select(Projection)
                .AsNoTracking()
                .ToListAsync();

            return Ok(products.Select(ToDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _db.Products
                .Where(p => p.Id == id)
                .Select(Projection)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return product is null ? NotFound() : Ok(ToDto(product));
        }

        [HttpGet("{id:int}/image")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _db.Products
                .Where(p => p.Id == id)
                .Select(p => new { p.ImageData, p.ImageContentType })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (image?.ImageData is not { Length: > 0 })
            {
                return NotFound();
            }

            return File(image.ImageData, image.ImageContentType ?? "application/octet-stream");
        }

        [HttpPost]
        [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
        public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductDto dto)
        {
            if (dto.IsNumbered && string.IsNullOrWhiteSpace(dto.Numbering))
            {
                ModelState.AddModelError(nameof(dto.Numbering), "La numeración es obligatoria cuando el producto es numerado.");
            }

            if (dto.Image is null || dto.Image.Length == 0)
            {
                ModelState.AddModelError(nameof(dto.Image), "La imagen es obligatoria.");
            }
            else if (dto.Image.Length > MaxImageBytes)
            {
                ModelState.AddModelError(nameof(dto.Image), "La imagen no puede superar los 5 MB.");
            }
            else if (!AllowedExtensions.Contains(Path.GetExtension(dto.Image.FileName).ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(dto.Image), $"Formato no permitido. Usa: {string.Join(", ", AllowedExtensions)}.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var extension = Path.GetExtension(dto.Image!.FileName).ToLowerInvariant();

            var product = new Product
            {
                Name = dto.Name.Trim(),
                ImageData = await ReadImageAsync(dto.Image),
                ImageContentType = ContentTypeFor(extension),
                ImageUpdatedAt = DateTimeOffset.UtcNow,
                StartingPrice = dto.StartingPrice,
                Publisher = dto.Publisher.Trim(),
                Collection = dto.Collection.Trim(),
                IsNumbered = dto.IsNumbered,
                Numbering = dto.IsNumbered ? dto.Numbering!.Trim() : null,
                AuctionEndTime = dto.AuctionEndTime
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product, product.StartingPrice, 0));
        }

        [HttpPut("{id:int}")]
        [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
        public async Task<ActionResult<ProductDto>> Update(int id, [FromForm] UpdateProductDto dto)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return NotFound();
            }

            if (dto.IsNumbered && string.IsNullOrWhiteSpace(dto.Numbering))
            {
                ModelState.AddModelError(nameof(dto.Numbering), "La numeración es obligatoria cuando el producto es numerado.");
            }

            if (dto.Image is { Length: > 0 })
            {
                if (dto.Image.Length > MaxImageBytes)
                {
                    ModelState.AddModelError(nameof(dto.Image), "La imagen no puede superar los 5 MB.");
                }
                else if (!AllowedExtensions.Contains(Path.GetExtension(dto.Image.FileName).ToLowerInvariant()))
                {
                    ModelState.AddModelError(nameof(dto.Image), $"Formato no permitido. Usa: {string.Join(", ", AllowedExtensions)}.");
                }
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (dto.Image is { Length: > 0 })
            {
                var extension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                product.ImageData = await ReadImageAsync(dto.Image);
                product.ImageContentType = ContentTypeFor(extension);
                product.ImageUpdatedAt = DateTimeOffset.UtcNow;
            }

            product.Name = dto.Name.Trim();
            product.StartingPrice = dto.StartingPrice;
            product.Publisher = dto.Publisher.Trim();
            product.Collection = dto.Collection.Trim();
            product.IsNumbered = dto.IsNumbered;
            product.Numbering = dto.IsNumbered ? dto.Numbering!.Trim() : null;
            product.AuctionEndTime = dto.AuctionEndTime;

            await _db.SaveChangesAsync();

            var highest = await _db.Bids
                .Where(b => b.ProductId == id)
                .MaxAsync(b => (decimal?)b.Amount);
            var bidCount = await _db.Bids.CountAsync(b => b.ProductId == id);

            return Ok(ToDto(product, highest ?? product.StartingPrice, bidCount));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return NotFound();
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static async Task<byte[]> ReadImageAsync(IFormFile image)
        {
            using var memory = new MemoryStream();
            await image.CopyToAsync(memory);
            return memory.ToArray();
        }

        private static string ContentTypeFor(string extension) => extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

        private static string ImageUrl(int id, bool hasImage, DateTimeOffset? updatedAt) =>
            hasImage ? $"/api/products/{id}/image?v={updatedAt?.ToUnixTimeMilliseconds() ?? 0}" : string.Empty;

        private static ProductDto ToDto(ProductView v) => new()
        {
            Id = v.Id,
            Name = v.Name,
            ImagePath = ImageUrl(v.Id, v.HasImage, v.ImageUpdatedAt),
            StartingPrice = v.StartingPrice,
            CurrentPrice = v.CurrentPrice,
            BidCount = v.BidCount,
            Publisher = v.Publisher,
            Collection = v.Collection,
            IsNumbered = v.IsNumbered,
            Numbering = v.Numbering,
            AuctionEndTime = v.AuctionEndTime
        };

        private static ProductDto ToDto(Product p, decimal currentPrice, int bidCount) => new()
        {
            Id = p.Id,
            Name = p.Name,
            ImagePath = ImageUrl(p.Id, p.ImageData is not null, p.ImageUpdatedAt),
            StartingPrice = p.StartingPrice,
            CurrentPrice = currentPrice,
            BidCount = bidCount,
            Publisher = p.Publisher,
            Collection = p.Collection,
            IsNumbered = p.IsNumbered,
            Numbering = p.Numbering,
            AuctionEndTime = p.AuctionEndTime
        };

        private sealed class ProductView
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool HasImage { get; set; }
            public DateTimeOffset? ImageUpdatedAt { get; set; }
            public decimal StartingPrice { get; set; }
            public decimal CurrentPrice { get; set; }
            public int BidCount { get; set; }
            public string Publisher { get; set; } = string.Empty;
            public string Collection { get; set; } = string.Empty;
            public bool IsNumbered { get; set; }
            public string? Numbering { get; set; }
            public DateTimeOffset AuctionEndTime { get; set; }
        }
    }
}

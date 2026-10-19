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

        private static readonly Expression<Func<Product, ProductDto>> Projection = p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ImagePath = p.ImagePath,
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
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _db.Products
                .OrderByDescending(p => p.Id)
                .Select(Projection)
                .AsNoTracking()
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _db.Products
                .Where(p => p.Id == id)
                .Select(Projection)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return product is null ? NotFound() : Ok(product);
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
            var imagePath = await SaveImageAsync(dto.Image, extension);

            var product = new Product
            {
                Name = dto.Name.Trim(),
                ImagePath = imagePath,
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
                var newImagePath = await SaveImageAsync(dto.Image, extension);
                DeleteImage(product.ImagePath);
                product.ImagePath = newImagePath;
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

            DeleteImage(product.ImagePath);
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<string> SaveImageAsync(IFormFile image, string extension)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await image.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var relative = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, relative);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private static ProductDto ToDto(Product p, decimal currentPrice, int bidCount) => new()
        {
            Id = p.Id,
            Name = p.Name,
            ImagePath = p.ImagePath,
            StartingPrice = p.StartingPrice,
            CurrentPrice = currentPrice,
            BidCount = bidCount,
            Publisher = p.Publisher,
            Collection = p.Collection,
            IsNumbered = p.IsNumbered,
            Numbering = p.Numbering,
            AuctionEndTime = p.AuctionEndTime
        };
    }
}

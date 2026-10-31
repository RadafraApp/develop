using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadafraApp.Contracts;
using RadafraApp.Data;
using RadafraApp.Entities;
using RadafraApp.Services;

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
        private readonly ImageProcessor _imageProcessor;

        public ProductsController(ApplicationDbContext db, ImageProcessor imageProcessor)
        {
            _db = db;
            _imageProcessor = imageProcessor;
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
        [ResponseCache(Duration = 31_536_000, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetImage(int id, CancellationToken cancellationToken)
        {
            var image = await _db.Products
                .Where(p => p.Id == id)
                .Select(p => new { p.ImageData, p.ImageContentType })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (image?.ImageData is not { Length: > 0 })
            {
                return NotFound();
            }

            return ImageFile(image.ImageData, image.ImageContentType);
        }

        [HttpGet("{id:int}/thumbnail")]
        [ResponseCache(Duration = 31_536_000, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetThumbnail(int id, CancellationToken cancellationToken)
        {
            var image = await _db.Products
                .Where(p => p.Id == id)
                .Select(p => new { p.ImageData, p.ThumbnailData, p.ThumbnailContentType })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (image is null)
            {
                return NotFound();
            }

            if (image.ThumbnailData is { Length: > 0 })
            {
                return ImageFile(image.ThumbnailData, image.ThumbnailContentType);
            }

            if (image.ImageData is not { Length: > 0 })
            {
                return NotFound();
            }

            // Rows created before thumbnails existed get one on first use.
            EncodedImage thumbnail;
            try
            {
                thumbnail = _imageProcessor.CreateThumbnail(image.ImageData);
            }
            catch (InvalidImageException)
            {
                return NotFound();
            }

            await _db.Products
                .Where(p => p.Id == id && p.ThumbnailData == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.ThumbnailData, thumbnail.Data)
                        .SetProperty(p => p.ThumbnailContentType, thumbnail.ContentType),
                    cancellationToken);

            return ImageFile(thumbnail.Data, thumbnail.ContentType);
        }

        [HttpPost]
        [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
        public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductDto dto)
        {
            if (dto.IsNumbered && string.IsNullOrWhiteSpace(dto.Numbering))
            {
                ModelState.AddModelError(nameof(dto.Numbering), "La numeración es obligatoria cuando el producto es numerado.");
            }

            ProcessedImage? image = null;
            if (dto.Image is null || dto.Image.Length == 0)
            {
                ModelState.AddModelError(nameof(dto.Image), "La imagen es obligatoria.");
            }
            else
            {
                image = await ProcessUploadAsync(dto.Image, nameof(dto.Image));
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                ImageData = image!.Full.Data,
                ImageContentType = image.Full.ContentType,
                ImageUpdatedAt = DateTimeOffset.UtcNow,
                ThumbnailData = image.Thumbnail.Data,
                ThumbnailContentType = image.Thumbnail.ContentType,
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

            ProcessedImage? image = null;
            if (dto.Image is { Length: > 0 })
            {
                image = await ProcessUploadAsync(dto.Image, nameof(dto.Image));
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (image is not null)
            {
                product.ImageData = image.Full.Data;
                product.ImageContentType = image.Full.ContentType;
                product.ImageUpdatedAt = DateTimeOffset.UtcNow;
                product.ThumbnailData = image.Thumbnail.Data;
                product.ThumbnailContentType = image.Thumbnail.ContentType;
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
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            // Bids are removed by the ON DELETE CASCADE constraint.
            var deleted = await _db.Products
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deleted == 0 ? NotFound() : NoContent();
        }

        private async Task<ProcessedImage?> ProcessUploadAsync(IFormFile image, string modelKey)
        {
            if (image.Length > MaxImageBytes)
            {
                ModelState.AddModelError(modelKey, "La imagen no puede superar los 5 MB.");
                return null;
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(modelKey, $"Formato no permitido. Usa: {string.Join(", ", AllowedExtensions)}.");
                return null;
            }

            await using var stream = image.OpenReadStream();

            try
            {
                return _imageProcessor.Process(stream);
            }
            catch (InvalidImageException ex)
            {
                ModelState.AddModelError(modelKey, ex.Message);
                return null;
            }
        }

        private IActionResult ImageFile(byte[] data, string? contentType)
        {
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(data, contentType ?? "application/octet-stream");
        }

        private static string ImageUrl(int id, bool hasImage, DateTimeOffset? updatedAt) =>
            hasImage ? $"/api/products/{id}/image?v={updatedAt?.ToUnixTimeMilliseconds() ?? 0}" : string.Empty;

        private static string ThumbnailUrl(int id, bool hasImage, DateTimeOffset? updatedAt) =>
            hasImage ? $"/api/products/{id}/thumbnail?v={updatedAt?.ToUnixTimeMilliseconds() ?? 0}" : string.Empty;

        private static ProductDto ToDto(ProductView v) => new()
        {
            Id = v.Id,
            Name = v.Name,
            ImagePath = ImageUrl(v.Id, v.HasImage, v.ImageUpdatedAt),
            ThumbnailPath = ThumbnailUrl(v.Id, v.HasImage, v.ImageUpdatedAt),
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
            ThumbnailPath = ThumbnailUrl(p.Id, p.ImageData is not null, p.ImageUpdatedAt),
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

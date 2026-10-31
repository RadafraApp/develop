using SkiaSharp;

namespace RadafraApp.Services
{
    public sealed record EncodedImage(byte[] Data, string ContentType);

    public sealed record ProcessedImage(EncodedImage Full, EncodedImage Thumbnail);

    /// <summary>
    /// Thrown when an upload cannot be decoded as an image (or is too large to be one).
    /// </summary>
    public sealed class InvalidImageException : Exception
    {
        public InvalidImageException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Decodes uploaded pictures and re-encodes compact WebP renditions. The database therefore
    /// never keeps multi-megabyte originals, and the auction cards can download a small thumbnail
    /// instead of the full picture. Re-encoding also strips EXIF metadata (GPS, camera, etc.).
    /// </summary>
    public sealed class ImageProcessor
    {
        /// <summary>Longest side kept for the full-size rendition.</summary>
        public const int FullMaxSide = 1600;

        /// <summary>The thumbnail is scaled down to fit inside this box, keeping its aspect ratio.</summary>
        public const int ThumbnailMaxWidth = 800;

        public const int ThumbnailMaxHeight = 600;

        private const long MaxPixels = 40_000_000;
        private const int FullQuality = 82;
        private const int ThumbnailQuality = 80;
        private const string NotAnImage = "El archivo no es una imagen válida.";

        public ProcessedImage Process(Stream source)
        {
            using var decoded = Decode(source, out var origin);
            using var rotated = ApplyOrigin(decoded, origin);
            var upright = rotated ?? decoded;

            using var full = ResizeToFit(upright, FullMaxSide, FullMaxSide);
            using var thumbnail = ResizeToFit(upright, ThumbnailMaxWidth, ThumbnailMaxHeight);

            return new ProcessedImage(
                Encode(full ?? upright, FullQuality),
                Encode(thumbnail ?? upright, ThumbnailQuality));
        }

        /// <summary>
        /// Builds only the thumbnail rendition. Used to backfill rows created before thumbnails existed.
        /// </summary>
        public EncodedImage CreateThumbnail(byte[] image)
        {
            using var stream = new MemoryStream(image, writable: false);
            using var decoded = Decode(stream, out var origin);
            using var rotated = ApplyOrigin(decoded, origin);
            var upright = rotated ?? decoded;

            using var thumbnail = ResizeToFit(upright, ThumbnailMaxWidth, ThumbnailMaxHeight);

            return Encode(thumbnail ?? upright, ThumbnailQuality);
        }

        private static SKBitmap Decode(Stream source, out SKEncodedOrigin origin)
        {
            using var data = SKData.Create(source) ?? throw new InvalidImageException(NotAnImage);
            using var codec = SKCodec.Create(data) ?? throw new InvalidImageException(NotAnImage);

            var info = codec.Info;
            if (info.Width <= 0 || info.Height <= 0)
            {
                throw new InvalidImageException(NotAnImage);
            }

            // Guard against decompression bombs: a small file can declare a huge canvas.
            if ((long)info.Width * info.Height > MaxPixels)
            {
                throw new InvalidImageException("La imagen tiene demasiados píxeles (máximo 40 megapíxeles).");
            }

            origin = codec.EncodedOrigin;

            return SKBitmap.Decode(codec) ?? throw new InvalidImageException(NotAnImage);
        }

        /// <summary>
        /// Applies the EXIF orientation so phone pictures are not stored sideways.
        /// Returns null when the image is already upright.
        /// </summary>
        private static SKBitmap? ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
        {
            if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            {
                return null;
            }

            var swapSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

            var width = swapSides ? source.Height : source.Width;
            var height = swapSides ? source.Width : source.Height;

            var target = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
            using var canvas = new SKCanvas(target);

            // Canvas transforms are applied to the drawing in reverse call order, so the
            // operations below are written from the outermost to the innermost transform.
            switch (origin)
            {
                case SKEncodedOrigin.TopRight: // mirror horizontally
                    canvas.Translate(source.Width, 0);
                    canvas.Scale(-1, 1);
                    break;

                case SKEncodedOrigin.BottomRight: // rotate 180
                    canvas.Translate(source.Width, source.Height);
                    canvas.RotateDegrees(180);
                    break;

                case SKEncodedOrigin.BottomLeft: // mirror vertically
                    canvas.Translate(0, source.Height);
                    canvas.Scale(1, -1);
                    break;

                case SKEncodedOrigin.LeftTop: // transpose: mirror horizontally, then rotate 90 clockwise
                    canvas.Translate(source.Height, 0);
                    canvas.Scale(-1, 1);
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                    break;

                case SKEncodedOrigin.RightTop: // rotate 90 clockwise
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                    break;

                case SKEncodedOrigin.RightBottom: // anti-transpose: mirror horizontally, then rotate 90 counter-clockwise
                    canvas.Translate(source.Height, 0);
                    canvas.Scale(-1, 1);
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(-90);
                    break;

                case SKEncodedOrigin.LeftBottom: // rotate 90 counter-clockwise
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(-90);
                    break;
            }

            // Integer 1:1 copy, so nearest sampling keeps the pixels untouched.
            canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None), null);
            canvas.Flush();

            return target;
        }

        /// <summary>
        /// Scales the bitmap down to fit inside the given box. Never enlarges. Returns null when
        /// the image already fits, so the caller can encode the original bitmap directly.
        /// </summary>
        private static SKBitmap? ResizeToFit(SKBitmap source, int maxWidth, int maxHeight)
        {
            var scale = Math.Min((float)maxWidth / source.Width, (float)maxHeight / source.Height);
            if (scale >= 1f)
            {
                return null;
            }

            var width = Math.Max(1, (int)MathF.Round(source.Width * scale));
            var height = Math.Max(1, (int)MathF.Round(source.Height * scale));
            var info = new SKImageInfo(width, height, source.ColorType, source.AlphaType);

            return source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))
                ?? throw new InvalidImageException(NotAnImage);
        }

        private static EncodedImage Encode(SKBitmap bitmap, int quality)
        {
            using var image = SKImage.FromBitmap(bitmap);

            // WebP keeps transparency and compresses much better than JPEG/PNG.
            using var webp = image.Encode(SKEncodedImageFormat.Webp, quality);
            if (webp is not null)
            {
                return new EncodedImage(webp.ToArray(), "image/webp");
            }

            var fallback = bitmap.AlphaType == SKAlphaType.Opaque
                ? SKEncodedImageFormat.Jpeg
                : SKEncodedImageFormat.Png;

            using var encoded = image.Encode(fallback, quality)
                ?? throw new InvalidImageException(NotAnImage);

            var contentType = fallback == SKEncodedImageFormat.Jpeg ? "image/jpeg" : "image/png";

            return new EncodedImage(encoded.ToArray(), contentType);
        }
    }
}

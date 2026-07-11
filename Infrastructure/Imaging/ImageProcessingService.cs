using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Imaging;

/// <summary>
/// Image processing service for converting and optimizing images
/// </summary>
public class ImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert base64 string to optimized JPG byte array
    /// </summary>
    public async Task<byte[]> ConvertBase64ToOptimizedJpgAsync(
        string base64Image,
        int quality = 85,
        int? maxWidth = null,
        int? maxHeight = null)
    {
        try
        {
            var base64Data = base64Image;
            if (base64Image.Contains(','))
            {
                base64Data = base64Image.Split(',')[1];
            }

            var imageBytes = Convert.FromBase64String(base64Data);

            await using var inputStream = new MemoryStream(imageBytes);
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream);

            if (maxWidth.HasValue || maxHeight.HasValue)
            {
                var options = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth ?? image.Width, maxHeight ?? image.Height)
                };
                image.Mutate(x => x.Resize(options));
            }

            await using var outputStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = quality };
            await image.SaveAsJpegAsync(outputStream, encoder);

            _logger.LogInformation("Image converted successfully. Original: {OriginalSize}KB, Optimized: {OptimizedSize}KB",
                imageBytes.Length / 1024,
                outputStream.Length / 1024);

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting base64 to JPG");
            throw;
        }
    }

    public string ConvertToDataUrl(string base64Image)
    {
        if (base64Image.StartsWith("data:image"))
            return base64Image;

        return $"data:image/jpeg;base64,{base64Image}";
    }
}

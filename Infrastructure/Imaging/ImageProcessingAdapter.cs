#nullable enable

using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Services.Imaging;

/// <summary>
/// Implements IImageProcessingService by delegating to the existing ImageProcessingService.
/// Adapts the interface signature (maxWidth int) to the concrete method signature (int? maxWidth).
/// </summary>
public class ImageProcessingAdapter : IImageProcessingService
{
    private readonly ImageProcessingService _inner;
    public ImageProcessingAdapter(ImageProcessingService inner) => _inner = inner;

    public async Task<byte[]?> ConvertBase64ToOptimizedJpgAsync(
        string base64Image, int maxWidth = 800, int quality = 85)
    {
        var result = await _inner.ConvertBase64ToOptimizedJpgAsync(
            base64Image, quality: quality, maxWidth: maxWidth);
        return result;
    }

    public string ConvertToDataUrl(string base64Image, string mimeType = "image/jpeg")
        => _inner.ConvertToDataUrl(base64Image);   // concrete only takes 1 arg
}

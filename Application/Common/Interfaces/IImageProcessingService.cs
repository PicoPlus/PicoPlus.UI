#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for image processing.
/// Implemented in Infrastructure (uses ImageSharp).
/// </summary>
public interface IImageProcessingService
{
    Task<byte[]?> ConvertBase64ToOptimizedJpgAsync(string base64Image, int maxWidth = 800, int quality = 85);
    string ConvertToDataUrl(string base64Image, string mimeType = "image/jpeg");
}

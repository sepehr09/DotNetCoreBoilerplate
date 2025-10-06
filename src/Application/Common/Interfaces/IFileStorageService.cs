namespace MyApp.Application.Common.Interfaces;

/// <summary>
/// Simplified interface for file storage operations - only handles MinIO uploads
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to MinIO storage from a stream
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">The file name</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="folderPath">Optional folder path within the bucket</param>
    /// <param name="makePublic">Whether the file should be publicly accessible</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL of the uploaded file</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folderPath = null, bool makePublic = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a relative image URL to an absolute URL by concatenating with the MinIO public base URL
    /// </summary>
    /// <param name="relativeUrl">The relative URL (e.g., "/bucket-name/path/to/image.jpg")</param>
    /// <returns>The absolute URL (e.g., "https://minio.example.com/bucket-name/path/to/image.jpg")</returns>
    string GetAbsoluteImageUrl(string relativeUrl);
}

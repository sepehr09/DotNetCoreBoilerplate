using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using MyApp.Application.Common.Interfaces;

namespace MyApp.Infrastructure.Services;

public class MinioStorageService : IFileStorageService
{
    private readonly MinioClient _minioClient;
    private readonly MinioSettings _minioSettings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IOptions<MinioSettings> minioSettings,
        ILogger<MinioStorageService> logger)
    {
        _minioSettings = minioSettings.Value;
        _logger = logger;

        // Initialize MinIO client
        _minioClient = (MinioClient)new MinioClient()
            .WithEndpoint(_minioSettings.Endpoint)
            .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
            .WithSSL(_minioSettings.UseSSL)
            .Build();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folderPath = null, bool makePublic = true, CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty or null", nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        }

        // Ensure bucket exists
        await InitializeAsync(cancellationToken);

        // Generate a unique file name
        string uniqueFileName = GenerateUniqueFileName(fileName);

        // Generate object name (path in the bucket)
        string objectName = GenerateObjectName(folderPath, uniqueFileName);

        // Upload the file to MinIO
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_minioSettings.BucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        // Return the relative URL (path only, without base URL)
        string relativeUrl = $"/{_minioSettings.BucketName}/{objectName}";

        _logger.LogInformation("File uploaded successfully: {FileName} -> {ObjectName} -> {RelativeUrl}", fileName, objectName, relativeUrl);

        return relativeUrl;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if bucket exists
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_minioSettings.BucketName);

            bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!bucketExists)
            {
                // Create the bucket if it doesn't exist
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_minioSettings.BucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
                _logger.LogInformation("Created bucket: {BucketName}", _minioSettings.BucketName);
            }
            else
            {
                _logger.LogDebug("Bucket {BucketName} already exists.", _minioSettings.BucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MinIO storage");
            throw;
        }
    }

    private string GenerateUniqueFileName(string originalFileName)
    {
        // Clean the file name to remove special characters
        string cleanFileName = CleanFileName(originalFileName);

        // Add timestamp and unique ID for collision avoidance
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(cleanFileName);
        string extension = Path.GetExtension(cleanFileName);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string uniqueId = Guid.NewGuid().ToString("N")[..8];

        return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    private string CleanFileName(string fileName)
    {
        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Remove spaces and special characters
        cleanName = cleanName.Replace(" ", "_");

        return cleanName;
    }

    private string GenerateObjectName(string? folderPath, string fileName)
    {
        // Use year/month folder structure for better organization
        var now = DateTime.UtcNow;
        string yearMonth = $"{now.Year:D4}/{now.Month:D2}";

        if (string.IsNullOrEmpty(folderPath))
        {
            return $"{yearMonth}/{fileName}";
        }

        // Ensure folder path doesn't have leading/trailing slashes
        folderPath = folderPath.Trim('/');
        return $"{folderPath}/{yearMonth}/{fileName}";
    }

    private async Task<string> GeneratePresignedUrl(string objectName, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_minioSettings.BucketName)
                .WithObject(objectName)
                .WithExpiry((int)expiresIn.TotalSeconds);

            return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for object {ObjectName}", objectName);
            throw;
        }
    }

    public string GetAbsoluteImageUrl(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return string.Empty;
        }

        // Ensure the relative URL starts with a forward slash
        string normalizedRelativeUrl = relativeUrl.StartsWith('/') ? relativeUrl : $"/{relativeUrl}";

        // Ensure the public base URL doesn't end with a forward slash
        string normalizedBaseUrl = _minioSettings.PublicBaseUrl.TrimEnd('/');

        // Concatenate the base URL with the relative URL
        return $"{normalizedBaseUrl}{normalizedRelativeUrl}";
    }
}

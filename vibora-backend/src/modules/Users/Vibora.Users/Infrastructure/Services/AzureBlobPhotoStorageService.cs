using Ardalis.Result;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Vibora.Users.Application.Services;

namespace Vibora.Users.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage-based photo storage implementation (for production).
/// Stores photos in Azure Blob Storage container: user-photos/users/{externalId}/profile.{extension}
/// </summary>
internal sealed class AzureBlobPhotoStorageService : IPhotoStorageService
{
    private const int MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB
    private const int TargetImageSize = 400; // 400x400px
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobPhotoStorageService> _logger;

    public AzureBlobPhotoStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobPhotoStorageService> logger)
    {
        var connectionString = configuration["PhotoStorage:AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("PhotoStorage:AzureBlob:ConnectionString is not configured");

        var containerName = configuration["PhotoStorage:AzureBlob:ContainerName"]
            ?? throw new InvalidOperationException("PhotoStorage:AzureBlob:ContainerName is not configured");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _logger = logger;
    }

    public async Task<Result<string>> UploadUserPhotoAsync(
        string userExternalId,
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate content type
            var contentTypeValidation = ValidateContentType(contentType);
            if (!contentTypeValidation.IsSuccess)
                return contentTypeValidation;

            // Validate file size (read stream to memory first to check size)
            using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var sizeValidation = ValidateFileSize(memoryStream.Length);
            if (!sizeValidation.IsSuccess)
                return Result<string>.Invalid(sizeValidation.ValidationErrors);

            // Load and process image
            using var image = await Image.LoadAsync(memoryStream, cancellationToken);

            // Resize to 400x400 (crop if necessary)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(TargetImageSize, TargetImageSize),
                Mode = ResizeMode.Crop
            }));

            // Determine blob path
            var extension = GetExtensionFromContentType(contentType);
            var blobName = $"users/{SanitizeExternalId(userExternalId)}/profile.{extension}";

            // Delete old photo if exists
            var existingBlob = _containerClient.GetBlobClient(blobName);
            if (await existingBlob.ExistsAsync(cancellationToken))
            {
                try
                {
                    await existingBlob.DeleteAsync(cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old photo at {BlobName}", blobName);
                }
            }

            // Upload to Azure Blob Storage
            using var uploadStream = new MemoryStream();
            await SaveImageToStreamAsync(image, uploadStream, contentType, cancellationToken);
            uploadStream.Position = 0;

            var blobClient = _containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
                CacheControl = "public, max-age=31536000" // Cache for 1 year
            };

            await blobClient.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                },
                cancellationToken);

            // Return public URL
            var publicUrl = blobClient.Uri.ToString();

            _logger.LogInformation(
                "Photo uploaded successfully for user {UserExternalId} to Azure Blob Storage at {BlobUrl}",
                userExternalId,
                publicUrl);

            return Result<string>.Success(publicUrl);
        }
        catch (UnknownImageFormatException)
        {
            return Result<string>.Invalid(new ValidationError
            {
                Identifier = nameof(contentType),
                ErrorMessage = "Invalid image format. Only JPEG, PNG, and WebP are supported."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload photo for user {UserExternalId} to Azure Blob Storage", userExternalId);
            return Result<string>.Error("Failed to upload photo. Please try again.");
        }
    }

    public async Task<Result> DeleteUserPhotoAsync(
        string photoUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
                return Result.Success();

            // Extract blob name from URL
            var uri = new Uri(photoUrl);
            var blobName = uri.AbsolutePath.TrimStart('/');

            // Remove container name from path if present
            var containerName = _containerClient.Name;
            if (blobName.StartsWith(containerName + "/"))
            {
                blobName = blobName.Substring(containerName.Length + 1);
            }

            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Blob not found at {BlobName}, skipping deletion", blobName);
                return Result.Success();
            }

            await blobClient.DeleteAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Photo deleted successfully from Azure Blob Storage at {BlobName}", blobName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo from Azure Blob Storage at {PhotoUrl}", photoUrl);
            return Result.Error("Failed to delete photo. Please try again.");
        }
    }

    private static Result<string> ValidateContentType(string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            return Result<string>.Invalid(new ValidationError
            {
                Identifier = nameof(contentType),
                ErrorMessage = $"Content type '{contentType}' is not supported. Allowed types: {string.Join(", ", AllowedContentTypes)}"
            });
        }

        return Result<string>.Success(contentType);
    }

    private static Result ValidateFileSize(long fileSizeBytes)
    {
        if (fileSizeBytes > MaxFileSizeBytes)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = "fileSize",
                ErrorMessage = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB"
            });
        }

        return Result.Success();
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };
    }

    private static async Task SaveImageToStreamAsync(
        Image image,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        switch (contentType.ToLowerInvariant())
        {
            case "image/jpeg":
                await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = 85 }, cancellationToken);
                break;
            case "image/png":
                await image.SaveAsPngAsync(stream, new PngEncoder(), cancellationToken);
                break;
            case "image/webp":
                await image.SaveAsWebpAsync(stream, new WebpEncoder { Quality = 85 }, cancellationToken);
                break;
            default:
                await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = 85 }, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Sanitizes external ID for use in blob paths (removes special characters).
    /// </summary>
    private static string SanitizeExternalId(string externalId)
    {
        // Replace pipe and other special characters with underscores
        return externalId.Replace("|", "_").Replace("/", "_").Replace("\\", "_");
    }
}

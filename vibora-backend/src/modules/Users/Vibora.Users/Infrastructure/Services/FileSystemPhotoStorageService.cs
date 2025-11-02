using Ardalis.Result;
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
/// File system-based photo storage implementation (for local development).
/// Stores photos in wwwroot/uploads/users/{externalId}/profile.{extension}
/// </summary>
internal sealed class FileSystemPhotoStorageService : IPhotoStorageService
{
    private const int MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB
    private const int TargetImageSize = 400; // 400x400px
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly string _basePath;
    private readonly ILogger<FileSystemPhotoStorageService> _logger;

    public FileSystemPhotoStorageService(
        IConfiguration configuration,
        ILogger<FileSystemPhotoStorageService> logger)
    {
        _basePath = configuration["PhotoStorage:FileSystem:BasePath"]
            ?? throw new InvalidOperationException("PhotoStorage:FileSystem:BasePath is not configured");
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

            // Determine file extension
            var extension = GetExtensionFromContentType(contentType);
            var fileName = $"profile.{extension}";

            // Create directory structure: basePath/users/{externalId}/
            var userDirectory = Path.Combine(_basePath, "users", SanitizeExternalId(userExternalId));
            Directory.CreateDirectory(userDirectory);

            // Delete old photo if exists
            var filePath = Path.Combine(userDirectory, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old photo at {FilePath}", filePath);
                }
            }

            // Save image
            await SaveImageAsync(image, filePath, contentType, cancellationToken);

            // Return relative URL
            var relativeUrl = $"/uploads/users/{SanitizeExternalId(userExternalId)}/{fileName}";

            _logger.LogInformation(
                "Photo uploaded successfully for user {UserExternalId} at {RelativeUrl}",
                userExternalId,
                relativeUrl);

            return Result<string>.Success(relativeUrl);
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
            _logger.LogError(ex, "Failed to upload photo for user {UserExternalId}", userExternalId);
            return Result<string>.Error("Failed to upload photo. Please try again.");
        }
    }

    public Task<Result> DeleteUserPhotoAsync(
        string photoUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract relative path from URL (e.g., /uploads/users/auth0|123/profile.jpg)
            if (string.IsNullOrWhiteSpace(photoUrl))
                return Task.FromResult(Result.Success());

            // Convert URL to file path
            var relativePath = photoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_basePath, relativePath.Replace("uploads" + Path.DirectorySeparatorChar, ""));

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Photo not found at {FilePath}, skipping deletion", filePath);
                return Task.FromResult(Result.Success());
            }

            File.Delete(filePath);

            _logger.LogInformation("Photo deleted successfully at {FilePath}", filePath);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo at {PhotoUrl}", photoUrl);
            return Task.FromResult(Result.Error("Failed to delete photo. Please try again."));
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

    private static async Task SaveImageAsync(
        Image image,
        string filePath,
        string contentType,
        CancellationToken cancellationToken)
    {
        switch (contentType.ToLowerInvariant())
        {
            case "image/jpeg":
                await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = 85 }, cancellationToken);
                break;
            case "image/png":
                await image.SaveAsPngAsync(filePath, new PngEncoder(), cancellationToken);
                break;
            case "image/webp":
                await image.SaveAsWebpAsync(filePath, new WebpEncoder { Quality = 85 }, cancellationToken);
                break;
            default:
                await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = 85 }, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Sanitizes external ID for use in file paths (removes special characters).
    /// </summary>
    private static string SanitizeExternalId(string externalId)
    {
        // Replace pipe and other special characters with underscores
        return externalId.Replace("|", "_").Replace("/", "_").Replace("\\", "_");
    }
}

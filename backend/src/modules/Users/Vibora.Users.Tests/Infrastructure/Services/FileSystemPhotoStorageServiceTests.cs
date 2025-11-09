using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vibora.Users.Infrastructure.Services;

namespace Vibora.Users.Tests.Infrastructure.Services;

public class FileSystemPhotoStorageServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileSystemPhotoStorageService> _logger;
    private readonly FileSystemPhotoStorageService _sut;

    public FileSystemPhotoStorageServiceTests()
    {
        // Create temporary directory for tests
        _testBasePath = Path.Combine(Path.GetTempPath(), "vibora-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);

        // Setup configuration
        var configData = new Dictionary<string, string>
        {
            { "PhotoStorage:FileSystem:BasePath", _testBasePath }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        _logger = Substitute.For<ILogger<FileSystemPhotoStorageService>>();
        _sut = new FileSystemPhotoStorageService(_configuration, _logger);
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithValidJpeg_ShouldReturnSuccessWithUrl()
    {
        // Arrange
        var userExternalId = "auth0|test123";
        using var image = CreateTestImage(800, 600);
        using var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        stream.Position = 0;

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("/uploads/users/");
        result.Value.Should().EndWith("profile.jpg");
        result.Value.Should().Contain("auth0_test123"); // Sanitized

        // Verify file exists
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_test123", "profile.jpg");
        File.Exists(expectedPath).Should().BeTrue();

        // Verify image was resized to 400x400
        using var savedImage = await Image.LoadAsync(expectedPath);
        savedImage.Width.Should().Be(400);
        savedImage.Height.Should().Be(400);
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithValidPng_ShouldReturnSuccessWithUrl()
    {
        // Arrange
        var userExternalId = "auth0|test456";
        using var image = CreateTestImage(600, 600);
        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/png",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().EndWith("profile.png");

        // Verify file exists
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_test456", "profile.png");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithValidWebP_ShouldReturnSuccessWithUrl()
    {
        // Arrange
        var userExternalId = "auth0|test789";
        using var image = CreateTestImage(500, 500);
        using var stream = new MemoryStream();
        await image.SaveAsWebpAsync(stream);
        stream.Position = 0;

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/webp",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().EndWith("profile.webp");

        // Verify file exists
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_test789", "profile.webp");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithInvalidContentType_ShouldReturnInvalid()
    {
        // Arrange
        var userExternalId = "auth0|test123";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/gif", // Not supported
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle();
        result.ValidationErrors.First().ErrorMessage.Should().Contain("not supported");
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithFileTooLarge_ShouldReturnInvalid()
    {
        // Arrange
        var userExternalId = "auth0|test123";
        // Create a 2 MB image (exceeds 1 MB limit)
        var largeData = new byte[2 * 1024 * 1024];
        new Random().NextBytes(largeData);
        using var stream = new MemoryStream(largeData);

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle();
        result.ValidationErrors.First().ErrorMessage.Should().Contain("exceeds maximum");
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithInvalidImageData_ShouldReturnInvalid()
    {
        // Arrange
        var userExternalId = "auth0|test123";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }); // Invalid image data

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle();
        result.ValidationErrors.First().ErrorMessage.Should().Contain("Invalid image format");
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WhenPhotoAlreadyExists_ShouldReplaceOldPhoto()
    {
        // Arrange
        var userExternalId = "auth0|test123";

        // Upload first photo
        using var image1 = CreateTestImage(800, 600);
        using var stream1 = new MemoryStream();
        await image1.SaveAsJpegAsync(stream1);
        stream1.Position = 0;

        var result1 = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream1,
            "image/jpeg",
            CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();

        // Upload second photo (should replace first)
        using var image2 = CreateTestImage(600, 600);
        using var stream2 = new MemoryStream();
        await image2.SaveAsJpegAsync(stream2);
        stream2.Position = 0;

        // Act
        var result2 = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream2,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result2.IsSuccess.Should().BeTrue();

        // Verify only one file exists
        var userDirectory = Path.Combine(_testBasePath, "users", "auth0_test123");
        var files = Directory.GetFiles(userDirectory);
        files.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithNonSquareImage_ShouldCropTo400x400()
    {
        // Arrange
        var userExternalId = "auth0|test123";
        using var image = CreateTestImage(1200, 800); // Wide image
        using var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        stream.Position = 0;

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify image was cropped to square 400x400
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_test123", "profile.jpg");
        using var savedImage = await Image.LoadAsync(expectedPath);
        savedImage.Width.Should().Be(400);
        savedImage.Height.Should().Be(400);
    }

    [Fact]
    public async Task DeleteUserPhotoAsync_WithValidUrl_ShouldDeleteFile()
    {
        // Arrange
        var userExternalId = "auth0|test123";

        // Upload photo first
        using var image = CreateTestImage(800, 600);
        using var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        stream.Position = 0;

        var uploadResult = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        var photoUrl = uploadResult.Value;

        // Act
        var deleteResult = await _sut.DeleteUserPhotoAsync(photoUrl, CancellationToken.None);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify file no longer exists
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_test123", "profile.jpg");
        File.Exists(expectedPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserPhotoAsync_WithNonExistentUrl_ShouldReturnSuccess()
    {
        // Arrange
        var photoUrl = "/uploads/users/auth0_test123/profile.jpg";

        // Act
        var result = await _sut.DeleteUserPhotoAsync(photoUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserPhotoAsync_WithEmptyUrl_ShouldReturnSuccess()
    {
        // Arrange
        var photoUrl = "";

        // Act
        var result = await _sut.DeleteUserPhotoAsync(photoUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadUserPhotoAsync_WithSpecialCharactersInExternalId_ShouldSanitize()
    {
        // Arrange
        var userExternalId = "auth0|user/123\\test";
        using var image = CreateTestImage(800, 600);
        using var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        stream.Position = 0;

        // Act
        var result = await _sut.UploadUserPhotoAsync(
            userExternalId,
            stream,
            "image/jpeg",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("auth0_user_123_test"); // Sanitized

        // Verify file exists with sanitized path
        var expectedPath = Path.Combine(_testBasePath, "users", "auth0_user_123_test", "profile.jpg");
        File.Exists(expectedPath).Should().BeTrue();
    }

    private static Image CreateTestImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);

        // Fill with a color to make it a valid image
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(100, 150, 200);
            }
        }

        return image;
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

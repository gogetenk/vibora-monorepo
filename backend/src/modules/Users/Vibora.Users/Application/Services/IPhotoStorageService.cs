using Ardalis.Result;

namespace Vibora.Users.Application.Services;

/// <summary>
/// Service for uploading and deleting user profile photos.
/// </summary>
internal interface IPhotoStorageService
{
    /// <summary>
    /// Uploads a user's profile photo.
    /// </summary>
    /// <param name="userExternalId">The user's external ID (Auth0/Supabase sub claim).</param>
    /// <param name="photoStream">The photo file stream.</param>
    /// <param name="contentType">The content type (e.g., image/jpeg, image/png).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the public URL of the uploaded photo on success.</returns>
    Task<Result<string>> UploadUserPhotoAsync(
        string userExternalId,
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user's profile photo by URL.
    /// </summary>
    /// <param name="photoUrl">The URL of the photo to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteUserPhotoAsync(
        string photoUrl,
        CancellationToken cancellationToken = default);
}

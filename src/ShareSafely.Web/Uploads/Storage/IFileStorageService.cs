namespace ShareSafely.Web.Uploads.Storage;

/// <summary>
/// Defines the contract for storing files in a storage backend.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores an uploaded file and returns the storage result with file location information.
    /// </summary>
    /// <param name="file">The file to store.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A StoredFileResult containing the storage outcome and file metadata.</returns>
    Task<StoredFileResult> StoreFileAsync(IFormFile file, CancellationToken cancellationToken = default);
}

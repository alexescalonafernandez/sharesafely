namespace ShareSafely.Web.Uploads;

/// <summary>
/// Implements file storage using the local file system.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly UploadOptions _options;

    /// <summary>
    /// Initializes a new instance of the LocalFileStorageService.
    /// </summary>
    /// <param name="options">Configuration options for file storage.</param>
    /// <exception cref="ArgumentException">Thrown if LocalStoragePath is not configured or is not a valid directory path.</exception>
    public LocalFileStorageService(UploadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LocalStoragePath))
        {
            throw new ArgumentException("LocalStoragePath must be configured.", nameof(options));
        }

        // Validate that LocalStoragePath is a valid possible directory path
        if (!IsValidDirectoryPath(options.LocalStoragePath))
        {
            throw new ArgumentException("LocalStoragePath must be a valid directory path.", nameof(options));
        }

        _options = options;
    }

    /// <summary>
    /// Validates that a path is a valid possible directory path.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    private static bool IsValidDirectoryPath(string path)
    {
        try
        {
            // Check for invalid path characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }

            // Ensure the path is rooted (absolute path)
            if (!Path.IsPathRooted(path))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stores a file to the local storage path asynchronously.
    /// </summary>
    /// <param name="file">The file to store.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A StoredFileResult containing the storage outcome and file metadata.</returns>
    public async Task<StoredFileResult> StoreFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null)
            {
                return StoredFileResult.Failure("File is required.");
            }

            // Generate a safe file name using a GUID and preserve the original extension
            var originalExtension = Path.GetExtension(file.FileName);
            var safeFileName = $"{Guid.NewGuid:N}{originalExtension}";

            // Ensure the target directory exists
            var storagePath = _options.LocalStoragePath;
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            // Build the full file path
            var fullPath = Path.Combine(storagePath, safeFileName);

            // Store the file asynchronously
            using (var stream = file.OpenReadStream())
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await stream.CopyToAsync(fileStream, bufferSize: 4096, cancellationToken);
                }
            }

            // Return success with file metadata
            return StoredFileResult.Success(file.FileName, safeFileName, fullPath);
        }
        catch (OperationCanceledException)
        {
            return StoredFileResult.Failure("File storage operation was cancelled.");
        }
        catch (IOException ex)
        {
            return StoredFileResult.Failure($"An I/O error occurred while storing the file: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StoredFileResult.Failure($"An unexpected error occurred while storing the file: {ex.Message}");
        }
    }
}

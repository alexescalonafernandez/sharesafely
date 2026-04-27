namespace ShareSafely.Web.Uploads;

/// <summary>
/// Represents the result of a file storage operation.
/// </summary>
public class StoredFileResult
{
    private StoredFileResult(bool isSuccess, string? originalFileName, string? storedFileName, string? fullStoredPath, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        FullStoredPath = fullStoredPath;
        Errors = errors;
    }

    /// <summary>
    /// Indicates whether the file was stored successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The original file name as provided by the uploader.
    /// </summary>
    public string? OriginalFileName { get; }

    /// <summary>
    /// The safe file name used for storage (e.g., a generated name).
    /// </summary>
    public string? StoredFileName { get; }

    /// <summary>
    /// The full path where the file was stored.
    /// </summary>
    public string? FullStoredPath { get; }

    /// <summary>
    /// List of error messages if the storage operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful storage result with file metadata.
    /// </summary>
    public static StoredFileResult Success(string originalFileName, string storedFileName, string fullStoredPath) =>
        new(isSuccess: true, originalFileName, storedFileName, fullStoredPath, errors: []);

    /// <summary>
    /// Creates a failed storage result with a single error message.
    /// </summary>
    public static StoredFileResult Failure(string error) =>
        new(isSuccess: false, originalFileName: null, storedFileName: null, fullStoredPath: null, errors: [error]);

    /// <summary>
    /// Creates a failed storage result with multiple error messages.
    /// </summary>
    public static StoredFileResult Failure(params string[] errors) =>
        new(isSuccess: false, originalFileName: null, storedFileName: null, fullStoredPath: null, errors: errors.ToList().AsReadOnly());
}

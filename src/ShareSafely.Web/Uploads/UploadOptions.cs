namespace ShareSafely.Web.Uploads;

/// <summary>
/// Configuration options for file uploads, including size limits, allowed file types, and storage path.
/// </summary>
public class UploadOptions
{
    /// <summary>
    /// Maximum allowed file size in bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>
    /// Array of allowed file extensions (e.g., [".pdf", ".docx"]).
    /// Extension comparison is case-insensitive.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>
    /// Local storage path for uploaded files.
    /// </summary>
    public string LocalStoragePath { get; set; } = string.Empty;
}

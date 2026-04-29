namespace ShareSafely.Web.Uploads.Validation;

/// <summary>
/// Validates IFormFile uploads against UploadOptions constraints.
/// </summary>
public class FileUploadValidator
{
    /// <summary>
    /// Validates a file upload against the specified upload options.
    /// </summary>
    /// <param name="file">The file to validate. Can be null.</param>
    /// <param name="options">Upload configuration options.</param>
    /// <returns>A FileUploadValidationResult indicating success or failure with error messages.</returns>
    public FileUploadValidationResult Validate(IFormFile? file, UploadOptions options)
    {
        // Validation: File is required
        if (file == null)
        {
            return FileUploadValidationResult.Failure("File is required.");
        }

        // Validation: File cannot be empty
        if (file.Length == 0)
        {
            return FileUploadValidationResult.Failure("File cannot be empty.");
        }

        // Validation: File size must not exceed MaxFileSizeBytes
        if (file.Length > options.MaxFileSizeBytes)
        {
            var maxSizeMB = options.MaxFileSizeBytes / (1024 * 1024);
            return FileUploadValidationResult.Failure(
                $"File size exceeds the maximum allowed size of {maxSizeMB} MB.");
        }

        // Validation: File extension must be in AllowedExtensions (case-insensitive)
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensionsLower = options.AllowedExtensions.Select(e => e.ToLowerInvariant()).ToList();

        if (!allowedExtensionsLower.Contains(fileExtension))
        {
            var allowedList = string.Join(", ", options.AllowedExtensions);
            return FileUploadValidationResult.Failure(
                $"File type '{fileExtension}' is not allowed. Allowed types: {allowedList}");
        }

        // All validations passed
        return FileUploadValidationResult.Success();
    }
}

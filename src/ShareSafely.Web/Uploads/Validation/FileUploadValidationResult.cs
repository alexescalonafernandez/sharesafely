namespace ShareSafely.Web.Uploads.Validation;

/// <summary>
/// Represents the result of a file upload validation operation.
/// </summary>
public class FileUploadValidationResult
{
    private FileUploadValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Indicates whether the validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// List of validation error messages. Empty if validation succeeded.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    public static FileUploadValidationResult Success() =>
        new(isValid: true, errors: []);

    /// <summary>
    /// Creates a failed validation result with a single error message.
    /// </summary>
    public static FileUploadValidationResult Failure(string error) =>
        new(isValid: false, errors: [error]);

    /// <summary>
    /// Creates a failed validation result with multiple error messages.
    /// </summary>
    public static FileUploadValidationResult Failure(params string[] errors) =>
        new(isValid: false, errors: errors.ToList().AsReadOnly());
}

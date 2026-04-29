using Microsoft.AspNetCore.Http;
using ShareSafely.Web.Uploads;
using ShareSafely.Web.Uploads.Validation;

namespace ShareSafely.Web.Tests.Uploads;

public class FileUploadValidatorTests
{
    private readonly FileUploadValidator _validator = new();

    #region Helper Methods

    /// <summary>
    /// Creates a mock IFormFile for testing.
    /// </summary>
    private static IFormFile CreateFormFile(string fileName, long fileSizeBytes)
    {
        var content = new byte[fileSizeBytes];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, fileSizeBytes, "file", fileName);
    }

    #endregion

    #region Null File Tests

    [Fact]
    public void Validate_WithNullFile_ReturnsFailure()
    {
        // Arrange
        IFormFile? file = null;
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("File is required", result.Errors[0]);
    }

    #endregion

    #region Empty File Tests

    [Fact]
    public void Validate_WithEmptyFile_ReturnsFailure()
    {
        // Arrange
        var file = CreateFormFile("empty.pdf", 0);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("File cannot be empty", result.Errors[0]);
    }

    #endregion

    #region File Size Tests

    [Fact]
    public void Validate_WithOversizedFile_ReturnsFailure()
    {
        // Arrange
        const long maxSize = 1024; // 1 KB
        var file = CreateFormFile("large.pdf", maxSize + 1);
        var options = new UploadOptions { MaxFileSizeBytes = maxSize, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("exceeds the maximum allowed size", result.Errors[0]);
    }

    [Fact]
    public void Validate_WithFileAtExactMaxSize_ReturnsSuccess()
    {
        // Arrange
        const long maxSize = 1024; // 1 KB
        var file = CreateFormFile("document.pdf", maxSize);
        var options = new UploadOptions { MaxFileSizeBytes = maxSize, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Extension Tests

    [Fact]
    public void Validate_WithDisallowedExtension_ReturnsFailure()
    {
        // Arrange
        var file = CreateFormFile("document.exe", 512);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf", ".docx"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not allowed", result.Errors[0]);
        Assert.Contains(".exe", result.Errors[0]);
    }

    [Fact]
    public void Validate_WithAllowedExtension_ReturnsSuccess()
    {
        // Arrange
        var file = CreateFormFile("document.pdf", 512);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf", ".docx"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithUppercaseExtension_ReturnsSuccess()
    {
        // Arrange
        var file = CreateFormFile("document.PDF", 512);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithMixedCaseExtension_ReturnsSuccess()
    {
        // Arrange
        var file = CreateFormFile("document.PdF", 512);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf", ".docx"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Valid File Tests

    [Fact]
    public void Validate_WithValidFile_ReturnsSuccess()
    {
        // Arrange
        var file = CreateFormFile("important_document.docx", 2048);
        var options = new UploadOptions
        {
            MaxFileSizeBytes = 5 * 1024 * 1024, // 5 MB
            AllowedExtensions = [".pdf", ".docx", ".txt"]
        };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithValidSmallFile_ReturnsSuccess()
    {
        // Arrange
        var file = CreateFormFile("test.txt", 10);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".txt"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Integration Tests

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".PDF")]
    [InlineData(".Pdf")]
    public void Validate_WithVariousCaseExtensions_AllReturnSuccess(string extension)
    {
        // Arrange
        var fileName = $"file{extension}";
        var file = CreateFormFile(fileName, 512);
        var options = new UploadOptions { MaxFileSizeBytes = 1024, AllowedExtensions = [".pdf"] };

        // Act
        var result = _validator.Validate(file, options);

        // Assert
        Assert.True(result.IsValid, $"Should accept extension {extension}");
        Assert.Empty(result.Errors);
    }

    #endregion
}

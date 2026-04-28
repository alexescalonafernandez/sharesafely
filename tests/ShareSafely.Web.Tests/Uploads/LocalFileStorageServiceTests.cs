using Microsoft.AspNetCore.Http;
using ShareSafely.Web.Uploads;

namespace ShareSafely.Web.Tests.Uploads;

public class LocalFileStorageServiceTests
{
    #region Helper Methods

    /// <summary>
    /// Creates a mock IFormFile for testing.
    /// </summary>
    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    /// <summary>
    /// Creates a temporary directory and returns its path.
    /// </summary>
    private static string CreateTempDirectory()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Safely deletes a directory and all its contents.
    /// </summary>
    private static void SafeDeleteDirectory(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
        }
        catch
        {
            // Best effort cleanup; ignore errors
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLocalStoragePath_ThrowsArgumentException()
    {
        // Arrange
        var options = new UploadOptions { LocalStoragePath = null! };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalFileStorageService(options));
        Assert.Contains("LocalStoragePath must be configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyLocalStoragePath_ThrowsArgumentException()
    {
        // Arrange
        var options = new UploadOptions { LocalStoragePath = string.Empty };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalFileStorageService(options));
        Assert.Contains("LocalStoragePath must be configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceLocalStoragePath_ThrowsArgumentException()
    {
        // Arrange
        var options = new UploadOptions { LocalStoragePath = "   " };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalFileStorageService(options));
        Assert.Contains("LocalStoragePath must be configured", exception.Message);
    }

    #endregion

    #region Null File Tests

    [Fact]
    public async Task StoreFileAsync_WithNullFile_ReturnsFailure()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);

            // Act
            var result = await service.StoreFileAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.Contains("File is required", result.Errors[0]);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region Directory Creation Tests

    [Fact]
    public async Task StoreFileAsync_WhenDirectoryDoesNotExist_CreatesTargetDirectory()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // Explicitly do NOT create the directory - test that StoreFileAsync creates it
            Assert.False(Directory.Exists(tempDir));

            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("test.txt", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(Directory.Exists(tempDir), "Target directory should have been created");
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region File Storage Tests

    [Fact]
    public async Task StoreFileAsync_WithValidFile_StoresTheUploadedFile()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var fileContent = "test file content";
            var file = CreateFormFile("test.txt", fileContent);

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(result.FullStoredPath), "File should be stored at the returned path");

            // Verify file content
            var storedContent = await File.ReadAllTextAsync(result.FullStoredPath);
            Assert.Equal(fileContent, storedContent);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region File Naming Tests

    [Fact]
    public async Task StoreFileAsync_GeneratesStoredFileNameDifferentFromOriginal()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var originalFileName = "test.txt";
            var file = CreateFormFile(originalFileName, "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEqual(originalFileName, result.StoredFileName);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task StoreFileAsync_PreservesOriginalFileExtension()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var originalFileName = "document.pdf";
            var file = CreateFormFile(originalFileName, "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            var originalExtension = Path.GetExtension(originalFileName);
            var storedExtension = Path.GetExtension(result.StoredFileName);
            Assert.Equal(originalExtension, storedExtension);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("spreadsheet.xlsx")]
    [InlineData("archive.zip")]
    [InlineData("document.docx")]
    public async Task StoreFileAsync_PreservesVariousFileExtensions(string fileName)
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile(fileName, "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            var expectedExtension = Path.GetExtension(fileName);
            var storedExtension = Path.GetExtension(result.StoredFileName);
            Assert.Equal(expectedExtension, storedExtension);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public async Task StoreFileAsync_ReturnsOriginalFileName()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var originalFileName = "myfile.txt";
            var file = CreateFormFile(originalFileName, "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(originalFileName, result.OriginalFileName);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task StoreFileAsync_ReturnsStoredFileName()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("test.txt", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.StoredFileName);
            Assert.NotEmpty(result.StoredFileName);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task StoreFileAsync_ReturnsFullStoredPath()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("test.txt", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.FullStoredPath);
            Assert.True(Path.IsPathRooted(result.FullStoredPath), "Full path should be absolute");
            Assert.True(File.Exists(result.FullStoredPath), "File should exist at the returned path");
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task StoreFileAsync_FullStoredPathContainsStoredFileName()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("test.txt", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.StoredFileName);
            Assert.Contains(result.StoredFileName, result.FullStoredPath);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region Relative Path Tests

    [Fact]
    public async Task StoreFileAsync_SupportsRelativeLocalStoragePath()
    {
        // Arrange
        var tempBaseDir = CreateTempDirectory();
        var relativePath = "uploads";
        var fullPath = Path.Combine(tempBaseDir, relativePath);
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            // Create the base directory first
            Directory.CreateDirectory(tempBaseDir);

            // Change to temp directory to test relative path
            Directory.SetCurrentDirectory(tempBaseDir);

            var options = new UploadOptions { LocalStoragePath = relativePath };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("test.txt", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(Directory.Exists(fullPath), "Relative directory should be created");
            Assert.True(File.Exists(result.FullStoredPath), "File should be stored");
        }
        finally
        {
            try
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
            catch
            {
                // Ignore if unable to restore directory
            }

            SafeDeleteDirectory(tempBaseDir);
        }
    }

    #endregion

    #region Multiple File Storage Tests

    [Fact]
    public async Task StoreFileAsync_MultipleFilesGenerateUniqueStoredFileNames()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);

            // Create files with fresh streams each time
            var file1Stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content 1"));
            var file1 = new FormFile(file1Stream, 0, file1Stream.Length, "file", "test.txt");

            var file2Stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content 2"));
            var file2 = new FormFile(file2Stream, 0, file2Stream.Length, "file", "test.txt");

            // Act
            var result1 = await service.StoreFileAsync(file1);
            var result2 = await service.StoreFileAsync(file2);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.NotEqual(result1.StoredFileName, result2.StoredFileName);
            Assert.True(File.Exists(result1.FullStoredPath));
            Assert.True(File.Exists(result2.FullStoredPath));
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task StoreFileAsync_WithEmptyFile_StoresSuccessfully()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("empty.txt", string.Empty);

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(result.FullStoredPath));
            var fileInfo = new FileInfo(result.FullStoredPath);
            Assert.Equal(0, fileInfo.Length);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task StoreFileAsync_WithFileHavingNoExtension_StoresSuccessfully()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            var options = new UploadOptions { LocalStoragePath = tempDir };
            var service = new LocalFileStorageService(options);
            var file = CreateFormFile("noextension", "test content");

            // Act
            var result = await service.StoreFileAsync(file);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(result.FullStoredPath));
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    #endregion
}

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace ShareSafely.Web.Uploads.Storage.Azure;

/// <summary>
/// Implements file storage using Azure Blob Storage.
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly IOptions<AzureStorageOptions> _options;
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Initializes a new instance of the AzureBlobStorageService.
    /// </summary>
    /// <param name="options">Configuration options for Azure Storage.</param>
    /// <exception cref="ArgumentException">Thrown if AccountName or BlobContainerName is not configured.</exception>
    public AzureBlobStorageService(IOptions<AzureStorageOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var azureStorageOptions = _options.Value;

        if (string.IsNullOrWhiteSpace(azureStorageOptions.AccountName))
        {
            throw new ArgumentException("AccountName must be configured.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(azureStorageOptions.BlobContainerName))
        {
            throw new ArgumentException("BlobContainerName must be configured.", nameof(options));
        }

        // Create the BlobServiceClient using DefaultAzureCredential
        var blobServiceUri = new Uri($"https://{azureStorageOptions.AccountName}.blob.core.windows.net");
        _blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
    }

    /// <summary>
    /// Stores a file to Azure Blob Storage asynchronously.
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

            // Get the configured blob container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.Value.BlobContainerName);

            // Generate a safe blob name using a GUID and preserve the original extension
            var originalExtension = Path.GetExtension(file.FileName);
            var safeBlobName = $"{Guid.NewGuid():N}{originalExtension}";

            // Get the blob client
            var blobClient = containerClient.GetBlobClient(safeBlobName);

            // Upload the file stream asynchronously
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = file.ContentType
                        }
                    },
                    cancellationToken);
            }

            // Build the blob URI
            var blobUri = blobClient.Uri.ToString();

            // Return success with file metadata
            return StoredFileResult.Success(file.FileName, safeBlobName, blobUri);
        }
        catch (OperationCanceledException)
        {
            return StoredFileResult.Failure("File storage operation was cancelled.");
        }
        catch (RequestFailedException ex) when (ex.Status == 401)
        {
            return StoredFileResult.Failure("Unauthorized: Unable to authenticate with Azure Storage.");
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
        {
            return StoredFileResult.Failure("Forbidden: Insufficient permissions to upload to the blob container.");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return StoredFileResult.Failure("Not Found: The blob container does not exist.");
        }
        catch (RequestFailedException ex)
        {
            return StoredFileResult.Failure($"Azure Storage error: {ex.Message}");
        }
        catch (IOException ex)
        {
            return StoredFileResult.Failure($"An I/O error occurred while uploading the file: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StoredFileResult.Failure($"An unexpected error occurred while storing the file: {ex.Message}");
        }
    }
}

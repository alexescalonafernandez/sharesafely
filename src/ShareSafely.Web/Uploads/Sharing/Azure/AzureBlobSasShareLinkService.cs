using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using ShareSafely.Web.Uploads.Storage.Azure;

namespace ShareSafely.Web.Uploads.Sharing.Azure;

public class AzureBlobSasShareLinkService : IShareLinkService
{
    private readonly AzureStorageOptions _storageOptions;
    private readonly ShareLinkOptions _shareLinkOptions;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobSasShareLinkService(
        IOptions<AzureStorageOptions> storageOptions,
        IOptions<ShareLinkOptions> shareLinkOptions)
    {
        if(storageOptions is null)
            throw new ArgumentNullException(nameof(storageOptions));

        if (shareLinkOptions is null)
            throw new ArgumentNullException(nameof(shareLinkOptions));

        _storageOptions = storageOptions.Value;
        _shareLinkOptions = shareLinkOptions.Value;

        if (string.IsNullOrWhiteSpace(_storageOptions.AccountName))
        {
            throw new ArgumentException("AccountName must be configured.", nameof(storageOptions));
        }

        if (string.IsNullOrWhiteSpace(_storageOptions.BlobContainerName))
        {
            throw new ArgumentException("BlobContainerName must be configured.", nameof(storageOptions));
        }

        if(_shareLinkOptions.ExpirationMinutes <= 0)
        {
            throw new ArgumentException("Expiration must be set and greater than 0.", nameof(shareLinkOptions));
        }

        // Create the BlobServiceClient using DefaultAzureCredential
        var blobServiceUri = new Uri($"https://{_storageOptions.AccountName}.blob.core.windows.net");
        _blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
    }

    public async Task<ShareLinkResult> CreateShareLinkAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return ShareLinkResult.Failure("Stored file name is required.");
            }

            // 1. Obtain the User Delegation Key
            // The start time should be slightly before 'now' to avoid clock synchronization problems
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_shareLinkOptions.ExpirationMinutes);
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                startsOn: DateTimeOffset.UtcNow.AddMinutes(-5),
                expiresOn: expiresOn,
                cancellationToken: cancellationToken);

            // 2. Configure the SAS values
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _storageOptions.BlobContainerName,
                BlobName = storedFileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = expiresOn,
                Protocol = SasProtocol.Https,
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // 3. Generate the full URI with the SAS token
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(_storageOptions.BlobContainerName)
                .GetBlobClient(storedFileName);

            var sasUri = blobClient.GenerateUserDelegationSasUri(sasBuilder, userDelegationKey);

            return ShareLinkResult.Success(sasUri.ToString(), expiresOn);
        }
        catch (ArgumentException ex)
        {
            return ShareLinkResult.Failure($"Invalid blob or container name: {ex.Message}");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ShareLinkResult.Failure("Blob or container not found.");
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            return ShareLinkResult.Failure("Insufficient permissions to generate SAS token.");
        }
        catch (RequestFailedException ex)
        {
            return ShareLinkResult.Failure($"Azure Storage error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ShareLinkResult.Failure($"Failed to create share link: {ex.Message}");
        }
    }
}

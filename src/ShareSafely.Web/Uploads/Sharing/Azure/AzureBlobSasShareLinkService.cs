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

    public AzureBlobSasShareLinkService(
        IOptions<AzureStorageOptions> storageOptions,
        IOptions<ShareLinkOptions> shareLinkOptions)
    {
        _storageOptions = storageOptions.Value;
        _shareLinkOptions = shareLinkOptions.Value;
    }

    public async Task<ShareLinkResult> CreateShareLinkAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceUri = new Uri($"https://{_storageOptions.AccountName}.blob.core.windows.net");
            var blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());

            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_shareLinkOptions.ExpirationMinutes);
            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
                startsOn: DateTimeOffset.UtcNow.AddMinutes(-5),
                expiresOn: expiresOn,
                cancellationToken: cancellationToken);

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

            var blobClient = blobServiceClient
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

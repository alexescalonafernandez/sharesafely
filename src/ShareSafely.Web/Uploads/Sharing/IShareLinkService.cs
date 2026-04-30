namespace ShareSafely.Web.Uploads.Sharing;

public interface IShareLinkService
{
    Task<ShareLinkResult> CreateShareLinkAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}

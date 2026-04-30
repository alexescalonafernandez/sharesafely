namespace ShareSafely.Web.Uploads.Sharing;

public class ShareLinkResult
{
    private ShareLinkResult(
        bool isSuccess,
        string? shareUrl,
        DateTimeOffset? expiresOn,
        IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        ShareUrl = shareUrl;
        ExpiresOn = expiresOn;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public string? ShareUrl { get; }

    public DateTimeOffset? ExpiresOn { get; }

    public IReadOnlyList<string> Errors { get; }

    public static ShareLinkResult Success(string shareUrl, DateTimeOffset expiresOn) =>
        new(isSuccess: true, shareUrl, expiresOn, errors: []);

    public static ShareLinkResult Failure(string error) =>
        new(isSuccess: false, shareUrl: null, expiresOn: null, errors: [error]);

    public static ShareLinkResult Failure(params string[] errors) =>
        new(isSuccess: false, shareUrl: null, expiresOn: null, errors: errors.ToList().AsReadOnly());
}

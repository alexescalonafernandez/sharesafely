using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ShareSafely.Web.Uploads;
using ShareSafely.Web.Uploads.Sharing;
using ShareSafely.Web.Uploads.Storage;
using ShareSafely.Web.Uploads.Validation;

namespace ShareSafely.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly FileUploadValidator _fileUploadValidator;
    private readonly IFileStorageService _fileStorageService;
    private readonly IShareLinkService _shareLinkService;
    private readonly UploadOptions _uploadOptions;

    public IndexModel(
        ILogger<IndexModel> logger,
        FileUploadValidator fileUploadValidator,
        IFileStorageService fileStorageService,
        IShareLinkService shareLinkService,
        IOptions<UploadOptions> uploadOptions)
    {
        _logger = logger;
        _fileUploadValidator = fileUploadValidator;
        _fileStorageService = fileStorageService;
        _shareLinkService = shareLinkService;
        _uploadOptions = uploadOptions.Value;
    }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public List<string> ValidationErrors { get; set; } = new();
    public List<string> StorageErrors { get; set; } = new();
    public List<string> ShareLinkErrors { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ShareUrl { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate the uploaded file
        var validationResult = _fileUploadValidator.Validate(UploadedFile, _uploadOptions);

        if (!validationResult.IsValid)
        {
            ValidationErrors.AddRange(validationResult.Errors);
            return Page();
        }

        // Store the file
        var storageResult = await _fileStorageService.StoreFileAsync(UploadedFile!, HttpContext.RequestAborted);

        if (!storageResult.IsSuccess)
        {
            StorageErrors.AddRange(storageResult.Errors);
            return Page();
        }

        // Generate share link for the stored file
        var shareLinkResult = await _shareLinkService.CreateShareLinkAsync(
            storageResult.StoredFileName!,
            HttpContext.RequestAborted);

        if (!shareLinkResult.IsSuccess)
        {
            ShareLinkErrors.AddRange(shareLinkResult.Errors);
            return Page();
        }

        // Success: Set success message and share link information
        SuccessMessage = $"File '{storageResult.OriginalFileName}' uploaded successfully.";
        ShareUrl = shareLinkResult.ShareUrl;
        ExpiresOn = shareLinkResult.ExpiresOn;
        UploadedFile = null;

        return Page();
    }
}

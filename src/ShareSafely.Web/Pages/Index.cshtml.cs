using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ShareSafely.Web.Uploads;

namespace ShareSafely.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly FileUploadValidator _fileUploadValidator;
    private readonly IFileStorageService _fileStorageService;
    private readonly UploadOptions _uploadOptions;

    public IndexModel(
        ILogger<IndexModel> logger,
        FileUploadValidator fileUploadValidator,
        IFileStorageService fileStorageService,
        IOptions<UploadOptions> uploadOptions)
    {
        _logger = logger;
        _fileUploadValidator = fileUploadValidator;
        _fileStorageService = fileStorageService;
        _uploadOptions = uploadOptions.Value;
    }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public List<string> ValidationErrors { get; set; } = new();
    public List<string> StorageErrors { get; set; } = new();
    public string? SuccessMessage { get; set; }

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

        // Success: Set success message with original and stored file names
        SuccessMessage = $"File '{storageResult.OriginalFileName}' uploaded successfully as '{storageResult.StoredFileName}'.";
        UploadedFile = null;

        return Page();
    }
}

using Microsoft.Extensions.Options;
using ShareSafely.Web.Uploads;
using ShareSafely.Web.Uploads.Sharing;
using ShareSafely.Web.Uploads.Sharing.Azure;
using ShareSafely.Web.Uploads.Storage;
using ShareSafely.Web.Uploads.Storage.Azure;
using ShareSafely.Web.Uploads.Storage.Local;
using ShareSafely.Web.Uploads.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<UploadOptions>(
    builder.Configuration.GetSection("Upload")
);

builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection("AzureStorage")
);

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage")
);

builder.Services.Configure<ShareLinkOptions>(
    builder.Configuration.GetSection("ShareLinks")
);

builder.Services.AddSingleton<FileUploadValidator>();

builder.Services.AddScoped<LocalFileStorageService>();
builder.Services.AddScoped<AzureBlobStorageService>();

builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

    return storageOptions.Provider switch
    {
        "Local" => sp.GetRequiredService<LocalFileStorageService>(),
        "AzureBlob" => sp.GetRequiredService<AzureBlobStorageService>(),
        _ => throw new InvalidOperationException($"Unsupported storage provider: {storageOptions.Provider}")
    };
});

builder.Services.AddScoped<IShareLinkService, AzureBlobSasShareLinkService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

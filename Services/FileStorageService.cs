using CrmApi.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace CrmApi.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    // Task<Stream> DownloadAsync(string blobName);
    // Task<bool> DeleteAsync(string blobName);
    // string GetDownloadUrl(string blobName, TimeSpan expiry);
}

public class FileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        var blobServiceClient = new BlobServiceClient(config["Azure:StorageConnectionString"]);
        _containerClient = blobServiceClient.GetBlobContainerClient(config["Azure:ContainerName"]);
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobName = $"client-note-documents/{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

        _logger.LogInformation("File uploaded to blob storage with name {BlobName}", blobName);
        return blobName;
    }
}
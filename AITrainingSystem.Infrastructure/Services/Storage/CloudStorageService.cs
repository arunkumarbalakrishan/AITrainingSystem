using System;
using System.IO;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Services;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace AITrainingSystem.Infrastructure.Services.Storage
{
    public class CloudStorageService : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly string _provider; // "S3" or "Azure"

        // S3 client fields
        private readonly IAmazonS3? _s3Client;
        private readonly string _s3BucketName = string.Empty;

        // Azure client fields
        private readonly BlobServiceClient? _blobServiceClient;
        private readonly string _azureContainerName = string.Empty;

        public CloudStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _provider = _configuration["Storage:Provider"] ?? "S3";

            if (_provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                var accessKey = _configuration["Storage:S3:AccessKey"];
                var secretKey = _configuration["Storage:S3:SecretKey"];
                var region = _configuration["Storage:S3:Region"] ?? "us-east-1";
                _s3BucketName = _configuration["Storage:S3:BucketName"] ?? "ai-training-system";

                var awsRegion = Amazon.RegionEndpoint.GetBySystemName(region);

                if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                {
                    _s3Client = new AmazonS3Client(accessKey, secretKey, awsRegion);
                }
                else
                {
                    // Fallback to local or default client setup
                    _s3Client = new AmazonS3Client(awsRegion);
                }
            }
            else if (_provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = _configuration["Storage:Azure:ConnectionString"];
                _azureContainerName = _configuration["Storage:Azure:ContainerName"] ?? "ai-training-system";

                if (!string.IsNullOrEmpty(connectionString))
                {
                    _blobServiceClient = new BlobServiceClient(connectionString);
                }
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName, string contentType)
        {
            var fileKey = $"{folderName}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            if (_provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                if (_s3Client == null) throw new InvalidOperationException("S3 Client not configured.");

                var fileTransferUtility = new TransferUtility(_s3Client);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = fileKey,
                    BucketName = _s3BucketName,
                    ContentType = contentType
                };

                await fileTransferUtility.UploadAsync(uploadRequest);
                return fileKey;
            }
            else if (_provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                if (_blobServiceClient == null) throw new InvalidOperationException("Azure Blob Service Client not configured.");

                var containerClient = _blobServiceClient.GetBlobContainerClient(_azureContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobClient = containerClient.GetBlobClient(fileKey);
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                };

                await blobClient.UploadAsync(fileStream, options);
                return fileKey;
            }

            throw new NotSupportedException($"Storage provider '{_provider}' is not supported.");
        }

        public async Task DeleteFileAsync(string fileKey)
        {
            if (_provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                if (_s3Client == null) return;
                await _s3Client.DeleteObjectAsync(_s3BucketName, fileKey);
            }
            else if (_provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                if (_blobServiceClient == null) return;
                var containerClient = _blobServiceClient.GetBlobContainerClient(_azureContainerName);
                var blobClient = containerClient.GetBlobClient(fileKey);
                await blobClient.DeleteIfExistsAsync();
            }
        }

        public async Task<Stream> GetFileStreamAsync(string fileKey)
        {
            if (_provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                if (_s3Client == null) throw new InvalidOperationException("S3 Client not configured.");
                var response = await _s3Client.GetObjectAsync(_s3BucketName, fileKey);
                return response.ResponseStream;
            }
            else if (_provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                if (_blobServiceClient == null) throw new InvalidOperationException("Azure Blob Service Client not configured.");
                var containerClient = _blobServiceClient.GetBlobContainerClient(_azureContainerName);
                var blobClient = containerClient.GetBlobClient(fileKey);
                var downloadInfo = await blobClient.DownloadStreamingAsync();
                return downloadInfo.Value.Content;
            }

            throw new NotSupportedException($"Storage provider '{_provider}' is not supported.");
        }

        public Task<string> GetSignedUrlAsync(string fileKey, double expiryMinutes = 60)
        {
            if (_provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                if (_s3Client == null) throw new InvalidOperationException("S3 Client not configured.");

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _s3BucketName,
                    Key = fileKey,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                };

                var url = _s3Client.GetPreSignedURL(request);
                return Task.FromResult(url);
            }
            else if (_provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                if (_blobServiceClient == null) throw new InvalidOperationException("Azure Blob Service Client not configured.");

                var containerClient = _blobServiceClient.GetBlobContainerClient(_azureContainerName);
                var blobClient = containerClient.GetBlobClient(fileKey);

                if (blobClient.CanGenerateSasUri)
                {
                    var sasUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(expiryMinutes));
                    return Task.FromResult(sasUri.ToString());
                }

                throw new InvalidOperationException("Azure SAS token generation not supported for this client configuration.");
            }

            throw new NotSupportedException($"Storage provider '{_provider}' is not supported.");
        }
    }
}

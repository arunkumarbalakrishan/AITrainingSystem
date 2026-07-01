using System;
using System.IO;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Services;

namespace AITrainingSystem.Infrastructure.Services.Storage
{
    public class LocalStorageService : IStorageService
    {
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Storage", folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var destinationStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            return $"{folderName}/{uniqueFileName}";
        }

        public Task DeleteFileAsync(string fileKey)
        {
            var key = CleanFileKey(fileKey);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", key);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        public Task<Stream> GetFileStreamAsync(string fileKey)
        {
            var key = CleanFileKey(fileKey);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", key);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Local storage file not found.", fullPath);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, useAsync: true);
            return Task.FromResult(stream);
        }

        public Task<string> GetSignedUrlAsync(string fileKey, double expiryMinutes = 60)
        {
            var key = CleanFileKey(fileKey);
            // For local storage, we fallback to streaming endpoint URLs
            return Task.FromResult($"/api/media/stream/{key}");
        }

        private string CleanFileKey(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey)) return string.Empty;
            var key = fileKey.TrimStart('/', '\\');
            if (key.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring("uploads/".Length);
            }
            return key;
        }
    }
}

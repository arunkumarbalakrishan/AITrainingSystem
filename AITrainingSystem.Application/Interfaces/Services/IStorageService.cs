using System.IO;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName, string contentType);
        Task DeleteFileAsync(string fileKey);
        Task<Stream> GetFileStreamAsync(string fileKey);
        Task<string> GetSignedUrlAsync(string fileKey, double expiryMinutes = 60);
    }
}

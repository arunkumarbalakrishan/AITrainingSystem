
using AITrainingSystem.Application.Interfaces.Lessons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MediaAccessService : IMediaAccessService
{
    public string GenerateSecureVideoUrl(string videoKey)
    {
        return $"https://securecdn.com/{videoKey}";
    }

    public string GenerateSecurePdfUrl(string pdfKey)
    {
        return $"https://securecdn.com/{pdfKey}"; 
    }
}

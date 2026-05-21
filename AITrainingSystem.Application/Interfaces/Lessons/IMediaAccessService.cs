using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Lessons
{
    public interface IMediaAccessService
    {
        string GenerateSecureVideoUrl(string videoKey);

        string GenerateSecurePdfUrl(string pdfKey);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IFileStorage
    {
        /// <summary>Upload file và trả về URL public để truy cập.</summary>
        Task<string> UploadAsync(
            string fileName,
            Stream fileContent,
            string contentType,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string fileUrl,
            CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsync(
            string fileUrl,
            CancellationToken cancellationToken = default);
    }

}

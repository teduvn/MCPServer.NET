using OrderManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.FileStorage
{
    public class AzureBlobFileStorage : IFileStorage
    {
        public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadAsync(string fileName, Stream fileContent, string contentType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

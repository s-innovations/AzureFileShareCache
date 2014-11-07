using Microsoft.WindowsAzure.Storage.Blob;
using SInnovations.Azure.FileShareCache.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache.Blobs
{
    public class CloudBlobFileAccess<T> : IFileAccess<T> where T : ICloudBlob
    {
        
        public Task DownloadFileToCacheAsync(string localPath)
        {
            return Target.DownloadToFileAsync(localPath, FileMode.Create);
        }

        public async Task<string> GetKeyAsync()
        {
            if (Target.Properties.ETag == null)
                await Target.FetchAttributesAsync();

            var key = Path.GetFileNameWithoutExtension(Target.Name) + "." + Target.Properties.ETag.Trim('"');

            return key;
        }


        public T Target
        {
            get;
            set;
        }
    }
}

using Microsoft.WindowsAzure.Storage.Blob;
using SInnovations.Azure.FileShareCache.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache
{
    public static class FileShareCacheExtensionMethods
    {
        public static IFileAccess<ICloudBlob> GetFileAccess(this ICloudBlob blob, IFileCache cache)
        {
            return cache.GetFileAccess<ICloudBlob>(blob);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache.Contracts
{
    public interface IFileAccess<T>
    {
        T Target { get; set; }
        Task DownloadFileToCacheAsync(string localPath);

        Task<string> GetKeyAsync();
        Task<bool> ConsistencyCheckAsync(string localPath,string contentMd5);

    }
}

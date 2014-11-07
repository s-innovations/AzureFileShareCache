using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Storage.Blob;
using SInnovations.Azure.FileShareCache.Blobs;
using SInnovations.Azure.FileShareCache.Contracts;
using SInnovations.Azure.FileShareCache.Unity;
using SInnovations.Azure.FileShareCache.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache.Cache
{
   
    public class FileSystemCache : IFileCache
    {

        public static void RegisterDefaultAzureBlobs(IUnityContainer container,string root)
        {
            container.RegisterInstance<IFileCache>(new FileSystemCache(container, root));
            container.RegisterType<IFileAccess<CloudBlockBlob>,CloudBlobFileAccess<CloudBlockBlob>>();
            container.RegisterType<IFileAccess<CloudPageBlob>,CloudBlobFileAccess<CloudPageBlob>>();

        }
        private readonly ConcurrentDictionary<string, AwaitableCriticalSection> _downloadLocks = new ConcurrentDictionary<string, AwaitableCriticalSection>();

        private readonly IUnityContainer _container;
        private readonly string _root;

        public FileSystemCache(IUnityContainer container, string root)
        {
            this._container = container;
            this._root = root;
        }
        public IFileAccess<T> GetFileAccess<T>(T target)
        {
            var accces= this._container.Resolve<IFileAccess<T>>();
            accces.Target = target;
            return accces;
        }
        public async Task<string> GetCachedPath<T>(T target)
        {
            var access = GetFileAccess<T>(target);
            var key = await access.GetKeyAsync();
            return await this.EnsureDownloadedAndReturnPathAsync(key, access.DownloadFileToCacheAsync);

        }

        private async Task<string> EnsureDownloadedAndReturnPathAsync(string key, Func<string, Task> func)
        {
            var path = Path.Combine(_root, key); var lockPath = path+".lock";
            if(File.Exists(path) && !File.Exists(lockPath))
            {
                return path;
            }

            //Download File;
            using (await _downloadLocks.GetOrAdd(key, c => new AwaitableCriticalSection()).EnterAsync())
            {
                if (File.Exists(path) && !File.Exists(lockPath))
                {
                    return path;
                }
                
                var dir = Path.GetDirectoryName(path);
                if(!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(lockPath, DateTimeOffset.UtcNow.ToString());
                await func(path);
                File.Delete(lockPath);
                return path;
            }
        }


  
    }
}

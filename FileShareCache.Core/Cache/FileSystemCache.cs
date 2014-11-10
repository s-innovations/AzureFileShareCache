using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Storage.Blob;
using SInnovations.Azure.FileShareCache.Blobs;
using SInnovations.Azure.FileShareCache.Contracts;
using SInnovations.Azure.FileShareCache.Unity;
using SInnovations.Azure.FileShareCache.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            string path = null;
            bool consistencyFail = false;
            int i = 3;
            do
            {
                
                try
                {
                    path = await this.EnsureDownloadedAndReturnPathAsync(key, access.DownloadFileToCacheAsync, consistencyFail);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    throw;
                }

                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(path))
                    {
                        var contentMd5 = Convert.ToBase64String(md5.ComputeHash(stream));
                        consistencyFail = !await access.ConsistencyCheckAsync(path, contentMd5);                       
                    }
                }

            } while (consistencyFail && i-->0);

            if (consistencyFail)
                throw new Exception("Consistency check failed");

            return path;
        }

        private async Task<string> EnsureDownloadedAndReturnPathAsync(string key, Func<string, Task> func, bool redownload=false)
        {
            var path = Path.Combine(_root, key); var lockPath = path+".lock";
            if(!redownload && File.Exists(path) && !File.Exists(lockPath))
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
                try
                {
                    await func(path);
                }catch(Exception ex)
                {
                    if(File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                finally
                {
                    File.Delete(lockPath);
                }
                
               
                return path;
            }
        }


  
    }
}

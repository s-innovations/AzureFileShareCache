using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache.Contracts
{
    public interface IFileCache
    {
        IFileAccess<T> GetFileAccess<T>(T blob);

        Task<string> GetCachedPath<T>(T target);
     
    }
}

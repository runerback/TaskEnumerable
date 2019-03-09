using System.Collections.Generic;
using System.Threading.Tasks;

namespace Runerback.Utils
{
    public interface ITaskEnumerable<T> : IEnumerable<Task<T>>
    {
    }
}

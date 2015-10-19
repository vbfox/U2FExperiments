using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackFox.U2F.Gnubby
{
    public interface IKeyFactory
    {
        Task<ICollection<IKeyId>> FindAllAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}

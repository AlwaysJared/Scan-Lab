using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Models;

namespace Libs.Interfaces
{
    public interface IRollRepository : IDisposable
    {
        Task<SystemResponse> ProcessRoll(Guid rollId);
        Task<List<Roll>?> RollsInProgress(Roll roll);
        void Save();
    }
}
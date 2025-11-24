using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Interfaces
{
    public interface IRollRepository : IDisposable
    {
        Task<SystemResponse> ProcessRoll(Guid rollId, Guid? staffId);
        Task<List<Roll>> RollsInProgress(Scanner? scnr = null);
        Task<SystemResponse> UpdateRollStatus(Roll roll, RollStatus status, Guid? staffId);
        void Save();
    }
}
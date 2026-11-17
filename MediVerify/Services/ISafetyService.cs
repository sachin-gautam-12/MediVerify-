using System.Collections.Generic;
using System.Threading.Tasks;
using MediVerify.Models;

namespace MediVerify.Services
{
    public interface ISafetyService
    {
        Task<List<SafetyAlert>> EvaluateAsync(int prescriptionId, string reviewedBy = null);
        Task<bool> AcknowledgeAsync(int alertId, string userId);
        Task<bool> ResolveAsync(int alertId, string userId);
    }
}

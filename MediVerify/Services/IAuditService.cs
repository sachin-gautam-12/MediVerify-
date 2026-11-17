using System.Threading.Tasks;

namespace MediVerify.Services
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string entityName, int? entityId = null, string details = null);
    }
}

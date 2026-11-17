using System.Threading.Tasks;

namespace MediVerify.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, string message, string link = null);
        Task NotifyRoleAsync(string roleName, string title, string message, string link = null);
        Task MarkAsReadAsync(int notificationId);
    }
}

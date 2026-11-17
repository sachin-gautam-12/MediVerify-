using System;
using System.Threading.Tasks;

namespace MediVerify.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, string message);
        Task MarkAsReadAsync(int notificationId);
    }
}

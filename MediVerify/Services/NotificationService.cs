using System;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        public NotificationService(ApplicationDbContext context) => _context = context;

        public async Task CreateAsync(string userId, string title, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            if (notificationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(notificationId));
            }

            var notif = await _context.Notifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
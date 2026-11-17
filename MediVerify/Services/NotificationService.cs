using System;
using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.AspNetCore.Identity;

namespace MediVerify.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task CreateAsync(string userId, string title, string message, string link = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyRoleAsync(string roleName, string title, string message, string link = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            var users = await _userManager.GetUsersInRoleAsync(roleName);
            if (users.Count == 0) return;

            foreach (var user in users)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
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

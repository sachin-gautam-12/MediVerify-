using System.Threading.Tasks;
using MediVerify.Data;
using MediVerify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediVerify.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationBellViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(UserClaimsPrincipal);
            var count = string.IsNullOrEmpty(userId)
                ? 0
                : await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return View(count);
        }
    }
}

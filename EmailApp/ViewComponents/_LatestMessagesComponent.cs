using EmailApp.Context;
using EmailApp.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.ViewComponents
{
    public class _LatestMessagesComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _LatestMessagesComponent(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var latestMessages = await _context.Messages
                .Where(m => m.ReceiverId == user.Id && !m.is_deleted) // silinmemiş mesajlar
                .OrderByDescending(m => m.SendDate)
                .Take(3)
                .Include(m => m.Sender)
                .ToListAsync();

            return View(latestMessages);
        }
    }
}

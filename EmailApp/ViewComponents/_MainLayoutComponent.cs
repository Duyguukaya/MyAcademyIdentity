using EmailApp.Context;
using EmailApp.Entities;
using EmailApp.Models; // eğer IdentityUser<int> veya özel user classın varsa
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EmailApp.ViewComponents
{
    public class _MainLayoutComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MainLayoutComponent(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var notReadCount = await _context.Messages
                .CountAsync(x => !x.is_read && x.ReceiverId == user.Id);

            ViewBag.not_read_message = notReadCount;

            return View();
        }
    }
}

using EmailApp.Context;
using EmailApp.Entities;
using EmailApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.Controllers
{
    [Authorize]
    public class MessageController(AppDbContext _context,UserManager<AppUser> _userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Messages.Include(x=>x.Sender).Where(x => x.ReceiverId == user.Id).ToList();
            return View(messages);
        }

        public IActionResult MessageDetail(int id)
        {
            var message = _context.Messages.Include(x => x.Sender).Include(x => x.Receiver).FirstOrDefault(x => x.MessageId == id);

            if (message == null)
                return NotFound();


            if (!message.is_read)
            {
                message.is_read = true;
                _context.Update(message);
                _context.SaveChanges();
            }

            return View(message);
        }

        public IActionResult SendMessage()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model, string actionType)
        {
            var sender = await _userManager.FindByNameAsync(User.Identity.Name);

            // Taslak veya gönderim için alıcı gerekli
            var receiver = await _userManager.FindByEmailAsync(model.ReciverEmail);
            if (receiver == null)
            {
                ModelState.AddModelError("", "Alıcı bulunamadı.");
                return View(model);
            }

            var message = new Message
            {
                Body = model.Body,
                Subject = model.Subject,
                SenderId = sender.Id,
                ReceiverId = receiver.Id, // Taslakta da alıcı bilgisi saklanacak
                SendDate = DateTime.Now,
                IsDraft = (actionType == "draft")
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return actionType == "draft"
                ? RedirectToAction("Drafts")
                : RedirectToAction("SentMessages");
        }

        // Taslak mesajlar
        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var drafts = await _context.Messages
                .Include(m => m.Receiver) // Burayı ekledik
                .Where(m => m.SenderId == user.Id && m.IsDraft)
                .ToListAsync();

            return View(drafts);
        }


        // Taslak mesajı gönder
        [HttpPost]
        public async Task<IActionResult> SendDraft(int id, string receiverEmail)
        {
            var sender = await _userManager.FindByNameAsync(User.Identity.Name);
            var receiver = await _userManager.FindByEmailAsync(receiverEmail);

            if (receiver == null)
            {
                TempData["Error"] = "Geçerli bir alıcı bulunamadı!";
                return RedirectToAction("Drafts");
            }

            var draft = await _context.Messages
                .FirstOrDefaultAsync(x => x.MessageId == id && x.SenderId == sender.Id && x.IsDraft);

            if (draft == null)
                return NotFound();

            draft.ReceiverId = receiver.Id;
            draft.SendDate = DateTime.Now;
            draft.IsDraft = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("SentMessages");
        }



        public async Task<IActionResult> SentMessages()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Messages
                .Include(x => x.Receiver)   // alıcı bilgilerini de ekle
                .Where(x => x.SenderId == user.Id)
                .ToList();
            return View(messages);
        }

    }
}

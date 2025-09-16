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
            var messages = _context.Messages.Include(x=>x.Sender).Where(x => x.ReceiverId == user.Id && !x.is_deleted).ToList();
            return View(messages);
        }

        public IActionResult MessageDetail(int id)
        {
            var message = _context.Messages.Include(x => x.Sender).Include(x => x.Receiver).FirstOrDefault(x => x.MessageId == id);

            if (message == null)
                return NotFound();


            var user = _userManager.FindByNameAsync(User.Identity.Name).Result;

            if (message.ReceiverId == user.Id && !message.is_read)
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
                IsDraft = (actionType == "draft"),
                is_deleted = (actionType == "trash")
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return actionType switch
            {
                "draft" => RedirectToAction("Drafts"),
                "trash" => RedirectToAction("Trash"),
                _ => RedirectToAction("SentMessages"),
            };
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

        public async Task<IActionResult> Important()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var important = await _context.Messages
        .Include(x => x.Sender)
        .Include(x => x.Receiver)
        .Where(m => (m.SenderId == user.Id || m.ReceiverId == user.Id) && m.is_important)
        .ToListAsync();

            return View(important);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleImportant(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            message.is_important = !message.is_important; // tersine çevir
            _context.Update(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, important = message.is_important });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var message = await _context.Messages
                .FirstOrDefaultAsync(x => x.MessageId == id && (x.SenderId == user.Id || x.ReceiverId == user.Id));

            if (message == null)
                return NotFound();

            // Çöp kutusuna at
            message.is_deleted = true;
            _context.Update(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Trash");
        }

        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var trash = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user.Id || m.ReceiverId == user.Id) && m.is_deleted)
                .ToListAsync();

            return View(trash);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreMessage(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var message = await _context.Messages
                .FirstOrDefaultAsync(x => x.MessageId == id && (x.SenderId == user.Id || x.ReceiverId == user.Id));

            if (message == null)
                return NotFound();

            // Çöp kutusundan geri al
            message.is_deleted = false;
            _context.Update(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Message");
        }

        [HttpPost]
        public async Task<IActionResult> HardDelete(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var message = await _context.Messages
                .FirstOrDefaultAsync(x => x.MessageId == id && (x.SenderId == user.Id || x.ReceiverId == user.Id));

            if (message == null)
                return NotFound();

            // DB’den tamamen sil
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Trash");
        }





    }
}

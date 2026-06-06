using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EducationPlatform.Controllers
{
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public CertificatesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> IssueCertificate(int courseId, string firstName, string lastName)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Перевіряємо, чи вже є сертифікат
            var existingCert = await _context.Certificates
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

            if (existingCert != null)
            {
                return RedirectToAction("Verify", new { token = existingCert.UniqueToken });
            }

            // Створюємо сертифікат з ІМ'ЯМ та ПРІЗВИЩЕМ
            var cert = new Certificate
            {
                UserId = userId,
                CourseId = courseId,
                IssueDate = DateTime.UtcNow,
                UniqueToken = Guid.NewGuid().ToString("N"),
                StudentFirstName = firstName, // Записуємо з форми
                StudentLastName = lastName    // Записуємо з форми
            };

            _context.Certificates.Add(cert);

            // Дзвіночок
            var notif = new Notification
            {
                UserId = userId,
                Message = "Вітаємо! Ви успішно завершили курс та отримали сертифікат!",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();

            return RedirectToAction("Verify", new { token = cert.UniqueToken });
        }

        //Публічна сторінка перевірки сертифіката за токеном
        [Route("verify/{token}")]
        public async Task<IActionResult> Verify(string token)
        {
            if (string.IsNullOrEmpty(token)) return NotFound();

            var certificate = await _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.UniqueToken == token);

            if (certificate == null)
            {
                ViewBag.IsValid = false;
                return View();
            }

            ViewBag.IsValid = true;
            return View(certificate);
        }

        // Метод, який генерує саму картинку QR-коду
        public IActionResult GetQrCode(string token)
        {
            // Формуємо повне посилання на метод Verify
            string verificationUrl = Url.Action("Verify", "Certificates", new { token = token }, protocol: Request.Scheme);

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                return File(qrCodeImage, "image/png");
            }
        }
    }
}
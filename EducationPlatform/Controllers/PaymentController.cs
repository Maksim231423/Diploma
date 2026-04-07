using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EducationPlatform.Services;

namespace EducationPlatform.Controllers
{
    [Authorize] // Тільки авторизовані користувачі можуть купувати
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public IActionResult Checkout(int courseId, string courseTitle, decimal price)
        {
            // 1. Формуємо унікальний номер замовлення
            // Для диплома згенеруємо його з ID курсу та випадкового тексту
            string orderId = $"ORDER_{courseId}_{Guid.NewGuid().ToString().Substring(0, 8)}";

            string description = $"Оплата курсу: {courseTitle} на платформі ITskill";

            // 2. Формуємо посилання, куди LiqPay поверне користувача після оплати
            string resultUrl = Url.Action("Success", "Payment", new { orderId = orderId }, protocol: Request.Scheme);

            // 3. Генеруємо платіжне посилання
            string paymentUrl = _paymentService.GeneratePaymentUrl(orderId, price, description, resultUrl);

            // 4. Перенаправляємо користувача на LiqPay
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public IActionResult Success(string orderId)
        {
            // Сюди користувач потрапить ПІСЛЯ оплати
            // В ідеалі тут треба перевірити в базі даних, чи дійсно пройшла оплата,
            // і додати цей курс у кабінет користувача.

            ViewBag.OrderId = orderId;
            return View();
        }
    }
}
namespace EducationPlatform.Services
{
    public interface IPaymentService
    {
        // Повертає готове посилання, на яке треба перекинути користувача для оплати
        string GeneratePaymentUrl(string orderId, decimal amount, string description, string resultUrl);
    }
}

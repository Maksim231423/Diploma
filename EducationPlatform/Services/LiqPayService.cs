using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EducationPlatform.Services
{
    public class LiqPayService : IPaymentService
    {
        private readonly string _publicKey;
        private readonly string _privateKey;

        public LiqPayService(IConfiguration config)
        {
            // Тепер програма братиме твої реальні ключі sandbox_i73072839311 з файлу appsettings.json
            _publicKey = config.GetSection("LiqPay:PublicKey").Value;
            _privateKey = config.GetSection("LiqPay:PrivateKey").Value;
        }

        public string GeneratePaymentUrl(string orderId, decimal amount, string description, string resultUrl)
        {
            // 1. Формуємо об'єкт з параметрами платежу
            var requestData = new
            {
                public_key = _publicKey,
                version = "3",
                action = "pay", // дія - оплата
                amount = amount,
                currency = "UAH",
                description = description,
                order_id = orderId,
                result_url = resultUrl
            };

            // 2. Перетворюємо в JSON та кодуємо в Base64
            string jsonString = JsonSerializer.Serialize(requestData);
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

            // 3. Генеруємо підпис (Signature) за формулою LiqPay: base64(sha1(private_key + data + private_key))
            string signString = _privateKey + data + _privateKey;
            string signature;

            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(signString));
                signature = Convert.ToBase64String(hashBytes);
            }

            // 4. Безпечно кодуємо параметри для URL
            string encodedData = Uri.EscapeDataString(data);
            string encodedSignature = Uri.EscapeDataString(signature);

            return $"https://www.liqpay.ua/api/3/checkout?data={encodedData}&signature={encodedSignature}";
        }
    }
}
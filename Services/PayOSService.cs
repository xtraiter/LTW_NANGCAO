using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using CinemaManagement.Models;

namespace CinemaManagement.Services
{
    public class PayOSService
    {
        private readonly PayOS _payOS;
        private readonly ILogger<PayOSService> _logger;

        public PayOSService(IConfiguration configuration, ILogger<PayOSService> logger)
        {
            _logger = logger;

            var clientId = configuration["Environment:PAYOS_CLIENT_ID"] ?? throw new Exception("PAYOS_CLIENT_ID not found");
            var apiKey = configuration["Environment:PAYOS_API_KEY"] ?? throw new Exception("PAYOS_API_KEY not found");
            var checksumKey = configuration["Environment:PAYOS_CHECKSUM_KEY"] ?? throw new Exception("PAYOS_CHECKSUM_KEY not found");

            _payOS = new PayOS(clientId, apiKey, checksumKey);
        }

        public async Task<CreatePaymentResult> CreatePaymentLink(PaymentData paymentData)
        {
            try
            {
                _logger.LogInformation("Creating PayOS payment link for order: {OrderCode}", paymentData.orderCode);

                var response = await _payOS.createPaymentLink(paymentData);

                _logger.LogInformation("PayOS payment link created successfully. Checkout URL: {CheckoutUrl}", response.checkoutUrl);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link for order: {OrderCode}", paymentData.orderCode);
                throw new Exception($"Không thể tạo liên kết thanh toán: {ex.Message}");
            }
        }

        public async Task<PaymentLinkInformation> GetPaymentLinkInformation(int orderCode)
        {
            try
            {
                _logger.LogInformation("Getting PayOS payment information for order: {OrderCode}", orderCode);

                var response = await _payOS.getPaymentLinkInformation(orderCode);

                _logger.LogInformation("PayOS payment information retrieved. Status: {Status}", response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayOS payment information for order: {OrderCode}", orderCode);
                throw new Exception($"Không thể lấy thông tin thanh toán: {ex.Message}");
            }
        }

        public WebhookData? VerifyPaymentWebhookData(string webhookBody, string receivedSignature)
        {
            try
            {
                _logger.LogInformation("Verifying PayOS webhook data");

                // Deserialize webhook body to get webhook type
                var webhookType = JsonSerializer.Deserialize<WebhookType>(webhookBody);
                
                if (webhookType != null)
                {
                    var webhookData = _payOS.verifyPaymentWebhookData(webhookType);
                    _logger.LogInformation("PayOS webhook data verified successfully for order: {OrderCode}", webhookData.orderCode);
                    return webhookData;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayOS webhook data");
                return null;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Services;
using System.Text.Json;

namespace CinemaManagement.Controllers
{
    public class ChatController : Controller
    {
        private readonly GeminiChatService _chatService;

        public ChatController(GeminiChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Tin nhắn không được để trống" });
                }

                // Tạo session ID nếu chưa có
                var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

                // Gọi service để lấy response từ AI
                var response = await _chatService.GetChatResponseAsync(request.Message, sessionId);

                return Json(new
                {
                    success = true,
                    response = response,
                    sessionId = sessionId,
                    timestamp = DateTime.Now.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = "Xin lỗi, tôi gặp sự cố khi xử lý yêu cầu của bạn. Vui lòng thử lại sau.",
                    details = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetChatWidget()
        {
            return PartialView("_ChatWidget");
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}

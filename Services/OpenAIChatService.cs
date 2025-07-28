using CinemaManagement.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace CinemaManagement.Services
{
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly CinemaDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public GeminiChatService(CinemaDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
            
            _apiKey = _configuration["Environment:GEMINI_API_KEY"] ?? "";
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key not configured");
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string sessionId)
        {
            try
            {
                // Nếu là test command, trả về test response
                if (userMessage.ToLower().Trim() == "test")
                {
                    return "Chào bạn! Tôi là trợ lý AI của D'CINE. Tôi có thể giúp bạn tìm hiểu về phim, lịch chiếu, giá vé và các dịch vụ khác. Hãy hỏi tôi bất cứ điều gì!";
                }

                // Lấy context từ database về rạp chiếu phim
                var databaseContext = await GetDatabaseContextAsync();
                
                // Tạo system message với thông tin về rạp chiếu phim
                var systemMessage = CreateSystemMessage(databaseContext);
                
                // Tạo prompt cho Gemini (không có lịch sử chat để tránh lỗi database)
                var prompt = BuildPromptForGemini(systemMessage, new List<Models.ChatMessage>(), userMessage);

                // Gọi API Gemini
                var response = await CallGeminiAsync(prompt);

                // Tạm thời không lưu vào database để tránh lỗi
                // TODO: Sau khi tạo migration sẽ uncomment dòng này
                // await SaveChatMessageAsync(sessionId, userMessage, response);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat Service Error: {ex.Message}");
                return "Xin lỗi, tôi gặp sự cố khi xử lý yêu cầu của bạn. Vui lòng thử lại sau.";
            }
        }

        private string BuildPromptForGemini(string systemMessage, List<Models.ChatMessage> chatHistory, string userMessage)
        {
            var prompt = new StringBuilder();
            
            // Thêm system message
            prompt.AppendLine(systemMessage);
            prompt.AppendLine("\n--- LỊCH SỬ TRƯỚC ĐÓ ---");
            
            // Thêm lịch sử chat
            foreach (var chat in chatHistory.TakeLast(5)) // Chỉ lấy 5 tin nhắn gần nhất
            {
                prompt.AppendLine($"Người dùng: {chat.UserMessage}");
                prompt.AppendLine($"Trợ lý: {chat.BotResponse}");
                prompt.AppendLine();
            }
            
            // Thêm câu hỏi hiện tại
            prompt.AppendLine("--- CÂUHỎI HIỆN TẠI ---");
            prompt.AppendLine($"Người dùng: {userMessage}");
            prompt.AppendLine("Trợ lý:");
            
            return prompt.ToString();
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 1,
                        topP = 1,
                        maxOutputTokens = 2048,
                        stopSequences = new string[] { }
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                    
                    var text = apiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;
                    return text ?? "Không thể nhận được phản hồi từ AI.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {response.StatusCode} - {errorContent}");
                    return "Dịch vụ AI hiện tại không khả dụng. Vui lòng liên hệ nhân viên để được hỗ trợ.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini API Exception: {ex.Message}");
                return "Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau.";
            }
        }

        private async Task<string> GetDatabaseContextAsync()
        {
            var context = new StringBuilder();
            
            try
            {
                // Thông tin về phim
                var phims = await _context.Phims.Take(10).ToListAsync();
                context.AppendLine("=== DANH SÁCH PHIM ĐANG CHIẾU ===");
                foreach (var phim in phims)
                {
                    context.AppendLine($"- {phim.TenPhim} ({phim.TheLoai}, {phim.ThoiLuong} phút, {phim.DoTuoiPhanAnh})");
                    if (!string.IsNullOrEmpty(phim.MoTa))
                        context.AppendLine($"  Mô tả: {phim.MoTa}");
                }

                // Thông tin về phòng chiếu
                var phongChieus = await _context.PhongChieus.ToListAsync();
                context.AppendLine("\n=== THÔNG TIN PHÒNG CHIẾU ===");
                foreach (var phong in phongChieus)
                {
                    context.AppendLine($"- {phong.TenPhong}");
                }

                // Thông tin về lịch chiếu hôm nay
                var today = DateTime.Today;
                var lichChieus = await _context.LichChieus
                    .Include(l => l.Phim)
                    .Include(l => l.PhongChieu)
                    .Where(l => l.ThoiGianBatDau.Date == today)
                    .Take(15)
                    .ToListAsync();
                
                context.AppendLine("\n=== LỊCH CHIẾU HÔM NAY ===");
                foreach (var lich in lichChieus)
                {
                    context.AppendLine($"- {lich.Phim?.TenPhim} tại {lich.PhongChieu?.TenPhong}");
                    context.AppendLine($"  Giờ chiếu: {lich.ThoiGianBatDau:HH:mm} - {lich.ThoiGianKetThuc:HH:mm}");
                    context.AppendLine($"  Giá vé: {lich.Gia:N0} VNĐ");
                }

                // Thông tin về voucher đang có
                var vouchers = await _context.Vouchers
                    .Where(v => v.ThoiGianBatDau <= DateTime.Now && v.ThoiGianKetThuc >= DateTime.Now)
                    .Take(5)
                    .ToListAsync();
                
                context.AppendLine("\n=== THÔNG TIN KHUYẾN MÃI ===");
                foreach (var voucher in vouchers)
                {
                    context.AppendLine($"- {voucher.TenGiamGia}: Giảm {voucher.PhanTramGiam}%");
                    if (!string.IsNullOrEmpty(voucher.MoTa))
                        context.AppendLine($"  Mô tả: {voucher.MoTa}");
                }

                return context.ToString();
            }
            catch (Exception)
            {
                return "Thông tin cơ sở dữ liệu không khả dụng.";
            }
        }

        private string CreateSystemMessage(string databaseContext)
        {
            return $@"🎬 Bạn là AI Assistant chuyên nghiệp của rạp chiếu phim D'CINE - một trợ lý thân thiện và hiểu biết sâu về điện ảnh.

🎯 VAI TRÒ CỦA BẠN:
• Tư vấn khách hàng về phim, lịch chiếu, giá vé
• Giúp khách hàng chọn phim phù hợp với sở thích  
• Hướng dẫn đặt vé online và các dịch vụ
• Chia sẻ thông tin khuyến mãi, ưu đai đặc biệt
• Giải đáp mọi thắc mắc về rạp chiếu phim

📊 DỮ LIỆU THỰC TẾ CỦA RẠP:
{databaseContext}

⚠️ NHỮNG ĐIỀU QUAN TRỌNG PHẢI NHỚ:
• TUYỆT ĐỐI KHÔNG được tự ý thêm/sửa/xóa dữ liệu
• KHÔNG có quyền truy cập hệ thống quản lý nội bộ
• CHỈ cung cấp thông tin dựa trên dữ liệu có sẵn
• Khi không biết thông tin, hãy thành thật nói không rõ
• KHÔNG tạo ra thông tin giả hoặc dữ liệu không tồn tại

💬 CÁCH TRẢ LỜI CHUYÊN NGHIỆP:
• Sử dụng emoji phù hợp để tạo không khí thân thiện  
• Trả lời ngắn gọn, súc tích nhưng đầy đủ thông tin
• Định dạng thông tin dễ đọc (bullet points, numbers)
• Gợi ý khách hàng những lựa chọn tốt nhất
• Luôn kết thúc bằng câu hỏi để tiếp tục hỗ trợ
• Khuyến khích đặt vé online để tiện lợi nhất

🚫 TUYỆT ĐỐI KHÔNG LÀM:
• Không hiển thị dữ liệu dạng bảng HTML thô
• Không trả lời về việc thao tác cơ sở dữ liệu  
• Không đưa ra thông tin không chính xác
• Không sử dụng ngôn ngữ quá kỹ thuật

Hãy trả lời như một nhân viên tư vấn chuyên nghiệp và thân thiện! 😊";
        }

        private async Task<List<Models.ChatMessage>> GetChatHistoryAsync(string sessionId)
        {
            return await _context.ChatMessages
                .Where(c => c.SessionId == sessionId)
                .OrderBy(c => c.CreatedAt)
                .Take(10) // Giới hạn lịch sử để tránh token quá dài
                .ToListAsync();
        }

        private async Task SaveChatMessageAsync(string sessionId, string userMessage, string botResponse)
        {
            var chatMessage = new Models.ChatMessage
            {
                SessionId = sessionId,
                UserMessage = userMessage,
                BotResponse = botResponse,
                CreatedAt = DateTime.Now
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
        }
    }

    // Classes để deserialize Gemini API response
    public class GeminiResponse
    {
        public GeminiCandidate[]? candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? content { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[]? parts { get; set; }
    }

    public class GeminiPart
    {
        public string? text { get; set; }
    }
}

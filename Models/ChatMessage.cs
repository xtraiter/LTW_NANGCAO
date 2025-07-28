using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        
        public string SessionId { get; set; } = string.Empty;
        
        public string UserMessage { get; set; } = string.Empty;
        
        public string BotResponse { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string? UserId { get; set; }
    }
}

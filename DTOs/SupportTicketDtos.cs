using System.ComponentModel.DataAnnotations;
using static R3AIA.Models.Enums;

namespace R3AIA.DTOs
{
    public class CreateSupportTicketDto
    {
        [Required]
        [StringLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class ReplySupportTicketDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class SupportTicketReturnDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TargetUserFullName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public List<SupportReplyReturnDto> Replies { get; set; } = new List<SupportReplyReturnDto>();
    }

    public class SupportReplyReturnDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

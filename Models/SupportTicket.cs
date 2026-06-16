using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public UserType UserType { get; set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public SupportStatus Status { get; set; } = SupportStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<SupportReply> Replies { get; set; } = new List<SupportReply>();
    }
}

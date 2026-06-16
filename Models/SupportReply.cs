using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
    public class SupportReply
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        
        [ForeignKey("TicketId")]
        public SupportTicket Ticket { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// User or Admin
        /// </summary>
        public string SenderType { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

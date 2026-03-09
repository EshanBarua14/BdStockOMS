using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public enum NotificationChannel { Email, Sms, Both }
    public enum NotificationEventType
    {
        PriceBreach,
        RmsBreach,
        OrderFilled,
        OrderRejected,
        KycStatusChanged,
        SettlementDue,
        FundRequestApproved,
        FundRequestRejected,
        TradeAlert,
        Custom
    }

    public class NotificationPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]

        public NotificationEventType EventType { get; set; }
        public NotificationChannel Channel { get; set; }
        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public NotificationEventType EventType { get; set; }
        public NotificationChannel Channel { get; set; }

        [Required, MaxLength(200)]
        public string Recipient { get; set; } = string.Empty;   // email or phone

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class ApplicationUser : IdentityUser
	{
		[Required, MaxLength(100)]
		public string FullName { get; set; } = null!;

		public UserType UserType { get; set; }

		[Required]
		public string NationalID { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true; 

		/// <summary>
		/// حالة الحساب العامة (نشط، قيد المراجعة، محظور).
		/// </summary>
		public AccountStatus AccountStatus { get; set; } = AccountStatus.Pending;

		/// <summary>
		/// هل أكمل المستخدم رفع المستندات والبيانات أم لا.
		/// </summary>
		public bool HasCompletedProfile { get; set; } = false;

		/// <summary>
		/// يستخدمها الأدمن للموافقة على الأطباء والصيدليات قبل استقبال الطلبات.
		/// </summary>
		public bool IsVerified { get; set; } = false;

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		/// <summary>
		/// هل المستخدم مشترك في الباقة المدفوعة (Premium).
		/// </summary>
		public bool IsPremium { get; set; } = false;

		/// <summary>
		/// تاريخ انتهاء الاشتراك المدفوع.
		/// </summary>
		public DateTime? SubscriptionEndDate { get; set; }

		/// <summary>
		/// هل أكمل المستخدم إعداد خدمة سند.
		/// </summary>
		public bool HasSanadSetup { get; set; } = false;
	}
}

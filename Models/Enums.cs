namespace R3AIA.Models
{
	public class Enums
	
	{
		public enum UserType { Admin = 1, Doctor, Pharmacist, Patient, Volunteer, Companion, Premium }

		public enum AccountStatus { Active = 1, Pending, Banned, Rejected }

		public enum RequestStatus { Pending, Accepted, Completed, Cancelled }

		
		public enum DeliveryStatus { Available, Taken, Delivered }
		public enum VolunteerRequestType { Delivery, Help, Transport, FinancialDonation }

		public enum DonationStatus { Pending, Approved, Rejected }

		public enum PaymentMethod { InstaPay, Wallet }

		public enum CaseStatus { Pending, Approved, Completed, Rejected }

		public enum CaseType { Operation, Treatment, Medicine, Device }

		public enum DonorType { User, Guest }

		public enum SupportStatus { Open, InProgress, Closed }

		// === طبيب الخير ===
		public enum ConsultationType { Free, Discounted }
		public enum KhairBookingStatus { Pending, Confirmed, Cancelled, Completed }

		// === خدمة سند والاشتراك ===
		public enum SubscriptionPaymentMethod { VodafoneCash, InstaPay, Paymob }
		public enum SubscriptionRequestStatus { Pending, Approved, Rejected }
	}
}


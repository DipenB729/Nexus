using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public class Role
{
    [Key, BsonElement("_id")] public long RoleId { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class User
{
    [Key, BsonElement("_id")] public long UserId { get; set; }
    public long RoleId { get; set; }
    public long? HospitalProfileId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    [Required] public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    [Required] public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Role? Role { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }
}

public class Patient
{
    [Key, BsonElement("_id")] public long PatientId { get; set; }
    public long UserId { get; set; }
    public long? PatientCategoryId { get; set; }
    [MaxLength(30)] public string? MedicalRecordNumber { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(10)] public string? BloodGroup { get; set; }
    [MaxLength(20)] public string? EmergencyContact { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public long? MergedIntoPatientId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PatientCategory? PatientCategory { get; set; }
}

public class Doctor
{
    [Key, BsonElement("_id")] public long DoctorId { get; set; }
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Specialization { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public int ExperienceYears { get; set; }
    [MaxLength(150)] public string? Qualification { get; set; }
    [MaxLength(80)] public string? LicenseNumber { get; set; }
    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(2000)] public string? Bio { get; set; }
    [MaxLength(80)] public string? OpdDays { get; set; }
    public TimeSpan? OpdStartTime { get; set; }
    public TimeSpan? OpdEndTime { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class DoctorSchedule
{
    [Key, BsonElement("_id")] public long ScheduleId { get; set; }
    public long DoctorId { get; set; }
    [Range(1, 7)] public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int MaxPatientsPerSlot { get; set; } = 1;
    public bool OnlineBookingEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Doctor? Doctor { get; set; }
}

public class Staff
{
    [Key, BsonElement("_id")] public long StaffId { get; set; }
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [MaxLength(40)] public string? EmployeeCode { get; set; }
    [Required, MaxLength(100)] public string Department { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Designation { get; set; } = string.Empty;
    [MaxLength(50)] public string? Shift { get; set; }
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow.Date;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? DepartmentMaster { get; set; }
}

public class HospitalService
{
    [Key, BsonElement("_id")] public long ServiceId { get; set; }
    [Required, MaxLength(150)] public string ServiceName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class LabTestMaster
{
    [Key, BsonElement("_id")] public long LabTestMasterId { get; set; }
    [Required, MaxLength(150)] public string TestName { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string DepartmentName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    [Required, MaxLength(80)] public string SampleType { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ReportFormat { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum ServicePackageKind
{
    HealthPackage,
    SurgeryPackage,
    CorporatePackage,
    DiscountedBundle
}

public class ServicePackage
{
    [Key, BsonElement("_id")] public long ServicePackageId { get; set; }
    public ServicePackageKind Kind { get; set; } = ServicePackageKind.HealthPackage;
    [Required, MaxLength(150)] public string PackageName { get; set; } = string.Empty;
    [MaxLength(120)] public string? DepartmentName { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class AuditLogEntry
{
    [Key, BsonElement("_id")] public long AuditLogEntryId { get; set; }
    [Required, MaxLength(80)] public string Category { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(120)] public string? EntityName { get; set; }
    public long? EntityId { get; set; }
    [MaxLength(180)] public string? TargetDisplayName { get; set; }
    [MaxLength(500)] public string Summary { get; set; } = string.Empty;
    [MaxLength(4000)] public string? MetadataJson { get; set; }
    public long? PerformedByUserId { get; set; }
    [MaxLength(150)] public string? PerformedByName { get; set; }
    [MaxLength(80)] public string? PerformedByRole { get; set; }
    [MaxLength(150)] public string? ActorEmail { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    [MaxLength(250)] public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RolePermission
{
    [Key, BsonElement("_id")] public long RolePermissionId { get; set; }
    public long RoleId { get; set; }
    [Required, MaxLength(80)] public string ModuleKey { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ModuleName { get; set; } = string.Empty;
    [MaxLength(80)] public string? MenuKey { get; set; }
    [MaxLength(250)] public string? PageRoute { get; set; }
    public bool? CanAccessMenu { get; set; }
    public bool? CanAccessPage { get; set; }
    public bool? CanView { get; set; }
    public bool? CanAdd { get; set; }
    public bool? CanEdit { get; set; }
    public bool? CanDelete { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Role? Role { get; set; }
}

public class HospitalProfile
{
    [Key, BsonElement("_id")] public long HospitalProfileId { get; set; }
    [Required, MaxLength(150)] public string HospitalName { get; set; } = string.Empty;
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(300)] public string AddressLine1 { get; set; } = string.Empty;
    [MaxLength(150)] public string City { get; set; } = string.Empty;
    [MaxLength(150)] public string StateOrProvince { get; set; } = string.Empty;
    [MaxLength(20)] public string PostalCode { get; set; } = string.Empty;
    [MaxLength(100)] public string Country { get; set; } = string.Empty;
    [MaxLength(150)] public string ContactEmail { get; set; } = string.Empty;
    [MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(50)] public string TaxLabel { get; set; } = "VAT";
    [MaxLength(50)] public string TaxRegistrationNumber { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    [MaxLength(10)] public string CurrencyCode { get; set; } = "NPR";
    [MaxLength(20)] public string InvoicePrefix { get; set; } = "INV";
    public int InvoiceStartingNumber { get; set; } = 1001;
    [MaxLength(300)] public string InvoiceFooterNote { get; set; } = string.Empty;
    public bool MultiBranchEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Branch
{
    [Key, BsonElement("_id")] public long BranchId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    [MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(150)] public string ContactEmail { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Department
{
    [Key, BsonElement("_id")] public long DepartmentId { get; set; }
    public long BranchId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}

public class PatientCategory
{
    [Key, BsonElement("_id")] public long PatientCategoryId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public int PriorityOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Ward
{
    [Key, BsonElement("_id")] public long WardId { get; set; }
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string WardType { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string RoomType { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class Bed
{
    [Key, BsonElement("_id")] public long BedId { get; set; }
    public long WardId { get; set; }
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(50)] public string BedNumber { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Ward? Ward { get; set; }
    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class MedicineInventoryItem
{
    [Key, BsonElement("_id")] public long MedicineInventoryItemId { get; set; }
    public long? BranchId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(60)] public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}

public enum BillingChargeType
{
    Consultation,
    Lab,
    Procedure,
    Bed,
    NursingService
}

public class BillingChargeDefinition
{
    [Key, BsonElement("_id")] public long BillingChargeDefinitionId { get; set; }
    public BillingChargeType ChargeType { get; set; } = BillingChargeType.Consultation;
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    [MaxLength(30)] public string UnitLabel { get; set; } = "unit";
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum PaymentMethodType
{
    Cash,
    Card,
    Bank,
    MobileWallet,
    InsuranceClaim
}

public class BillingPaymentMethod
{
    [Key, BsonElement("_id")] public long BillingPaymentMethodId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public PaymentMethodType MethodType { get; set; } = PaymentMethodType.Cash;
    [MaxLength(100)] public string? ProviderName { get; set; }
    public bool RequiresReference { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum BillingPartnerKind
{
    InsuranceCompany,
    PanelOrganization
}

public class BillingPartner
{
    [Key, BsonElement("_id")] public long BillingPartnerId { get; set; }
    public BillingPartnerKind Kind { get; set; } = BillingPartnerKind.InsuranceCompany;
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Code { get; set; } = string.Empty;
    [MaxLength(150)] public string? ContactPerson { get; set; }
    [MaxLength(150)] public string? ContactEmail { get; set; }
    [MaxLength(30)] public string? ContactPhone { get; set; }
    public decimal CreditLimit { get; set; }
    [MaxLength(80)] public string ClaimSubmissionMode { get; set; } = "Manual";
    [MaxLength(250)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class BillingRule
{
    [Key, BsonElement("_id")] public long BillingRuleId { get; set; }
    public long BillingPartnerId { get; set; }
    [Required, MaxLength(120)] public string RuleName { get; set; } = string.Empty;
    [MaxLength(120)] public string? PolicyName { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CoPayPercentage { get; set; }
    public decimal CreditLimit { get; set; }
    public int ClaimSubmissionWindowDays { get; set; }
    public bool RequiresPreApproval { get; set; }
    [MaxLength(300)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public BillingPartner? BillingPartner { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Partial,
    Paid,
    Cancelled
}

public enum InvoicePayerType
{
    SelfPay,
    Insurance,
    Corporate
}

public enum BillingClaimStatus
{
    None,
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Settled
}

public class BillingInvoice
{
    [Key, BsonElement("_id")] public long BillingInvoiceId { get; set; }
    [Required, MaxLength(30)] public string InvoiceNumber { get; set; } = string.Empty;
    public long? PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? BranchId { get; set; }
    public InvoicePayerType PayerType { get; set; } = InvoicePayerType.SelfPay;
    public long? BillingPartnerId { get; set; }
    public long? BillingRuleId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RequestedDiscountAmount { get; set; }
    public decimal ApprovedDiscountAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public BillingClaimStatus ClaimStatus { get; set; } = BillingClaimStatus.None;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? ClaimSubmittedAt { get; set; }
    public DateTime? ClaimSettledAt { get; set; }
    [MaxLength(60)] public string? ClaimReferenceNumber { get; set; }
    [MaxLength(250)] public string? DiscountNotes { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient? Patient { get; set; }
    public Branch? Branch { get; set; }
    public Appointment? Appointment { get; set; }
    public BillingPartner? BillingPartner { get; set; }
    public BillingRule? BillingRule { get; set; }
}

public class BillingInvoiceItem
{
    [Key, BsonElement("_id")] public long BillingInvoiceItemId { get; set; }
    public long BillingInvoiceId { get; set; }
    public long? BillingChargeDefinitionId { get; set; }
    public BillingChargeType ChargeType { get; set; } = BillingChargeType.Consultation;
    [Required, MaxLength(150)] public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public BillingInvoice? BillingInvoice { get; set; }
    public BillingChargeDefinition? BillingChargeDefinition { get; set; }
}

public class BillingInvoicePayment
{
    [Key, BsonElement("_id")] public long BillingInvoicePaymentId { get; set; }
    public long BillingInvoiceId { get; set; }
    public long BillingPaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? ReferenceNumber { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BillingInvoice? BillingInvoice { get; set; }
    public BillingPaymentMethod? BillingPaymentMethod { get; set; }
}

public enum RefundStatus
{
    Requested,
    Approved,
    Processed,
    Rejected
}

public class BillingRefund
{
    [Key, BsonElement("_id")] public long BillingRefundId { get; set; }
    public long BillingInvoiceId { get; set; }
    public long? BillingPaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Requested;
    [Required, MaxLength(250)] public string Reason { get; set; } = string.Empty;
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BillingInvoice? BillingInvoice { get; set; }
    public BillingPaymentMethod? BillingPaymentMethod { get; set; }
}

public enum AppointmentStatus
{
    Pending,
    Approved,
    Rescheduled,
    Cancelled,
    Completed,
    NoShow
}

public class Appointment
{
    [Key, BsonElement("_id")] public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    [MaxLength(40)] public string? TokenNumber { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(500)] public string? AdminRemarks { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class AppointmentTokenSetting
{
    [Key, BsonElement("_id")] public long AppointmentTokenSettingId { get; set; }
    [Required, MaxLength(20)] public string Prefix { get; set; } = "OPD";
    public int StartingNumber { get; set; } = 1;
    public int NumberPadding { get; set; } = 3;
    public bool ResetDaily { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AdmissionStatus
{
    Active,
    DischargePending,
    Discharged
}

public class PatientAdmission
{
    [Key, BsonElement("_id")] public long PatientAdmissionId { get; set; }
    [Required, MaxLength(30)] public string AdmissionNumber { get; set; } = string.Empty;
    public long PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? DoctorId { get; set; }
    public long BranchId { get; set; }
    public long WardId { get; set; }
    public long BedId { get; set; }
    public AdmissionStatus Status { get; set; } = AdmissionStatus.Active;
    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDischargeDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(2000)] public string? DischargeSummary { get; set; }
    public long? DischargeApprovedByUserId { get; set; }
    public DateTime? DischargeApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient? Patient { get; set; }
    public Appointment? Appointment { get; set; }
    public Doctor? Doctor { get; set; }
    public Branch? Branch { get; set; }
    public Ward? Ward { get; set; }
    public Bed? Bed { get; set; }
}

public class AdmissionTransfer
{
    [Key, BsonElement("_id")] public long AdmissionTransferId { get; set; }
    public long PatientAdmissionId { get; set; }
    public long? FromWardId { get; set; }
    public long? FromBedId { get; set; }
    public long ToWardId { get; set; }
    public long ToBedId { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PatientAdmission? PatientAdmission { get; set; }
    public Ward? FromWard { get; set; }
    public Bed? FromBed { get; set; }
    public Ward? ToWard { get; set; }
    public Bed? ToBed { get; set; }
}

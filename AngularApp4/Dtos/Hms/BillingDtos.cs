namespace AngularApp4.Dtos.Hms;

public class BillingChargeDefinitionDto
{
    public long BillingChargeDefinitionId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBillingChargeDefinitionDto
{
    public string ChargeType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BillingPaymentMethodDto
{
    public long BillingPaymentMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MethodType { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public bool RequiresReference { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBillingPaymentMethodDto
{
    public string Name { get; set; } = string.Empty;
    public string MethodType { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public bool RequiresReference { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BillingPartnerDto
{
    public long BillingPartnerId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public decimal CreditLimit { get; set; }
    public string ClaimSubmissionMode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBillingPartnerDto
{
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public decimal CreditLimit { get; set; }
    public string ClaimSubmissionMode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BillingRuleDto
{
    public long BillingRuleId { get; set; }
    public long BillingPartnerId { get; set; }
    public string BillingPartnerName { get; set; } = string.Empty;
    public string BillingPartnerKind { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? PolicyName { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CoPayPercentage { get; set; }
    public decimal CreditLimit { get; set; }
    public int ClaimSubmissionWindowDays { get; set; }
    public bool RequiresPreApproval { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBillingRuleDto
{
    public long BillingPartnerId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? PolicyName { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CoPayPercentage { get; set; }
    public decimal CreditLimit { get; set; }
    public int ClaimSubmissionWindowDays { get; set; }
    public bool RequiresPreApproval { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BillingInvoiceItemDto
{
    public long BillingInvoiceItemId { get; set; }
    public long? BillingChargeDefinitionId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class SaveBillingInvoiceItemDto
{
    public long? BillingInvoiceItemId { get; set; }
    public long? BillingChargeDefinitionId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
}

public class BillingPaymentDto
{
    public long BillingInvoicePaymentId { get; set; }
    public long BillingPaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string PaymentMethodType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class RecordBillingPaymentDto
{
    public long BillingPaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class BillingRefundDto
{
    public long BillingRefundId { get; set; }
    public long? BillingPaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class ProcessBillingRefundDto
{
    public long? BillingPaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ApproveBillingDiscountDto
{
    public decimal RequestedDiscountAmount { get; set; }
    public decimal ApprovedDiscountAmount { get; set; }
    public string? DiscountNotes { get; set; }
}

public class BillingInvoiceDto
{
    public long BillingInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public long? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public long? AppointmentId { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string PayerType { get; set; } = string.Empty;
    public long? BillingPartnerId { get; set; }
    public string? BillingPartnerName { get; set; }
    public string? BillingPartnerKind { get; set; }
    public long? BillingRuleId { get; set; }
    public string? BillingRuleName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RequestedDiscountAmount { get; set; }
    public decimal ApprovedDiscountAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClaimStatus { get; set; } = string.Empty;
    public string? ClaimReferenceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? ClaimSubmittedAt { get; set; }
    public DateTime? ClaimSettledAt { get; set; }
    public string? DiscountNotes { get; set; }
    public string? Notes { get; set; }
    public int ItemCount { get; set; }
    public IEnumerable<BillingInvoiceItemDto> Items { get; set; } = Array.Empty<BillingInvoiceItemDto>();
    public IEnumerable<BillingPaymentDto> Payments { get; set; } = Array.Empty<BillingPaymentDto>();
    public IEnumerable<BillingRefundDto> Refunds { get; set; } = Array.Empty<BillingRefundDto>();
}

public class SaveBillingInvoiceDto
{
    public long? PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? BranchId { get; set; }
    public string PayerType { get; set; } = string.Empty;
    public long? BillingPartnerId { get; set; }
    public long? BillingRuleId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string ClaimStatus { get; set; } = string.Empty;
    public string? ClaimReferenceNumber { get; set; }
    public decimal RequestedDiscountAmount { get; set; }
    public string? DiscountNotes { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<SaveBillingInvoiceItemDto> Items { get; set; } = Array.Empty<SaveBillingInvoiceItemDto>();
}

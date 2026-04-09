export type BillingChargeType = 'Consultation' | 'Lab' | 'Procedure' | 'Bed' | 'NursingService';
export type PaymentMethodType = 'Cash' | 'Card' | 'Bank' | 'MobileWallet' | 'InsuranceClaim';
export type BillingPartnerKind = 'InsuranceCompany' | 'PanelOrganization';
export type BillingPayerType = 'SelfPay' | 'Insurance' | 'Corporate';
export type BillingClaimStatus = 'None' | 'Draft' | 'Submitted' | 'UnderReview' | 'Approved' | 'Rejected' | 'Settled';
export type InvoiceStatus = 'Pending' | 'Partial' | 'Paid' | 'Cancelled';
export type RefundStatus = 'Requested' | 'Approved' | 'Processed' | 'Rejected';

export interface BillingChargeDefinition {
  billingChargeDefinitionId: number;
  chargeType: BillingChargeType;
  name: string;
  code: string;
  description?: string | null;
  unitLabel: string;
  defaultAmount: number;
  isActive: boolean;
}

export interface SaveBillingChargeDefinitionPayload {
  chargeType: BillingChargeType;
  name: string;
  code: string;
  description?: string | null;
  unitLabel: string;
  defaultAmount: number;
  isActive: boolean;
}

export interface BillingPaymentMethod {
  billingPaymentMethodId: number;
  name: string;
  methodType: PaymentMethodType;
  providerName?: string | null;
  requiresReference: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface SaveBillingPaymentMethodPayload {
  name: string;
  methodType: PaymentMethodType;
  providerName?: string | null;
  requiresReference: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface BillingPartner {
  billingPartnerId: number;
  kind: BillingPartnerKind;
  name: string;
  code: string;
  contactPerson?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  creditLimit: number;
  claimSubmissionMode: string;
  notes?: string | null;
  isActive: boolean;
}

export interface SaveBillingPartnerPayload {
  kind: BillingPartnerKind;
  name: string;
  code: string;
  contactPerson?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  creditLimit: number;
  claimSubmissionMode: string;
  notes?: string | null;
  isActive: boolean;
}

export interface BillingRule {
  billingRuleId: number;
  billingPartnerId: number;
  billingPartnerName: string;
  billingPartnerKind: BillingPartnerKind;
  ruleName: string;
  policyName?: string | null;
  discountPercentage: number;
  coPayPercentage: number;
  creditLimit: number;
  claimSubmissionWindowDays: number;
  requiresPreApproval: boolean;
  notes?: string | null;
  isActive: boolean;
}

export interface SaveBillingRulePayload {
  billingPartnerId: number;
  ruleName: string;
  policyName?: string | null;
  discountPercentage: number;
  coPayPercentage: number;
  creditLimit: number;
  claimSubmissionWindowDays: number;
  requiresPreApproval: boolean;
  notes?: string | null;
  isActive: boolean;
}

export interface BillingInvoiceItem {
  billingInvoiceItemId: number;
  billingChargeDefinitionId?: number | null;
  chargeType: BillingChargeType;
  description: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  totalAmount: number;
  notes?: string | null;
}

export interface SaveBillingInvoiceItemPayload {
  billingInvoiceItemId?: number | null;
  billingChargeDefinitionId?: number | null;
  chargeType: BillingChargeType;
  description: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  notes?: string | null;
}

export interface BillingPayment {
  billingInvoicePaymentId: number;
  billingPaymentMethodId: number;
  paymentMethodName: string;
  paymentMethodType: PaymentMethodType;
  amount: number;
  paymentDate: string;
  referenceNumber?: string | null;
  notes?: string | null;
}

export interface RecordBillingPaymentPayload {
  billingPaymentMethodId: number;
  amount: number;
  paymentDate: string;
  referenceNumber?: string | null;
  notes?: string | null;
}

export interface BillingRefund {
  billingRefundId: number;
  billingPaymentMethodId?: number | null;
  paymentMethodName?: string | null;
  amount: number;
  status: RefundStatus;
  reason: string;
  notes?: string | null;
  requestedAt: string;
  processedAt?: string | null;
}

export interface ProcessBillingRefundPayload {
  billingPaymentMethodId?: number | null;
  amount: number;
  status: RefundStatus;
  reason: string;
  notes?: string | null;
}

export interface ApproveBillingDiscountPayload {
  requestedDiscountAmount: number;
  approvedDiscountAmount: number;
  discountNotes?: string | null;
}

export interface BillingInvoice {
  billingInvoiceId: number;
  invoiceNumber: string;
  patientId?: number | null;
  patientName: string;
  medicalRecordNumber: string;
  appointmentId?: number | null;
  branchId?: number | null;
  branchName: string;
  payerType: BillingPayerType;
  billingPartnerId?: number | null;
  billingPartnerName?: string | null;
  billingPartnerKind?: BillingPartnerKind | null;
  billingRuleId?: number | null;
  billingRuleName?: string | null;
  totalAmount: number;
  amountPaid: number;
  requestedDiscountAmount: number;
  approvedDiscountAmount: number;
  refundedAmount: number;
  dueAmount: number;
  status: InvoiceStatus;
  claimStatus: BillingClaimStatus;
  claimReferenceNumber?: string | null;
  invoiceDate: string;
  dueDate?: string | null;
  lastPaymentDate?: string | null;
  claimSubmittedAt?: string | null;
  claimSettledAt?: string | null;
  discountNotes?: string | null;
  notes?: string | null;
  itemCount: number;
  items: BillingInvoiceItem[];
  payments: BillingPayment[];
  refunds: BillingRefund[];
}

export interface SaveBillingInvoicePayload {
  patientId?: number | null;
  appointmentId?: number | null;
  branchId?: number | null;
  payerType: BillingPayerType;
  billingPartnerId?: number | null;
  billingRuleId?: number | null;
  invoiceDate: string;
  dueDate?: string | null;
  claimStatus: BillingClaimStatus;
  claimReferenceNumber?: string | null;
  requestedDiscountAmount: number;
  discountNotes?: string | null;
  notes?: string | null;
  items: SaveBillingInvoiceItemPayload[];
}

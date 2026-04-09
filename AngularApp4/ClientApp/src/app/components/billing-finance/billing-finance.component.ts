import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, filter, forkJoin } from 'rxjs';
import { BranchSettings } from '../../core/models/hms/admin-ops.model';
import { AppointmentAdminRecord, PatientRecord } from '../../core/models/hms/phase3-control.model';
import {
  ApproveBillingDiscountPayload,
  BillingChargeDefinition,
  BillingChargeType,
  BillingClaimStatus,
  BillingInvoice,
  BillingPartner,
  BillingPartnerKind,
  BillingPaymentMethod,
  BillingPayerType,
  BillingRule,
  PaymentMethodType,
  ProcessBillingRefundPayload,
  RecordBillingPaymentPayload,
  RefundStatus,
  SaveBillingChargeDefinitionPayload,
  SaveBillingInvoiceItemPayload,
  SaveBillingInvoicePayload,
  SaveBillingPartnerPayload,
  SaveBillingPaymentMethodPayload,
  SaveBillingRulePayload
} from '../../core/models/hms/phase4-billing.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';
import { Phase3ControlService } from '../../core/services/hms/phase3-control.service';
import { Phase4BillingService } from '../../core/services/hms/phase4-billing.service';

type FinanceSection = 'charges' | 'bills' | 'paymentMethods' | 'insurance' | 'panels' | 'rules';

interface SectionOption {
  key: FinanceSection;
  label: string;
  icon: string;
  description: string;
}

interface InvoiceItemFormState {
  billingChargeDefinitionId: number | null;
  chargeType: BillingChargeType;
  description: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  notes: string;
}

interface InvoiceFormState extends Omit<SaveBillingInvoicePayload, 'items'> {
  items: InvoiceItemFormState[];
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'bills', label: 'Bills', icon: 'receipt_long', description: 'Invoices, dues, discounts, refunds, and claim follow-up.' },
  { key: 'charges', label: 'Charges', icon: 'sell', description: 'Consultation, lab, procedure, bed, and nursing charge setup.' },
  { key: 'paymentMethods', label: 'Payment Methods', icon: 'payments', description: 'Cash, card, bank, wallet, and claim settlement methods.' },
  { key: 'insurance', label: 'Insurance', icon: 'shield', description: 'Insurance company list and credit controls.' },
  { key: 'panels', label: 'Corporate Panels', icon: 'apartment', description: 'Panel organizations and employer billing partners.' },
  { key: 'rules', label: 'Billing Rules', icon: 'rule', description: 'Coverage rules, co-pay, claims, and credit limits.' }
];

const EMPTY_CHARGE_FORM: SaveBillingChargeDefinitionPayload = {
  chargeType: 'Consultation',
  name: '',
  code: '',
  description: '',
  unitLabel: 'unit',
  defaultAmount: 0,
  isActive: true
};

const EMPTY_PAYMENT_METHOD_FORM: SaveBillingPaymentMethodPayload = {
  name: '',
  methodType: 'Cash',
  providerName: '',
  requiresReference: false,
  sortOrder: 1,
  isActive: true
};

const EMPTY_PARTNER_FORM: SaveBillingPartnerPayload = {
  kind: 'InsuranceCompany',
  name: '',
  code: '',
  contactPerson: '',
  contactEmail: '',
  contactPhone: '',
  creditLimit: 0,
  claimSubmissionMode: 'Manual',
  notes: '',
  isActive: true
};

const EMPTY_RULE_FORM: SaveBillingRulePayload = {
  billingPartnerId: 0,
  ruleName: '',
  policyName: '',
  discountPercentage: 0,
  coPayPercentage: 0,
  creditLimit: 0,
  claimSubmissionWindowDays: 0,
  requiresPreApproval: false,
  notes: '',
  isActive: true
};

const EMPTY_INVOICE_ITEM: InvoiceItemFormState = {
  billingChargeDefinitionId: null,
  chargeType: 'Consultation',
  description: '',
  quantity: 1,
  unitPrice: 0,
  discountAmount: 0,
  notes: ''
};

const EMPTY_INVOICE_FORM: InvoiceFormState = {
  patientId: null,
  appointmentId: null,
  branchId: null,
  payerType: 'SelfPay',
  billingPartnerId: null,
  billingRuleId: null,
  invoiceDate: new Date().toISOString().slice(0, 10),
  dueDate: '',
  claimStatus: 'None',
  claimReferenceNumber: '',
  requestedDiscountAmount: 0,
  discountNotes: '',
  notes: '',
  items: [{ ...EMPTY_INVOICE_ITEM }]
};

const EMPTY_DISCOUNT_FORM: ApproveBillingDiscountPayload = {
  requestedDiscountAmount: 0,
  approvedDiscountAmount: 0,
  discountNotes: ''
};

const EMPTY_PAYMENT_FORM: RecordBillingPaymentPayload = {
  billingPaymentMethodId: 0,
  amount: 0,
  paymentDate: new Date().toISOString().slice(0, 10),
  referenceNumber: '',
  notes: ''
};

const EMPTY_REFUND_FORM: ProcessBillingRefundPayload = {
  billingPaymentMethodId: null,
  amount: 0,
  status: 'Processed',
  reason: '',
  notes: ''
};

@Component({
  selector: 'app-billing-finance',
  templateUrl: './billing-finance.component.html',
  styleUrls: ['./billing-finance.component.scss']
})
export class BillingFinanceComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;
  readonly chargeTypeOptions: BillingChargeType[] = ['Consultation', 'Lab', 'Procedure', 'Bed', 'NursingService'];
  readonly payerTypeOptions: BillingPayerType[] = ['SelfPay', 'Insurance', 'Corporate'];
  readonly claimStatusOptions: BillingClaimStatus[] = ['None', 'Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected', 'Settled'];
  readonly paymentMethodTypeOptions: PaymentMethodType[] = ['Cash', 'Card', 'Bank', 'MobileWallet', 'InsuranceClaim'];
  readonly refundStatusOptions: RefundStatus[] = ['Requested', 'Approved', 'Processed', 'Rejected'];

  activeSection: FinanceSection = 'bills';
  searchTerm = '';

  charges: BillingChargeDefinition[] = [];
  invoices: BillingInvoice[] = [];
  paymentMethods: BillingPaymentMethod[] = [];
  partners: BillingPartner[] = [];
  rules: BillingRule[] = [];
  patients: PatientRecord[] = [];
  appointments: AppointmentAdminRecord[] = [];
  branches: BranchSettings[] = [];

  selectedChargeId: number | null = null;
  selectedInvoiceId: number | null = null;
  selectedPaymentMethodId: number | null = null;
  selectedPartnerId: number | null = null;
  selectedRuleId: number | null = null;

  chargeForm: SaveBillingChargeDefinitionPayload = { ...EMPTY_CHARGE_FORM };
  paymentMethodForm: SaveBillingPaymentMethodPayload = { ...EMPTY_PAYMENT_METHOD_FORM };
  partnerForm: SaveBillingPartnerPayload = { ...EMPTY_PARTNER_FORM };
  ruleForm: SaveBillingRulePayload = { ...EMPTY_RULE_FORM };
  invoiceForm: InvoiceFormState = { ...EMPTY_INVOICE_FORM, items: [{ ...EMPTY_INVOICE_ITEM }] };
  discountForm: ApproveBillingDiscountPayload = { ...EMPTY_DISCOUNT_FORM };
  paymentForm: RecordBillingPaymentPayload = { ...EMPTY_PAYMENT_FORM };
  refundForm: ProcessBillingRefundPayload = { ...EMPTY_REFUND_FORM };

  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly adminOps: AdminOpsService,
    private readonly phase3: Phase3ControlService,
    private readonly phase4: Phase4BillingService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get currentSection(): SectionOption {
    return this.sections.find((section) => section.key === this.activeSection) ?? this.sections[0];
  }

  get selectedInvoice(): BillingInvoice | null {
    return this.selectedInvoiceId ? this.invoices.find((invoice) => invoice.billingInvoiceId === this.selectedInvoiceId) ?? null : null;
  }

  get insurancePartners(): BillingPartner[] {
    return this.partners.filter((partner) => partner.kind === 'InsuranceCompany');
  }

  get panelPartners(): BillingPartner[] {
    return this.partners.filter((partner) => partner.kind === 'PanelOrganization');
  }

  get currentPartners(): BillingPartner[] {
    return this.activeSection === 'insurance' ? this.insurancePartners : this.panelPartners;
  }

  get availableInvoicePartners(): BillingPartner[] {
    return this.invoiceForm.payerType === 'Insurance' ? this.insurancePartners : this.invoiceForm.payerType === 'Corporate' ? this.panelPartners : [];
  }

  get availableInvoiceRules(): BillingRule[] {
    return this.rules.filter((rule) => !this.invoiceForm.billingPartnerId || rule.billingPartnerId === this.invoiceForm.billingPartnerId);
  }

  get availableAppointments(): AppointmentAdminRecord[] {
    return this.appointments.filter((appointment) => !this.invoiceForm.patientId || appointment.patientId === this.invoiceForm.patientId);
  }

  get filteredCharges(): BillingChargeDefinition[] {
    return this.charges.filter((item) => this.matchesSearch(item.name, item.code, item.chargeType, item.description));
  }

  get filteredInvoices(): BillingInvoice[] {
    return this.invoices.filter((item) => this.matchesSearch(item.invoiceNumber, item.patientName, item.billingPartnerName, item.status, item.claimStatus));
  }

  get filteredPaymentMethods(): BillingPaymentMethod[] {
    return this.paymentMethods.filter((item) => this.matchesSearch(item.name, item.methodType, item.providerName));
  }

  get filteredPartners(): BillingPartner[] {
    return this.currentPartners.filter((item) => this.matchesSearch(item.name, item.code, item.contactPerson, item.contactEmail, item.claimSubmissionMode));
  }

  get filteredRules(): BillingRule[] {
    return this.rules.filter((item) => this.matchesSearch(item.ruleName, item.policyName, item.billingPartnerName, item.billingPartnerKind));
  }

  get activeChargeCount(): number {
    return this.charges.filter((item) => item.isActive).length;
  }

  get activePaymentMethodCount(): number {
    return this.paymentMethods.filter((item) => item.isActive).length;
  }

  get referencePaymentMethodCount(): number {
    return this.paymentMethods.filter((item) => item.requiresReference).length;
  }

  get activeInsurancePartnerCount(): number {
    return this.insurancePartners.filter((item) => item.isActive).length;
  }

  get activePanelPartnerCount(): number {
    return this.panelPartners.filter((item) => item.isActive).length;
  }

  get activeRuleCount(): number {
    return this.rules.filter((item) => item.isActive).length;
  }

  get insuranceRuleCount(): number {
    return this.rules.filter((item) => item.billingPartnerKind === 'InsuranceCompany').length;
  }

  get panelRuleCount(): number {
    return this.rules.filter((item) => item.billingPartnerKind === 'PanelOrganization').length;
  }

  get preApprovalRuleCount(): number {
    return this.rules.filter((item) => item.requiresPreApproval).length;
  }

  get totalDueAmount(): number {
    return this.invoices.reduce((sum, invoice) => sum + invoice.dueAmount, 0);
  }

  get dueInvoiceCount(): number {
    return this.invoices.filter((invoice) => invoice.dueAmount > 0).length;
  }

  get overdueInvoiceCount(): number {
    const today = new Date().toISOString().slice(0, 10);
    return this.invoices.filter((invoice) => invoice.dueAmount > 0 && !!invoice.dueDate && invoice.dueDate.slice(0, 10) < today).length;
  }

  get pendingClaimCount(): number {
    return this.invoices.filter((invoice) => !['None', 'Settled', 'Rejected'].includes(invoice.claimStatus)).length;
  }

  get createActionLabel(): string {
    switch (this.activeSection) {
      case 'charges':
        return 'New charge';
      case 'paymentMethods':
        return 'New payment method';
      case 'insurance':
        return 'New insurance company';
      case 'panels':
        return 'New panel organization';
      case 'rules':
        return 'New billing rule';
      default:
        return 'New bill';
    }
  }

  get partnerSectionLabel(): string {
    return this.activeSection === 'insurance' ? 'Insurance company' : 'Panel organization';
  }

  get selectedPaymentMethodRequiresReference(): boolean {
    const paymentMethod = this.paymentMethods.find((item) => item.billingPaymentMethodId === this.paymentForm.billingPaymentMethodId);
    return paymentMethod?.requiresReference ?? false;
  }

  setSection(section: FinanceSection): void {
    this.router.navigate(['/admin/billing', section]);
  }

  refresh(): void {
    this.loadData();
  }

  resetActiveForm(): void {
    this.errorMessage = '';
    this.successMessage = '';

    switch (this.activeSection) {
      case 'charges':
        this.selectedChargeId = null;
        this.chargeForm = { ...EMPTY_CHARGE_FORM };
        break;
      case 'bills':
        this.selectedInvoiceId = null;
        this.invoiceForm = { ...EMPTY_INVOICE_FORM, items: [{ ...EMPTY_INVOICE_ITEM }] };
        this.discountForm = { ...EMPTY_DISCOUNT_FORM };
        this.paymentForm = { ...EMPTY_PAYMENT_FORM };
        this.refundForm = { ...EMPTY_REFUND_FORM };
        break;
      case 'paymentMethods':
        this.selectedPaymentMethodId = null;
        this.paymentMethodForm = { ...EMPTY_PAYMENT_METHOD_FORM };
        break;
      case 'insurance':
      case 'panels':
        this.selectedPartnerId = null;
        this.partnerForm = { ...EMPTY_PARTNER_FORM, kind: this.activeSection === 'insurance' ? 'InsuranceCompany' : 'PanelOrganization' };
        break;
      case 'rules':
        this.selectedRuleId = null;
        this.ruleForm = { ...EMPTY_RULE_FORM };
        break;
    }
  }

  selectCharge(item: BillingChargeDefinition): void {
    this.selectedChargeId = item.billingChargeDefinitionId;
    this.chargeForm = { ...item };
  }

  selectPaymentMethod(item: BillingPaymentMethod): void {
    this.selectedPaymentMethodId = item.billingPaymentMethodId;
    this.paymentMethodForm = { ...item };
  }

  selectPartner(item: BillingPartner): void {
    this.selectedPartnerId = item.billingPartnerId;
    this.partnerForm = { ...item };
  }

  selectRule(item: BillingRule): void {
    this.selectedRuleId = item.billingRuleId;
    this.ruleForm = {
      billingPartnerId: item.billingPartnerId,
      ruleName: item.ruleName,
      policyName: item.policyName ?? '',
      discountPercentage: item.discountPercentage,
      coPayPercentage: item.coPayPercentage,
      creditLimit: item.creditLimit,
      claimSubmissionWindowDays: item.claimSubmissionWindowDays,
      requiresPreApproval: item.requiresPreApproval,
      notes: item.notes ?? '',
      isActive: item.isActive
    };
  }

  selectInvoice(item: BillingInvoice): void {
    this.selectedInvoiceId = item.billingInvoiceId;
    this.invoiceForm = {
      patientId: item.patientId ?? null,
      appointmentId: item.appointmentId ?? null,
      branchId: item.branchId ?? null,
      payerType: item.payerType,
      billingPartnerId: item.billingPartnerId ?? null,
      billingRuleId: item.billingRuleId ?? null,
      invoiceDate: item.invoiceDate.slice(0, 10),
      dueDate: item.dueDate ? item.dueDate.slice(0, 10) : '',
      claimStatus: item.claimStatus,
      claimReferenceNumber: item.claimReferenceNumber ?? '',
      requestedDiscountAmount: item.requestedDiscountAmount,
      discountNotes: item.discountNotes ?? '',
      notes: item.notes ?? '',
      items: item.items.map((line) => ({
        billingChargeDefinitionId: line.billingChargeDefinitionId ?? null,
        chargeType: line.chargeType,
        description: line.description,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        discountAmount: line.discountAmount,
        notes: line.notes ?? ''
      }))
    };
    this.discountForm = {
      requestedDiscountAmount: item.requestedDiscountAmount,
      approvedDiscountAmount: item.approvedDiscountAmount,
      discountNotes: item.discountNotes ?? ''
    };
    this.paymentForm = { ...EMPTY_PAYMENT_FORM };
    this.refundForm = { ...EMPTY_REFUND_FORM };
  }

  addInvoiceItem(): void {
    this.invoiceForm.items = [...this.invoiceForm.items, { ...EMPTY_INVOICE_ITEM }];
  }

  removeInvoiceItem(index: number): void {
    this.invoiceForm.items = this.invoiceForm.items.filter((_, currentIndex) => currentIndex !== index);
    if (this.invoiceForm.items.length === 0) {
      this.addInvoiceItem();
    }
  }

  onInvoiceItemChargeChange(item: InvoiceItemFormState): void {
    const definition = this.charges.find((charge) => charge.billingChargeDefinitionId === item.billingChargeDefinitionId);
    if (!definition) {
      return;
    }

    item.chargeType = definition.chargeType;
    if (!item.description) {
      item.description = definition.name;
    }
    if (!item.unitPrice) {
      item.unitPrice = definition.defaultAmount;
    }
  }

  onInvoicePayerTypeChange(): void {
    if (this.invoiceForm.payerType === 'SelfPay') {
      this.invoiceForm.billingPartnerId = null;
      this.invoiceForm.billingRuleId = null;
      this.invoiceForm.claimStatus = 'None';
      this.invoiceForm.claimReferenceNumber = '';
      return;
    }

    if (this.invoiceForm.claimStatus === 'None') {
      this.invoiceForm.claimStatus = 'Draft';
    }
    this.invoiceForm.billingPartnerId = null;
    this.invoiceForm.billingRuleId = null;
  }

  onInvoicePartnerChange(): void {
    if (!this.availableInvoiceRules.some((rule) => rule.billingRuleId === this.invoiceForm.billingRuleId)) {
      this.invoiceForm.billingRuleId = null;
    }
  }

  onInvoiceRuleChange(): void {
    const rule = this.rules.find((item) => item.billingRuleId === this.invoiceForm.billingRuleId);
    if (!rule) {
      return;
    }

    this.invoiceForm.billingPartnerId = rule.billingPartnerId;
    if (!this.invoiceForm.dueDate && this.invoiceForm.invoiceDate && rule.claimSubmissionWindowDays > 0) {
      this.invoiceForm.dueDate = this.addDaysToDate(this.invoiceForm.invoiceDate, rule.claimSubmissionWindowDays);
    }
  }

  saveCharge(): void {
    if (!this.chargeForm.name.trim() || !this.chargeForm.code.trim()) {
      this.errorMessage = 'Charge name and code are required.';
      return;
    }

    this.runSave(
      this.selectedChargeId
        ? this.phase4.updateChargeDefinition(this.selectedChargeId, { ...this.chargeForm, name: this.chargeForm.name.trim(), code: this.chargeForm.code.trim() })
        : this.phase4.createChargeDefinition({ ...this.chargeForm, name: this.chargeForm.name.trim(), code: this.chargeForm.code.trim() }),
      (item) => {
        this.charges = this.sortCharges(this.replaceOrAppend(this.charges, item, 'billingChargeDefinitionId'));
        this.selectCharge(item);
      },
      this.selectedChargeId ? 'Charge definition updated.' : 'Charge definition created.'
    );
  }

  savePaymentMethod(): void {
    if (!this.paymentMethodForm.name.trim()) {
      this.errorMessage = 'Payment method name is required.';
      return;
    }

    this.runSave(
      this.selectedPaymentMethodId
        ? this.phase4.updatePaymentMethod(this.selectedPaymentMethodId, { ...this.paymentMethodForm, name: this.paymentMethodForm.name.trim() })
        : this.phase4.createPaymentMethod({ ...this.paymentMethodForm, name: this.paymentMethodForm.name.trim() }),
      (item) => {
        this.paymentMethods = this.sortPaymentMethods(this.replaceOrAppend(this.paymentMethods, item, 'billingPaymentMethodId'));
        this.selectPaymentMethod(item);
      },
      this.selectedPaymentMethodId ? 'Payment method updated.' : 'Payment method created.'
    );
  }

  savePartner(): void {
    if (!this.partnerForm.name.trim() || !this.partnerForm.code.trim()) {
      this.errorMessage = 'Partner name and code are required.';
      return;
    }

    const payload: SaveBillingPartnerPayload = {
      ...this.partnerForm,
      kind: this.activeSection === 'insurance' ? 'InsuranceCompany' : 'PanelOrganization',
      name: this.partnerForm.name.trim(),
      code: this.partnerForm.code.trim()
    };

    this.runSave(
      this.selectedPartnerId ? this.phase4.updatePartner(this.selectedPartnerId, payload) : this.phase4.createPartner(payload),
      (item) => {
        this.partners = this.sortPartners(this.replaceOrAppend(this.partners, item, 'billingPartnerId'));
        this.selectPartner(item);
      },
      this.selectedPartnerId ? 'Billing partner updated.' : 'Billing partner created.'
    );
  }

  saveRule(): void {
    if (!this.ruleForm.billingPartnerId || !this.ruleForm.ruleName.trim()) {
      this.errorMessage = 'Billing partner and rule name are required.';
      return;
    }

    this.runSave(
      this.selectedRuleId
        ? this.phase4.updateBillingRule(this.selectedRuleId, { ...this.ruleForm, ruleName: this.ruleForm.ruleName.trim() })
        : this.phase4.createBillingRule({ ...this.ruleForm, ruleName: this.ruleForm.ruleName.trim() }),
      (item) => {
        this.rules = this.sortRules(this.replaceOrAppend(this.rules, item, 'billingRuleId'));
        this.selectRule(item);
      },
      this.selectedRuleId ? 'Billing rule updated.' : 'Billing rule created.'
    );
  }

  saveInvoice(): void {
    if (!this.invoiceForm.patientId || !this.invoiceForm.branchId) {
      this.errorMessage = 'Patient and branch are required.';
      return;
    }

    if (this.invoiceForm.payerType !== 'SelfPay' && !this.invoiceForm.billingPartnerId) {
      this.errorMessage = 'A billing partner is required for insurance and corporate bills.';
      return;
    }

    const validItems = this.invoiceForm.items.filter((item) => item.description.trim());
    if (validItems.length === 0) {
      this.errorMessage = 'At least one bill item is required.';
      return;
    }

    const payload: SaveBillingInvoicePayload = {
      ...this.invoiceForm,
      items: validItems.map((item): SaveBillingInvoiceItemPayload => ({
        billingChargeDefinitionId: item.billingChargeDefinitionId,
        chargeType: item.chargeType,
        description: item.description.trim(),
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discountAmount: item.discountAmount,
        notes: item.notes?.trim() || null
      })),
      dueDate: this.invoiceForm.dueDate || null,
      claimReferenceNumber: this.invoiceForm.claimReferenceNumber?.trim() || null,
      discountNotes: this.invoiceForm.discountNotes?.trim() || null,
      notes: this.invoiceForm.notes?.trim() || null
    };

    this.runSave(
      this.selectedInvoiceId ? this.phase4.updateInvoice(this.selectedInvoiceId, payload) : this.phase4.createInvoice(payload),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.selectInvoice(item);
      },
      this.selectedInvoiceId ? 'Bill updated.' : 'Bill created.'
    );
  }

  approveDiscount(): void {
    if (!this.selectedInvoiceId) {
      return;
    }

    this.runSave(
      this.phase4.approveDiscount(this.selectedInvoiceId, this.discountForm),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.selectInvoice(item);
      },
      'Discount approval saved.'
    );
  }

  recordPayment(): void {
    if (!this.selectedInvoiceId || !this.paymentForm.billingPaymentMethodId || this.paymentForm.amount <= 0) {
      this.errorMessage = 'Payment method and amount are required.';
      return;
    }

    this.runSave(
      this.phase4.recordPayment(this.selectedInvoiceId, this.paymentForm),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.selectInvoice(item);
      },
      'Payment recorded.'
    );
  }

  processRefund(): void {
    if (!this.selectedInvoiceId || this.refundForm.amount <= 0 || !this.refundForm.reason.trim()) {
      this.errorMessage = 'Refund amount and reason are required.';
      return;
    }

    this.runSave(
      this.phase4.processRefund(this.selectedInvoiceId, { ...this.refundForm, reason: this.refundForm.reason.trim() }),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.selectInvoice(item);
      },
      'Refund recorded.'
    );
  }

  trackById(_: number, item: { billingChargeDefinitionId?: number | null; billingInvoiceId?: number; billingPaymentMethodId?: number; billingPartnerId?: number; billingRuleId?: number }): number | null | undefined {
    return item.billingChargeDefinitionId ?? item.billingInvoiceId ?? item.billingPaymentMethodId ?? item.billingPartnerId ?? item.billingRuleId;
  }

  invoiceItemTotal(item: InvoiceItemFormState): number {
    return Math.max(item.quantity * item.unitPrice - item.discountAmount, 0);
  }

  formatEnum(value?: string | null): string {
    if (!value) {
      return 'Not set';
    }

    return value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  getStatusTone(value?: string | null): 'success' | 'warning' | 'danger' | 'info' {
    const normalized = String(value ?? '').toLowerCase();

    if (['paid', 'approved', 'processed', 'settled', 'active'].includes(normalized)) {
      return 'success';
    }

    if (['cancelled', 'rejected', 'inactive'].includes(normalized)) {
      return 'danger';
    }

    if (['partial', 'submitted', 'underreview', 'draft', 'requested', 'pending'].includes(normalized)) {
      return 'warning';
    }

    return 'info';
  }

  private syncRoute(url: string): void {
    const section = this.toSection(url.split('?')[0].split('/').filter(Boolean)[2] ?? null);
    if (this.activeSection !== section) {
      this.activeSection = section;
      this.searchTerm = '';
      this.resetActiveForm();
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      settings: this.adminOps.getOrganizationSettings(),
      patients: this.phase3.getPatients('', null, false),
      appointments: this.phase3.getAppointments(),
      charges: this.phase4.getChargeDefinitions(),
      invoices: this.phase4.getInvoices(),
      paymentMethods: this.phase4.getPaymentMethods(),
      partners: this.phase4.getPartners(),
      rules: this.phase4.getBillingRules()
    }).subscribe({
      next: ({ settings, patients, appointments, charges, invoices, paymentMethods, partners, rules }) => {
        this.branches = settings.branches.filter((branch) => branch.isActive);
        this.patients = patients.filter((patient) => patient.isActive && !patient.isMerged);
        this.appointments = appointments;
        this.charges = this.sortCharges(charges);
        this.invoices = this.sortInvoices(invoices);
        this.paymentMethods = this.sortPaymentMethods(paymentMethods);
        this.partners = this.sortPartners(partners);
        this.rules = this.sortRules(rules);
        this.restoreSelectionState();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load billing and finance data right now.';
      }
    });
  }

  private restoreSelectionState(): void {
    if (this.selectedChargeId) {
      const item = this.charges.find((entry) => entry.billingChargeDefinitionId === this.selectedChargeId);
      if (item) {
        this.chargeForm = { ...item };
      }
    }

    if (this.selectedPaymentMethodId) {
      const item = this.paymentMethods.find((entry) => entry.billingPaymentMethodId === this.selectedPaymentMethodId);
      if (item) {
        this.paymentMethodForm = { ...item };
      }
    }

    if (this.selectedPartnerId) {
      const item = this.partners.find((entry) => entry.billingPartnerId === this.selectedPartnerId);
      if (item) {
        this.partnerForm = { ...item };
      }
    }

    if (this.selectedRuleId) {
      const item = this.rules.find((entry) => entry.billingRuleId === this.selectedRuleId);
      if (item) {
        this.selectRule(item);
      }
    }

    if (this.selectedInvoiceId) {
      const item = this.invoices.find((entry) => entry.billingInvoiceId === this.selectedInvoiceId);
      if (item) {
        this.selectInvoice(item);
      }
    }
  }

  private runSave<T>(request: Observable<T>, onSuccess: (value: T) => void, successMessage: string): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    request.subscribe({
      next: (value: T) => {
        onSuccess(value);
        this.isSaving = false;
        this.successMessage = successMessage;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to save billing and finance changes.';
      }
    });
  }

  private matchesSearch(...values: Array<string | number | null | undefined>): boolean {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return true;
    }

    return values.some((value) => String(value ?? '').toLowerCase().includes(term));
  }

  private toSection(value: string | null): FinanceSection {
    return this.sections.some((section) => section.key === value) ? (value as FinanceSection) : 'bills';
  }

  private replaceOrAppend<T, K extends keyof T>(items: T[], item: T, key: K): T[] {
    const index = items.findIndex((entry) => entry[key] === item[key]);
    if (index === -1) {
      return [...items, item];
    }

    return items.map((entry) => entry[key] === item[key] ? item : entry);
  }

  private sortCharges(items: BillingChargeDefinition[]): BillingChargeDefinition[] {
    return [...items].sort((a, b) => `${a.chargeType} ${a.name}`.localeCompare(`${b.chargeType} ${b.name}`));
  }

  private sortPaymentMethods(items: BillingPaymentMethod[]): BillingPaymentMethod[] {
    return [...items].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  }

  private sortPartners(items: BillingPartner[]): BillingPartner[] {
    return [...items].sort((a, b) => `${a.kind} ${a.name}`.localeCompare(`${b.kind} ${b.name}`));
  }

  private sortRules(items: BillingRule[]): BillingRule[] {
    return [...items].sort((a, b) => a.ruleName.localeCompare(b.ruleName));
  }

  private sortInvoices(items: BillingInvoice[]): BillingInvoice[] {
    return [...items].sort((a, b) => `${b.invoiceDate} ${b.invoiceNumber}`.localeCompare(`${a.invoiceDate} ${a.invoiceNumber}`));
  }

  private addDaysToDate(dateValue: string, days: number): string {
    const baseDate = new Date(dateValue);
    if (Number.isNaN(baseDate.getTime())) {
      return '';
    }

    baseDate.setDate(baseDate.getDate() + days);
    return baseDate.toISOString().slice(0, 10);
  }
}

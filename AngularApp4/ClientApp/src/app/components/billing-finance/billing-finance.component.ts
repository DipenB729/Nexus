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
type StatusFilter = 'all' | 'active' | 'inactive' | 'open' | 'closed';
type ViewMode = 'index' | 'details' | 'create' | 'edit';

interface SectionOption {
  key: FinanceSection;
  label: string;
  icon: string;
  description: string;
}

interface FilterOption {
  key: StatusFilter;
  label: string;
}

interface ChargeFormState {
  chargeType: BillingChargeType;
  name: string;
  code: string;
  description: string;
  unitLabel: string;
  defaultAmount: number;
  isActive: boolean;
}

interface PaymentMethodFormState {
  name: string;
  methodType: PaymentMethodType;
  providerName: string;
  requiresReference: boolean;
  sortOrder: number;
  isActive: boolean;
}

interface PartnerFormState {
  kind: BillingPartnerKind;
  name: string;
  code: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  creditLimit: number;
  claimSubmissionMode: string;
  notes: string;
  isActive: boolean;
}

interface RuleFormState {
  billingPartnerId: number;
  ruleName: string;
  policyName: string;
  discountPercentage: number;
  coPayPercentage: number;
  creditLimit: number;
  claimSubmissionWindowDays: number;
  requiresPreApproval: boolean;
  notes: string;
  isActive: boolean;
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

interface InvoiceFormState {
  patientId: number | null;
  appointmentId: number | null;
  branchId: number | null;
  payerType: BillingPayerType;
  billingPartnerId: number | null;
  billingRuleId: number | null;
  invoiceDate: string;
  dueDate: string;
  claimStatus: BillingClaimStatus;
  claimReferenceNumber: string;
  requestedDiscountAmount: number;
  discountNotes: string;
  notes: string;
  items: InvoiceItemFormState[];
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'bills', label: 'Bills', icon: 'receipt_long', description: 'Invoices, payment tracking, refunds, discounts, and claim follow-up.' },
  { key: 'charges', label: 'Charges', icon: 'sell', description: 'Consultation, lab, procedure, bed, and nursing charge setup.' },
  { key: 'paymentMethods', label: 'Payment Methods', icon: 'payments', description: 'Cash, card, bank, wallet, and claim settlement methods.' },
  { key: 'insurance', label: 'Insurance', icon: 'shield', description: 'Insurance company master with credit and claim submission controls.' },
  { key: 'panels', label: 'Corporate Panels', icon: 'apartment', description: 'Corporate and panel organizations used for billed accounts.' },
  { key: 'rules', label: 'Billing Rules', icon: 'rule', description: 'Coverage rules, co-pay, claims window, and pre-approval policies.' }
];

function currentDateInput(): string {
  return new Date().toISOString().slice(0, 10);
}

function createEmptyChargeForm(): ChargeFormState {
  return {
    chargeType: 'Consultation',
    name: '',
    code: '',
    description: '',
    unitLabel: 'unit',
    defaultAmount: 0,
    isActive: true
  };
}

function createEmptyPaymentMethodForm(): PaymentMethodFormState {
  return {
    name: '',
    methodType: 'Cash',
    providerName: '',
    requiresReference: false,
    sortOrder: 1,
    isActive: true
  };
}

function createEmptyPartnerForm(kind: BillingPartnerKind): PartnerFormState {
  return {
    kind,
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
}

function createEmptyRuleForm(): RuleFormState {
  return {
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
}

function createEmptyInvoiceItem(): InvoiceItemFormState {
  return {
    billingChargeDefinitionId: null,
    chargeType: 'Consultation',
    description: '',
    quantity: 1,
    unitPrice: 0,
    discountAmount: 0,
    notes: ''
  };
}

function createEmptyInvoiceForm(): InvoiceFormState {
  return {
    patientId: null,
    appointmentId: null,
    branchId: null,
    payerType: 'SelfPay',
    billingPartnerId: null,
    billingRuleId: null,
    invoiceDate: currentDateInput(),
    dueDate: '',
    claimStatus: 'None',
    claimReferenceNumber: '',
    requestedDiscountAmount: 0,
    discountNotes: '',
    notes: '',
    items: [createEmptyInvoiceItem()]
  };
}

function createEmptyDiscountForm(): ApproveBillingDiscountPayload {
  return {
    requestedDiscountAmount: 0,
    approvedDiscountAmount: 0,
    discountNotes: ''
  };
}

function createEmptyPaymentForm(): RecordBillingPaymentPayload {
  return {
    billingPaymentMethodId: 0,
    amount: 0,
    paymentDate: currentDateInput(),
    referenceNumber: '',
    notes: ''
  };
}

function createEmptyRefundForm(): ProcessBillingRefundPayload {
  return {
    billingPaymentMethodId: null,
    amount: 0,
    status: 'Processed',
    reason: '',
    notes: ''
  };
}

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
  viewMode: ViewMode = 'index';
  selectedRecordId: number | null = null;
  statusFilter: StatusFilter = 'all';
  searchTerm = '';

  charges: BillingChargeDefinition[] = [];
  invoices: BillingInvoice[] = [];
  paymentMethods: BillingPaymentMethod[] = [];
  partners: BillingPartner[] = [];
  rules: BillingRule[] = [];
  patients: PatientRecord[] = [];
  appointments: AppointmentAdminRecord[] = [];
  branches: BranchSettings[] = [];

  chargeForm: ChargeFormState = createEmptyChargeForm();
  paymentMethodForm: PaymentMethodFormState = createEmptyPaymentMethodForm();
  partnerForm: PartnerFormState = createEmptyPartnerForm('InsuranceCompany');
  ruleForm: RuleFormState = createEmptyRuleForm();
  invoiceForm: InvoiceFormState = createEmptyInvoiceForm();
  discountForm: ApproveBillingDiscountPayload = createEmptyDiscountForm();
  paymentForm: RecordBillingPaymentPayload = createEmptyPaymentForm();
  refundForm: ProcessBillingRefundPayload = createEmptyRefundForm();

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

  get currentRouteBase(): string[] {
    return ['/admin/billing', this.activeSection];
  }

  get isIndexView(): boolean {
    return this.viewMode === 'index';
  }

  get isDetailsView(): boolean {
    return this.viewMode === 'details';
  }

  get isCreateView(): boolean {
    return this.viewMode === 'create';
  }

  get isEditView(): boolean {
    return this.viewMode === 'edit';
  }

  get isFormView(): boolean {
    return this.isCreateView || this.isEditView;
  }

  get currentRecordLabel(): string {
    const labels: Record<FinanceSection, string> = {
      charges: 'Charge',
      bills: 'Bill',
      paymentMethods: 'Payment Method',
      insurance: 'Insurance Company',
      panels: 'Corporate Panel',
      rules: 'Billing Rule'
    };

    return labels[this.activeSection];
  }

  get currentFilterOptions(): ReadonlyArray<FilterOption> {
    return this.activeSection === 'bills'
      ? [
          { key: 'all', label: 'All' },
          { key: 'open', label: 'Open' },
          { key: 'closed', label: 'Closed' }
        ]
      : [
          { key: 'all', label: 'All' },
          { key: 'active', label: 'Active' },
          { key: 'inactive', label: 'Inactive' }
        ];
  }

  get selectedCharge(): BillingChargeDefinition | null {
    return this.selectedRecordId ? this.charges.find((item) => item.billingChargeDefinitionId === this.selectedRecordId) ?? null : null;
  }

  get selectedInvoice(): BillingInvoice | null {
    return this.selectedRecordId ? this.invoices.find((item) => item.billingInvoiceId === this.selectedRecordId) ?? null : null;
  }

  get selectedPaymentMethod(): BillingPaymentMethod | null {
    return this.selectedRecordId ? this.paymentMethods.find((item) => item.billingPaymentMethodId === this.selectedRecordId) ?? null : null;
  }

  get selectedPartner(): BillingPartner | null {
    return this.selectedRecordId ? this.currentPartners.find((item) => item.billingPartnerId === this.selectedRecordId) ?? null : null;
  }

  get selectedRule(): BillingRule | null {
    return this.selectedRecordId ? this.rules.find((item) => item.billingRuleId === this.selectedRecordId) ?? null : null;
  }

  get hasDetailsRecord(): boolean {
    return !!(this.selectedCharge || this.selectedInvoice || this.selectedPaymentMethod || this.selectedPartner || this.selectedRule);
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
    if (this.invoiceForm.payerType === 'Insurance') {
      return this.insurancePartners.filter((item) => item.isActive);
    }

    if (this.invoiceForm.payerType === 'Corporate') {
      return this.panelPartners.filter((item) => item.isActive);
    }

    return [];
  }

  get availableInvoiceRules(): BillingRule[] {
    return this.rules.filter((rule) =>
      rule.isActive && (!this.invoiceForm.billingPartnerId || rule.billingPartnerId === this.invoiceForm.billingPartnerId));
  }

  get availableAppointments(): AppointmentAdminRecord[] {
    return this.appointments.filter((appointment) => !this.invoiceForm.patientId || appointment.patientId === this.invoiceForm.patientId);
  }

  get filteredCharges(): BillingChargeDefinition[] {
    return this.charges.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.name, item.code, item.chargeType, item.description));
  }

  get filteredInvoices(): BillingInvoice[] {
    return this.invoices.filter((item) =>
      this.matchesInvoiceFilter(item) &&
      this.matchesSearch(item.invoiceNumber, item.patientName, item.billingPartnerName, item.status, item.claimStatus, item.branchName));
  }

  get filteredPaymentMethods(): BillingPaymentMethod[] {
    return this.paymentMethods.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.name, item.methodType, item.providerName));
  }

  get filteredPartners(): BillingPartner[] {
    return this.currentPartners.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.name, item.code, item.contactPerson, item.contactEmail, item.contactPhone, item.claimSubmissionMode));
  }

  get filteredRules(): BillingRule[] {
    return this.rules.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.ruleName, item.policyName, item.billingPartnerName, item.billingPartnerKind));
  }

  get sectionRecordCount(): number {
    switch (this.activeSection) {
      case 'charges':
        return this.filteredCharges.length;
      case 'paymentMethods':
        return this.filteredPaymentMethods.length;
      case 'insurance':
      case 'panels':
        return this.filteredPartners.length;
      case 'rules':
        return this.filteredRules.length;
      case 'bills':
      default:
        return this.filteredInvoices.length;
    }
  }

  get selectedPaymentMethodRequiresReference(): boolean {
    const paymentMethod = this.paymentMethods.find((item) => item.billingPaymentMethodId === this.paymentForm.billingPaymentMethodId);
    return paymentMethod?.requiresReference ?? false;
  }

  setSection(section: FinanceSection): void {
    this.router.navigate(['/admin/billing', section]);
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter = filter;
  }

  goToCreate(): void {
    this.router.navigate([...this.currentRouteBase, 'create']);
  }

  backToIndex(): void {
    this.router.navigate(this.currentRouteBase);
  }

  goToDetails(recordId: number): void {
    this.router.navigate([...this.currentRouteBase, recordId]);
  }

  goToEdit(recordId: number): void {
    this.router.navigate([...this.currentRouteBase, recordId, 'edit']);
  }

  refresh(): void {
    this.loadData();
  }

  resetCurrentForm(): void {
    if (this.isEditView && this.selectedRecordId) {
      this.loadSelectedRecordIntoForm();
      return;
    }

    this.resetFormForSection(this.activeSection);
  }

  addInvoiceItem(): void {
    this.invoiceForm.items = [...this.invoiceForm.items, createEmptyInvoiceItem()];
  }

  removeInvoiceItem(index: number): void {
    this.invoiceForm.items = this.invoiceForm.items.filter((_, currentIndex) => currentIndex !== index);
    if (this.invoiceForm.items.length === 0) {
      this.invoiceForm.items = [createEmptyInvoiceItem()];
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

    const payload = this.buildChargePayload(this.chargeForm);
    const request = this.selectedRecordId
      ? this.phase4.updateChargeDefinition(this.selectedRecordId, payload)
      : this.phase4.createChargeDefinition(payload);

    this.runSave(
      request,
      (item) => {
        this.charges = this.sortCharges(this.replaceOrAppend(this.charges, item, 'billingChargeDefinitionId'));
        this.goToDetails(item.billingChargeDefinitionId);
      },
      this.selectedRecordId ? 'Charge updated successfully.' : 'Charge created successfully.'
    );
  }

  savePaymentMethod(): void {
    if (!this.paymentMethodForm.name.trim()) {
      this.errorMessage = 'Payment method name is required.';
      return;
    }

    const payload = this.buildPaymentMethodPayload(this.paymentMethodForm);
    const request = this.selectedRecordId
      ? this.phase4.updatePaymentMethod(this.selectedRecordId, payload)
      : this.phase4.createPaymentMethod(payload);

    this.runSave(
      request,
      (item) => {
        this.paymentMethods = this.sortPaymentMethods(this.replaceOrAppend(this.paymentMethods, item, 'billingPaymentMethodId'));
        this.goToDetails(item.billingPaymentMethodId);
      },
      this.selectedRecordId ? 'Payment method updated successfully.' : 'Payment method created successfully.'
    );
  }

  savePartner(): void {
    if (!this.partnerForm.name.trim() || !this.partnerForm.code.trim()) {
      this.errorMessage = 'Partner name and code are required.';
      return;
    }

    const payload = this.buildPartnerPayload(this.partnerForm);
    const request = this.selectedRecordId
      ? this.phase4.updatePartner(this.selectedRecordId, payload)
      : this.phase4.createPartner(payload);

    this.runSave(
      request,
      (item) => {
        this.partners = this.sortPartners(this.replaceOrAppend(this.partners, item, 'billingPartnerId'));
        this.goToDetails(item.billingPartnerId);
      },
      this.selectedRecordId ? `${this.currentRecordLabel} updated successfully.` : `${this.currentRecordLabel} created successfully.`
    );
  }

  saveRule(): void {
    if (!this.ruleForm.billingPartnerId || !this.ruleForm.ruleName.trim()) {
      this.errorMessage = 'Billing partner and rule name are required.';
      return;
    }

    const payload = this.buildRulePayload(this.ruleForm);
    const request = this.selectedRecordId
      ? this.phase4.updateBillingRule(this.selectedRecordId, payload)
      : this.phase4.createBillingRule(payload);

    this.runSave(
      request,
      (item) => {
        this.rules = this.sortRules(this.replaceOrAppend(this.rules, item, 'billingRuleId'));
        this.goToDetails(item.billingRuleId);
      },
      this.selectedRecordId ? 'Billing rule updated successfully.' : 'Billing rule created successfully.'
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

    const payload = this.buildInvoicePayload(validItems);
    const request = this.selectedRecordId
      ? this.phase4.updateInvoice(this.selectedRecordId, payload)
      : this.phase4.createInvoice(payload);

    this.runSave(
      request,
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.goToDetails(item.billingInvoiceId);
      },
      this.selectedRecordId ? 'Bill updated successfully.' : 'Bill created successfully.'
    );
  }

  approveDiscount(): void {
    if (!this.selectedInvoice) {
      return;
    }

    this.runSave(
      this.phase4.approveDiscount(this.selectedInvoice.billingInvoiceId, {
        requestedDiscountAmount: this.discountForm.requestedDiscountAmount,
        approvedDiscountAmount: this.discountForm.approvedDiscountAmount,
        discountNotes: this.normalizeOptional(this.discountForm.discountNotes ?? '')
      }),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.populateInvoiceForm(item);
      },
      'Discount approval saved successfully.'
    );
  }

  recordPayment(): void {
    if (!this.selectedInvoice || !this.paymentForm.billingPaymentMethodId || this.paymentForm.amount <= 0) {
      this.errorMessage = 'Payment method and amount are required.';
      return;
    }

    this.runSave(
      this.phase4.recordPayment(this.selectedInvoice.billingInvoiceId, {
        billingPaymentMethodId: this.paymentForm.billingPaymentMethodId,
        amount: this.paymentForm.amount,
        paymentDate: this.paymentForm.paymentDate,
        referenceNumber: this.normalizeOptional(this.paymentForm.referenceNumber ?? ''),
        notes: this.normalizeOptional(this.paymentForm.notes ?? '')
      }),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.populateInvoiceForm(item);
      },
      'Payment recorded successfully.'
    );
  }

  processRefund(): void {
    if (!this.selectedInvoice || this.refundForm.amount <= 0 || !(this.refundForm.reason ?? '').trim()) {
      this.errorMessage = 'Refund amount and reason are required.';
      return;
    }

    this.runSave(
      this.phase4.processRefund(this.selectedInvoice.billingInvoiceId, {
        billingPaymentMethodId: this.refundForm.billingPaymentMethodId ?? null,
        amount: this.refundForm.amount,
        status: this.refundForm.status,
        reason: (this.refundForm.reason ?? '').trim(),
        notes: this.normalizeOptional(this.refundForm.notes ?? '')
      }),
      (item) => {
        this.invoices = this.sortInvoices(this.replaceOrAppend(this.invoices, item, 'billingInvoiceId'));
        this.populateInvoiceForm(item);
      },
      'Refund recorded successfully.'
    );
  }

  updateChargeStatus(item: BillingChargeDefinition): void {
    this.runSave(
      this.phase4.updateChargeDefinition(item.billingChargeDefinitionId, this.buildChargePayload({
        chargeType: item.chargeType,
        name: item.name,
        code: item.code,
        description: item.description ?? '',
        unitLabel: item.unitLabel,
        defaultAmount: item.defaultAmount,
        isActive: !item.isActive
      })),
      (updated) => {
        this.charges = this.sortCharges(this.replaceOrAppend(this.charges, updated, 'billingChargeDefinitionId'));
        if (this.selectedRecordId === updated.billingChargeDefinitionId && this.isEditView) {
          this.populateChargeForm(updated);
        }
      },
      `Charge ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      'Unable to update charge status.'
    );
  }

  updatePaymentMethodStatus(item: BillingPaymentMethod): void {
    this.runSave(
      this.phase4.updatePaymentMethod(item.billingPaymentMethodId, this.buildPaymentMethodPayload({
        name: item.name,
        methodType: item.methodType,
        providerName: item.providerName ?? '',
        requiresReference: item.requiresReference,
        sortOrder: item.sortOrder,
        isActive: !item.isActive
      })),
      (updated) => {
        this.paymentMethods = this.sortPaymentMethods(this.replaceOrAppend(this.paymentMethods, updated, 'billingPaymentMethodId'));
        if (this.selectedRecordId === updated.billingPaymentMethodId && this.isEditView) {
          this.populatePaymentMethodForm(updated);
        }
      },
      `Payment method ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      'Unable to update payment method status.'
    );
  }

  updatePartnerStatus(item: BillingPartner): void {
    this.runSave(
      this.phase4.updatePartner(item.billingPartnerId, this.buildPartnerPayload({
        kind: item.kind,
        name: item.name,
        code: item.code,
        contactPerson: item.contactPerson ?? '',
        contactEmail: item.contactEmail ?? '',
        contactPhone: item.contactPhone ?? '',
        creditLimit: item.creditLimit,
        claimSubmissionMode: item.claimSubmissionMode,
        notes: item.notes ?? '',
        isActive: !item.isActive
      })),
      (updated) => {
        this.partners = this.sortPartners(this.replaceOrAppend(this.partners, updated, 'billingPartnerId'));
        if (this.selectedRecordId === updated.billingPartnerId && this.isEditView) {
          this.populatePartnerForm(updated);
        }
      },
      `${this.currentRecordLabel} ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      'Unable to update partner status.'
    );
  }

  updateRuleStatus(item: BillingRule): void {
    this.runSave(
      this.phase4.updateBillingRule(item.billingRuleId, this.buildRulePayload({
        billingPartnerId: item.billingPartnerId,
        ruleName: item.ruleName,
        policyName: item.policyName ?? '',
        discountPercentage: item.discountPercentage,
        coPayPercentage: item.coPayPercentage,
        creditLimit: item.creditLimit,
        claimSubmissionWindowDays: item.claimSubmissionWindowDays,
        requiresPreApproval: item.requiresPreApproval,
        notes: item.notes ?? '',
        isActive: !item.isActive
      })),
      (updated) => {
        this.rules = this.sortRules(this.replaceOrAppend(this.rules, updated, 'billingRuleId'));
        if (this.selectedRecordId === updated.billingRuleId && this.isEditView) {
          this.populateRuleForm(updated);
        }
      },
      `Billing rule ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      'Unable to update billing rule status.'
    );
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
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const nextSection = this.toSection(parts[2] ?? null);
    const nextId = this.parseId(parts[3] ?? null);
    const previousSection = this.activeSection;

    this.activeSection = nextSection;
    this.selectedRecordId = nextId;

    if (parts[3] === 'create') {
      this.viewMode = 'create';
      this.selectedRecordId = null;
      this.resetFormForSection(this.activeSection);
    } else if (nextId && parts[4] === 'edit') {
      this.viewMode = 'edit';
      this.loadSelectedRecordIntoForm();
    } else if (nextId) {
      this.viewMode = 'details';
    } else {
      this.viewMode = 'index';
    }

    if (previousSection !== nextSection) {
      this.searchTerm = '';
      this.statusFilter = 'all';
      this.errorMessage = '';
      this.successMessage = '';
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
        this.loadSelectedRecordIntoForm();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load billing data right now.';
      }
    });
  }

  private resetFormForSection(section: FinanceSection): void {
    this.errorMessage = '';
    this.successMessage = '';

    switch (section) {
      case 'charges':
        this.chargeForm = createEmptyChargeForm();
        break;
      case 'paymentMethods':
        this.paymentMethodForm = createEmptyPaymentMethodForm();
        break;
      case 'insurance':
        this.partnerForm = createEmptyPartnerForm('InsuranceCompany');
        break;
      case 'panels':
        this.partnerForm = createEmptyPartnerForm('PanelOrganization');
        break;
      case 'rules':
        this.ruleForm = createEmptyRuleForm();
        break;
      case 'bills':
      default:
        this.populateInvoiceForm();
        break;
    }
  }

  private loadSelectedRecordIntoForm(): void {
    if (!this.isEditView) {
      return;
    }

    switch (this.activeSection) {
      case 'charges':
        this.populateChargeForm(this.selectedCharge ?? undefined);
        break;
      case 'paymentMethods':
        this.populatePaymentMethodForm(this.selectedPaymentMethod ?? undefined);
        break;
      case 'insurance':
      case 'panels':
        this.populatePartnerForm(this.selectedPartner ?? undefined);
        break;
      case 'rules':
        this.populateRuleForm(this.selectedRule ?? undefined);
        break;
      case 'bills':
      default:
        this.populateInvoiceForm(this.selectedInvoice ?? undefined);
        break;
    }
  }

  private populateChargeForm(item?: BillingChargeDefinition): void {
    this.chargeForm = item
      ? {
          chargeType: item.chargeType,
          name: item.name,
          code: item.code,
          description: item.description ?? '',
          unitLabel: item.unitLabel,
          defaultAmount: item.defaultAmount,
          isActive: item.isActive
        }
      : createEmptyChargeForm();
  }

  private populatePaymentMethodForm(item?: BillingPaymentMethod): void {
    this.paymentMethodForm = item
      ? {
          name: item.name,
          methodType: item.methodType,
          providerName: item.providerName ?? '',
          requiresReference: item.requiresReference,
          sortOrder: item.sortOrder,
          isActive: item.isActive
        }
      : createEmptyPaymentMethodForm();
  }

  private populatePartnerForm(item?: BillingPartner): void {
    this.partnerForm = item
      ? {
          kind: item.kind,
          name: item.name,
          code: item.code,
          contactPerson: item.contactPerson ?? '',
          contactEmail: item.contactEmail ?? '',
          contactPhone: item.contactPhone ?? '',
          creditLimit: item.creditLimit,
          claimSubmissionMode: item.claimSubmissionMode,
          notes: item.notes ?? '',
          isActive: item.isActive
        }
      : createEmptyPartnerForm(this.activeSection === 'insurance' ? 'InsuranceCompany' : 'PanelOrganization');
  }

  private populateRuleForm(item?: BillingRule): void {
    this.ruleForm = item
      ? {
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
        }
      : createEmptyRuleForm();
  }

  private populateInvoiceForm(item?: BillingInvoice): void {
    this.invoiceForm = item
      ? {
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
          items: item.items.length
            ? item.items.map((line) => ({
                billingChargeDefinitionId: line.billingChargeDefinitionId ?? null,
                chargeType: line.chargeType,
                description: line.description,
                quantity: line.quantity,
                unitPrice: line.unitPrice,
                discountAmount: line.discountAmount,
                notes: line.notes ?? ''
              }))
            : [createEmptyInvoiceItem()]
        }
      : createEmptyInvoiceForm();

    this.discountForm = item
      ? {
          requestedDiscountAmount: item.requestedDiscountAmount,
          approvedDiscountAmount: item.approvedDiscountAmount,
          discountNotes: item.discountNotes ?? ''
        }
      : createEmptyDiscountForm();

    this.paymentForm = createEmptyPaymentForm();
    this.refundForm = createEmptyRefundForm();
  }

  private buildChargePayload(form: ChargeFormState): SaveBillingChargeDefinitionPayload {
    return {
      chargeType: form.chargeType,
      name: form.name.trim(),
      code: form.code.trim(),
      description: this.normalizeOptional(form.description),
      unitLabel: form.unitLabel.trim() || 'unit',
      defaultAmount: form.defaultAmount,
      isActive: form.isActive
    };
  }

  private buildPaymentMethodPayload(form: PaymentMethodFormState): SaveBillingPaymentMethodPayload {
    return {
      name: form.name.trim(),
      methodType: form.methodType,
      providerName: this.normalizeOptional(form.providerName),
      requiresReference: form.requiresReference,
      sortOrder: form.sortOrder,
      isActive: form.isActive
    };
  }

  private buildPartnerPayload(form: PartnerFormState): SaveBillingPartnerPayload {
    return {
      kind: form.kind,
      name: form.name.trim(),
      code: form.code.trim(),
      contactPerson: this.normalizeOptional(form.contactPerson),
      contactEmail: this.normalizeOptional(form.contactEmail),
      contactPhone: this.normalizeOptional(form.contactPhone),
      creditLimit: form.creditLimit,
      claimSubmissionMode: form.claimSubmissionMode.trim() || 'Manual',
      notes: this.normalizeOptional(form.notes),
      isActive: form.isActive
    };
  }

  private buildRulePayload(form: RuleFormState): SaveBillingRulePayload {
    return {
      billingPartnerId: form.billingPartnerId,
      ruleName: form.ruleName.trim(),
      policyName: this.normalizeOptional(form.policyName),
      discountPercentage: form.discountPercentage,
      coPayPercentage: form.coPayPercentage,
      creditLimit: form.creditLimit,
      claimSubmissionWindowDays: form.claimSubmissionWindowDays,
      requiresPreApproval: form.requiresPreApproval,
      notes: this.normalizeOptional(form.notes),
      isActive: form.isActive
    };
  }

  private buildInvoicePayload(items: InvoiceItemFormState[]): SaveBillingInvoicePayload {
    return {
      patientId: this.invoiceForm.patientId,
      appointmentId: this.invoiceForm.appointmentId,
      branchId: this.invoiceForm.branchId,
      payerType: this.invoiceForm.payerType,
      billingPartnerId: this.invoiceForm.payerType === 'SelfPay' ? null : this.invoiceForm.billingPartnerId,
      billingRuleId: this.invoiceForm.billingRuleId,
      invoiceDate: this.invoiceForm.invoiceDate,
      dueDate: this.invoiceForm.dueDate || null,
      claimStatus: this.invoiceForm.claimStatus,
      claimReferenceNumber: this.normalizeOptional(this.invoiceForm.claimReferenceNumber),
      requestedDiscountAmount: this.invoiceForm.requestedDiscountAmount,
      discountNotes: this.normalizeOptional(this.invoiceForm.discountNotes),
      notes: this.normalizeOptional(this.invoiceForm.notes),
      items: items.map((item): SaveBillingInvoiceItemPayload => ({
        billingChargeDefinitionId: item.billingChargeDefinitionId,
        chargeType: item.chargeType,
        description: item.description.trim(),
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discountAmount: item.discountAmount,
        notes: this.normalizeOptional(item.notes)
      }))
    };
  }

  private runSave<T>(
    request: Observable<T>,
    onSuccess: (value: T) => void,
    successMessage: string,
    fallbackError = 'Unable to save billing changes.'
  ): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    request.subscribe({
      next: (value) => {
        onSuccess(value);
        this.isSaving = false;
        this.successMessage = successMessage;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || fallbackError;
      }
    });
  }

  private matchesActiveFilter(isActive: boolean): boolean {
    if (this.statusFilter === 'active') {
      return isActive;
    }

    if (this.statusFilter === 'inactive') {
      return !isActive;
    }

    return true;
  }

  private matchesInvoiceFilter(invoice: BillingInvoice): boolean {
    if (this.statusFilter === 'open') {
      return invoice.dueAmount > 0 || !['Paid', 'Cancelled'].includes(invoice.status);
    }

    if (this.statusFilter === 'closed') {
      return invoice.dueAmount <= 0 && ['Paid', 'Cancelled'].includes(invoice.status);
    }

    return true;
  }

  private matchesSearch(...values: Array<string | number | null | undefined>): boolean {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return true;
    }

    return values.some((value) => String(value ?? '').toLowerCase().includes(term));
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
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
    return [...items].sort((a, b) => `${a.billingPartnerName} ${a.ruleName}`.localeCompare(`${b.billingPartnerName} ${b.ruleName}`));
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

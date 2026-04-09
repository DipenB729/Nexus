import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  ApproveBillingDiscountPayload,
  BillingChargeDefinition,
  BillingInvoice,
  BillingPartner,
  BillingPaymentMethod,
  BillingRule,
  ProcessBillingRefundPayload,
  RecordBillingPaymentPayload,
  SaveBillingChargeDefinitionPayload,
  SaveBillingInvoicePayload,
  SaveBillingPartnerPayload,
  SaveBillingPaymentMethodPayload,
  SaveBillingRulePayload
} from '../../models/hms/phase4-billing.model';

@Injectable({ providedIn: 'root' })
export class Phase4BillingService {
  constructor(private readonly http: HttpClient) {}

  getChargeDefinitions(): Observable<BillingChargeDefinition[]> {
    return this.http
      .get<ApiResponse<BillingChargeDefinition[]>>('/api/admin/billing/charges')
      .pipe(map((res) => res.data));
  }

  createChargeDefinition(payload: SaveBillingChargeDefinitionPayload): Observable<BillingChargeDefinition> {
    return this.http
      .post<ApiResponse<BillingChargeDefinition>>('/api/admin/billing/charges', payload)
      .pipe(map((res) => res.data));
  }

  updateChargeDefinition(chargeDefinitionId: number, payload: SaveBillingChargeDefinitionPayload): Observable<BillingChargeDefinition> {
    return this.http
      .put<ApiResponse<BillingChargeDefinition>>(`/api/admin/billing/charges/${chargeDefinitionId}`, payload)
      .pipe(map((res) => res.data));
  }

  getPaymentMethods(): Observable<BillingPaymentMethod[]> {
    return this.http
      .get<ApiResponse<BillingPaymentMethod[]>>('/api/admin/billing/payment-methods')
      .pipe(map((res) => res.data));
  }

  createPaymentMethod(payload: SaveBillingPaymentMethodPayload): Observable<BillingPaymentMethod> {
    return this.http
      .post<ApiResponse<BillingPaymentMethod>>('/api/admin/billing/payment-methods', payload)
      .pipe(map((res) => res.data));
  }

  updatePaymentMethod(paymentMethodId: number, payload: SaveBillingPaymentMethodPayload): Observable<BillingPaymentMethod> {
    return this.http
      .put<ApiResponse<BillingPaymentMethod>>(`/api/admin/billing/payment-methods/${paymentMethodId}`, payload)
      .pipe(map((res) => res.data));
  }

  getPartners(kind?: string | null): Observable<BillingPartner[]> {
    const query = kind ? `?kind=${encodeURIComponent(kind)}` : '';
    return this.http
      .get<ApiResponse<BillingPartner[]>>(`/api/admin/billing/partners${query}`)
      .pipe(map((res) => res.data));
  }

  createPartner(payload: SaveBillingPartnerPayload): Observable<BillingPartner> {
    return this.http
      .post<ApiResponse<BillingPartner>>('/api/admin/billing/partners', payload)
      .pipe(map((res) => res.data));
  }

  updatePartner(partnerId: number, payload: SaveBillingPartnerPayload): Observable<BillingPartner> {
    return this.http
      .put<ApiResponse<BillingPartner>>(`/api/admin/billing/partners/${partnerId}`, payload)
      .pipe(map((res) => res.data));
  }

  getBillingRules(): Observable<BillingRule[]> {
    return this.http
      .get<ApiResponse<BillingRule[]>>('/api/admin/billing/rules')
      .pipe(map((res) => res.data));
  }

  createBillingRule(payload: SaveBillingRulePayload): Observable<BillingRule> {
    return this.http
      .post<ApiResponse<BillingRule>>('/api/admin/billing/rules', payload)
      .pipe(map((res) => res.data));
  }

  updateBillingRule(ruleId: number, payload: SaveBillingRulePayload): Observable<BillingRule> {
    return this.http
      .put<ApiResponse<BillingRule>>(`/api/admin/billing/rules/${ruleId}`, payload)
      .pipe(map((res) => res.data));
  }

  getInvoices(): Observable<BillingInvoice[]> {
    return this.http
      .get<ApiResponse<BillingInvoice[]>>('/api/admin/billing/invoices')
      .pipe(map((res) => res.data));
  }

  createInvoice(payload: SaveBillingInvoicePayload): Observable<BillingInvoice> {
    return this.http
      .post<ApiResponse<BillingInvoice>>('/api/admin/billing/invoices', payload)
      .pipe(map((res) => res.data));
  }

  updateInvoice(invoiceId: number, payload: SaveBillingInvoicePayload): Observable<BillingInvoice> {
    return this.http
      .put<ApiResponse<BillingInvoice>>(`/api/admin/billing/invoices/${invoiceId}`, payload)
      .pipe(map((res) => res.data));
  }

  approveDiscount(invoiceId: number, payload: ApproveBillingDiscountPayload): Observable<BillingInvoice> {
    return this.http
      .post<ApiResponse<BillingInvoice>>(`/api/admin/billing/invoices/${invoiceId}/discount`, payload)
      .pipe(map((res) => res.data));
  }

  recordPayment(invoiceId: number, payload: RecordBillingPaymentPayload): Observable<BillingInvoice> {
    return this.http
      .post<ApiResponse<BillingInvoice>>(`/api/admin/billing/invoices/${invoiceId}/payments`, payload)
      .pipe(map((res) => res.data));
  }

  processRefund(invoiceId: number, payload: ProcessBillingRefundPayload): Observable<BillingInvoice> {
    return this.http
      .post<ApiResponse<BillingInvoice>>(`/api/admin/billing/invoices/${invoiceId}/refunds`, payload)
      .pipe(map((res) => res.data));
  }
}

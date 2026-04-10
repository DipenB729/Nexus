import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BranchOccupancy, DashboardSummary, PendingPayment, StockAlert } from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

interface MetricCard {
  label: string;
  value: number;
  icon: string;
  accent: string;
  suffix?: string;
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  summary: DashboardSummary | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly adminOps: AdminOpsService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  get metrics(): MetricCard[] {
    if (!this.summary) {
      return [];
    }

    return [
      { label: 'Total patients', value: this.summary.totalPatients, icon: 'groups', accent: 'blue' },
      { label: 'Today appointments', value: this.summary.todayAppointments, icon: 'event_available', accent: 'teal' },
      { label: 'Today sales', value: this.summary.todaySales, icon: 'currency_rupee', accent: 'green', suffix: 'money' },
      { label: 'Low stock items', value: this.summary.lowStockItems, icon: 'inventory_2', accent: 'amber' },
      { label: 'Expired medicines', value: this.summary.expiredMedicines, icon: 'warning_amber', accent: 'rose' },
      { label: 'Pending payments', value: this.summary.pendingPayments, icon: 'payments', accent: 'violet' },
      { label: 'Bed occupancy', value: this.summary.bedOccupancyRate, icon: 'hotel', accent: 'slate', suffix: '%' }
    ];
  }

  get lowStockAlerts(): StockAlert[] {
    return this.summary?.lowStockAlerts ?? [];
  }

  get expiredMedicineAlerts(): StockAlert[] {
    return this.summary?.expiredMedicineAlerts ?? [];
  }

  get pendingPayments(): PendingPayment[] {
    return this.summary?.pendingPaymentDetails ?? [];
  }

  get branchOccupancy(): BranchOccupancy[] {
    return this.summary?.bedOccupancyByBranch ?? [];
  }

  formatMetric(metric: MetricCard): string {
    if (metric.suffix === 'money') {
      return `NPR ${metric.value.toLocaleString()}`;
    }

    if (metric.suffix) {
      return `${metric.value}${metric.suffix}`;
    }

    return metric.value.toLocaleString();
  }

  openPendingPayment(item: PendingPayment): void {
    this.router.navigate(['/admin/billing/bills', item.invoiceId]);
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.adminOps.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load dashboard metrics right now.';
      }
    });
  }
}

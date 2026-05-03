import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CreateAdminUser,
  SuperAdminBranch,
  SuperAdminHospitalProfile,
  SuperAdminSummary,
  SuperAdminUser,
  UpsertBranch
} from '../../core/models/hms/superadmin.model';
import { SuperAdminService } from '../../core/services/hms/superadmin.service';

type SuperAdminSection = 'dashboard' | 'hospitals' | 'admins';

@Component({
  selector: 'app-superadmin-dashboard',
  templateUrl: './superadmin-dashboard.component.html',
  styleUrls: ['./superadmin-dashboard.component.scss']
})
export class SuperAdminDashboardComponent implements OnInit {
  activeSection: SuperAdminSection = 'dashboard';
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';
  activeModal: 'branch' | 'admin' | null = null;
  editingBranchId: number | null = null;

  summary: SuperAdminSummary = {
    totalHospitals: 0,
    activeBranches: 0,
    totalAdmins: 0,
    activeAdmins: 0,
    totalUsers: 0,
    lastUpdatedAt: new Date().toISOString()
  };

  profile: SuperAdminHospitalProfile = this.emptyProfile();
  hospitals: SuperAdminHospitalProfile[] = [];
  branches: SuperAdminBranch[] = [];
  admins: SuperAdminUser[] = [];
  branchForm: UpsertBranch = this.emptyBranch();
  adminForm: CreateAdminUser = this.emptyAdmin();

  constructor(
    private readonly superadmin: SuperAdminService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.route.url.subscribe((segments) => {
      const section = segments[0]?.path as SuperAdminSection | undefined;
      this.activeSection = section && ['dashboard', 'hospitals', 'admins'].includes(section) ? section : 'dashboard';
      this.load();
    });
  }

  setSection(section: SuperAdminSection): void {
    void this.router.navigate(['/superadmin', section]);
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.superadmin.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        if (this.activeSection === 'admins') {
          this.loadAdminWorkspace();
        } else {
          this.loadHospital();
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load superadmin workspace right now.';
      }
    });
  }

  saveHospital(): void {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.clearMessages();
    this.superadmin.updateHospital(this.profile).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isSaving = false;
        this.successMessage = 'Hospital profile updated.';
        this.refreshSummary();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to update hospital profile.';
      }
    });
  }

  openBranchModal(branch?: SuperAdminBranch): void {
    this.editingBranchId = branch?.branchId ?? null;
    this.branchForm = branch ? {
      name: branch.name,
      code: branch.code,
      address: branch.address,
      contactPhone: branch.contactPhone,
      contactEmail: branch.contactEmail,
      isPrimary: branch.isPrimary,
      isActive: branch.isActive,
      totalBeds: branch.totalBeds,
      occupiedBeds: branch.occupiedBeds
    } : this.emptyBranch();
    this.activeModal = 'branch';
  }

  saveBranch(): void {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.clearMessages();
    const request = this.editingBranchId
      ? this.superadmin.updateBranch(this.editingBranchId, this.branchForm)
      : this.superadmin.createBranch(this.branchForm);

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = this.editingBranchId ? 'Branch updated.' : 'Branch created.';
        this.closeModal();
        this.loadHospital();
        this.refreshSummary();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to save branch.';
      }
    });
  }

  openAdminModal(): void {
    this.adminForm = this.emptyAdmin();
    this.activeModal = 'admin';
  }

  saveAdmin(): void {
    if (this.isSaving) {
      return;
    }

    if (!this.adminForm.hospitalProfileId) {
      this.errorMessage = 'Select a hospital for this admin account.';
      return;
    }

    this.isSaving = true;
    this.clearMessages();
    this.superadmin.createAdmin(this.adminForm).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Admin account created.';
        this.closeModal();
        this.loadAdmins();
        this.refreshSummary();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to create admin account.';
      }
    });
  }

  toggleAdmin(admin: SuperAdminUser): void {
    this.clearMessages();
    this.superadmin.updateAdminStatus(admin.userId, !admin.isActive).subscribe({
      next: (updated) => {
        this.admins = this.admins.map((item) => item.userId === updated.userId ? updated : item);
        this.successMessage = `${updated.fullName} is now ${updated.isActive ? 'active' : 'inactive'}.`;
        this.refreshSummary();
      },
      error: () => {
        this.errorMessage = 'Unable to update admin status.';
      }
    });
  }

  closeModal(): void {
    this.activeModal = null;
    this.editingBranchId = null;
  }

  private loadHospital(): void {
    this.superadmin.getHospital().subscribe({
      next: (hospital) => {
        this.profile = hospital.profile ?? this.emptyProfile();
        this.hospitals = this.profile.hospitalProfileId ? [this.profile] : [];
        this.branches = hospital.branches ?? [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load hospital records.';
      }
    });
  }

  private loadAdmins(): void {
    this.superadmin.getAdmins().subscribe({
      next: (admins) => {
        this.admins = admins;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load admin accounts.';
      }
    });
  }

  private loadAdminWorkspace(): void {
    this.superadmin.getHospitals().subscribe({
      next: (hospitals) => {
        this.hospitals = hospitals;
        this.loadAdmins();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load hospital list.';
      }
    });
  }

  private refreshSummary(): void {
    this.superadmin.getSummary().subscribe({ next: (summary) => this.summary = summary });
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  private emptyProfile(): SuperAdminHospitalProfile {
    return {
      hospitalProfileId: 0,
      hospitalName: '',
      logoUrl: '',
      addressLine1: '',
      city: '',
      stateOrProvince: '',
      postalCode: '',
      country: '',
      contactEmail: '',
      contactPhone: '',
      taxLabel: 'VAT',
      taxRegistrationNumber: '',
      taxPercentage: 0,
      currencyCode: 'NPR',
      invoicePrefix: 'NEX',
      invoiceStartingNumber: 1,
      invoiceFooterNote: '',
      multiBranchEnabled: true
    };
  }

  private emptyBranch(): UpsertBranch {
    return {
      name: '',
      code: '',
      address: '',
      contactPhone: '',
      contactEmail: '',
      isPrimary: false,
      isActive: true,
      totalBeds: 0,
      occupiedBeds: 0
    };
  }

  private emptyAdmin(): CreateAdminUser {
    return {
      hospitalProfileId: this.hospitals[0]?.hospitalProfileId ?? 0,
      fullName: '',
      email: '',
      phone: '',
      password: ''
    };
  }
}

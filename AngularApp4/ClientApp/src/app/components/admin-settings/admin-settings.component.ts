import { Component, OnInit } from '@angular/core';
import { BranchSettings, HospitalProfile, OrganizationSettings } from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

const EMPTY_PROFILE: HospitalProfile = {
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
  taxPercentage: 13,
  currencyCode: 'NPR',
  invoicePrefix: 'NEX',
  invoiceStartingNumber: 5001,
  invoiceFooterNote: '',
  multiBranchEnabled: true
};

@Component({
  selector: 'app-admin-settings',
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.scss']
})
export class AdminSettingsComponent implements OnInit {
  profile: HospitalProfile = { ...EMPTY_PROFILE };
  branches: BranchSettings[] = [];
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  saveMessage = '';

  constructor(private readonly adminOps: AdminOpsService) {}

  ngOnInit(): void {
    this.loadSettings();
  }

  addBranch(): void {
    this.branches = [
      ...this.branches,
      {
        branchId: 0,
        name: '',
        code: '',
        address: '',
        contactPhone: '',
        contactEmail: '',
        isPrimary: this.branches.length === 0,
        isActive: true,
        totalBeds: 0,
        occupiedBeds: 0
      }
    ];
  }

  removeBranch(index: number): void {
    const branch = this.branches[index];
    if (!branch) {
      return;
    }

    if (branch.branchId > 0) {
      this.branches[index] = { ...branch, isActive: false, isPrimary: false };
    } else {
      this.branches = this.branches.filter((_, currentIndex) => currentIndex !== index);
    }

    if (!this.branches.some((item) => item.isPrimary) && this.branches.length) {
      this.branches[0].isPrimary = true;
    }
  }

  setPrimary(selectedIndex: number): void {
    this.branches = this.branches.map((branch, index) => ({
      ...branch,
      isPrimary: index === selectedIndex
    }));
  }

  saveSettings(): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.saveMessage = '';

    const payload: OrganizationSettings = {
      profile: { ...this.profile },
      branches: this.branches.map((branch) => ({
        ...branch,
        occupiedBeds: Math.min(branch.occupiedBeds, branch.totalBeds)
      }))
    };

    this.adminOps.updateOrganizationSettings(payload).subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.isSaving = false;
        this.saveMessage = 'Hospital profile and branch settings saved.';
      },
      error: () => {
        this.isSaving = false;
        this.errorMessage = 'Unable to save organization settings.';
      }
    });
  }

  private loadSettings(): void {
    this.isLoading = true;
    this.adminOps.getOrganizationSettings().subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load hospital settings.';
      }
    });
  }

  private applySettings(settings: OrganizationSettings): void {
    this.profile = { ...EMPTY_PROFILE, ...settings.profile };
    this.branches = settings.branches.map((branch) => ({ ...branch }));
  }
}

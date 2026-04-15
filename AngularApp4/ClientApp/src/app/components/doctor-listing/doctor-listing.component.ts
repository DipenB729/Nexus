import { Component, OnInit } from '@angular/core';
import { DoctorMaster } from '../../core/models/hms/master-setup.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';

@Component({
  selector: 'app-doctor-listing',
  templateUrl: './doctor-listing.component.html',
  styleUrls: ['./doctor-listing.component.scss']
})
export class DoctorListingComponent implements OnInit {
  doctors: DoctorMaster[] = [];
  isLoading = true;
  errorMessage = '';
  searchTerm = '';
  departmentFilter = 'All';
  expertiseFilter = 'All';

  constructor(private readonly masterSetup: MasterSetupService) {}

  ngOnInit(): void {
    this.loadDoctors();
  }

  get departmentOptions(): string[] {
    return ['All', ...new Set(this.doctors.map((doctor) => doctor.departmentName).filter(Boolean).sort((a, b) => a.localeCompare(b)))];
  }

  get expertiseOptions(): string[] {
    return ['All', ...new Set(this.doctors.map((doctor) => doctor.specialization).filter(Boolean).sort((a, b) => a.localeCompare(b)))];
  }

  get filteredDoctors(): DoctorMaster[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.doctors.filter((doctor) => {
      if (this.departmentFilter !== 'All' && doctor.departmentName !== this.departmentFilter) {
        return false;
      }

      if (this.expertiseFilter !== 'All' && doctor.specialization !== this.expertiseFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [
        doctor.fullName,
        doctor.specialization,
        doctor.departmentName,
        doctor.branchName,
        this.formatDays(doctor.opdDays)
      ]
        .join(' ')
        .toLowerCase()
        .includes(term);
    });
  }

  trackByDoctor(index: number, doctor: DoctorMaster): number {
    return doctor.doctorId;
  }

  formatDays(opdDays?: string | null): string {
    if (!opdDays?.trim()) {
      return 'Schedule available on request';
    }

    return opdDays
      .split(',')
      .map((token) => token.trim())
      .filter(Boolean)
      .map((token) => this.mapDayToken(token))
      .join(', ');
  }

  formatTimeRange(startTime?: string | null, endTime?: string | null): string {
    if (!startTime || !endTime) {
      return 'Shared on detail page';
    }

    return `${this.formatTime(startTime)} - ${this.formatTime(endTime)}`;
  }

  private loadDoctors(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.masterSetup.getDoctors(true).subscribe({
      next: (doctors) => {
        this.doctors = doctors;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load doctors right now.';
      }
    });
  }

  private mapDayToken(token: string): string {
    const normalized = token.toLowerCase();
    const labels: Record<string, string> = {
      sun: 'Sun',
      sunday: 'Sun',
      mon: 'Mon',
      monday: 'Mon',
      tue: 'Tue',
      tues: 'Tue',
      tuesday: 'Tue',
      wed: 'Wed',
      wednesday: 'Wed',
      thu: 'Thu',
      thur: 'Thu',
      thurs: 'Thu',
      thursday: 'Thu',
      fri: 'Fri',
      friday: 'Fri',
      sat: 'Sat',
      saturday: 'Sat'
    };

    return labels[normalized] ?? token.trim();
  }

  private formatTime(value: string): string {
    return value.length >= 5 ? value.slice(0, 5) : value;
  }
}

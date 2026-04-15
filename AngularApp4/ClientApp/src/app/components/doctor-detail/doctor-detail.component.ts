import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DoctorMaster } from '../../core/models/hms/master-setup.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';

@Component({
  selector: 'app-doctor-detail',
  templateUrl: './doctor-detail.component.html',
  styleUrls: ['./doctor-detail.component.scss']
})
export class DoctorDetailComponent implements OnInit {
  doctor: DoctorMaster | null = null;
  isLoadingDoctor = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly masterSetup: MasterSetupService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const doctorId = Number(params.get('doctorId'));

      if (!Number.isFinite(doctorId) || doctorId <= 0) {
        this.doctor = null;
        this.isLoadingDoctor = false;
        this.errorMessage = 'Invalid doctor selection.';
        return;
      }

      this.loadDoctor(doctorId);
    });
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
      return 'Set on the booking page';
    }

    return `${this.formatTime(startTime)} - ${this.formatTime(endTime)}`;
  }

  private loadDoctor(doctorId: number): void {
    this.isLoadingDoctor = true;
    this.errorMessage = '';

    this.masterSetup.getDoctor(doctorId).subscribe({
      next: (doctor) => {
        this.doctor = doctor;
        this.isLoadingDoctor = false;
      },
      error: () => {
        this.doctor = null;
        this.isLoadingDoctor = false;
        this.errorMessage = 'Unable to load doctor details right now.';
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

import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { PatientProfile } from '../../core/models/hms/auth.model';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-patient-profile',
  templateUrl: './patient-profile.component.html',
  styleUrls: ['./patient-profile.component.scss']
})
export class PatientProfileComponent implements OnInit {
  profile: PatientProfile | null = null;
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  statusMessage = '';

  readonly genderOptions = ['Female', 'Male', 'Other', 'Prefer not to say'];
  readonly bloodGroups = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  readonly form = this.fb.group({
    fullName: ['', [Validators.required]],
    email: [{ value: '', disabled: true }],
    phone: [''],
    gender: [''],
    dateOfBirth: [''],
    address: [''],
    bloodGroup: [''],
    emergencyContact: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthApiService
  ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.auth.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.form.reset({
          fullName: profile.fullName,
          email: profile.email,
          phone: profile.phone ?? '',
          gender: profile.gender ?? '',
          dateOfBirth: this.toDateInput(profile.dateOfBirth),
          address: profile.address ?? '',
          bloodGroup: profile.bloodGroup ?? '',
          emergencyContact: profile.emergencyContact ?? ''
        });
        this.isLoading = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isLoading = false;
        this.errorMessage = error?.error?.message || 'Unable to load your patient profile.';
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const raw = this.form.getRawValue();
    this.auth
      .updateProfile({
        fullName: raw.fullName || '',
        phone: this.normalize(raw.phone),
        gender: this.normalize(raw.gender),
        dateOfBirth: raw.dateOfBirth || null,
        address: this.normalize(raw.address),
        bloodGroup: this.normalize(raw.bloodGroup),
        emergencyContact: this.normalize(raw.emergencyContact)
      })
      .subscribe({
        next: (profile) => {
          this.profile = profile;
          this.isSaving = false;
          this.statusMessage = 'Profile updated successfully.';
          this.form.reset({
            fullName: profile.fullName,
            email: profile.email,
            phone: profile.phone ?? '',
            gender: profile.gender ?? '',
            dateOfBirth: this.toDateInput(profile.dateOfBirth),
            address: profile.address ?? '',
            bloodGroup: profile.bloodGroup ?? '',
            emergencyContact: profile.emergencyContact ?? ''
          });
        },
        error: (error: { error?: { message?: string } }) => {
          this.isSaving = false;
          this.errorMessage = error?.error?.message || 'Unable to update your profile.';
        }
      });
  }

  get medicalRecordNumber(): string {
    return this.profile?.medicalRecordNumber ?? 'Pending';
  }

  get patientCategoryName(): string {
    return this.profile?.patientCategoryName ?? 'Unassigned';
  }

  private normalize(value: string | null | undefined): string | null {
    const normalized = value?.trim() ?? '';
    return normalized ? normalized : null;
  }

  private toDateInput(value?: string | null): string {
    return value ? value.slice(0, 10) : '';
  }
}

import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { DoctorProfile, DoctorProfileDepartmentOption } from '../../core/models/hms/auth.model';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-doctor-profile',
  templateUrl: './doctor-profile.component.html',
  styleUrls: ['./doctor-profile.component.scss']
})
export class DoctorProfileComponent implements OnInit {
  profile: DoctorProfile | null = null;
  departmentOptions: DoctorProfileDepartmentOption[] = [];
  selectedPhotoName = '';
  isLoading = true;
  isSaving = false;
  isUploadingPhoto = false;
  isChangingPassword = false;
  errorMessage = '';
  statusMessage = '';
  passwordErrorMessage = '';
  passwordStatusMessage = '';

  readonly profileForm = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    departmentId: [null as number | null],
    specialization: ['', [Validators.required]],
    qualification: [''],
    licenseNumber: [''],
    experienceYears: [0, [Validators.required, Validators.min(0)]],
    consultationFee: [0, [Validators.required, Validators.min(0)]],
    bio: [''],
    address: ['']
  });

  readonly passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
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

    this.auth.getDoctorProfileOptions().subscribe({
      next: (options) => {
        this.departmentOptions = options;
      }
    });

    this.auth.getDoctorProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.profileForm.reset({
          fullName: profile.fullName,
          email: profile.email,
          phone: profile.phone ?? '',
          departmentId: profile.departmentId ?? null,
          specialization: profile.specialization ?? '',
          qualification: profile.qualification ?? '',
          licenseNumber: profile.licenseNumber ?? '',
          experienceYears: profile.experienceYears,
          consultationFee: profile.consultationFee,
          bio: profile.bio ?? '',
          address: profile.address ?? ''
        });
        this.isLoading = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isLoading = false;
        this.errorMessage = error?.error?.message || 'Unable to load your doctor profile.';
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.isSaving) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const raw = this.profileForm.getRawValue();
    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.auth.updateDoctorProfile({
      fullName: raw.fullName || '',
      email: raw.email || '',
      phone: this.normalize(raw.phone),
      departmentId: raw.departmentId ?? null,
      specialization: this.normalize(raw.specialization),
      qualification: this.normalize(raw.qualification),
      licenseNumber: this.normalize(raw.licenseNumber),
      experienceYears: Number(raw.experienceYears || 0),
      consultationFee: Number(raw.consultationFee || 0),
      bio: this.normalize(raw.bio),
      address: this.normalize(raw.address)
    }).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isSaving = false;
        this.statusMessage = 'Doctor profile updated successfully.';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to update your doctor profile.';
      }
    });
  }

  uploadPhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || this.isUploadingPhoto) {
      return;
    }

    this.selectedPhotoName = file.name;
    this.isUploadingPhoto = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.auth.uploadDoctorProfilePhoto(file).subscribe({
      next: (photoUrl) => {
        this.isUploadingPhoto = false;
        this.statusMessage = 'Profile photo uploaded successfully.';
        if (this.profile) {
          this.profile = { ...this.profile, photoUrl };
        }
        input.value = '';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isUploadingPhoto = false;
        this.errorMessage = error?.error?.message || 'Unable to upload doctor photo.';
        input.value = '';
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid || this.isChangingPassword) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const raw = this.passwordForm.getRawValue();
    if (raw.newPassword !== raw.confirmPassword) {
      this.passwordErrorMessage = 'New password and confirm password must match.';
      this.passwordStatusMessage = '';
      return;
    }

    this.isChangingPassword = true;
    this.passwordErrorMessage = '';
    this.passwordStatusMessage = '';

    this.auth.changePassword({
      currentPassword: raw.currentPassword || '',
      newPassword: raw.newPassword || ''
    }).subscribe({
      next: () => {
        this.isChangingPassword = false;
        this.passwordStatusMessage = 'Password changed successfully.';
        this.passwordForm.reset({
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        });
      },
      error: (error: { error?: { message?: string } }) => {
        this.isChangingPassword = false;
        this.passwordErrorMessage = error?.error?.message || 'Unable to change password.';
      }
    });
  }

  get avatarUrl(): string | null {
    return this.profile?.photoUrl ?? null;
  }

  get departmentDisplayName(): string {
    return this.profile?.departmentName || 'Not assigned';
  }

  private normalize(value: string | null | undefined): string | null {
    const normalized = value?.trim() ?? '';
    return normalized ? normalized : null;
  }
}

import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import {
  CareThreadDetail,
  CareThreadSummary,
  PatientCaseReport
} from '../../core/models/hms/care-communication.model';
import { CareCommunicationService } from '../../core/services/hms/care-communication.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

type CareMode = 'messages' | 'reports';

interface PatientReportFormState {
  symptoms: string;
  previousReportSummary: string;
  reportTitle: string;
  reportUrl: string;
  reportCategory: string;
  reportNotes: string;
}

const EMPTY_REPORT_FORM: PatientReportFormState = {
  symptoms: '',
  previousReportSummary: '',
  reportTitle: '',
  reportUrl: '',
  reportCategory: 'Previous report',
  reportNotes: ''
};

@Component({
  selector: 'app-care-communication',
  templateUrl: './care-communication.component.html',
  styleUrls: ['./care-communication.component.scss']
})
export class CareCommunicationComponent implements OnInit, OnDestroy {
  threads: CareThreadSummary[] = [];
  selectedThread: CareThreadDetail | null = null;
  searchTerm = '';
  messageText = '';
  reportForm: PatientReportFormState = { ...EMPTY_REPORT_FORM };
  mode: CareMode = 'messages';
  isLoading = true;
  isLoadingThread = false;
  isSavingMessage = false;
  isSavingReport = false;
  errorMessage = '';
  statusMessage = '';

  private routeSub?: Subscription;

  constructor(
    private readonly api: CareCommunicationService,
    private readonly auth: AuthApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.syncRoute();
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.syncRoute());
    this.loadThreads();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get isDoctor(): boolean {
    return this.auth.getDisplayRole(this.auth.getSession()?.role) === 'Doctor';
  }

  get isPatient(): boolean {
    return this.auth.getDisplayRole(this.auth.getSession()?.role) === 'Patient';
  }

  get pageTitle(): string {
    return this.mode === 'messages' ? 'Message Box' : 'Reports';
  }

  get pageSubtitle(): string {
    if (this.mode === 'messages') {
      return 'Direct conversations are available after an appointment is approved.';
    }

    return this.isDoctor
      ? 'Review patient symptoms and previous reports, then use the appointment workspace for advice, prescription, and diagnostics.'
      : 'Share symptoms and previous reports with the approved doctor before or after the visit.';
  }

  get selectedAppointmentId(): number | null {
    const raw = this.route.snapshot.paramMap.get('appointmentId');
    const parsed = Number(raw);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  get filteredThreads(): CareThreadSummary[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return this.threads;
    }

    return this.threads.filter((thread) =>
      [
        thread.patientName,
        thread.doctorName,
        thread.doctorSpecialization,
        thread.medicalRecordNumber,
        thread.serviceName,
        thread.status,
        thread.lastMessage
      ]
        .map((value) => String(value ?? '').toLowerCase())
        .some((value) => value.includes(term)));
  }

  openThread(appointmentId: number): void {
    const base = this.isDoctor
      ? this.mode === 'messages' ? '/doctor/messages' : '/doctor/reports'
      : this.mode === 'messages' ? '/patient/messages' : '/patient/reports';

    void this.router.navigate([base, appointmentId]);
  }

  backToThreads(): void {
    this.selectedThread = null;
    this.messageText = '';
    this.reportForm = { ...EMPTY_REPORT_FORM };
    const base = this.isDoctor
      ? this.mode === 'messages' ? '/doctor/messages' : '/doctor/reports'
      : this.mode === 'messages' ? '/patient/messages' : '/patient/reports';
    void this.router.navigate([base]);
  }

  sendMessage(): void {
    if (!this.selectedAppointmentId || !this.messageText.trim()) {
      return;
    }

    this.isSavingMessage = true;
    this.errorMessage = '';
    this.api.sendMessage(this.selectedAppointmentId, { message: this.messageText }).subscribe({
      next: (message) => {
        this.isSavingMessage = false;
        this.messageText = '';
        this.statusMessage = 'Message sent.';
        if (this.selectedThread) {
          this.selectedThread = {
            ...this.selectedThread,
            messages: [...this.selectedThread.messages, message],
            thread: {
              ...this.selectedThread.thread,
              lastMessage: message.message,
              lastMessageAt: message.createdAt
            }
          };
        }
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingMessage = false;
        this.errorMessage = error?.error?.message || 'Unable to send message.';
      }
    });
  }

  submitReport(): void {
    if (!this.selectedAppointmentId || !this.reportForm.symptoms.trim()) {
      return;
    }

    this.isSavingReport = true;
    this.errorMessage = '';
    this.api.submitReport(this.selectedAppointmentId, {
      symptoms: this.reportForm.symptoms,
      previousReportSummary: this.normalizeOptional(this.reportForm.previousReportSummary),
      reportTitle: this.normalizeOptional(this.reportForm.reportTitle),
      reportUrl: this.normalizeOptional(this.reportForm.reportUrl),
      reportCategory: this.normalizeOptional(this.reportForm.reportCategory),
      reportNotes: this.normalizeOptional(this.reportForm.reportNotes)
    }).subscribe({
      next: (report) => {
        this.isSavingReport = false;
        this.statusMessage = 'Report submitted.';
        this.reportForm = { ...EMPTY_REPORT_FORM };
        this.addReport(report);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingReport = false;
        this.errorMessage = error?.error?.message || 'Unable to submit report.';
      }
    });
  }

  doctorWorkspaceLink(thread: CareThreadSummary): string[] {
    return ['/doctor/appointments', String(thread.appointmentId)];
  }

  formatTime(value: string): string {
    return value ? value.slice(0, 5) : '';
  }

  private syncRoute(): void {
    this.mode = this.router.url.includes('/reports') ? 'reports' : 'messages';
    const appointmentId = this.selectedAppointmentId;
    if (appointmentId) {
      this.loadThread(appointmentId);
      return;
    }

    this.selectedThread = null;
  }

  private loadThreads(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.api.getThreads().subscribe({
      next: (threads) => {
        this.threads = threads;
        this.isLoading = false;
        if (this.selectedAppointmentId) {
          this.loadThread(this.selectedAppointmentId);
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load approved appointment threads.';
      }
    });
  }

  private loadThread(appointmentId: number): void {
    this.isLoadingThread = true;
    this.errorMessage = '';
    this.api.getThread(appointmentId).subscribe({
      next: (thread) => {
        this.selectedThread = thread;
        this.isLoadingThread = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.selectedThread = null;
        this.isLoadingThread = false;
        this.errorMessage = error?.error?.message || 'Unable to load this communication thread.';
      }
    });
  }

  private addReport(report: PatientCaseReport): void {
    if (!this.selectedThread) {
      return;
    }

    this.selectedThread = {
      ...this.selectedThread,
      reports: [report, ...this.selectedThread.reports],
      thread: {
        ...this.selectedThread.thread,
        reportCount: this.selectedThread.thread.reportCount + 1
      }
    };
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }
}

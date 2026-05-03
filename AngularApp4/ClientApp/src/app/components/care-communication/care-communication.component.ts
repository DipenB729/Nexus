import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import {
  CareConversationMessage,
  CareThreadDetail,
  CareThreadSummary,
  PatientCaseReport
} from '../../core/models/hms/care-communication.model';
import { CareCommunicationService } from '../../core/services/hms/care-communication.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { CareCommunicationRealtimeService } from '../../core/services/hms/care-communication-realtime.service';
import { DoctorWorkspaceService } from '../../core/services/hms/doctor-workspace.service';
import { DiagnosticRequestRecord, DoctorConsultationRecord, PrescriptionItemRecord, PrescriptionRecord } from '../../core/models/hms/doctor-workspace.model';

type CareMode = 'messages' | 'reports';
type ReportSection = 'reports' | 'consultation' | 'prescriptions' | 'diagnostics';
type PatientReportSection = 'submitted' | 'consultation' | 'prescriptions' | 'diagnostics';

interface PatientReportFormState {
  symptoms: string;
  previousReportSummary: string;
  reportTitle: string;
  reportUrl: string;
  reportCategory: string;
  reportNotes: string;
}

interface ConsultationFormState {
  symptoms: string;
  diagnosis: string;
  notes: string;
  vitalObservations: string;
  advice: string;
  followUpDate: string;
  status: string;
}

interface PrescriptionFormState {
  notes: string;
  items: PrescriptionItemRecord[];
}

interface DiagnosticRequestFormState {
  requestType: string;
  labTestMasterId: number | null;
  requestedItemName: string;
  remarks: string;
}

const EMPTY_REPORT_FORM: PatientReportFormState = {
  symptoms: '',
  previousReportSummary: '',
  reportTitle: '',
  reportUrl: '',
  reportCategory: 'Previous report',
  reportNotes: ''
};

const EMPTY_CONSULTATION_FORM: ConsultationFormState = {
  symptoms: '',
  diagnosis: '',
  notes: '',
  vitalObservations: '',
  advice: '',
  followUpDate: '',
  status: 'Draft'
};

const EMPTY_PRESCRIPTION_ITEM: PrescriptionItemRecord = {
  medicineName: '',
  dosage: '',
  frequency: '',
  duration: '',
  instructions: ''
};

const EMPTY_PRESCRIPTION_FORM: PrescriptionFormState = {
  notes: '',
  items: [{ ...EMPTY_PRESCRIPTION_ITEM }]
};

const EMPTY_REQUEST_FORM: DiagnosticRequestFormState = {
  requestType: 'Lab',
  labTestMasterId: null,
  requestedItemName: '',
  remarks: ''
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
  reportAttachment: File | null = null;
  reportAttachmentName = '';
  consultationForm: ConsultationFormState = { ...EMPTY_CONSULTATION_FORM };
  prescriptionForm: PrescriptionFormState = { ...EMPTY_PRESCRIPTION_FORM, items: [{ ...EMPTY_PRESCRIPTION_ITEM }] };
  diagnosticRequestForm: DiagnosticRequestFormState = { ...EMPTY_REQUEST_FORM };
  mode: CareMode = 'messages';
  activeReportSection: ReportSection = 'reports';
  activeCreatePanel: Exclude<ReportSection, 'reports'> | null = null;
  activePatientReportSection: PatientReportSection = 'submitted';
  isPatientReportModalOpen = false;
  readonly reportSections: { id: ReportSection; label: string }[] = [
    { id: 'reports', label: 'Patient Reports' },
    { id: 'consultation', label: 'Consultation' },
    { id: 'prescriptions', label: 'Prescription' },
    { id: 'diagnostics', label: 'Diagnostics' }
  ];
  readonly patientReportSections: { id: PatientReportSection; label: string }[] = [
    { id: 'submitted', label: 'Submitted Reports' },
    { id: 'consultation', label: 'Consultation' },
    { id: 'prescriptions', label: 'Prescription' },
    { id: 'diagnostics', label: 'Diagnostics' }
  ];
  isLoading = true;
  isLoadingThread = false;
  isSavingMessage = false;
  isSavingReport = false;
  isSavingConsultation = false;
  isSavingPrescription = false;
  isSavingRequest = false;
  errorMessage = '';
  statusMessage = '';
  readonly consultationStatuses = ['Draft', 'Completed', 'FollowUpPlanned'];
  readonly requestTypes = ['Lab', 'Radiology'];

  private routeSub?: Subscription;
  private messageSub?: Subscription;

  constructor(
    private readonly api: CareCommunicationService,
    private readonly auth: AuthApiService,
    private readonly realtime: CareCommunicationRealtimeService,
    private readonly workspaceApi: DoctorWorkspaceService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.syncRoute();
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.syncRoute());
    this.messageSub = this.realtime.messages$.subscribe((message) => this.handleRealtimeMessage(message));
    this.realtime.start().catch(() => {
      this.errorMessage = 'Realtime chat connection is unavailable. Messages may not update live.';
    });
    this.loadThreads();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.messageSub?.unsubscribe();
    void this.realtime.stop();
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
    const text = this.messageText.trim();
    this.realtime.sendMessage(this.selectedAppointmentId, text)
      .then(() => {
        this.isSavingMessage = false;
        this.messageText = '';
        this.statusMessage = 'Message sent.';
      })
      .catch((error: { message?: string }) => {
        this.isSavingMessage = false;
        this.errorMessage = error?.message || 'Unable to send message.';
      });
  }

  submitReport(): void {
    if (!this.selectedAppointmentId || !this.reportForm.symptoms.trim()) {
      return;
    }

    this.isSavingReport = true;
    this.errorMessage = '';
    const formData = new FormData();
    formData.append('symptoms', this.reportForm.symptoms);
    this.appendOptional(formData, 'previousReportSummary', this.reportForm.previousReportSummary);
    this.appendOptional(formData, 'reportTitle', this.reportForm.reportTitle);
    this.appendOptional(formData, 'reportUrl', this.reportForm.reportUrl);
    this.appendOptional(formData, 'reportCategory', this.reportForm.reportCategory);
    this.appendOptional(formData, 'reportNotes', this.reportForm.reportNotes);
    if (this.reportAttachment) {
      formData.append('attachment', this.reportAttachment);
    }

    this.api.submitReportForm(this.selectedAppointmentId, formData).subscribe({
      next: (report) => {
        this.isSavingReport = false;
        this.statusMessage = 'Report submitted.';
        this.resetReportForm();
        this.closePatientReportModal();
        this.addReport(report);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingReport = false;
        this.errorMessage = error?.error?.message || 'Unable to submit report.';
      }
    });
  }

  saveConsultation(): void {
    if (!this.selectedAppointmentId || !this.isDoctor) {
      return;
    }

    this.isSavingConsultation = true;
    this.errorMessage = '';
    this.workspaceApi.saveConsultation(this.selectedAppointmentId, {
      symptoms: this.normalizeOptional(this.consultationForm.symptoms),
      diagnosis: this.normalizeOptional(this.consultationForm.diagnosis),
      notes: this.normalizeOptional(this.consultationForm.notes),
      vitalObservations: this.normalizeOptional(this.consultationForm.vitalObservations),
      advice: this.normalizeOptional(this.consultationForm.advice),
      followUpDate: this.consultationForm.followUpDate || null,
      status: this.consultationForm.status
    }).subscribe({
      next: (consultation) => {
        this.isSavingConsultation = false;
        this.statusMessage = 'Consultation saved.';
        this.patchConsultation(consultation);
        this.closeCreatePanel();
        this.loadThread(this.selectedAppointmentId!);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingConsultation = false;
        this.errorMessage = error?.error?.message || 'Unable to save consultation.';
      }
    });
  }

  addPrescriptionItem(): void {
    this.prescriptionForm.items = [...this.prescriptionForm.items, { ...EMPTY_PRESCRIPTION_ITEM }];
  }

  removePrescriptionItem(index: number): void {
    this.prescriptionForm.items = this.prescriptionForm.items.length > 1
      ? this.prescriptionForm.items.filter((_, itemIndex) => itemIndex !== index)
      : [{ ...EMPTY_PRESCRIPTION_ITEM }];
  }

  savePrescription(): void {
    if (!this.selectedAppointmentId || !this.isDoctor) {
      return;
    }

    this.isSavingPrescription = true;
    this.errorMessage = '';
    this.workspaceApi.savePrescription(this.selectedAppointmentId, {
      notes: this.normalizeOptional(this.prescriptionForm.notes),
      items: this.prescriptionForm.items
    }).subscribe({
      next: (prescription) => {
        this.isSavingPrescription = false;
        this.statusMessage = 'Prescription saved.';
        this.patchPrescription(prescription);
        this.prescriptionForm = { ...EMPTY_PRESCRIPTION_FORM, items: [{ ...EMPTY_PRESCRIPTION_ITEM }] };
        this.closeCreatePanel();
        this.loadThread(this.selectedAppointmentId!);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingPrescription = false;
        this.errorMessage = error?.error?.message || 'Unable to save prescription.';
      }
    });
  }

  saveDiagnosticRequest(): void {
    if (!this.selectedAppointmentId || !this.isDoctor) {
      return;
    }

    this.isSavingRequest = true;
    this.errorMessage = '';
    this.workspaceApi.saveDiagnosticRequest(this.selectedAppointmentId, {
      requestType: this.diagnosticRequestForm.requestType,
      labTestMasterId: this.diagnosticRequestForm.requestType === 'Lab' ? this.diagnosticRequestForm.labTestMasterId : null,
      requestedItemName: this.diagnosticRequestForm.requestedItemName,
      remarks: this.normalizeOptional(this.diagnosticRequestForm.remarks)
    }).subscribe({
      next: (request) => {
        this.isSavingRequest = false;
        this.statusMessage = `${request.requestType} request saved.`;
        this.patchDiagnosticRequest(request);
        this.diagnosticRequestForm = { ...EMPTY_REQUEST_FORM };
        this.closeCreatePanel();
        this.loadThread(this.selectedAppointmentId!);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingRequest = false;
        this.errorMessage = error?.error?.message || 'Unable to save diagnostic request.';
      }
    });
  }

  doctorWorkspaceLink(thread: CareThreadSummary): string[] {
    return ['/doctor/appointments', String(thread.appointmentId)];
  }

  setReportSection(section: ReportSection): void {
    this.activeReportSection = section;
    this.activeCreatePanel = null;
  }

  setPatientReportSection(section: PatientReportSection): void {
    this.activePatientReportSection = section;
  }

  openPatientReportModal(): void {
    this.isPatientReportModalOpen = true;
  }

  closePatientReportModal(): void {
    this.isPatientReportModalOpen = false;
  }

  selectReportAttachment(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.reportAttachment = file;
    this.reportAttachmentName = file?.name ?? '';
  }

  openCreatePanel(section: Exclude<ReportSection, 'reports'>): void {
    this.activeCreatePanel = section;
  }

  closeCreatePanel(): void {
    this.activeCreatePanel = null;
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
        this.syncDoctorForms(thread);
        this.realtime.joinThread(appointmentId).catch(() => {
          this.errorMessage = 'Realtime chat connection is unavailable. Messages may not update live.';
        });
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

  private resetReportForm(): void {
    this.reportForm = { ...EMPTY_REPORT_FORM };
    this.reportAttachment = null;
    this.reportAttachmentName = '';
  }

  private appendOptional(formData: FormData, key: string, value: string): void {
    const normalized = this.normalizeOptional(value);
    if (normalized) {
      formData.append(key, normalized);
    }
  }

  private patchConsultation(consultation: DoctorConsultationRecord): void {
    if (!this.selectedThread) {
      return;
    }

    this.selectedThread = {
      ...this.selectedThread,
      consultation
    };
  }

  private patchPrescription(prescription: PrescriptionRecord): void {
    if (!this.selectedThread) {
      return;
    }

    this.selectedThread = {
      ...this.selectedThread,
      prescriptions: [prescription, ...this.selectedThread.prescriptions]
    };
  }

  private patchDiagnosticRequest(request: DiagnosticRequestRecord): void {
    if (!this.selectedThread) {
      return;
    }

    this.selectedThread = {
      ...this.selectedThread,
      diagnosticRequests: [request, ...this.selectedThread.diagnosticRequests]
    };
  }

  private syncDoctorForms(thread: CareThreadDetail): void {
    this.consultationForm = {
      symptoms: thread.consultation.symptoms || '',
      diagnosis: thread.consultation.diagnosis || '',
      notes: thread.consultation.notes || '',
      vitalObservations: thread.consultation.vitalObservations || '',
      advice: thread.consultation.advice || '',
      followUpDate: thread.consultation.followUpDate?.slice(0, 10) || '',
      status: thread.consultation.status || 'Draft'
    };
  }

  private handleRealtimeMessage(message: CareConversationMessage): void {
    this.threads = this.threads.map((thread) =>
      thread.appointmentId === message.appointmentId
        ? { ...thread, lastMessage: message.message, lastMessageAt: message.createdAt }
        : thread);

    if (!this.selectedThread || this.selectedThread.thread.appointmentId !== message.appointmentId) {
      return;
    }

    if (this.selectedThread.messages.some((item) => item.careConversationMessageId === message.careConversationMessageId)) {
      return;
    }

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

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }
}

import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { CareConversationMessage, CareThreadDetail, CareThreadSummary } from '../../core/models/hms/care-communication.model';
import { CareCommunicationService } from '../../core/services/hms/care-communication.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { CareCommunicationRealtimeService } from '../../core/services/hms/care-communication-realtime.service';

@Component({
  selector: 'app-floating-chat',
  templateUrl: './floating-chat.component.html',
  styleUrls: ['./floating-chat.component.scss']
})
export class FloatingChatComponent implements OnInit, OnDestroy {
  threads: CareThreadSummary[] = [];
  selectedThread: CareThreadDetail | null = null;
  isOpen = false;
  isLoading = false;
  isSending = false;
  messageText = '';
  errorMessage = '';

  private sessionSub?: Subscription;
  private messageSub?: Subscription;

  constructor(
    private readonly api: CareCommunicationService,
    private readonly auth: AuthApiService,
    private readonly realtime: CareCommunicationRealtimeService
  ) {}

  ngOnInit(): void {
    this.loadThreads();
    this.sessionSub = this.auth.session$.subscribe(() => this.loadThreads());
    this.messageSub = this.realtime.messages$.subscribe((message) => this.handleRealtimeMessage(message));
    this.realtime.start().catch(() => {
      this.errorMessage = 'Chat connection is unavailable.';
    });
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
    this.messageSub?.unsubscribe();
    void this.realtime.stop();
  }

  get isDoctor(): boolean {
    return this.auth.getDisplayRole(this.auth.getSession()?.role) === 'Doctor';
  }

  get unreadLabel(): string {
    return this.threads.length > 9 ? '9+' : String(this.threads.length);
  }

  get panelTitle(): string {
    return this.isDoctor ? 'Patient chats' : 'Doctor chats';
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && !this.selectedThread && this.threads.length > 0) {
      this.openThread(this.threads[0].appointmentId);
    }
  }

  openThread(appointmentId: number): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.api.getThread(appointmentId).subscribe({
      next: (thread) => {
        this.selectedThread = thread;
        this.isLoading = false;
        this.realtime.joinThread(appointmentId).catch(() => {
          this.errorMessage = 'Chat connection is unavailable.';
        });
      },
      error: () => {
        this.selectedThread = null;
        this.isLoading = false;
        this.errorMessage = 'Unable to open this chat.';
      }
    });
  }

  sendMessage(): void {
    const appointmentId = this.selectedThread?.thread.appointmentId;
    const message = this.messageText.trim();
    if (!appointmentId || !message) {
      return;
    }

    this.isSending = true;
    this.errorMessage = '';
    this.realtime.sendMessage(appointmentId, message)
      .then(() => {
        this.isSending = false;
        this.messageText = '';
      })
      .catch(() => {
        this.isSending = false;
        this.errorMessage = 'Unable to send message.';
      });
  }

  participantName(thread: CareThreadSummary): string {
    return this.isDoctor ? thread.patientName : thread.doctorName;
  }

  participantMeta(thread: CareThreadSummary): string {
    return this.isDoctor
      ? thread.medicalRecordNumber || 'MRN pending'
      : thread.doctorSpecialization || 'General practice';
  }

  participantInitial(thread: CareThreadSummary): string {
    return this.participantName(thread).trim().charAt(0).toUpperCase() || 'C';
  }

  isOwnMessage(message: CareConversationMessage): boolean {
    return this.isDoctor ? message.senderRole === 'Doctor' : message.senderRole === 'Patient';
  }

  private loadThreads(showLoading = true): void {
    if (showLoading) {
      this.isLoading = true;
    }

    this.api.getThreads().subscribe({
      next: (threads) => {
        this.threads = threads;
        this.isLoading = false;
      },
      error: () => {
        this.threads = [];
        this.isLoading = false;
      }
    });
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
}

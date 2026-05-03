import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { CareThreadDetail, CareThreadSummary } from '../../core/models/hms/care-communication.model';
import { CareCommunicationService } from '../../core/services/hms/care-communication.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

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

  constructor(
    private readonly api: CareCommunicationService,
    private readonly auth: AuthApiService
  ) {}

  ngOnInit(): void {
    this.loadThreads();
    this.sessionSub = this.auth.session$.subscribe(() => this.loadThreads());
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
  }

  get isDoctor(): boolean {
    return this.auth.getDisplayRole(this.auth.getSession()?.role) === 'Doctor';
  }

  get unreadLabel(): string {
    return this.threads.length > 9 ? '9+' : String(this.threads.length);
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
    this.api.sendMessage(appointmentId, { message }).subscribe({
      next: (saved) => {
        this.isSending = false;
        this.messageText = '';
        if (this.selectedThread) {
          this.selectedThread = {
            ...this.selectedThread,
            messages: [...this.selectedThread.messages, saved],
            thread: {
              ...this.selectedThread.thread,
              lastMessage: saved.message,
              lastMessageAt: saved.createdAt
            }
          };
        }
        this.loadThreads(false);
      },
      error: () => {
        this.isSending = false;
        this.errorMessage = 'Unable to send message.';
      }
    });
  }

  participantName(thread: CareThreadSummary): string {
    return this.isDoctor ? thread.patientName : thread.doctorName;
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
}

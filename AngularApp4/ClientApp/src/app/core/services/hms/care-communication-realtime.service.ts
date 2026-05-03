import { Injectable, NgZone } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CareConversationMessage } from '../../models/hms/care-communication.model';
import { AuthApiService } from './auth-api.service';

@Injectable({ providedIn: 'root' })
export class CareCommunicationRealtimeService {
  private connection?: HubConnection;
  private startPromise?: Promise<void>;
  private readonly messageSubject = new Subject<CareConversationMessage>();

  readonly messages$: Observable<CareConversationMessage> = this.messageSubject.asObservable();

  constructor(
    private readonly auth: AuthApiService,
    private readonly zone: NgZone
  ) {}

  start(): Promise<void> {
    const token = this.auth.getToken();
    if (!token) {
      return Promise.resolve();
    }

    if (this.connection?.state === HubConnectionState.Connected) {
      return Promise.resolve();
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    const hubUrl = `${environment.apiOrigin}/hubs/care-communication`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('careMessageReceived', (message: CareConversationMessage) => {
      this.zone.run(() => this.messageSubject.next(message));
    });

    this.startPromise = this.connection.start()
      .catch((error) => {
        this.startPromise = undefined;
        throw error;
      });

    return this.startPromise;
  }

  async joinThread(appointmentId: number): Promise<void> {
    await this.start();
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinThread', appointmentId);
    }
  }

  async sendMessage(appointmentId: number, message: string): Promise<CareConversationMessage> {
    await this.start();
    if (this.connection?.state !== HubConnectionState.Connected) {
      throw new Error('Realtime chat is not connected.');
    }

    return this.connection.invoke<CareConversationMessage>('SendMessage', appointmentId, message);
  }

  async stop(): Promise<void> {
    this.startPromise = undefined;
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }
}

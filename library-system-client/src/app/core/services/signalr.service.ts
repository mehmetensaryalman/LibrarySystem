import { Injectable } from '@angular/core';

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';

import {
  Observable,
  Subject
} from 'rxjs';

import {
  AuthService
} from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private readonly hubUrl =
    'https://localhost:7008/hubs/library';

  private readonly booksChangedSubject =
    new Subject<void>();

  private readonly borrowsChangedSubject =
    new Subject<void>();

  readonly booksChanged$: Observable<void> =
    this.booksChangedSubject
      .asObservable();

  readonly borrowsChanged$: Observable<void> =
    this.borrowsChangedSubject
      .asObservable();

  private readonly hubConnection:
    HubConnection;

  constructor(
    private readonly authService:
      AuthService
  ) {
    this.hubConnection =
      new HubConnectionBuilder()
        .withUrl(
          this.hubUrl,
          {
            accessTokenFactory: () =>
              this.authService
                .getToken() ?? ''
          }
        )
        .withAutomaticReconnect([
          0,
          2000,
          5000,
          10000
        ])
        .configureLogging(
          LogLevel.Information
        )
        .build();

    this.registerServerEvents();

    this.registerConnectionEvents();

    this.authService
      .logout$
      .subscribe(() => {
        void this.stopConnection();
      });
  }

  async startConnection():
    Promise<void> {

    if (
      !this.authService
        .isLoggedIn()
    ) {
      return;
    }

    if (
      this.hubConnection.state !==
      HubConnectionState.Disconnected
    ) {
      return;
    }

    try {
      await this.hubConnection
        .start();

      console.log(
        'SignalR bağlantısı kuruldu.'
      );
    } catch (error) {
      console.error(
        'SignalR bağlantısı kurulamadı:',
        error
      );
    }
  }

  async stopConnection():
    Promise<void> {

    if (
      this.hubConnection.state ===
      HubConnectionState.Disconnected
    ) {
      return;
    }

    try {
      await this.hubConnection
        .stop();
    } catch (error) {
      console.error(
        'SignalR bağlantısı durdurulamadı:',
        error
      );
    }
  }

  private registerServerEvents():
    void {

    this.hubConnection.on(
      'BooksChanged',
      () => {
        console.log(
          'SignalR: BooksChanged alındı.'
        );

        this.booksChangedSubject
          .next();
      }
    );

    this.hubConnection.on(
      'BorrowsChanged',
      () => {
        console.log(
          'SignalR: BorrowsChanged alındı.'
        );

        this.borrowsChangedSubject
          .next();
      }
    );
  }

  private registerConnectionEvents():
    void {

    this.hubConnection
      .onreconnecting(
        error => {
          if (error) {
            console.warn(
              'SignalR bağlantısı yeniden kuruluyor:',
              error
            );
            return;
          }

          console.log(
            'SignalR bağlantısı yeniden kuruluyor.'
          );
        }
      );

    this.hubConnection
      .onreconnected(
        connectionId => {
          console.log(
            'SignalR bağlantısı yeniden kuruldu.',
            connectionId
          );
        }
      );

    this.hubConnection
      .onclose(
        error => {
          if (error) {
            console.warn(
              'SignalR bağlantısı hata nedeniyle kapandı:',
              error
            );
            return;
          }

          console.log(
            'SignalR bağlantısı normal şekilde kapandı.'
          );
        }
      );
  }
}
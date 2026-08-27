import {
  DatePipe
} from '@angular/common';

import {
  Component,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';

import {
  RouterLink
} from '@angular/router';

import {
  ConfirmationService
} from 'primeng/api';

import {
  ButtonModule
} from 'primeng/button';

import {
  ConfirmDialogModule
} from 'primeng/confirmdialog';

import {
  TelegramConnectionLink,
  TelegramConnectionStatus
} from '../../../core/models/telegram-notification.models';

import {
  TelegramNotificationService
} from '../../../core/services/telegram-notification.service';

@Component({
  selector:
    'app-telegram-notifications',

  imports: [
    DatePipe,
    RouterLink,
    ButtonModule,
    ConfirmDialogModule
  ],

  providers: [
    ConfirmationService
  ],

  templateUrl:
    './telegram-notifications.component.html',

  styleUrl:
    './telegram-notifications.component.scss'
})
export class TelegramNotificationsComponent
  implements OnInit, OnDestroy {

  readonly loading =
    signal(true);

  readonly actionLoading =
    signal(false);

  readonly checkingConnection =
    signal(false);

  readonly status =
    signal<
      TelegramConnectionStatus |
      null
    >(null);

  readonly connectionLink =
    signal<
      TelegramConnectionLink |
      null
    >(null);

  readonly connectionCommand =
    signal('');

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  readonly commandCopied =
    signal(false);

  private pollingTimer:
    ReturnType<typeof setInterval> |
    null = null;

  private copiedMessageTimer:
    ReturnType<typeof setTimeout> |
    null = null;

  private pollingAttemptCount = 0;

  constructor(
    private readonly telegramNotificationService:
      TelegramNotificationService,

    private readonly confirmationService:
      ConfirmationService
  ) {
  }

  ngOnInit(): void {
    this.loadStatus();
  }

  loadStatus(): void {
    this.loading.set(true);

    this.errorMessage.set('');

    this.telegramNotificationService
      .getStatus()
      .subscribe({
        next: result => {
          this.loading.set(false);

          this.status.set(result);

          if (result.isConnected) {
            this.clearConnectionData();
          }
        },

        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Telegram bağlantı durumu alınamadı.'
          );
        }
      });
  }

  createConnectionLink(): void {
    this.actionLoading.set(true);

    this.errorMessage.set('');

    this.successMessage.set('');

    this.telegramNotificationService
      .createConnectionLink()
      .subscribe({
        next: result => {
          this.actionLoading.set(false);

          const connectionCode =
            this.extractConnectionCode(
              result.connectionUrl
            );

          if (!connectionCode) {
            this.errorMessage.set(
              'Telegram bağlantı kodu oluşturulamadı.'
            );

            return;
          }

          this.connectionLink.set(
            result
          );

          this.connectionCommand.set(
            `/start ${connectionCode}`
          );

          this.successMessage.set(
            'Bağlantı kodu oluşturuldu. Kodu 10 dakika içinde Telegram botuna gönderin.'
          );

          this.startStatusPolling();
        },

        error: error => {
          this.actionLoading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Telegram bağlantısı oluşturulamadı.'
          );
        }
      });
  }

  openTelegram(): void {
    const link =
      this.connectionLink();

    if (!link) {
      return;
    }

    window.open(
      link.connectionUrl,
      '_blank',
      'noopener,noreferrer'
    );
  }

  async copyCommand(): Promise<void> {
    const command =
      this.connectionCommand();

    if (!command) {
      return;
    }

    try {
      await navigator.clipboard
        .writeText(command);

      this.commandCopied.set(true);

      this.successMessage.set(
        'Telegram komutu panoya kopyalandı.'
      );

      if (
        this.copiedMessageTimer !==
        null
      ) {
        clearTimeout(
          this.copiedMessageTimer
        );
      }

      this.copiedMessageTimer =
        setTimeout(
          () => {
            this.commandCopied.set(
              false
            );
          },
          2200
        );
    }
    catch {
      this.errorMessage.set(
        'Komut panoya kopyalanamadı. Komutu seçip elle kopyalayabilirsiniz.'
      );
    }
  }

  checkConnection(): void {
    this.checkingConnection.set(true);

    this.errorMessage.set('');

    this.telegramNotificationService
      .getStatus()
      .subscribe({
        next: result => {
          this.checkingConnection.set(
            false
          );

          this.status.set(result);

          if (result.isConnected) {
            this.successMessage.set(
              'Telegram hesabınız başarıyla bağlandı.'
            );

            this.clearConnectionData();

            return;
          }

          this.successMessage.set(
            'Bağlantı henüz tamamlanmadı. Telegram botuna oluşturulan komutu gönderin.'
          );
        },

        error: error => {
          this.checkingConnection.set(
            false
          );

          this.errorMessage.set(
            error.error?.message ??
            'Telegram bağlantısı kontrol edilemedi.'
          );
        }
      });
  }

  confirmDisconnect(): void {
    this.confirmationService.confirm({
      header:
        'Telegram Bağlantısını Kaldır',

      message:
        'Telegram bağlantısını kaldırmak istediğinize emin misiniz? Bildirimler artık Telegram üzerinden gönderilmeyecektir.',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Bağlantıyı Kaldır',

      rejectLabel:
        'Vazgeç',

      acceptButtonStyleClass:
        'p-button-danger',

      rejectButtonStyleClass:
        'reject-cancel-button',

      defaultFocus:
        'accept',

      accept: () =>
        this.disconnect()
    });
  }

  ngOnDestroy(): void {
    this.stopStatusPolling();

    if (
      this.copiedMessageTimer !==
      null
    ) {
      clearTimeout(
        this.copiedMessageTimer
      );
    }
  }

  private disconnect(): void {
    this.actionLoading.set(true);

    this.errorMessage.set('');

    this.successMessage.set('');

    this.telegramNotificationService
      .disconnect()
      .subscribe({
        next: () => {
          this.actionLoading.set(false);

          this.status.set({
            isConnected: false,
            isEnabled: false,
            telegramUsername: null,
            connectedAt: null
          });

          this.clearConnectionData();

          this.successMessage.set(
            'Telegram bağlantısı kaldırıldı.'
          );
        },

        error: error => {
          this.actionLoading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Telegram bağlantısı kaldırılamadı.'
          );
        }
      });
  }

  private startStatusPolling(): void {
    this.stopStatusPolling();

    this.pollingAttemptCount = 0;

    this.pollingTimer =
      setInterval(
        () => {
          this.pollingAttemptCount += 1;

          if (
            this.pollingAttemptCount > 24
          ) {
            this.stopStatusPolling();

            return;
          }

          this.pollStatus();
        },
        2500
      );
  }

  private pollStatus(): void {
    this.telegramNotificationService
      .getStatus()
      .subscribe({
        next: result => {
          this.status.set(result);

          if (!result.isConnected) {
            return;
          }

          this.successMessage.set(
            'Telegram hesabınız başarıyla bağlandı.'
          );

          this.clearConnectionData();
        }
      });
  }

  private clearConnectionData(): void {
    this.stopStatusPolling();

    this.connectionLink.set(null);

    this.connectionCommand.set('');
  }

  private stopStatusPolling(): void {
    if (this.pollingTimer === null) {
      return;
    }

    clearInterval(
      this.pollingTimer
    );

    this.pollingTimer = null;
  }

  private extractConnectionCode(
    connectionUrl: string
  ): string {
    try {
      return new URL(
        connectionUrl
      ).searchParams.get('start') ?? '';
    }
    catch {
      return '';
    }
  }
}

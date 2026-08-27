import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  TelegramConnectionLink,
  TelegramConnectionStatus
} from '../models/telegram-notification.models';

@Injectable({
  providedIn: 'root'
})
export class TelegramNotificationService {
  private readonly apiUrl =
    'https://localhost:7008/api/telegram-notifications';

  constructor(
    private readonly http:
      HttpClient
  ) {
  }

  getStatus(): Observable<
    TelegramConnectionStatus
  > {
    return this.http.get<
      TelegramConnectionStatus
    >(
      `${this.apiUrl}/status`
    );
  }

  createConnectionLink(): Observable<
    TelegramConnectionLink
  > {
    return this.http.post<
      TelegramConnectionLink
    >(
      `${this.apiUrl}/connection-link`,
      null
    );
  }

  disconnect(): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/connection`
    );
  }
}

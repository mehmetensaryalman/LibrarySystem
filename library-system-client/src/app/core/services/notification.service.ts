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
  NotificationSummary
} from '../models/notification.models';

export interface DeleteReadNotificationsResponse {
  deletedCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly apiUrl =
    'https://localhost:7008/api/admin/notifications';

  constructor(
    private readonly http:
      HttpClient
  ) {
  }

  getSummary():
    Observable<NotificationSummary> {

    return this.http
      .get<NotificationSummary>(
        this.apiUrl
      );
  }

  markAsRead(
    notificationId: number
  ): Observable<void> {

    return this.http
      .put<void>(
        `${this.apiUrl}/${notificationId}/read`,
        {}
      );
  }

  markAllAsRead():
    Observable<void> {

    return this.http
      .put<void>(
        `${this.apiUrl}/read-all`,
        {}
      );
  }

  deleteReadNotifications():
    Observable<DeleteReadNotificationsResponse> {

    return this.http
      .delete<DeleteReadNotificationsResponse>(
        `${this.apiUrl}/read`
      );
  }
}
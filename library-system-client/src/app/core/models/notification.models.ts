export interface AdminNotification {
  id: number;
  type: string;
  title: string;
  message: string;
  borrowRecordId: number | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

export interface NotificationSummary {
  unreadCount: number;
  notifications: AdminNotification[];
}
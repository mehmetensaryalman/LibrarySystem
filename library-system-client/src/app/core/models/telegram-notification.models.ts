export interface TelegramConnectionStatus {
  isConnected: boolean;

  isEnabled: boolean;

  telegramUsername:
    string | null;

  connectedAt:
    string | null;
}

export interface TelegramConnectionLink {
  connectionUrl: string;

  expiresAt: string;
}

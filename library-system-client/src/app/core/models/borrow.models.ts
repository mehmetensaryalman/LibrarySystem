export interface OperationResult {
  success: boolean;
  message: string;
  penaltyEndDate?: string | null;
}

export interface BorrowedBook {
  borrowRecordId: number;

  bookId: number;
  bookName: string;
  author: string;

  borrowDate: string;
  dueDate: string;
  returnDate: string | null;

  isReturned: boolean;
}

export interface AdminBorrow {
  borrowRecordId: number;

  userId: string;
  userEmail: string;

  bookId: number;
  bookName: string;
  author: string;

  borrowDate: string;
  dueDate: string;
  returnDate: string | null;

  isReturned: boolean;
}

export interface AdminBorrowNotification {
  bookId: number;
  bookName: string;
  userEmail: string;
  borrowDate: string;
}

export interface BorrowPenaltyStatus {
  hasOverdueBorrow: boolean;
  hasActivePenalty: boolean;
  penaltyEndDate: string | null;
}
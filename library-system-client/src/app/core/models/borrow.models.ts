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
  returnRequestedAt: string | null;
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
  returnRequestedAt: string | null;
  returnDate: string | null;
  returnedToAdminUserId: string | null;
  isReturned: boolean;
}

export interface MyBorrowRequest {
  borrowRequestId: number;
  bookId: number;
  bookName: string;
  author: string;
  requestedAt: string;
}

export interface AdminBorrowRequest {
  borrowRequestId: number;
  userId: string;
  userEmail: string;
  bookId: number;
  bookName: string;
  author: string;
  requestedAt: string;
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
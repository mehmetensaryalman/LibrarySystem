export interface OperationResult {
  success: boolean;
  message: string;
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
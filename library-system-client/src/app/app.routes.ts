import {
  Routes
} from '@angular/router';

import {
  authGuard
} from './core/guards/auth.guard';

import {
  adminGuard
} from './core/guards/admin.guard';

import {
  LoginComponent
} from './pages/auth/login/login.component';

import {
  RegisterComponent
} from './pages/auth/register/register.component';

import {
  ForgotPasswordComponent
} from './pages/auth/forgot-password/forgot-password.component';

import {
  ResetPasswordComponent
} from './pages/auth/reset-password/reset-password.component';

import {
  ChangePasswordComponent
} from './pages/auth/change-password/change-password.component';

import {
  TelegramNotificationsComponent
} from './pages/account/telegram-notifications/telegram-notifications.component';

import {
  BookListComponent
} from './pages/books/book-list/book-list.component';

import {
  MyBooksComponent
} from './pages/my-books/my-books.component';

import {
  AdminDashboardComponent
} from './pages/admin/admin-dashboard/admin-dashboard.component';

import {
  BorrowManagementComponent
} from './pages/admin/borrow-management/borrow-management.component';

import {
  ArchivedBooksComponent
} from './pages/admin/archived-books/archived-books.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'books',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'forgot-password',
    component: ForgotPasswordComponent
  },
  {
    path: 'reset-password',
    component: ResetPasswordComponent
  },
  {
    path: 'change-password',
    component: ChangePasswordComponent,
    canActivate: [
      authGuard
    ]
  },
  {
    path: 'telegram-notifications',
    component:
      TelegramNotificationsComponent,
    canActivate: [
      authGuard
    ]
  },
  {
    path: 'books',
    component: BookListComponent,
    canActivate: [
      authGuard
    ]
  },
  {
    path: 'my-books',
    component: MyBooksComponent,
    canActivate: [
      authGuard
    ]
  },
  {
    path: 'admin',
    component: AdminDashboardComponent,
    canActivate: [
      authGuard,
      adminGuard
    ]
  },
  {
    path: 'admin/borrows',
    component: BorrowManagementComponent,
    canActivate: [
      authGuard,
      adminGuard
    ]
  },
  {
    path: 'admin/archived-books',
    component: ArchivedBooksComponent,
    canActivate: [
      authGuard,
      adminGuard
    ]
  },
  {
    path: '**',
    redirectTo: 'books'
  }
];

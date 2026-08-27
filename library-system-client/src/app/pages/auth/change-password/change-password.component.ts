import {
  Component,
  OnDestroy,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  ButtonModule
} from 'primeng/button';

import {
  PasswordModule
} from 'primeng/password';

import {
  AccountService
} from '../../../core/services/account.service';

import {
  AuthService
} from '../../../core/services/auth.service';

@Component({
  selector: 'app-change-password',

  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    PasswordModule
  ],

  templateUrl:
    './change-password.component.html',

  styleUrl:
    './change-password.component.scss'
})
export class ChangePasswordComponent
  implements OnDestroy {

  currentPassword = '';

  newPassword = '';

  confirmNewPassword = '';

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  private redirectTimer:
    ReturnType<typeof setTimeout> |
    null = null;

  constructor(
    private readonly accountService:
      AccountService,

    private readonly authService:
      AuthService,

    private readonly router:
      Router
  ) {
  }

  changePassword(): void {
    this.errorMessage.set('');

    this.successMessage.set('');

    if (!this.currentPassword) {
      this.errorMessage.set(
        'Mevcut parolanızı girin.'
      );

      return;
    }

    if (!this.newPassword) {
      this.errorMessage.set(
        'Yeni parolanızı girin.'
      );

      return;
    }

    if (
      this.newPassword.length < 6
    ) {
      this.errorMessage.set(
        'Yeni parola en az 6 karakter olmalıdır.'
      );

      return;
    }

    if (!this.confirmNewPassword) {
      this.errorMessage.set(
        'Yeni parola tekrarını girin.'
      );

      return;
    }

    if (
      this.newPassword !==
      this.confirmNewPassword
    ) {
      this.errorMessage.set(
        'Yeni parola ile parola tekrarı uyuşmuyor.'
      );

      return;
    }

    if (
      this.currentPassword ===
      this.newPassword
    ) {
      this.errorMessage.set(
        'Yeni parola mevcut parolanızdan farklı olmalıdır.'
      );

      return;
    }

    this.loading.set(true);

    this.accountService
      .changePassword({
        currentPassword:
          this.currentPassword,

        newPassword:
          this.newPassword,

        confirmNewPassword:
          this.confirmNewPassword
      })
      .subscribe({
        next: result => {
          this.loading.set(false);

          if (!result.success) {
            this.errorMessage.set(
              result.message
            );

            return;
          }

          this.currentPassword = '';

          this.newPassword = '';

          this.confirmNewPassword = '';

          this.successMessage.set(
            result.message
          );

          this.redirectTimer =
            setTimeout(
              () => {
                this.authService.logout();

                void this.router.navigate(
                  ['/login']
                );
              },
              1800
            );
        },

        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Parola değiştirilirken bir hata oluştu.'
          );
        }
      });
  }

  ngOnDestroy(): void {
    if (
      this.redirectTimer !== null
    ) {
      clearTimeout(
        this.redirectTimer
      );
    }
  }
}
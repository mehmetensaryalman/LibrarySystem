import {
  Component,
  OnInit,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import {
  ButtonModule
} from 'primeng/button';

import {
  PasswordModule
} from 'primeng/password';

import {
  AuthService
} from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',

  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    PasswordModule
  ],

  templateUrl:
    './reset-password.component.html',

  styleUrl:
    './reset-password.component.scss'
})
export class ResetPasswordComponent
  implements OnInit {

  newPassword = '';

  confirmPassword = '';

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  readonly invalidLink =
    signal(false);

  private email = '';

  private token = '';

  constructor(
    private readonly route:
      ActivatedRoute,

    private readonly authService:
      AuthService
  ) {
  }

  ngOnInit(): void {
    this.email =
      this.route.snapshot
        .queryParamMap
        .get('email')
        ?.trim() ?? '';

    this.token =
      this.route.snapshot
        .queryParamMap
        .get('token') ?? '';

    if (
      !this.email ||
      !this.token
    ) {
      this.invalidLink.set(true);

      this.errorMessage.set(
        'Parola sıfırlama bağlantısı eksik veya geçersiz.'
      );
    }
  }

  resetPassword(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.invalidLink()) {
      this.errorMessage.set(
        'Parola sıfırlama bağlantısı eksik veya geçersiz.'
      );

      return;
    }

    if (!this.newPassword) {
      this.errorMessage.set(
        'Yeni parola zorunludur.'
      );

      return;
    }

    if (this.newPassword.length < 6) {
      this.errorMessage.set(
        'Yeni parola en az 6 karakter olmalıdır.'
      );

      return;
    }

    if (
      !/[A-ZÇĞİÖŞÜ]/
        .test(this.newPassword)
    ) {
      this.errorMessage.set(
        'Yeni parola en az bir büyük harf içermelidir.'
      );

      return;
    }

    if (
      !/[a-zçğıöşü]/
        .test(this.newPassword)
    ) {
      this.errorMessage.set(
        'Yeni parola en az bir küçük harf içermelidir.'
      );

      return;
    }

    if (
      !/[0-9]/
        .test(this.newPassword)
    ) {
      this.errorMessage.set(
        'Yeni parola en az bir rakam içermelidir.'
      );

      return;
    }

    if (!this.confirmPassword) {
      this.errorMessage.set(
        'Yeni parola tekrarı zorunludur.'
      );

      return;
    }

    if (
      this.newPassword !==
      this.confirmPassword
    ) {
      this.errorMessage.set(
        'Yeni parola ve parola tekrarı eşleşmiyor.'
      );

      return;
    }

    this.loading.set(true);

    this.authService
      .resetPassword({
        email:
          this.email,

        token:
          this.token,

        newPassword:
          this.newPassword,

        confirmPassword:
          this.confirmPassword
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

          this.authService.logout();

          this.successMessage.set(
            result.message
          );

          this.newPassword = '';
          this.confirmPassword = '';
        },

        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Parola yenilenemedi. Bağlantının süresi dolmuş olabilir.'
          );
        }
      });
  }
}
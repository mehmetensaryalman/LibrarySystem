import {
  Component,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  RouterLink
} from '@angular/router';

import {
  ButtonModule
} from 'primeng/button';

import {
  InputTextModule
} from 'primeng/inputtext';

import {
  AuthService
} from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',

  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule
  ],

  templateUrl:
    './forgot-password.component.html',

  styleUrl:
    './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  email = '';

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  constructor(
    private readonly authService:
      AuthService
  ) {
  }

  requestPasswordReset(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    const email =
      this.email.trim();

    if (!email) {
      this.errorMessage.set(
        'E-posta adresi zorunludur.'
      );

      return;
    }

    if (!this.isValidEmail(email)) {
      this.errorMessage.set(
        'Geçerli bir e-posta adresi girin.'
      );

      return;
    }

    this.loading.set(true);

    this.authService
      .forgotPassword({
        email
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

          this.successMessage.set(
            result.message
          );
        },

        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Parola sıfırlama isteği gönderilemedi.'
          );
        }
      });
  }

  private isValidEmail(
    email: string
  ): boolean {

    const emailPattern =
      /^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$/;

    return emailPattern.test(
      email
    );
  }
}
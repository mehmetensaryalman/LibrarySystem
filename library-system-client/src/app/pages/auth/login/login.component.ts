import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    PasswordModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = '';
  password = '';

  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
  }

  login(): void {
    this.errorMessage.set('');

    const email = this.email.trim();

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

    if (!this.password) {
      this.errorMessage.set(
        'Şifre zorunludur.'
      );
      return;
    }

    this.loading.set(true);

    this.authService
      .login({
        email,
        password: this.password
      })
      .subscribe({
        next: result => {
          this.loading.set(false);

          if (result.success) {
            this.router.navigate(['/books']);
            return;
          }

          this.errorMessage.set(result.message);
        },
        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'E-posta veya şifre hatalı.'
          );
        }
      });
  }

  private isValidEmail(email: string): boolean {
    const emailPattern =
      /^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$/;

    return emailPattern.test(email);
  }
}
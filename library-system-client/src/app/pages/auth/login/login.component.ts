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

  if (!this.email || !this.password) {
    this.errorMessage.set(
      'E-posta ve parola zorunludur.'
    );
    return;
  }

  this.loading.set(true);

  this.authService
    .login({
      email: this.email,
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
          'Giriş sırasında bir hata oluştu.'
        );
      }
    });
  }
}
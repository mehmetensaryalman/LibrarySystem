import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    PasswordModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  email = '';
  password = '';

  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private readonly authService: AuthService
  ) {
  }

  register(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.email || !this.password) {
      this.errorMessage.set(
        'E-posta ve parola zorunludur.'
      );
      return;
    }

    this.loading.set(true);

    this.authService
      .register({
        email: this.email,
        password: this.password
      })
      .subscribe({
        next: result => {
          this.loading.set(false);

          if (!result.success) {
            this.errorMessage.set(result.message);
            return;
          }

          this.successMessage.set(
            result.message ||
            'Kullanıcı başarıyla kaydedildi.'
          );

          this.email = '';
          this.password = '';
        },
        error: error => {
          this.loading.set(false);

          this.errorMessage.set(
            error.error?.message ??
            'Kayıt sırasında bir hata oluştu.'
          );
        }
      });
  }
}
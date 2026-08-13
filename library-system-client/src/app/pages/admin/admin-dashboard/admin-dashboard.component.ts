import { Component } from '@angular/core';
import { Router } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import {
  AuthService
} from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-dashboard',

  imports: [
    ButtonModule
  ],

  templateUrl:
    './admin-dashboard.component.html',

  styleUrl:
    './admin-dashboard.component.scss'
})
export class AdminDashboardComponent {

  constructor(
    private readonly router:
      Router,

    private readonly authService:
      AuthService
  ) {
  }

  goToBorrowManagement(): void {
    this.router.navigate([
      '/admin/borrows'
    ]);
  }

  goToArchiveManagement(): void {
    this.router.navigate([
      '/admin/archived-books'
    ]);
  }

  goToBooks(): void {
    this.router.navigate([
      '/books'
    ]);
  }

  logout(): void {
    this.authService.logout();

    this.router.navigate([
      '/login'
    ]);
  }
}
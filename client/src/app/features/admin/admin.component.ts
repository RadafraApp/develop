import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AdminAuthService } from '../../core/services/admin-auth.service';
import { AdminLoginComponent } from './admin-login/admin-login.component';
import { AdminNavbarComponent } from './admin-navbar/admin-navbar.component';

@Component({
  selector: 'app-admin',
  imports: [RouterOutlet, AdminLoginComponent, AdminNavbarComponent],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css',
})
export class AdminComponent {
  readonly auth = inject(AdminAuthService);
}

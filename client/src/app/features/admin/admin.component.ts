import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AdminAuthService } from '../../core/services/admin-auth.service';
import { AdminLoginComponent } from './admin-login/admin-login.component';

@Component({
  selector: 'app-admin',
  imports: [RouterOutlet, AdminLoginComponent],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css',
})
export class AdminComponent {
  readonly auth = inject(AdminAuthService);
}

import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AdminAuthService } from '../../../core/services/admin-auth.service';

@Component({
  selector: 'app-admin-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './admin-navbar.component.html',
  styleUrl: './admin-navbar.component.css',
})
export class AdminNavbarComponent {
  private readonly auth = inject(AdminAuthService);

  logout(): void {
    this.auth.logout();
    window.location.assign('/');
  }
}

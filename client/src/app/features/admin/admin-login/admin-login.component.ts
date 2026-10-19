import { AfterViewInit, Component, ElementRef, HostListener, ViewChild, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AdminAuthService } from '../../../core/services/admin-auth.service';

@Component({
  selector: 'app-admin-login',
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.css',
})
export class AdminLoginComponent implements AfterViewInit {
  private readonly auth = inject(AdminAuthService);
  private readonly router = inject(Router);

  @ViewChild('codeInput') private codeInput?: ElementRef<HTMLInputElement>;

  readonly value = signal('');
  readonly errorMessage = signal<string | null>(null);

  private redirectTimer?: ReturnType<typeof setTimeout>;

  ngAfterViewInit(): void {
    this.codeInput?.nativeElement.focus();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.goHome();
  }

  onInput(event: Event): void {
    this.value.set((event.target as HTMLInputElement).value);
  }

  submit(): void {
    if (this.auth.submit(this.value())) {
      clearTimeout(this.redirectTimer);
      this.errorMessage.set(null);
      return;
    }

    this.errorMessage.set('Acceso denegado. Volviendo a la página principal…');
    clearTimeout(this.redirectTimer);
    this.redirectTimer = setTimeout(() => this.goHome(), 1200);
  }

  goHome(): void {
    clearTimeout(this.redirectTimer);
    this.router.navigate(['/']);
  }
}

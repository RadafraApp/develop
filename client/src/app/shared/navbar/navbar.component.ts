import { Component, ElementRef, ViewChild, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MyBidsDialogComponent } from '../my-bids-dialog/my-bids-dialog.component';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, MyBidsDialogComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css',
})
export class NavbarComponent {
  @ViewChild('phoneInput') private phoneInput?: ElementRef<HTMLInputElement>;

  readonly myBidsPhone = signal<string | null>(null);
  readonly myBidsError = signal<string | null>(null);

  onSearchMyBids(event: Event): void {
    event.preventDefault();
    this.myBidsError.set(null);

    const phone = (this.phoneInput?.nativeElement.value ?? '').trim();
    const error = this.validatePhone(phone);
    if (error) {
      this.myBidsError.set(error);
      return;
    }

    this.myBidsPhone.set(phone);
  }

  onMyBidsClosed(): void {
    this.myBidsPhone.set(null);
  }

  private validatePhone(value: string): string | null {
    if (!value) {
      return 'El teléfono es obligatorio.';
    }
    if (value.length > 20) {
      return 'El teléfono no puede superar los 20 caracteres.';
    }
    if (/[^0-9 ()+\-.]/.test(value)) {
      return 'El teléfono contiene caracteres no válidos.';
    }
    const digits = value.replace(/\D/g, '');
    if (digits.length < 9 || digits.length > 15) {
      return 'Introduce un teléfono válido (entre 9 y 15 dígitos).';
    }
    return null;
  }
}

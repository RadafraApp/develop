import { Component, ElementRef, ViewChild, signal } from '@angular/core';

import { Bid } from '../../core/models/bid.model';
import { Product } from '../../core/models/product.model';
import { BidDialogComponent } from '../../shared/bid-dialog/bid-dialog.component';
import { MyBidsDialogComponent } from '../../shared/my-bids-dialog/my-bids-dialog.component';
import { ProductGridComponent } from '../../shared/product-grid/product-grid.component';

@Component({
  selector: 'app-home',
  imports: [ProductGridComponent, BidDialogComponent, MyBidsDialogComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  @ViewChild(ProductGridComponent) private grid?: ProductGridComponent;
  @ViewChild('myPhoneInput') private myPhoneInput?: ElementRef<HTMLInputElement>;

  readonly notice = signal<string | null>(null);
  readonly bidTarget = signal<Product | null>(null);
  readonly myBidsPhone = signal<string | null>(null);
  readonly myBidsError = signal<string | null>(null);

  onBid(product: Product): void {
    this.bidTarget.set(product);
  }

  onBidClosed(): void {
    this.bidTarget.set(null);
  }

  onBidPlaced(bid: Bid): void {
    this.bidTarget.set(null);
    this.notice.set(`¡Puja registrada por ${this.money(bid.amount)}!`);
    this.grid?.load();
  }

  onSearchMyBids(event: Event): void {
    event.preventDefault();
    this.myBidsError.set(null);

    const phone = (this.myPhoneInput?.nativeElement.value ?? '').trim();
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

  private money(value: number): string {
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(value);
  }
}

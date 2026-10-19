import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';

import { Product } from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';

@Component({
  selector: 'app-ended-auctions',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './ended-auctions.component.html',
  styleUrl: './ended-auctions.component.css',
})
export class EndedAuctionsComponent {
  private readonly productService = inject(ProductService);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly deletingId = signal<number | null>(null);
  readonly confirmingId = signal<number | null>(null);
  readonly actionError = signal<string | null>(null);

  constructor() {
    this.load();
  }

  isEnded(product: Product): boolean {
    return new Date(product.auctionEndTime).getTime() <= Date.now();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getProducts().subscribe({
      next: (products) => {
        this.products.set(products.filter((product) => this.isEnded(product)));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las subastas. Inténtalo de nuevo.');
        this.loading.set(false);
      },
    });
  }

  confirmDelete(product: Product): void {
    this.confirmingId.set(product.id);
  }

  cancelDelete(): void {
    this.confirmingId.set(null);
  }

  delete(product: Product): void {
    this.actionError.set(null);
    this.deletingId.set(product.id);

    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.confirmingId.set(null);
        this.load();
      },
      error: () => {
        this.deletingId.set(null);
        this.confirmingId.set(null);
        this.actionError.set('No se pudo borrar el producto. Inténtalo de nuevo.');
      },
    });
  }
}

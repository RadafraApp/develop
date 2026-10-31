import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';

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
  readonly bidFilter = signal<'all' | 'without-bids' | 'with-winner'>('all');
  readonly visibleProducts = computed(() => {
    const filter = this.bidFilter();
    const products = this.products();

    if (filter === 'without-bids') {
      return products.filter((product) => product.bidCount === 0);
    }
    if (filter === 'with-winner') {
      return products.filter((product) => product.bidCount > 0);
    }
    return products;
  });
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly deletingId = signal<number | null>(null);
  readonly confirmingId = signal<number | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly editingFulfillmentId = signal<number | null>(null);
  readonly savingFulfillmentId = signal<number | null>(null);
  readonly pagadoDraft = signal(false);
  readonly enviadoDraft = signal(false);
  readonly reactivatingId = signal<number | null>(null);
  readonly savingReactivationId = signal<number | null>(null);
  readonly reactivationPrice = signal('');
  readonly reactivationEndTime = signal('');
  readonly reactivationError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  constructor() {
    this.load();
  }

  isEnded(product: Product): boolean {
    return new Date(product.auctionEndTime).getTime() <= Date.now();
  }

  hasProductsWithBids(): boolean {
    return this.visibleProducts().some((product) => product.bidCount > 0);
  }

  toggleBidFilter(filter: 'without-bids' | 'with-winner', enabled: boolean): void {
    this.bidFilter.set(enabled ? filter : 'all');
  }

  reactivatingProduct(): Product | null {
    const id = this.reactivatingId();
    return id === null ? null : (this.products().find((product) => product.id === id) ?? null);
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

  editFulfillment(product: Product): void {
    if (!this.isEnded(product) || product.bidCount === 0) {
      return;
    }

    this.actionError.set(null);
    this.editingFulfillmentId.set(product.id);
    this.pagadoDraft.set(product.pagado);
    this.enviadoDraft.set(product.enviado);
  }

  cancelFulfillmentEdit(): void {
    this.editingFulfillmentId.set(null);
    this.actionError.set(null);
  }

  startReactivation(product: Product): void {
    if (!this.isEnded(product) || product.bidCount !== 0) {
      return;
    }

    this.reactivatingId.set(product.id);
    this.reactivationPrice.set(String(product.startingPrice));
    this.reactivationEndTime.set('');
    this.reactivationError.set(null);
    this.actionError.set(null);
    this.successMessage.set(null);
  }

  cancelReactivation(): void {
    if (this.savingReactivationId() !== null) {
      return;
    }
    this.reactivatingId.set(null);
    this.reactivationError.set(null);
  }

  onReactivationBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.cancelReactivation();
    }
  }

  saveReactivation(product: Product): void {
    const rawPrice = this.reactivationPrice().trim();
    if (!/^\d+([.,]\d{1,2})?$/.test(rawPrice)) {
      this.reactivationError.set('Indica un precio válido con un máximo de dos decimales.');
      return;
    }

    const startingPrice = Number(rawPrice.replace(',', '.'));
    if (startingPrice < 0 || startingPrice > 1_000_000) {
      this.reactivationError.set('El precio debe estar entre 0 y 1.000.000 €.');
      return;
    }

    const endTime = new Date(this.reactivationEndTime());
    if (!this.reactivationEndTime() || Number.isNaN(endTime.getTime()) || endTime.getTime() <= Date.now()) {
      this.reactivationError.set('Indica una fecha y hora futuras para la nueva subasta.');
      return;
    }

    this.reactivationError.set(null);
    this.savingReactivationId.set(product.id);
    this.productService
      .reactivateProduct(product.id, startingPrice, endTime.toISOString())
      .subscribe({
        next: () => {
          this.products.update((products) => products.filter((item) => item.id !== product.id));
          this.reactivatingId.set(null);
          this.savingReactivationId.set(null);
          this.successMessage.set(`«${product.name}» se ha reactivado correctamente.`);
        },
        error: () => {
          this.savingReactivationId.set(null);
          this.reactivationError.set('No se pudo reactivar la subasta. Comprueba los datos e inténtalo de nuevo.');
        },
      });
  }

  saveFulfillment(product: Product): void {
    this.actionError.set(null);
    this.savingFulfillmentId.set(product.id);

    this.productService
      .updateFulfillment(product.id, this.pagadoDraft(), this.enviadoDraft())
      .subscribe({
        next: (status) => {
          this.products.update((products) =>
            products.map((item) => (item.id === product.id ? { ...item, ...status } : item)),
          );
          this.savingFulfillmentId.set(null);
          this.editingFulfillmentId.set(null);
        },
        error: () => {
          this.savingFulfillmentId.set(null);
          this.actionError.set('No se pudo actualizar el estado de pago y envío. Inténtalo de nuevo.');
        },
      });
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

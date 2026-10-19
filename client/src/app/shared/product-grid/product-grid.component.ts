import { Component, EventEmitter, Input, Output, computed, inject, signal } from '@angular/core';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { ProductCardComponent } from '../product-card/product-card.component';

@Component({
  selector: 'app-product-grid',
  imports: [ProductCardComponent],
  templateUrl: './product-grid.component.html',
  styleUrl: './product-grid.component.css',
})
export class ProductGridComponent {
  private readonly productService = inject(ProductService);

  @Input() editable = false;
  @Input() biddable = false;
  @Input() hideEnded = false;
  @Output() bid = new EventEmitter<Product>();
  @Output() viewBids = new EventEmitter<Product>();

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly deletingId = signal<number | null>(null);

  readonly visibleProducts = computed(() =>
    this.hideEnded ? this.products().filter((product) => !this.isEnded(product)) : this.products(),
  );

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
        this.products.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las subastas. Inténtalo de nuevo.');
        this.loading.set(false);
      },
    });
  }

  onDelete(product: Product): void {
    this.actionError.set(null);
    this.deletingId.set(product.id);

    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.load();
      },
      error: () => {
        this.deletingId.set(null);
        this.actionError.set('No se pudo borrar el producto. Inténtalo de nuevo.');
      },
    });
  }
}

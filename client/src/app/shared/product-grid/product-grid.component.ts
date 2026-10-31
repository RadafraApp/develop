import { Component, EventEmitter, Input, Output, computed, inject, signal } from '@angular/core';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { normalizeText } from '../../core/utils/text';
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
  @Input() onlyEnded = false;
  private readonly onlyNumberedValue = signal(false);

  @Input()
  set onlyNumbered(value: boolean) {
    this.onlyNumberedValue.set(value);
  }

  get onlyNumbered(): boolean {
    return this.onlyNumberedValue();
  }

  private readonly filterValue = signal('');

  @Input()
  set filter(value: string) {
    this.filterValue.set(value ?? '');
  }

  get filter(): string {
    return this.filterValue();
  }

  @Output() bid = new EventEmitter<Product>();
  @Output() viewBids = new EventEmitter<Product>();

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly deletingId = signal<number | null>(null);

  readonly visibleProducts = computed(() => {
    const items = this.products();
    const needle = normalizeText(this.filterValue().trim());

    const filtered = items.filter((product) => {
      if (this.onlyEnded && !this.isEnded(product)) {
        return false;
      }
      if (this.hideEnded && this.isEnded(product)) {
        return false;
      }
      if (this.onlyNumberedValue() && !product.isNumbered) {
        return false;
      }
      if (!needle) {
        return true;
      }
      return [product.name, product.equipo, product.collection, product.tipoCromo].some((value) =>
        normalizeText(value ?? '').includes(needle),
      );
    });

    return filtered;
  });

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

import { Component, EventEmitter, HostListener, Input, Output, computed, inject, signal } from '@angular/core';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { TextSegment, normalizeText, splitMatch } from '../../core/utils/text';

@Component({
  selector: 'app-auction-search',
  imports: [],
  templateUrl: './auction-search.component.html',
  styleUrl: './auction-search.component.css',
})
export class AuctionSearchComponent {
  private readonly productService = inject(ProductService);

  @Input() hideEnded = false;
  @Output() selected = new EventEmitter<Product>();
  @Output() queryChange = new EventEmitter<string>();

  readonly query = signal('');
  readonly open = signal(false);
  readonly activeIndex = signal(0);
  readonly products = signal<Product[]>([]);

  readonly suggestions = computed(() => {
    const needle = normalizeText(this.query().trim());
    if (!needle) {
      return [] as { product: Product; segments: TextSegment[] }[];
    }

    const items = this.hideEnded ? this.products().filter((p) => !this.isEnded(p)) : this.products();

    return items
      .filter((product) =>
        [product.name, product.equipo, product.collection, product.tipoCromo].some((value) =>
          normalizeText(value ?? '').includes(needle),
        ),
      )
      .slice(0, 8)
      .map((product) => ({ product, segments: splitMatch(product.name, needle) }));
  });

  readonly showPanel = computed(() => this.open() && this.query().trim().length > 0);

  constructor() {
    this.productService.getProducts().subscribe((products) => this.products.set(products));
  }

  onInput(value: string): void {
    this.query.set(value);
    this.queryChange.emit(value);
    this.open.set(true);
    this.activeIndex.set(0);
  }

  onFocus(): void {
    if (this.query().trim()) {
      this.open.set(true);
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    const count = this.suggestions().length;
    if (!count) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.activeIndex.update((i) => (i + 1) % count);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update((i) => (i - 1 + count) % count);
        break;
      case 'Enter': {
        const match = this.suggestions()[this.activeIndex()];
        if (this.open() && match) {
          event.preventDefault();
          this.choose(match.product);
        }
        break;
      }
      case 'Escape':
        this.open.set(false);
        break;
    }
  }

  choose(product: Product): void {
    this.query.set(product.name);
    this.queryChange.emit(product.name);
    this.open.set(false);
    this.selected.emit(product);
  }

  clear(): void {
    this.query.set('');
    this.queryChange.emit('');
    this.open.set(false);
    this.activeIndex.set(0);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.search')) {
      this.open.set(false);
    }
  }

  trackByProduct(index: number): number {
    return index;
  }

  private isEnded(product: Product): boolean {
    return new Date(product.auctionEndTime).getTime() <= Date.now();
  }
}

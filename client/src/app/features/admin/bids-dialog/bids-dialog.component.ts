import { CurrencyPipe } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, OnInit, Output, inject, signal } from '@angular/core';

import { Bid } from '../../../core/models/bid.model';
import { Product } from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';

@Component({
  selector: 'app-bids-dialog',
  imports: [CurrencyPipe],
  templateUrl: './bids-dialog.component.html',
  styleUrl: './bids-dialog.component.css',
})
export class BidsDialogComponent implements OnInit {
  @Input({ required: true }) product!: Product;
  @Output() closed = new EventEmitter<void>();
  @Output() changed = new EventEmitter<void>();

  private readonly productService = inject(ProductService);

  readonly bids = signal<Bid[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly confirmingId = signal<number | null>(null);
  readonly deletingId = signal<number | null>(null);

  ngOnInit(): void {
    this.load();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }

  close(): void {
    this.closed.emit();
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getBids(this.product.id).subscribe({
      next: (bids) => {
        this.bids.set(bids);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las pujas.');
        this.loading.set(false);
      },
    });
  }

  askDelete(bid: Bid): void {
    this.actionError.set(null);
    this.confirmingId.set(bid.id);
  }

  cancelDelete(): void {
    this.confirmingId.set(null);
  }

  confirmDelete(bid: Bid): void {
    this.actionError.set(null);
    this.deletingId.set(bid.id);

    this.productService.deleteBid(this.product.id, bid.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.confirmingId.set(null);
        this.bids.update((bids) => bids.filter((b) => b.id !== bid.id));
        this.changed.emit();
      },
      error: () => {
        this.deletingId.set(null);
        this.actionError.set('No se pudo borrar la puja. Inténtalo de nuevo.');
      },
    });
  }
}

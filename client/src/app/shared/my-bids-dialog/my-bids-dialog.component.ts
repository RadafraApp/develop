import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, HostListener, Input, OnInit, Output, computed, inject, signal } from '@angular/core';

import { PhoneBid } from '../../core/models/phone-bid.model';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-my-bids-dialog',
  imports: [CurrencyPipe],
  templateUrl: './my-bids-dialog.component.html',
  styleUrl: './my-bids-dialog.component.css',
})
export class MyBidsDialogComponent implements OnInit {
  @Input({ required: true }) phone!: string;
  @Output() closed = new EventEmitter<void>();

  private readonly productService = inject(ProductService);

  readonly pageSize = 5;

  readonly bids = signal<PhoneBid[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly activeBids = computed(() => this.bids().filter((bid) => !this.isEnded(bid)));
  readonly endedBids = computed(() => this.bids().filter((bid) => this.isEnded(bid)));

  readonly activePage = signal(1);
  readonly endedPage = signal(1);

  readonly activeTotalPages = computed(() => this.totalPages(this.activeBids().length));
  readonly endedTotalPages = computed(() => this.totalPages(this.endedBids().length));

  readonly activePageItems = computed(() => this.slice(this.activeBids(), this.activePage()));
  readonly endedPageItems = computed(() => this.slice(this.endedBids(), this.endedPage()));

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

  isEnded(bid: PhoneBid): boolean {
    return new Date(bid.auctionEndTime).getTime() <= Date.now();
  }

  prevActive(): void {
    this.activePage.update((page) => Math.max(1, page - 1));
  }

  nextActive(): void {
    this.activePage.update((page) => Math.min(this.activeTotalPages(), page + 1));
  }

  prevEnded(): void {
    this.endedPage.update((page) => Math.max(1, page - 1));
  }

  nextEnded(): void {
    this.endedPage.update((page) => Math.min(this.endedTotalPages(), page + 1));
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getBidsByPhone(this.phone).subscribe({
      next: (bids) => {
        this.bids.set(bids);
        this.activePage.set(1);
        this.endedPage.set(1);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        const fieldErrors =
          error.status === 400 ? (error.error?.errors as Record<string, string[]> | undefined) : undefined;
        const key = fieldErrors ? Object.keys(fieldErrors).find((k) => k.toLowerCase() === 'phone') : undefined;
        const message = key ? fieldErrors?.[key]?.[0] : undefined;
        this.error.set(message ?? 'No se pudieron cargar las pujas.');
      },
    });
  }

  private totalPages(count: number): number {
    return Math.max(1, Math.ceil(count / this.pageSize));
  }

  private slice(items: PhoneBid[], page: number): PhoneBid[] {
    const start = (page - 1) * this.pageSize;
    return items.slice(start, start + this.pageSize);
  }
}

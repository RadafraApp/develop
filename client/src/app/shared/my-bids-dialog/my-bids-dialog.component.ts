import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, HostListener, Input, OnInit, Output, inject, signal } from '@angular/core';

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

  readonly bids = signal<PhoneBid[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

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

    this.productService.getBidsByPhone(this.phone).subscribe({
      next: (bids) => {
        this.bids.set(bids);
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
}

import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Product } from '../../core/models/product.model';

@Component({
  selector: 'app-product-card',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css',
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;
  @Input() editable = false;
  @Input() biddable = false;
  @Input() deleting = false;
  @Output() delete = new EventEmitter<Product>();
  @Output() bid = new EventEmitter<Product>();
  @Output() viewBids = new EventEmitter<Product>();

  readonly confirming = signal(false);

  // The card first tries the small rendition; if it is missing or broken it falls back
  // to the full picture, and finally to the "no image" placeholder.
  private readonly thumbnailFailed = signal(false);
  private readonly imageFailed = signal(false);

  imageSource(): string {
    if (!this.thumbnailFailed() && this.product.thumbnailPath) {
      return this.product.thumbnailPath;
    }
    if (!this.imageFailed() && this.product.imagePath) {
      return this.product.imagePath;
    }
    return '';
  }

  onImageError(): void {
    if (!this.thumbnailFailed() && this.product.thumbnailPath) {
      this.thumbnailFailed.set(true);
      return;
    }
    this.imageFailed.set(true);
  }

  isEnded(product: Product): boolean {
    return new Date(product.auctionEndTime).getTime() <= Date.now();
  }

  confirmDelete(): void {
    this.delete.emit(this.product);
  }
}

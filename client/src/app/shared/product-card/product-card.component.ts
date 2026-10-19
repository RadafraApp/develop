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

  isEnded(product: Product): boolean {
    return new Date(product.auctionEndTime).getTime() <= Date.now();
  }

  confirmDelete(): void {
    this.delete.emit(this.product);
  }
}

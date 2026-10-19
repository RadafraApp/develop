import { Component, ViewChild, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Product } from '../../../core/models/product.model';
import { ProductGridComponent } from '../../../shared/product-grid/product-grid.component';
import { BidsDialogComponent } from '../bids-dialog/bids-dialog.component';

@Component({
  selector: 'app-product-list',
  imports: [ProductGridComponent, RouterLink, BidsDialogComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css',
})
export class ProductListComponent {
  @ViewChild(ProductGridComponent) private grid?: ProductGridComponent;

  readonly bidsTarget = signal<Product | null>(null);

  onViewBids(product: Product): void {
    this.bidsTarget.set(product);
  }

  onBidsClosed(): void {
    this.bidsTarget.set(null);
  }

  onBidsChanged(): void {
    this.grid?.load();
  }
}

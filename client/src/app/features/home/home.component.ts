import { Component, ViewChild, signal } from '@angular/core';

import { Bid } from '../../core/models/bid.model';
import { Product } from '../../core/models/product.model';
import { AuctionSearchComponent } from '../../shared/auction-search/auction-search.component';
import { BidDialogComponent } from '../../shared/bid-dialog/bid-dialog.component';
import { ProductGridComponent } from '../../shared/product-grid/product-grid.component';

@Component({
  selector: 'app-home',
  imports: [AuctionSearchComponent, ProductGridComponent, BidDialogComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  @ViewChild(ProductGridComponent) private grid?: ProductGridComponent;
  @ViewChild(AuctionSearchComponent) private search?: AuctionSearchComponent;

  readonly notice = signal<string | null>(null);
  readonly bidTarget = signal<Product | null>(null);
  readonly filter = signal('');
  readonly onlyNumbered = signal(false);

  onNumberedToggle(enabled: boolean): void {
    this.onlyNumbered.set(enabled);
  }

  onSearch(product: Product): void {
    this.filter.set(product.name);
  }

  onFilterInput(value: string): void {
    this.filter.set(value);
  }

  clearFilter(): void {
    this.filter.set('');
    this.search?.clear();
  }

  onBid(product: Product): void {
    this.bidTarget.set(product);
  }

  onBidClosed(): void {
    this.bidTarget.set(null);
  }

  onBidPlaced(bid: Bid): void {
    this.bidTarget.set(null);
    this.notice.set(`¡Puja registrada por ${this.money(bid.amount)}!`);
    this.grid?.load();
  }

  private money(value: number): string {
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(value);
  }
}

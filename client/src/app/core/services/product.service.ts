import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Bid } from '../models/bid.model';
import { PhoneBid } from '../models/phone-bid.model';
import { Product } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  createProduct(formData: FormData): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, formData);
  }

  updateProduct(id: number, formData: FormData): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, formData);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  placeBid(productId: number, phone: string, amount: number): Observable<Bid> {
    return this.http.post<Bid>(`${this.baseUrl}/${productId}/bids`, { phone, amount });
  }

  getBids(productId: number): Observable<Bid[]> {
    return this.http.get<Bid[]>(`${this.baseUrl}/${productId}/bids`);
  }

  deleteBid(productId: number, bidId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${productId}/bids/${bidId}`);
  }

  getBidsByPhone(phone: string): Observable<PhoneBid[]> {
    return this.http.get<PhoneBid[]>('/api/bids', { params: { phone } });
  }
}

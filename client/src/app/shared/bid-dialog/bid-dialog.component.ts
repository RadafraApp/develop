import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Bid } from '../../core/models/bid.model';
import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-bid-dialog',
  imports: [CurrencyPipe, ReactiveFormsModule],
  templateUrl: './bid-dialog.component.html',
  styleUrl: './bid-dialog.component.css',
})
export class BidDialogComponent implements AfterViewInit {
  @Input({ required: true }) product!: Product;
  @Output() closed = new EventEmitter<void>();
  @Output() placed = new EventEmitter<Bid>();

  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);

  @ViewChild('phoneInput') private phoneInput?: ElementRef<HTMLInputElement>;

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly phoneError = signal<string | null>(null);

  readonly form = this.fb.group({
    phone: ['', [Validators.required, Validators.maxLength(20)]],
    amount: ['', [Validators.required]],
  });

  get minimum(): number {
    if (this.product.bidCount > 0) {
      return Math.round((this.product.currentPrice + 0.01) * 100) / 100;
    }
    return this.product.startingPrice;
  }

  ngAfterViewInit(): void {
    this.phoneInput?.nativeElement.focus();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }

  close(): void {
    if (!this.submitting()) {
      this.closed.emit();
    }
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  quickAdd(increment: number): void {
    this.errorMessage.set(null);

    const raw = String(this.form.controls.amount.value ?? '').trim().replace(',', '.');
    const parsed = Number(raw);
    const base =
      raw && Number.isFinite(parsed) && parsed > 0
        ? parsed
        : this.product.bidCount > 0
          ? this.product.currentPrice
          : this.product.startingPrice;

    const next = Math.round((base + increment) * 100) / 100;
    this.form.controls.amount.setValue(next.toFixed(2));
  }

  submit(): void {
    this.errorMessage.set(null);
    this.phoneError.set(null);

    const phone = String(this.form.controls.phone.value ?? '').trim();
    const phoneValidationError = this.validatePhone(phone);
    if (phoneValidationError) {
      this.phoneError.set(phoneValidationError);
      return;
    }

    const raw = String(this.form.controls.amount.value ?? '').trim().replace(',', '.');
    const amount = Number(raw);

    if (!raw || !Number.isFinite(amount) || amount <= 0) {
      this.errorMessage.set('Introduce un importe válido.');
      return;
    }

    if (Math.round(amount * 100) % 50 !== 0) {
      this.errorMessage.set('La puja debe ser un múltiplo de 0,50 €.');
      return;
    }

    if (this.product.bidCount === 0) {
      if (amount < this.product.startingPrice) {
        this.errorMessage.set(`La puja debe ser al menos el precio inicial (${this.money(this.product.startingPrice)}).`);
        return;
      }
    } else if (amount <= this.product.currentPrice) {
      this.errorMessage.set(`La puja debe superar la puja actual (${this.money(this.product.currentPrice)}).`);
      return;
    }

    this.submitting.set(true);
    this.productService.placeBid(this.product.id, phone, amount).subscribe({
      next: (bid) => {
        this.submitting.set(false);
        this.placed.emit(bid);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        const errors = error.status === 400 ? (error.error?.errors as Record<string, string[]> | undefined) : undefined;
        const fieldError = (name: string): string | undefined => {
          if (!errors) {
            return undefined;
          }
          const key = Object.keys(errors).find((k) => k.toLowerCase() === name);
          return key ? errors[key]?.[0] : undefined;
        };
        const phoneMessage = fieldError('phone');
        const amountMessage = fieldError('amount');

        this.phoneError.set(phoneMessage ?? null);
        this.errorMessage.set(
          amountMessage ?? (phoneMessage ? null : 'No se pudo registrar la puja. Inténtalo de nuevo.'),
        );
      },
    });
  }

  private validatePhone(value: string): string | null {
    if (!value) {
      return 'El teléfono es obligatorio.';
    }
    if (value.length > 20) {
      return 'El teléfono no puede superar los 20 caracteres.';
    }
    if (/[^0-9 ()+\-.]/.test(value)) {
      return 'El teléfono contiene caracteres no válidos.';
    }
    const digits = value.replace(/\D/g, '');
    if (digits.length < 9 || digits.length > 15) {
      return 'Introduce un teléfono válido (entre 9 y 15 dígitos).';
    }
    return null;
  }

  private money(value: number): string {
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(value);
  }
}

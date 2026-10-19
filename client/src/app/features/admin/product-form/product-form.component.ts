import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ProductService } from '../../../core/services/product.service';

const twoDecimalNumber: ValidatorFn = (control) => {
  const value = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }
  return /^\d+([.,]\d{1,2})?$/.test(String(value).trim()) ? null : { decimals: true };
};

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.css',
})
export class ProductFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);

  readonly productId: number | null = this.readId();

  readonly submitting = signal(false);
  readonly loading = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly imagePreview = signal<string | null>(null);
  readonly serverErrors = signal<Record<string, string[]>>({});

  private selectedImage: File | null = null;

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    startingPrice: [null as number | null, [Validators.required, Validators.min(0), twoDecimalNumber]],
    publisher: ['', [Validators.required, Validators.maxLength(200)]],
    collection: ['', [Validators.required, Validators.maxLength(200)]],
    isNumbered: [false],
    numbering: ['', [Validators.maxLength(100)]],
    auctionEndTime: ['', [Validators.required]],
  });

  constructor() {
    this.form.controls.numbering.disable();

    this.form.controls.isNumbered.valueChanges.subscribe((isNumbered) => {
      const numbering = this.form.controls.numbering;
      if (isNumbered) {
        numbering.enable();
        numbering.addValidators(Validators.required);
      } else {
        numbering.disable();
        numbering.removeValidators(Validators.required);
        numbering.setValue('');
      }
      numbering.updateValueAndValidity();
    });

    if (this.productId !== null) {
      this.loadProduct(this.productId);
    }
  }

  get isEditMode(): boolean {
    return this.productId !== null;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedImage = file;

    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => this.imagePreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  submit(): void {
    this.successMessage.set(null);
    this.errorMessage.set(null);
    this.serverErrors.set({});

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.isEditMode && !this.selectedImage) {
      this.errorMessage.set('Selecciona una imagen para el producto.');
      return;
    }

    const value = this.form.getRawValue();

    const formData = new FormData();
    formData.append('name', value.name!.trim());
    formData.append('startingPrice', String(value.startingPrice).trim().replace(',', '.'));
    formData.append('publisher', value.publisher!.trim());
    formData.append('collection', value.collection!.trim());
    formData.append('isNumbered', String(value.isNumbered));
    if (value.isNumbered && value.numbering) {
      formData.append('numbering', value.numbering.trim());
    }
    formData.append('auctionEndTime', new Date(value.auctionEndTime!).toISOString());
    if (this.selectedImage) {
      formData.append('image', this.selectedImage, this.selectedImage.name);
    }

    this.submitting.set(true);

    const request$ = this.isEditMode
      ? this.productService.updateProduct(this.productId!, formData)
      : this.productService.createProduct(formData);

    request$.subscribe({
      next: () => {
        this.submitting.set(false);
        this.successMessage.set(
          this.isEditMode ? 'Producto actualizado correctamente.' : 'Producto creado correctamente.',
        );
        if (!this.isEditMode) {
          this.reset();
        }
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        if (error.status === 400 && error.error?.errors) {
          this.serverErrors.set(this.normalizeErrors(error.error.errors));
        }
        this.errorMessage.set(
          this.isEditMode
            ? 'No se pudo actualizar el producto. Revisa los campos marcados.'
            : 'No se pudo crear el producto. Revisa los campos marcados.',
        );
      },
    });
  }

  controlError(name: string): string | null {
    const serverError = this.serverErrors()[name]?.[0];
    if (serverError) {
      return serverError;
    }

    const control = this.form.get(name);
    if (!control || !(control.touched || control.dirty)) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Este campo es obligatorio.';
    }
    if (control.hasError('maxlength')) {
      return 'Has superado la longitud máxima.';
    }
    if (control.hasError('min')) {
      return 'El precio no puede ser negativo.';
    }
    if (control.hasError('decimals')) {
      return 'Usa como máximo dos decimales.';
    }
    return null;
  }

  private readId(): number | null {
    const raw = this.route.snapshot.paramMap.get('id');
    if (!raw) {
      return null;
    }
    const parsed = Number(raw);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }

  private loadProduct(id: number): void {
    this.loading.set(true);

    this.productService.getProduct(id).subscribe({
      next: (product) => {
        this.form.patchValue({
          name: product.name,
          startingPrice: product.startingPrice,
          publisher: product.publisher,
          collection: product.collection,
          isNumbered: product.isNumbered,
          numbering: product.numbering ?? '',
          auctionEndTime: this.toLocalInputValue(product.auctionEndTime),
        });
        this.imagePreview.set(product.imagePath);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el producto.');
      },
    });
  }

  private toLocalInputValue(iso: string): string {
    const date = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  private normalizeErrors(errors: Record<string, string[]>): Record<string, string[]> {
    const normalized: Record<string, string[]> = {};
    for (const [key, value] of Object.entries(errors)) {
      normalized[key.charAt(0).toLowerCase() + key.slice(1)] = value;
    }
    return normalized;
  }

  private reset(): void {
    this.form.reset({
      name: '',
      startingPrice: null,
      publisher: '',
      collection: '',
      isNumbered: false,
      numbering: '',
      auctionEndTime: '',
    });
    this.selectedImage = null;
    this.imagePreview.set(null);
  }
}

import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PriceVerdict } from '../analysis/analysis.models';
import { ImportResult, PriceListItem } from './price-list.models';
import { PriceListService } from './price-list.service';

@Component({
  selector: 'app-price-list',
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './price-list.html',
  styleUrl: './price-list.css',
})
export class PriceList {
  private readonly priceListService = inject(PriceListService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly items = signal<PriceListItem[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly importResult = signal<ImportResult | null>(null);
  protected readonly isImporting = signal(false);

  protected readonly PriceVerdict = PriceVerdict;

  /** How many rows the catalog could actually assess - the rest have no market counterpart. */
  protected readonly analyzedCount = computed(() => this.items().filter((i) => i.recommendation).length);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    price: [null as number | null, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.priceListService.getAll().subscribe({
      next: (items) => {
        this.items.set(items);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);
        // A 401 is already handled by the interceptor (which signs the user out).
        if (error.status !== 401) {
          this.errorMessage.set('Dohvaćanje cjenika nije uspjelo. Provjerite radi li poslužitelj.');
        }
      },
    });
  }

  protected addService(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, price } = this.form.getRawValue();
    this.priceListService.add(name, price!).subscribe({
      next: () => {
        this.form.reset({ name: '', price: null });
        this.reload();
      },
      error: () => this.errorMessage.set('Dodavanje usluge nije uspjelo.'),
    });
  }

  protected deleteService(item: PriceListItem): void {
    this.priceListService.delete(item.id).subscribe({
      next: () => this.reload(),
      error: () => this.errorMessage.set('Brisanje usluge nije uspjelo.'),
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.isImporting.set(true);
    this.importResult.set(null);
    this.errorMessage.set(null);

    this.priceListService.import(file).subscribe({
      next: (result) => {
        this.isImporting.set(false);
        this.importResult.set(result);
        // Let the same file be picked again after a correction.
        input.value = '';
        this.reload();
      },
      error: (error: HttpErrorResponse) => {
        this.isImporting.set(false);
        input.value = '';
        this.errorMessage.set(error.error?.error ?? 'Uvoz datoteke nije uspio.');
      },
    });
  }

  protected verdictLabel(verdict: PriceVerdict): string {
    switch (verdict) {
      case PriceVerdict.TooLow:
        return 'Preniska';
      case PriceVerdict.TooHigh:
        return 'Previsoka';
      default:
        return 'U redu';
    }
  }

  protected verdictClass(verdict: PriceVerdict): string {
    switch (verdict) {
      case PriceVerdict.TooLow:
        return 'badge-low';
      case PriceVerdict.TooHigh:
        return 'badge-high';
      default:
        return 'badge-fair';
    }
  }
}

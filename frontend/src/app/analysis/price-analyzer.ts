import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AvailableService, PriceRecommendation, PriceVerdict } from './analysis.models';
import { AnalysisService } from './analysis.service';
import { DistributionChart } from './distribution-chart';

@Component({
  selector: 'app-price-analyzer',
  imports: [ReactiveFormsModule, DecimalPipe, DistributionChart],
  templateUrl: './price-analyzer.html',
  styleUrl: './price-analyzer.css',
})
export class PriceAnalyzer {
  private readonly analysisService = inject(AnalysisService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.nonNullable.group({
    service: ['', Validators.required],
    price: [null as number | null, [Validators.required, Validators.min(0)]],
  });

  protected readonly services = signal<AvailableService[]>([]);
  protected readonly result = signal<PriceRecommendation | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);

  protected readonly PriceVerdict = PriceVerdict;

  /** Drives the colour and wording of the verdict banner. */
  protected readonly verdictInfo = computed(() => {
    const recommendation = this.result();
    if (!recommendation) {
      return null;
    }

    switch (recommendation.verdict) {
      case PriceVerdict.TooLow:
        return {
          cssClass: 'verdict-low',
          title: 'Cijena je preniska',
          explanation: 'Ispod je donje granice uobičajenog raspona na tržištu — vjerojatno ostavljate zaradu na stolu.',
        };
      case PriceVerdict.TooHigh:
        return {
          cssClass: 'verdict-high',
          title: 'Cijena je previsoka',
          explanation: 'Iznad je gornje granice uobičajenog raspona — provjerite opravdava li je vaša ponuda.',
        };
      default:
        return {
          cssClass: 'verdict-fair',
          title: 'Cijena je u redu',
          explanation: 'Nalazi se unutar uobičajenog raspona cijena na tržištu.',
        };
    }
  });

  /** How many entries were discarded as outliers before the statistics were computed. */
  protected readonly removedOutliers = computed(() => {
    const recommendation = this.result();
    return recommendation ? recommendation.sampleSizeBeforeOutlierRemoval - recommendation.sampleSize : 0;
  });

  constructor() {
    this.analysisService.getServices().subscribe({
      next: (services) => this.services.set(services),
      // A failed lookup only costs the user the suggestion list - they can still type a name.
      error: () => this.services.set([]),
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { service, price } = this.form.getRawValue();
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.analysisService.analyze(service, price!).subscribe({
      next: (recommendation) => {
        this.result.set(recommendation);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.result.set(null);
        this.errorMessage.set(
          error.status === 404
            ? `Usluga "${service}" nije prepoznata. Pokušajte s jednim od ponuđenih naziva.`
            : 'Analiza nije uspjela. Provjerite je li poslužitelj pokrenut.',
        );
        this.isLoading.set(false);
      },
    });
  }

  protected useSuggestion(displayName: string): void {
    this.form.patchValue({ service: displayName });
  }
}

import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  viewChild,
} from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { HistogramBin } from './analysis.models';

Chart.register(...registerables);

// Keep the chart's typography in step with the rest of the page.
Chart.defaults.font.family = "system-ui, -apple-system, 'Segoe UI', Roboto, Arial, sans-serif";
Chart.defaults.color = '#6c757d';

/**
 * Bar chart of the market price distribution for a canonical service.
 * The bin containing the user's own price is highlighted so they can see
 * where they sit relative to everyone else.
 */
@Component({
  selector: 'app-distribution-chart',
  template: `<canvas #canvas></canvas>`,
  styles: `
    :host {
      display: block;
      position: relative;
      height: 260px;
    }
  `,
})
export class DistributionChart implements AfterViewInit, OnDestroy {
  readonly distribution = input.required<HistogramBin[]>();
  readonly userPrice = input.required<number>();

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart?: Chart;

  constructor() {
    // Redraw whenever new analysis results arrive.
    effect(() => {
      const bins = this.distribution();
      const price = this.userPrice();
      if (this.chart) {
        this.applyData(bins, price);
      }
    });
  }

  ngAfterViewInit(): void {
    this.chart = new Chart(this.canvas().nativeElement, this.buildConfig());
    this.applyData(this.distribution(), this.userPrice());
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private applyData(bins: HistogramBin[], userPrice: number): void {
    if (!this.chart) {
      return;
    }

    const userBinIndex = this.findUserBin(bins, userPrice);

    this.chart.data.labels = bins.map((bin) => `${this.round(bin.lowerBound)}–${this.round(bin.upperBound)}`);
    this.chart.data.datasets[0].data = bins.map((bin) => bin.count);
    this.chart.data.datasets[0].backgroundColor = bins.map((_, i) =>
      i === userBinIndex ? '#0d6efd' : '#ced4da',
    );
    this.chart.update();
  }

  /** Index of the bin the user's price falls into, or -1 if it sits outside the charted range. */
  private findUserBin(bins: HistogramBin[], userPrice: number): number {
    return bins.findIndex((bin, i) =>
      userPrice >= bin.lowerBound && (i === bins.length - 1 ? userPrice <= bin.upperBound : userPrice < bin.upperBound),
    );
  }

  private round(value: number): string {
    return value.toFixed(0);
  }

  private buildConfig(): ChartConfiguration<'bar'> {
    return {
      type: 'bar',
      data: {
        labels: [],
        datasets: [{ label: 'Broj obrta', data: [], borderRadius: 2 }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              title: (items) => `${items[0].label} €`,
              label: (item) => `${item.parsed.y} obrta u ovom rasponu`,
            },
          },
        },
        scales: {
          x: {
            title: { display: true, text: 'Cijena (€)' },
            grid: { display: false },
            ticks: { maxRotation: 45, minRotation: 45, font: { size: 10 } },
          },
          y: {
            title: { display: true, text: 'Broj obrta' },
            beginAtZero: true,
            ticks: { precision: 0 },
          },
        },
      },
    };
  }
}

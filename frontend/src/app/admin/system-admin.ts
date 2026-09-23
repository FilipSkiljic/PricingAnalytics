import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { AdminService } from './admin.service';
import { BusinessSummary, SystemStatistics } from './admin.models';

@Component({
  selector: 'app-system-admin',
  imports: [DecimalPipe],
  templateUrl: './system-admin.html',
  styleUrl: './system-admin.css',
})
export class SystemAdmin {
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);

  protected readonly statistics = signal<SystemStatistics | null>(null);
  protected readonly businesses = signal<BusinessSummary[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly isImporting = signal(false);

  /** Own account id - the row for it must not offer a delete button. */
  protected readonly ownId = this.authService.currentBusiness()?.businessId ?? 0;

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.adminService.getStatistics().subscribe({
      next: (stats) => this.statistics.set(stats),
      error: () => this.errorMessage.set('Dohvaćanje statistike nije uspjelo.'),
    });

    this.adminService.getBusinesses().subscribe({
      next: (list) => this.businesses.set(list),
      error: () => this.errorMessage.set('Dohvaćanje popisa obrta nije uspjelo.'),
    });
  }

  protected deleteBusiness(business: BusinessSummary): void {
    this.clearMessages();

    this.adminService.deleteBusiness(business.id).subscribe({
      next: () => {
        this.successMessage.set(`Račun "${business.name}" je obrisan.`);
        this.reload();
      },
      error: (error: HttpErrorResponse) =>
        this.errorMessage.set(error.error?.error ?? 'Brisanje računa nije uspjelo.'),
    });
  }

  protected onCorpusFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.isImporting.set(true);
    this.clearMessages();

    this.adminService.importCorpus(file).subscribe({
      next: (result) => {
        this.isImporting.set(false);
        input.value = '';
        this.successMessage.set(`Uvezeno ${result.imported} unosa u tržišni korpus.`);
        this.reload();
      },
      error: (error: HttpErrorResponse) => {
        this.isImporting.set(false);
        input.value = '';
        this.errorMessage.set(error.error?.error ?? 'Uvoz korpusa nije uspio.');
      },
    });
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }
}

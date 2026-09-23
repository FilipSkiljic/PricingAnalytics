import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { AdminService } from './admin.service';
import { CatalogEntry, MatchExplanation, RulePreview, UnmatchedName } from './admin.models';

@Component({
  selector: 'app-catalog-admin',
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './catalog-admin.html',
  styleUrl: './catalog-admin.css',
})
export class CatalogAdmin {
  private readonly adminService = inject(AdminService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly catalog = signal<CatalogEntry[]>([]);
  protected readonly unmatched = signal<UnmatchedName[]>([]);
  protected readonly preview = signal<RulePreview | null>(null);
  protected readonly explanation = signal<MatchExplanation | null>(null);

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isPreviewing = signal(false);

  /** Id of the rule being edited, or null when the form creates a new one. */
  protected readonly editingId = signal<number | null>(null);

  protected readonly isEditing = computed(() => this.editingId() !== null);
  protected readonly totalMatched = computed(() =>
    this.catalog().reduce((sum, entry) => sum + entry.matchedEntries, 0),
  );

  protected readonly form = this.formBuilder.nonNullable.group({
    slug: ['', [Validators.required, Validators.minLength(2)]],
    displayName: ['', [Validators.required, Validators.minLength(2)]],
    requiredPhrase: ['', [Validators.required, Validators.minLength(2)]],
    excludedWords: [''],
  });

  protected readonly testForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
  });

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.isLoading.set(true);

    this.adminService.getCatalog().subscribe({
      next: (entries) => {
        this.catalog.set(entries);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Dohvaćanje kataloga nije uspjelo.');
      },
    });

    this.adminService.getUnmatched(30).subscribe({
      next: (names) => this.unmatched.set(names),
      error: () => this.errorMessage.set('Dohvaćanje nesparenih naziva nije uspjelo.'),
    });
  }

  /** Loads a rule into the form for editing. Slug stays fixed - it identifies the rule. */
  protected startEdit(entry: CatalogEntry): void {
    this.editingId.set(entry.id);
    this.preview.set(null);
    this.clearMessages();

    this.form.setValue({
      slug: entry.slug,
      displayName: entry.displayName,
      requiredPhrase: entry.requiredPhrase,
      excludedWords: entry.excludedWords,
    });
    this.form.controls.slug.disable();
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.preview.set(null);
    this.clearMessages();
    this.form.reset({ slug: '', displayName: '', requiredPhrase: '', excludedWords: '' });
    this.form.controls.slug.enable();
  }

  /** Runs the current rule against the corpus without saving it. */
  protected runPreview(): void {
    const { requiredPhrase, excludedWords } = this.form.getRawValue();
    if (!requiredPhrase || requiredPhrase.trim().length < 2) {
      this.errorMessage.set('Unesite tražene riječi prije provjere.');
      return;
    }

    this.isPreviewing.set(true);
    this.clearMessages();

    this.adminService.previewRule(requiredPhrase, excludedWords, this.editingId()).subscribe({
      next: (result) => {
        this.preview.set(result);
        this.isPreviewing.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.isPreviewing.set(false);
        this.errorMessage.set(error.error?.error ?? 'Provjera pravila nije uspjela.');
      },
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { slug, displayName, requiredPhrase, excludedWords } = this.form.getRawValue();
    const editingId = this.editingId();

    // Update returns nothing, create returns the new row; neither result is used here.
    const request$: Observable<unknown> = editingId
      ? this.adminService.updateService(editingId, displayName, requiredPhrase, excludedWords)
      : this.adminService.createService(slug, displayName, requiredPhrase, excludedWords);

    request$.subscribe({
      next: () => {
        this.successMessage.set(editingId ? 'Usluga je izmijenjena.' : 'Usluga je dodana.');
        this.cancelEdit();
        this.reload();
      },
      error: (error: HttpErrorResponse) =>
        this.errorMessage.set(error.error?.error ?? 'Spremanje nije uspjelo.'),
    });
  }

  protected remove(entry: CatalogEntry): void {
    this.adminService.deleteService(entry.id).subscribe({
      next: () => {
        this.successMessage.set(`Usluga "${entry.displayName}" je obrisana.`);
        this.reload();
      },
      error: (error: HttpErrorResponse) =>
        this.errorMessage.set(error.error?.error ?? 'Brisanje nije uspjelo.'),
    });
  }

  /** Traces how a single name is matched against the live catalog. */
  protected runTest(): void {
    const name = this.testForm.getRawValue().name;
    if (!name.trim()) {
      return;
    }

    this.adminService.explain(name).subscribe({
      next: (result) => this.explanation.set(result),
      error: () => this.errorMessage.set('Provjera naziva nije uspjela.'),
    });
  }

  /** Puts an unmatched name into the test box, so its rejection can be inspected. */
  protected testUnmatched(name: string): void {
    this.testForm.setValue({ name });
    this.runTest();
  }

  /** Seeds the rule form from an unmatched name, as a starting point for a new rule. */
  protected draftRuleFrom(name: string): void {
    this.cancelEdit();
    this.form.patchValue({
      slug: this.toSlug(name),
      displayName: name,
      requiredPhrase: name,
    });
  }

  /** Candidates worth showing: the ones that nearly matched or were explicitly blocked. */
  protected relevantCandidates(explanation: MatchExplanation) {
    return explanation.allCandidates.filter(
      (candidate) => candidate.isCandidate || candidate.blockingTokens.length > 0,
    );
  }

  private toSlug(value: string): string {
    return value
      .toLowerCase()
      .replace(/[šś]/g, 's')
      .replace(/[čćc]/g, 'c')
      .replace(/[žz]/g, 'z')
      .replace(/đ/g, 'd')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 100);
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }
}

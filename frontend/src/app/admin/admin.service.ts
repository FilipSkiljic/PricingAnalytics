import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BusinessSummary,
  CatalogEntry,
  CorpusImportResult,
  MatchExplanation,
  RulePreview,
  SystemStatistics,
  UnmatchedName,
} from './admin.models';

/** Talks to the /admin endpoints. Every call requires an administrator token. */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5049';

  // ---- catalog ----

  getCatalog(): Observable<CatalogEntry[]> {
    return this.http.get<CatalogEntry[]>(`${this.baseUrl}/admin/catalog`);
  }

  createService(
    slug: string,
    displayName: string,
    requiredPhrase: string,
    excludedWords: string,
  ): Observable<CatalogEntry> {
    return this.http.post<CatalogEntry>(`${this.baseUrl}/admin/catalog`, {
      slug,
      displayName,
      requiredPhrase,
      excludedWords,
    });
  }

  updateService(
    id: number,
    displayName: string,
    requiredPhrase: string,
    excludedWords: string,
  ): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/admin/catalog/${id}`, {
      displayName,
      requiredPhrase,
      excludedWords,
    });
  }

  deleteService(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/catalog/${id}`);
  }

  /** Evaluates a rule against the corpus without saving it. */
  previewRule(requiredPhrase: string, excludedWords: string, editingId: number | null): Observable<RulePreview> {
    return this.http.post<RulePreview>(`${this.baseUrl}/admin/catalog/preview`, {
      requiredPhrase,
      excludedWords,
      editingId,
    });
  }

  getUnmatched(limit = 50): Observable<UnmatchedName[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<UnmatchedName[]>(`${this.baseUrl}/admin/catalog/unmatched`, { params });
  }

  explain(name: string): Observable<MatchExplanation> {
    const params = new HttpParams().set('name', name);
    return this.http.get<MatchExplanation>(`${this.baseUrl}/admin/catalog/explain`, { params });
  }

  // ---- system ----

  getStatistics(): Observable<SystemStatistics> {
    return this.http.get<SystemStatistics>(`${this.baseUrl}/admin/statistics`);
  }

  getBusinesses(): Observable<BusinessSummary[]> {
    return this.http.get<BusinessSummary[]>(`${this.baseUrl}/admin/businesses`);
  }

  deleteBusiness(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/businesses/${id}`);
  }

  importCorpus(file: File): Observable<CorpusImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<CorpusImportResult>(`${this.baseUrl}/admin/corpus/import`, formData);
  }
}

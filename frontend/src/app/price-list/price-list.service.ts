import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResult, PriceListItem } from './price-list.models';

/** Talks to the authenticated /my/services endpoints. */
@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5049';

  getAll(): Observable<PriceListItem[]> {
    return this.http.get<PriceListItem[]>(`${this.baseUrl}/my/services`);
  }

  add(name: string, price: number): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/my/services`, { name, price });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/my/services/${id}`);
  }

  /** Replaces the whole price list with the contents of the uploaded CSV. */
  import(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportResult>(`${this.baseUrl}/my/services/import`, formData);
  }
}

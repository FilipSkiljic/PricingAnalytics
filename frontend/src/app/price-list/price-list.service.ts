import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResult, PriceListItem } from './price-list.models';
import { API_BASE_URL } from '../constants';

/** Talks to the authenticated /my/services endpoints. */
@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<PriceListItem[]> {
    return this.http.get<PriceListItem[]>(`${API_BASE_URL}/my/services`);
  }

  add(name: string, price: number): Observable<unknown> {
    return this.http.post(`${API_BASE_URL}/my/services`, { name, price });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/my/services/${id}`);
  }

  /** Replaces the whole price list with the contents of the uploaded CSV. */
  import(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportResult>(`${API_BASE_URL}/my/services/import`, formData);
  }
}

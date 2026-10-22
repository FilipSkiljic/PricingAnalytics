import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AvailableService, PriceRecommendation } from './analysis.models';
import { API_BASE_URL } from '../constants';

/** Talks to the PricingAnalytics Web API. */
@Injectable({ providedIn: 'root' })
export class AnalysisService {
  private readonly http = inject(HttpClient);

  analyze(service: string, price: number): Observable<PriceRecommendation> {
    const params = new HttpParams().set('service', service).set('price', price);
    return this.http.get<PriceRecommendation>(`${API_BASE_URL}/analyze`, { params });
  }

  getServices(): Observable<AvailableService[]> {
    return this.http.get<AvailableService[]>(`${API_BASE_URL}/services`);
  }
}

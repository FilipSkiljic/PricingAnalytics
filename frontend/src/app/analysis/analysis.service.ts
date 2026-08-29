import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AvailableService, PriceRecommendation } from './analysis.models';

/** Talks to the PricingAnalytics Web API. */
@Injectable({ providedIn: 'root' })
export class AnalysisService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5049';

  analyze(service: string, price: number): Observable<PriceRecommendation> {
    const params = new HttpParams().set('service', service).set('price', price);
    return this.http.get<PriceRecommendation>(`${this.baseUrl}/analyze`, { params });
  }

  getServices(): Observable<AvailableService[]> {
    return this.http.get<AvailableService[]>(`${this.baseUrl}/services`);
  }
}

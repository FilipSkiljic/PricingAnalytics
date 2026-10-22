import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthResult, LoginRequest, RegisterRequest, SignedInBusiness } from './auth.models';
import { API_BASE_URL, SESSION_STORAGE_KEY } from '../constants';

/**
 * Holds the signed-in session. The token is kept in localStorage so a page reload
 * does not log the user out; expiry is checked on read rather than with a timer,
 * so a token that expired while the tab was closed is treated as absent.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly session = signal<SignedInBusiness | null>(this.readStoredSession());

  readonly currentBusiness = computed(() => this.session());
  readonly isSignedIn = computed(() => this.session() !== null);

  /**
   * Only decides what the UI offers. The server checks the role on every admin request,
   * so editing this in the browser gains nothing.
   */
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');

  register(request: RegisterRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${API_BASE_URL}/auth/register`, request)
      .pipe(tap((result) => this.storeSession(result)));
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${API_BASE_URL}/auth/login`, request)
      .pipe(tap((result) => this.storeSession(result)));
  }

  logout(): void {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    this.session.set(null);
  }

  /** The bearer token to attach to API calls, or null when signed out or expired. */
  get token(): string | null {
    return this.session()?.token ?? null;
  }

  private storeSession(result: AuthResult): void {
    const session: SignedInBusiness = { ...result };
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private readStoredSession(): SignedInBusiness | null {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      const session = JSON.parse(raw) as SignedInBusiness;
      if (new Date(session.expiresAtUtc).getTime() <= Date.now()) {
        localStorage.removeItem(SESSION_STORAGE_KEY);
        return null;
      }

      return session;
    } catch {
      // Corrupted or hand-edited storage entry - start signed out rather than crash.
      localStorage.removeItem(SESSION_STORAGE_KEY);
      return null;
    }
  }
}

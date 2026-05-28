import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginRequest, RegisterRequest, AuthResponse } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly TOKEN_KEY = 'auth_token';
  private readonly EXPIRES_KEY = 'auth_expires';

  readonly isAuthenticated = signal(this.checkAuth());

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(response => { this.saveToken(response); this.isAuthenticated.set(true); })
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, request).pipe(
      tap(response => { this.saveToken(response); this.isAuthenticated.set(true); })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.EXPIRES_KEY);
    this.isAuthenticated.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private checkAuth(): boolean {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const expires = localStorage.getItem(this.EXPIRES_KEY);
    if (!token || !expires) return false;
    return new Date(expires) > new Date();
  }

  private saveToken(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.EXPIRES_KEY, response.expiresAt);
  }
}

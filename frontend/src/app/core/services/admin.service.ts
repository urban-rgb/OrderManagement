import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OrderResponse, AdminOrderListParams, UserResponse, AnalyticsResponse } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/admin`;

  getOrders(params: AdminOrderListParams = {}): Observable<OrderResponse[]> {
    let httpParams = new HttpParams();
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.isDescending !== undefined) httpParams = httpParams.set('isDescending', String(params.isDescending));
    if (params.page) httpParams = httpParams.set('page', String(params.page));
    if (params.limit) httpParams = httpParams.set('limit', String(params.limit));
    if (params.userId) httpParams = httpParams.set('userId', params.userId);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    return this.http.get<OrderResponse[]>(`${this.base}/orders`, { params: httpParams });
  }

  getOrder(id: string): Observable<OrderResponse> {
    return this.http.get<OrderResponse>(`${this.base}/orders/${id}`);
  }

  forceUpdateStatus(id: string, newStatus: number): Observable<void> {
    return this.http.post<void>(`${this.base}/orders/${id}/status`, { newStatus });
  }

  getAnalytics(): Observable<AnalyticsResponse> {
    return this.http.get<AnalyticsResponse>(`${this.base}/analytics`);
  }

  getUsers(): Observable<UserResponse[]> {
    return this.http.get<UserResponse[]>(`${this.base}/users`);
  }

  updateUserRole(id: string, newRole: number): Observable<void> {
    return this.http.patch<void>(`${this.base}/users/${id}/role`, { newRole });
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/${id}`);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OrderResponse, CreateOrderRequest, UpdateOrderAddressRequest, OrderListParams } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/orders`;

  getOrders(params: OrderListParams = {}): Observable<OrderResponse[]> {
    let httpParams = new HttpParams();
    if (params.userId) httpParams = httpParams.set('userId', params.userId);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.isDescending !== undefined) httpParams = httpParams.set('isDescending', String(params.isDescending));
    if (params.page) httpParams = httpParams.set('page', String(params.page));
    if (params.limit) httpParams = httpParams.set('limit', String(params.limit));
    return this.http.get<OrderResponse[]>(this.base, { params: httpParams });
  }

  getOrder(id: string): Observable<OrderResponse> {
    return this.http.get<OrderResponse>(`${this.base}/${id}`);
  }

  createOrder(request: CreateOrderRequest): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(this.base, request);
  }

  updateAddress(id: string, request: UpdateOrderAddressRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/address`, request);
  }

  cancelOrder(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }
}

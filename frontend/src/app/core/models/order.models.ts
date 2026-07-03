export enum OrderStatus {
  Pending = 'Pending',
  Paid = 'Paid',
  InTransit = 'InTransit',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled'
}

export interface OrderItemRequest {
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderItemResponse {
  id: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderResponse {
  id: string;
  userId: string;
  status: OrderStatus;
  items: OrderItemResponse[];
  shippingAddress: string;
  totalAmount: number;
  createdAt: string;
}

export interface CreateOrderRequest {
  items: OrderItemRequest[];
  shippingAddress: string;
}

export interface UpdateOrderAddressRequest {
  newAddress: string;
}

export interface OrderListParams {
  sortBy?: 'amount' | 'status' | 'createdAt';
  isDescending?: boolean;
  page?: number;
  limit?: number;
}

export interface AdminOrderListParams {
  sortBy?: 'amount' | 'status' | 'createdAt';
  isDescending?: boolean;
  page?: number;
  limit?: number;
  userId?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface UserResponse {
  id: string;
  email: string;
  role: string;
}

export interface AnalyticsResponse {
  totalRevenue: number;
  ordersByStatus: OrderStatusCount[];
  topProducts: TopProduct[];
}

export interface OrderStatusCount {
  status: string;
  count: number;
}

export interface TopProduct {
  name: string;
  totalQuantity: number;
}

export enum OrderStatus {
  Pending = 'Pending',
  Paid = 'Paid',
  InTransit = 'InTransit',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled'
}

export interface OrderResponse {
  id: string;
  status: OrderStatus;
  products: string;
  shippingAddress: string;
  totalAmount: number;
  createdAt: string;
}

export interface CreateOrderRequest {
  userId: string;
  products: string;
  shippingAddress: string;
  totalAmount: number;
}

export interface UpdateOrderAddressRequest {
  newAddress: string;
}

export interface OrderListParams {
  userId?: string;
  sortBy?: 'amount' | 'status' | 'createdAt';
  isDescending?: boolean;
  page?: number;
  limit?: number;
}

import { Component, Input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderResponse, OrderStatus } from '../../core/models/order.models';

interface StatSlice {
  status: OrderStatus;
  count: number;
  percent: number;
  color: string;
  offset: number;
}

const STATUS_COLORS: Record<OrderStatus, string> = {
  [OrderStatus.Pending]:   '#f59e0b',
  [OrderStatus.Paid]:      '#3b82f6',
  [OrderStatus.InTransit]: '#8b5cf6',
  [OrderStatus.Delivered]: '#10b981',
  [OrderStatus.Cancelled]: '#ef4444',
};

@Component({
  selector: 'app-order-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-stats.component.html',
  styleUrl: './order-stats.component.css'
})
export class OrderStatsComponent {
  @Input() set orders(value: OrderResponse[]) {
    this._orders = value;
    this.slices = this.buildSlices(value);
  }

  _orders: OrderResponse[] = [];
  slices: StatSlice[] = [];
  hovered: OrderStatus | null = null;

  readonly r = 54;
  readonly cx = 70;
  readonly cy = 70;
  readonly circumference = 2 * Math.PI * this.r;

  private buildSlices(orders: OrderResponse[]): StatSlice[] {
    const total = orders.length;
    if (total === 0) return [];

    const counts = orders.reduce((acc, o) => {
      acc[o.status] = (acc[o.status] ?? 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    let offset = 0;
    return Object.values(OrderStatus).map(status => {
      const count = counts[status] ?? 0;
      const percent = total > 0 ? (count / total) * 100 : 0;
      const slice: StatSlice = {
        status,
        count,
        percent,
        color: STATUS_COLORS[status],
        offset
      };
      offset += percent;
      return slice;
    });
  }

  getStrokeDasharray(percent: number): string {
    const filled = (percent / 100) * this.circumference;
    return `${filled} ${this.circumference - filled}`;
  }

  getStrokeDashoffset(offset: number): string {
    const shift = (offset / 100) * this.circumference;
    return `${this.circumference / 4 - shift}`;
  }

  get total(): number {
    return this._orders.length;
  }

  get hoveredSlice(): StatSlice | null {
    return this.slices.find(s => s.status === this.hovered) ?? null;
  }
}

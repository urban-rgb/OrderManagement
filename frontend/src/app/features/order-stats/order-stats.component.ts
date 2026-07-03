import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderResponse, OrderStatus } from '../../core/models/order.models';

interface StatSlice {
  status: OrderStatus;
  count: number;
  percent: number;
  color: string;
  offset: number;
}

interface RevenueSlice {
  status: OrderStatus;
  total: number;
  percent: number;
  color: string;
}

interface DayBar {
  date: string;
  count: number;
  heightPx: number;
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
    this.revenueSlices = this.buildRevenueSlices(value);
    this.dayBars = this.buildDayBars(value);
  }

  _orders: OrderResponse[] = [];
  slices: StatSlice[] = [];
  revenueSlices: RevenueSlice[] = [];
  dayBars: DayBar[] = [];
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
      const percent = (count / total) * 100;
      const slice: StatSlice = { status, count, percent, color: STATUS_COLORS[status], offset };
      offset += percent;
      return slice;
    });
  }

  private buildRevenueSlices(orders: OrderResponse[]): RevenueSlice[] {
    if (orders.length === 0) return [];
    const totals = orders.reduce((acc, o) => {
      acc[o.status] = (acc[o.status] ?? 0) + o.totalAmount;
      return acc;
    }, {} as Record<string, number>);
    const max = Math.max(...Object.values(totals));
    return Object.values(OrderStatus)
      .map(status => ({
        status,
        total: totals[status] ?? 0,
        percent: max > 0 ? ((totals[status] ?? 0) / max) * 100 : 0,
        color: STATUS_COLORS[status],
      }))
      .filter(s => s.total > 0);
  }

  private buildDayBars(orders: OrderResponse[]): DayBar[] {
    if (orders.length === 0) return [];
    const counts = orders.reduce((acc, o) => {
      const day = o.createdAt.slice(0, 10);
      acc[day] = (acc[day] ?? 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    const sorted = Object.entries(counts).sort(([a], [b]) => a.localeCompare(b));
    const max = Math.max(...sorted.map(([, c]) => c));
    return sorted.map(([date, count]) => ({
      date,
      count,
      heightPx: max > 0 ? Math.max(4, Math.round((count / max) * 64)) : 4,
    }));
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

  get totalRevenue(): number {
    return this._orders.reduce((sum, o) => sum + o.totalAmount, 0);
  }

  get hoveredSlice(): StatSlice | null {
    return this.slices.find(s => s.status === this.hovered) ?? null;
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return `${d.getDate()}/${d.getMonth() + 1}`;
  }
}

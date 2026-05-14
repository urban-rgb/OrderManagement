import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-order-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './order-form.component.html',
  styleUrl: './order-form.component.css'
})
export class OrderFormComponent {
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  loading = false;
  error: string | null = null;

  form = this.fb.group({
    userId: ['', [Validators.required, Validators.pattern(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)]],
    products: ['', Validators.required],
    shippingAddress: ['', Validators.required],
    totalAmount: [null as number | null, [Validators.required, Validators.min(0.01)]]
  });

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control?.touched);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    this.error = null;
    const value = this.form.getRawValue();

    this.orderService.createOrder({
      userId: value.userId!,
      products: value.products!,
      shippingAddress: value.shippingAddress!,
      totalAmount: value.totalAmount!
    }).subscribe({
      next: (order) => this.router.navigate(['/orders', order.id]),
      error: (err) => {
        this.error = err.error?.error ?? 'Failed to create order';
        this.loading = false;
      }
    });
  }
}

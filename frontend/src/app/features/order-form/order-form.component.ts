import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormArray } from '@angular/forms';
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
    shippingAddress: ['', Validators.required],
    items: this.fb.array([this.createItem()])
  });

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  private createItem() {
    return this.fb.group({
      name: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [null as number | null, [Validators.required, Validators.min(0.01)]]
    });
  }

  addItem(): void {
    this.items.push(this.createItem());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) this.items.removeAt(index);
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control?.touched);
  }

  isItemInvalid(index: number, field: string): boolean {
    const control = this.items.at(index).get(field);
    return !!(control?.invalid && control?.touched);
  }

  computedTotal(): number {
    return this.items.controls.reduce((sum, ctrl) => {
      return sum + (ctrl.get('quantity')?.value ?? 0) * (ctrl.get('unitPrice')?.value ?? 0);
    }, 0);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    this.error = null;
    const value = this.form.getRawValue();

    this.orderService.createOrder({
      shippingAddress: value.shippingAddress!,
      items: value.items!.map(i => ({
        name: i.name!,
        quantity: i.quantity!,
        unitPrice: i.unitPrice!
      }))
    }).subscribe({
      next: (order) => this.router.navigate(['/orders', order.id]),
      error: (err) => {
        this.error = err.error?.error ?? 'Failed to create order';
        this.loading = false;
      }
    });
  }
}

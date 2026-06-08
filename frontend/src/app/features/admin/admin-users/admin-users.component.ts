import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { UserResponse } from '../../../core/models/order.models';

interface UserRow extends UserResponse {
  selectedRole: number;
  saving: boolean;
  deleting: boolean;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  private readonly allUsers = signal<UserRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchEmail = signal('');

  readonly roles = [
    { label: 'User', value: 0 },
    { label: 'Admin', value: 1 }
  ];

  readonly users = computed(() => {
    const q = this.searchEmail().toLowerCase().trim();
    if (!q) return this.allUsers();
    return this.allUsers().filter(u => u.email.toLowerCase().includes(q));
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getUsers().subscribe({
      next: (data) => {
        this.allUsers.set(data.map(u => ({
          ...u,
          selectedRole: u.role === 'Admin' ? 1 : 0,
          saving: false,
          deleting: false
        })));
        this.loading.set(false);
      },
      error: (err) => { this.error.set(err.error?.error ?? 'Failed to load users'); this.loading.set(false); }
    });
  }

  setSelectedRole(id: string, value: number): void {
    this.allUsers.update(list => list.map(u => u.id === id ? { ...u, selectedRole: value } : u));
  }

  saveRole(id: string): void {
    const user = this.allUsers().find(u => u.id === id);
    if (!user) return;
    this.allUsers.update(list => list.map(u => u.id === id ? { ...u, saving: true } : u));
    this.adminService.updateUserRole(id, user.selectedRole).subscribe({
      next: () => {
        this.allUsers.update(list => list.map(u =>
          u.id === id ? { ...u, role: u.selectedRole === 1 ? 'Admin' : 'User', saving: false } : u
        ));
      },
      error: () => {
        this.allUsers.update(list => list.map(u => u.id === id ? { ...u, saving: false } : u));
      }
    });
  }

  deleteUser(id: string, email: string): void {
    if (!confirm(`Delete user ${email}?`)) return;
    this.allUsers.update(list => list.map(u => u.id === id ? { ...u, deleting: true } : u));
    this.adminService.deleteUser(id).subscribe({
      next: () => { this.allUsers.update(list => list.filter(u => u.id !== id)); },
      error: () => { this.allUsers.update(list => list.map(u => u.id === id ? { ...u, deleting: false } : u)); }
    });
  }
}

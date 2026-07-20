import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AuditLogService } from '../../core/services/audit-log.service';
import { AuditLogEntry } from '../../core/models/audit-log.model';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent {
  readonly productIdControl = new FormControl<number | null>(null);
  readonly loading = signal(false);
  readonly searched = signal(false);
  readonly entries = signal<AuditLogEntry[]>([]);

  constructor(private auditLogService: AuditLogService) {}

  search(): void {
    const productId = this.productIdControl.value;
    if (!productId) return;

    this.loading.set(true);
    this.searched.set(true);
    this.auditLogService.getByProduct(productId).subscribe({
      next: (res) => {
        this.entries.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}

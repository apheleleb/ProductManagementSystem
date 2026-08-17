import { Component, OnInit, signal } from '@angular/core';
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
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  readonly productIdControl = new FormControl<number | null>(null); // now optional
  readonly loading = signal(false);
  readonly entries = signal<AuditLogEntry[]>([]);

  constructor(private auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.search(); // loads everything immediately, no ID needed
  }

  search(): void {
    this.loading.set(true);
    const productId = this.productIdControl.value;
    this.auditLogService.getAll(productId ? { productId } : undefined).subscribe({
      next: (res) => { this.entries.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  clear(): void {
    this.productIdControl.setValue(null);
    this.search();
  }

}

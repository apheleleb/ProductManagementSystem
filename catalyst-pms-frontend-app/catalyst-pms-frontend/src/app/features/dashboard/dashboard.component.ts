import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/services/auth.service';
import { ProductStats, ProductSummary } from '../../core/models/product.model';
import { statusSlug } from '../../core/models/enums';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  readonly loading = signal(true);
  readonly stats = signal<ProductStats | null>(null);
  readonly recent = signal<ProductSummary[]>([]);
  readonly statusSlug = statusSlug;

  constructor(
    private productService: ProductService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.productService.getStats().subscribe({
      next: (res) => this.stats.set(res.data),
      error: () => {}
    });

    this.productService.getRecent(6).subscribe({
      next: (res) => {
        this.recent.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}

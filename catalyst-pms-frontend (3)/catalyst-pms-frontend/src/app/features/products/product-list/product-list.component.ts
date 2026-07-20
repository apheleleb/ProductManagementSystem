import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProductSummary } from '../../../core/models/product.model';
import { Category } from '../../../core/models/category.model';
import { statusSlug, ProductStatusName } from '../../../core/models/enums';

const STATUS_OPTIONS: { id: number; name: string }[] = [
  { id: 1, name: ProductStatusName.Draft },
  { id: 2, name: ProductStatusName.PendingApproval },
  { id: 3, name: ProductStatusName.Approved },
  { id: 4, name: ProductStatusName.Rejected },
  { id: 5, name: ProductStatusName.Published }
  // Archived is intentionally excluded — archived products are soft-deleted and
  // excluded from search by the backend's global query filter. Managers can view
  // and restore them from the recycle bin tab on the Workflow page instead.
];

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatButtonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  readonly loading = signal(true);
  readonly products = signal<ProductSummary[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly statusSlug = statusSlug;
  readonly statusOptions = STATUS_OPTIONS;

  readonly searchControl = new FormControl('');
  readonly categoryControl = new FormControl<number | null>(null);
  readonly statusControl = new FormControl<number | null>(null);

  readonly displayedColumns = ['name', 'sku', 'brand', 'category', 'status', 'price', 'updated', 'actions'];

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.categoryService.getAll().subscribe({
      next: (res) => this.categories.set(res.data),
      error: () => {}
    });

    this.searchControl.valueChanges.pipe(debounceTime(350), distinctUntilChanged()).subscribe(() => {
      this.page.set(1);
      this.loadProducts();
    });

    this.categoryControl.valueChanges.subscribe(() => {
      this.page.set(1);
      this.loadProducts();
    });

    this.statusControl.valueChanges.subscribe(() => {
      this.page.set(1);
      this.loadProducts();
    });

    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService
      .search({
        search: this.searchControl.value || undefined,
        categoryId: this.categoryControl.value || undefined,
        statusId: this.statusControl.value || undefined,
        page: this.page(),
        pageSize: this.pageSize()
      })
      .subscribe({
        next: (res) => {
          this.products.set(res.data.items);
          this.totalCount.set(res.data.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadProducts();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.categoryControl.setValue(null);
    this.statusControl.setValue(null);
  }
}

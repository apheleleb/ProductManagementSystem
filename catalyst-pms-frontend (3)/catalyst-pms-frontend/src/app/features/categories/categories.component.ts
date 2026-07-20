import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';

import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models/category.model';
import { ApiResponse } from '../../core/models/api-response.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly categories = signal<Category[]>([]);
  readonly editingId = signal<number | null>(null);
  readonly displayedColumns = ['name', 'description', 'status', 'actions'];

  readonly form = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.categoryService.getAll().subscribe({
      next: (res) => {
        this.categories.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  startEdit(category: Category): void {
    this.editingId.set(category.categoryId);
    this.form.patchValue({ name: category.name, description: category.description });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue() as { name: string; description: string };
    const editingId = this.editingId();

    const request$: Observable<ApiResponse<unknown>> = editingId
      ? this.categoryService.update(editingId, value)
      : this.categoryService.create(value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open(editingId ? 'Category updated.' : 'Category created.', 'Dismiss', { duration: 3000 });
        this.form.reset();
        this.editingId.set(null);
        this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  deactivate(category: Category): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Deactivate category',
          message: `Deactivate "${category.name}"? It will no longer be selectable for new products.`,
          confirmLabel: 'Deactivate',
          tone: 'danger'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.categoryService.deactivate(category.categoryId).subscribe({
          next: () => {
            this.snackBar.open('Category deactivated.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => {}
        });
      });
  }
}


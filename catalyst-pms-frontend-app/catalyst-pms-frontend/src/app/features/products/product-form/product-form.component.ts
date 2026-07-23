import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../core/models/category.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly isEditMode = signal(false);
  readonly productId = signal<number | null>(null);
  readonly imagePreviewUrl = signal<string | null>(null);
  readonly selectedImage = signal<File | null>(null);
  readonly existingHasImage = signal(false);

  readonly form = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    sku: ['', Validators.required],
    brand: ['', Validators.required],
    unitPrice: [0, [Validators.required, Validators.min(0.01)]],
    categoryId: [null as number | null, Validators.required],
    specifications: this.fb.array([])
  });

  get specifications(): FormArray {
    return this.form.get('specifications') as FormArray;
  }

  ngOnInit(): void {
    this.categoryService.getAll().subscribe({
      next: (res) => this.categories.set(res.data.filter((c) => c.isActive)),
      error: () => {}
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.productId.set(id);
      this.isEditMode.set(true);
      this.loading.set(true);

      this.productService.getById(id).subscribe({
        next: (res) => {
          const p = res.data;
          this.form.patchValue({
            name: p.name,
            description: p.description,
            sku: p.sku,
            brand: p.brand,
            unitPrice: p.unitPrice,
            categoryId: p.categoryId
          });
          this.form.get('sku')?.disable();
          p.specifications.forEach((spec) => this.addSpecification(spec.key, spec.value));
          this.existingHasImage.set(p.hasImage);
          if (p.hasImage) {
            this.imagePreviewUrl.set(this.productService.getImageUrl(id));
          }
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else {
      this.addSpecification();
    }
  }

  addSpecification(key = '', value = ''): void {
    this.specifications.push(
      this.fb.group({
        key: [key, Validators.required],
        value: [value, Validators.required]
      })
    );
  }

  removeSpecification(index: number): void {
    this.specifications.removeAt(index);
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.selectedImage.set(file);
    const reader = new FileReader();
    reader.onload = () => this.imagePreviewUrl.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  clearImage(): void {
    this.selectedImage.set(null);
    this.imagePreviewUrl.set(null);
    this.existingHasImage.set(false);
  }

  private buildFormValue() {
    const raw = this.form.getRawValue();
    return {
      name: raw.name!,
      description: raw.description!,
      sku: raw.sku!,
      brand: raw.brand!,
      unitPrice: Number(raw.unitPrice),
      categoryId: Number(raw.categoryId),
      specifications: (raw.specifications as { key: string; value: string }[]) ?? []
    };
  }

  saveDraft(): void {
    this.submit(false);
  }

  saveAndSubmit(): void {
    this.submit(true);
  }

  private submit(submitForApproval: boolean): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Please fill in all required fields.', 'Dismiss', { duration: 4000 });
      return;
    }

    this.saving.set(true);
    const value = this.buildFormValue();

    if (this.isEditMode() && this.productId()) {
      this.productService.update(this.productId()!, value, this.selectedImage()).subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Product updated.', 'Dismiss', { duration: 3000 });
          this.router.navigate(['/products', this.productId()]);
        },
        error: () => this.saving.set(false)
      });
    } else {
      this.productService.create(value, this.selectedImage(), submitForApproval).subscribe({
        next: (res) => {
          this.saving.set(false);
          this.snackBar.open(
            submitForApproval ? 'Product submitted for approval.' : 'Draft saved.',
            'Dismiss',
            { duration: 3000 }
          );
          this.router.navigate(['/products', res.data.productId]);
        },
        error: () => this.saving.set(false)
      });
    }
  }
}

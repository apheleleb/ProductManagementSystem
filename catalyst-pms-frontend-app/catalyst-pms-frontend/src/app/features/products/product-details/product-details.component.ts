import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ProductService } from '../../../core/services/product.service';
import { WorkflowService } from '../../../core/services/workflow.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProductDetail } from '../../../core/models/product.model';
import { statusSlug, ProductStatusName } from '../../../core/models/enums';
import {
  ConfirmDialogComponent,
  ConfirmDialogResult
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTabsModule
  ],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss'
})
export class ProductDetailsComponent implements OnInit {
  readonly loading = signal(true);
  readonly product = signal<ProductDetail | null>(null);
  readonly acting = signal(false);
  readonly statusSlug = statusSlug;
  readonly ProductStatusName = ProductStatusName;

  private productId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private workflowService: WorkflowService,
    public authService: AuthService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.productId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.productService.getById(this.productId).subscribe({
      next: (res) => {
        this.product.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  get imageUrl(): string {
    return this.productService.getImageUrl(this.productId);
  }

  canSubmit(): boolean {
    const p = this.product();
    if (!p) return false;
    return (
      this.authService.isCapturer() &&
      p.createdByUserId === this.authService.currentUser()?.userId &&
      (p.statusName === ProductStatusName.Draft || p.statusName === ProductStatusName.Rejected)
    );
  }

  canEdit(): boolean {
    const p = this.product();
    if (!p) return false;
    return (
      this.authService.isCapturer() &&
      p.createdByUserId === this.authService.currentUser()?.userId &&
      p.statusName !== ProductStatusName.Published &&
      p.statusName !== ProductStatusName.Archived
    );
  }

  submitForApproval(): void {
    this.acting.set(true);
    this.productService.submitForApproval(this.productId).subscribe({
      next: () => {
        this.acting.set(false);
        this.snackBar.open('Product submitted for approval.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: () => this.acting.set(false)
    });
  }

  approve(): void {
    this.openConfirm({
      title: 'Approve product',
      message: `Approve "${this.product()?.name}"? It will be ready to publish.`,
      confirmLabel: 'Approve'
    }).subscribe((result) => {
      if (!result?.confirmed) return;
      this.acting.set(true);
      this.workflowService.approve(this.productId, { comment: result.comment }).subscribe({
        next: () => {
          this.acting.set(false);
          this.snackBar.open('Product approved.', 'Dismiss', { duration: 3000 });
          this.load();
        },
        error: () => this.acting.set(false)
      });
    });
  }

  reject(): void {
    this.openConfirm({
      title: 'Reject product',
      message: `Reject "${this.product()?.name}"? The capturer will be notified.`,
      confirmLabel: 'Reject',
      tone: 'danger',
      requireComment: true,
      commentLabel: 'Reason for rejection'
    }).subscribe((result) => {
      if (!result?.confirmed) return;
      this.acting.set(true);
      this.workflowService.reject(this.productId, { comment: result.comment! }).subscribe({
        next: () => {
          this.acting.set(false);
          this.snackBar.open('Product rejected.', 'Dismiss', { duration: 3000 });
          this.load();
        },
        error: () => this.acting.set(false)
      });
    });
  }

  publish(): void {
    this.openConfirm({
      title: 'Publish product',
      message: `Publish "${this.product()?.name}" and sync it to the Data Lake?`,
      confirmLabel: 'Publish'
    }).subscribe((result) => {
      if (!result?.confirmed) return;
      this.acting.set(true);
      this.workflowService.publish(this.productId).subscribe({
        next: () => {
          this.acting.set(false);
          this.snackBar.open('Product published.', 'Dismiss', { duration: 3000 });
          this.load();
        },
        error: () => this.acting.set(false)
      });
    });
  }

  unpublish(): void {
    this.openConfirm({
      title: 'Unpublish product',
      message: `Take "${this.product()?.name}" off the live catalog?`,
      confirmLabel: 'Unpublish',
      tone: 'danger'
    }).subscribe((result) => {
      if (!result?.confirmed) return;
      this.acting.set(true);
      this.workflowService.unpublish(this.productId).subscribe({
        next: () => {
          this.acting.set(false);
          this.snackBar.open('Product unpublished.', 'Dismiss', { duration: 3000 });
          this.load();
        },
        error: () => this.acting.set(false)
      });
    });
  }

  archive(): void {
    this.openConfirm({
      title: 'Archive product',
      message: `Archive "${this.product()?.name}"? This soft-deletes it — it will be hidden from the catalog and search, but a manager can restore it later from the workflow queue's recycle bin.`,
      confirmLabel: 'Archive',
      tone: 'danger'
    }).subscribe((result) => {
      if (!result?.confirmed) return;
      this.acting.set(true);
      this.workflowService.archive(this.productId).subscribe({
        next: () => {
          this.acting.set(false);
          this.snackBar.open('Product archived.', 'Dismiss', { duration: 3000 });
          // The product is now soft-deleted and excluded from the default query filter,
          // so reloading this same detail page would 404. Navigate back to the list instead.
          this.router.navigate(['/products']);
        },
        error: () => this.acting.set(false)
      });
    });
  }

  private openConfirm(data: {
    title: string;
    message: string;
    confirmLabel: string;
    tone?: 'default' | 'danger';
    requireComment?: boolean;
    commentLabel?: string;
  }) {
    return this.dialog
      .open<ConfirmDialogComponent, typeof data, ConfirmDialogResult>(ConfirmDialogComponent, { data, width: '420px' })
      .afterClosed();
  }
}

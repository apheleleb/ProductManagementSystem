import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ProductService } from '../../core/services/product.service';
import { WorkflowService } from '../../core/services/workflow.service';
import { ProductSummary, ArchivedProduct } from '../../core/models/product.model';
import { statusSlug, ProductStatusName } from '../../core/models/enums';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-workflow',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './workflow.component.html',
  styleUrl: './workflow.component.scss'
})
export class WorkflowComponent implements OnInit {
  readonly loading = signal(true);
  readonly acting = signal<number | null>(null);
  readonly pending = signal<ProductSummary[]>([]);
  readonly approved = signal<ProductSummary[]>([]);
  readonly published = signal<ProductSummary[]>([]);
  readonly archived = signal<ArchivedProduct[]>([]);
  readonly statusSlug = statusSlug;

  constructor(
    private productService: ProductService,
    private workflowService: WorkflowService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.productService.search({ statusId: 2, pageSize: 50 }).subscribe({
      next: (res) => this.pending.set(res.data.items),
      error: () => {}
    });

    this.productService.search({ statusId: 3, pageSize: 50 }).subscribe({
      next: (res) => this.approved.set(res.data.items),
      error: () => {}
    });

    this.productService.search({ statusId: 5, pageSize: 50 }).subscribe({
      next: (res) => this.published.set(res.data.items),
      error: () => {}
    });

    this.productService.getDeleted(1, 50).subscribe({
      next: (res) => {
        this.archived.set(res.data.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  approve(product: ProductSummary): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Approve product',
          message: `Approve "${product.name}"?`,
          confirmLabel: 'Approve'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.acting.set(product.productId);
        this.workflowService.approve(product.productId, { comment: result.comment }).subscribe({
          next: () => {
            this.acting.set(null);
            this.snackBar.open('Product approved.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => this.acting.set(null)
        });
      });
  }

  reject(product: ProductSummary): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Reject product',
          message: `Reject "${product.name}"?`,
          confirmLabel: 'Reject',
          tone: 'danger',
          requireComment: true,
          commentLabel: 'Reason for rejection'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.acting.set(product.productId);
        this.workflowService.reject(product.productId, { comment: result.comment! }).subscribe({
          next: () => {
            this.acting.set(null);
            this.snackBar.open('Product rejected.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => this.acting.set(null)
        });
      });
  }

  publish(product: ProductSummary): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Publish product',
          message: `Publish "${product.name}" to the live catalog?`,
          confirmLabel: 'Publish'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.acting.set(product.productId);
        this.workflowService.publish(product.productId).subscribe({
          next: () => {
            this.acting.set(null);
            this.snackBar.open('Product published.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => this.acting.set(null)
        });
      });
  }

  unpublish(product: ProductSummary): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Unpublish product',
          message: `Take "${product.name}" off the live catalog?`,
          confirmLabel: 'Unpublish',
          tone: 'danger'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.acting.set(product.productId);
        this.workflowService.unpublish(product.productId).subscribe({
          next: () => {
            this.acting.set(null);
            this.snackBar.open('Product unpublished.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => this.acting.set(null)
        });
      });
  }

  restore(product: ArchivedProduct): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '420px',
        data: {
          title: 'Restore product',
          message: `Restore "${product.name}" from the archive? It will return to Approved status.`,
          confirmLabel: 'Restore'
        }
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result?.confirmed) return;
        this.acting.set(product.productId);
        this.workflowService.restore(product.productId).subscribe({
          next: () => {
            this.acting.set(null);
            this.snackBar.open('Product restored.', 'Dismiss', { duration: 3000 });
            this.load();
          },
          error: () => this.acting.set(null)
        });
      });
  }
}

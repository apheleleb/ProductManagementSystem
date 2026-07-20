import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: 'default' | 'danger';
  requireComment?: boolean;
  commentLabel?: string;
}

export interface ConfirmDialogResult {
  confirmed: boolean;
  comment?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  comment = '';

  constructor(
    private dialogRef: MatDialogRef<ConfirmDialogComponent, ConfirmDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) {}

  get canConfirm(): boolean {
    return !this.data.requireComment || this.comment.trim().length > 0;
  }

  onCancel(): void {
    this.dialogRef.close({ confirmed: false });
  }

  onConfirm(): void {
    if (!this.canConfirm) return;
    this.dialogRef.close({ confirmed: true, comment: this.comment.trim() });
  }
}

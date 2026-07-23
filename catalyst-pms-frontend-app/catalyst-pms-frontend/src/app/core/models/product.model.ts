import { AuditLogEntry } from './audit-log.model';
import { ApprovalHistoryEntry } from './approval.model';

export interface ProductSpecification {
  key: string;
  value: string;
}

export interface ProductSummary {
  productId: number;
  name: string;
  sku: string;
  brand: string;
  categoryName: string;
  statusName: string;
  unitPrice: number;
  updatedAt: string;
  createdByUserId: string;
}

export interface ProductDetail {
  productId: number;
  name: string;
  description: string;
  sku: string;
  brand: string;
  unitPrice: number;
  categoryId: number;
  categoryName: string;
  statusId: number;
  statusName: string;
  createdByUserId: string;
  approvedByUserId?: string | null;
  createdAt: string;
  updatedAt: string;
  hasImage: boolean;
  specifications: ProductSpecification[];
  approvalHistory: ApprovalHistoryEntry[];
  auditLog: AuditLogEntry[];
}

export interface ProductFormValue {
  name: string;
  description: string;
  sku: string;
  brand: string;
  unitPrice: number;
  categoryId: number;
  specifications: ProductSpecification[];
}

export interface ProductSearchParams {
  search?: string;
  categoryId?: number;
  statusId?: number;
  page?: number;
  pageSize?: number;
}

export interface ProductStats {
  total: number;
  pendingApproval: number;
  approved: number;
  rejected: number;
  active: number;
}

export interface ArchivedProduct {
  productId: number;
  name: string;
  sku: string;
  brand: string;
  categoryName: string;
  unitPrice: number;
  deletedAt: string;
  deletedByUserId: string;
  createdByUserId: string;
}

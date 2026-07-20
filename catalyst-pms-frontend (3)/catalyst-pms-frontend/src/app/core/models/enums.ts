export enum UserRole {
  ProductCapturer = 'ProductCapturer',
  ProductManager = 'ProductManager'
}

// Mirrors ProductStatus lookup table on the backend (StatusId -> StatusName)
export enum ProductStatusName {
  Draft = 'Draft',
  PendingApproval = 'Pending Approval',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Published = 'Published',
  Archived = 'Archived'
}

// Slug used for CSS class / chip styling, derived from the status name.
export function statusSlug(statusName: string): string {
  return statusName.toLowerCase().replace(/\s+/g, '-');
}

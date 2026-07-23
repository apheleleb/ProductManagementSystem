export interface ApprovalHistoryEntry {
  action: string;
  comment?: string | null;
  actorUserId: string;
  actionDate: string;
}

export interface ApproveProductRequest {
  comment?: string | null;
}

export interface RejectProductRequest {
  comment: string;
}

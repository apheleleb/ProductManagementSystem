export interface AuditLogEntry {
  logId: number;
  actionType: string;
  fieldName?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  actorUserId: string;
  loggedAt: string;
}

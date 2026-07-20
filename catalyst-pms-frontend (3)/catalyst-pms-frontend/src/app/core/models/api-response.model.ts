export interface ApiResponse<T> {
  success: boolean;
  message?: string | null;
  data: T;
  errors: string[];
}

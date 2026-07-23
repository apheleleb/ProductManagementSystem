export interface Category {
  categoryId: number;
  name: string;
  description: string;
  isActive: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  description: string;
}

export interface UpdateCategoryRequest {
  name: string;
  description: string;
}

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { PagedResponse } from '../models/paged-response.model';
import {
  ArchivedProduct,
  ProductDetail,
  ProductFormValue,
  ProductSearchParams,
  ProductStats,
  ProductSummary
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  search(params: ProductSearchParams): Observable<ApiResponse<PagedResponse<ProductSummary>>> {
    let httpParams = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));

    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.categoryId) httpParams = httpParams.set('categoryId', String(params.categoryId));
    if (params.statusId) httpParams = httpParams.set('statusId', String(params.statusId));

    return this.http.get<ApiResponse<PagedResponse<ProductSummary>>>(this.baseUrl, { params: httpParams });
  }

  getById(id: number): Observable<ApiResponse<ProductDetail>> {
    return this.http.get<ApiResponse<ProductDetail>>(`${this.baseUrl}/${id}`);
  }

  getMyDrafts(): Observable<ApiResponse<ProductSummary[]>> {
    return this.http.get<ApiResponse<ProductSummary[]>>(`${this.baseUrl}/my-drafts`);
  }

  create(value: ProductFormValue, image: File | null, submitForApproval: boolean): Observable<ApiResponse<{ productId: number }>> {
    const formData = this.toFormData(value, image);
    const params = new HttpParams().set('submitForApproval', String(submitForApproval));
    return this.http.post<ApiResponse<{ productId: number }>>(this.baseUrl, formData, { params });
  }

  update(id: number, value: ProductFormValue, image: File | null): Observable<ApiResponse<null>> {
    const formData = this.toFormData(value, image);
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}`, formData);
  }

  submitForApproval(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${id}/submit`, {});
  }

  getStats(): Observable<ApiResponse<ProductStats>> {
    return this.http.get<ApiResponse<ProductStats>>(`${this.baseUrl}/stats`);
  }

  getRecent(count = 5): Observable<ApiResponse<ProductSummary[]>> {
    return this.http.get<ApiResponse<ProductSummary[]>>(`${this.baseUrl}/recent`, {
      params: new HttpParams().set('count', String(count))
    });
  }

  getImageUrl(productId: number): string {
    return `${this.baseUrl}/${productId}/image`;
  }

  /** Manager-only "recycle bin" — soft-deleted products, bypassing the default query filter. */
  getDeleted(page = 1, pageSize = 20): Observable<ApiResponse<PagedResponse<ArchivedProduct>>> {
    const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    return this.http.get<ApiResponse<PagedResponse<ArchivedProduct>>>(`${this.baseUrl}/deleted`, { params });
  }

  getDeletedById(id: number): Observable<ApiResponse<ProductDetail>> {
    return this.http.get<ApiResponse<ProductDetail>>(`${this.baseUrl}/deleted/${id}`);
  }

  private toFormData(value: ProductFormValue, image: File | null): FormData {
    const formData = new FormData();
    formData.append('Name', value.name);
    formData.append('Description', value.description);
    if ('sku' in value && value.sku) formData.append('Sku', value.sku);
    formData.append('Brand', value.brand);
    formData.append('UnitPrice', String(value.unitPrice));
    formData.append('CategoryId', String(value.categoryId));

    value.specifications.forEach((spec, index) => {
      formData.append(`Specifications[${index}].Key`, spec.key);
      formData.append(`Specifications[${index}].Value`, spec.value);
    });

    if (image) formData.append('Image', image);

    return formData;
  }
}

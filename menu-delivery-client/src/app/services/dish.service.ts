
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Dish {
  id: number;
  name: string;
  price: number;
  weightGrams: number;
  calories: number;
  isAvailable: boolean;
  categoryId: number;
  imageFileName: string | null;
  authorId: number;
}

export interface DishResponse {
  items: Dish[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DishFilters {
  categoryId?: number;
  isAvailable?: boolean;
  maxPrice?: number;
  sortBy?: 'price' | 'calories';
}

export interface CreateDishRequest {
  name: string;
  price: number;
  weightGrams: number;
  calories: number;
  isAvailable: boolean;
  categoryId: number;
  imageFileName: string | null;
}

export interface UpdateDishRequest {
  name: string;
  price: number;
  weightGrams: number;
  calories: number;
  isAvailable: boolean;
  categoryId: number;
  imageFileName: string | null;
}

export interface UploadImageResponse {
  fileName: string;
}

@Injectable({
  providedIn: 'root'
})
export class DishService {
  private http = inject(HttpClient);

  private apiUrl = 'http://localhost:5242/api/dishes/';

  getDishes(
    page: number = 1,
    pageSize: number = 50,
    filters: DishFilters = {}
  ): Observable<DishResponse> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (filters.categoryId !== undefined) {
      params = params.set('categoryId', filters.categoryId);
    }

    if (filters.isAvailable !== undefined) {
      params = params.set('isAvailable', filters.isAvailable);
    }

    if (filters.maxPrice !== undefined) {
      params = params.set('maxPrice', filters.maxPrice);
    }

    if (filters.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }

    return this.http.get<DishResponse>(this.apiUrl, { params });
  }

  uploadImage(file: File): Observable<UploadImageResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<UploadImageResponse>(
      `${this.apiUrl}upload`,
      formData
    );
  }

  createDish(dish: CreateDishRequest): Observable<Dish> {
    return this.http.post<Dish>(this.apiUrl, dish);
  }

  updateDish(
    id: number,
    dish: UpdateDishRequest
  ): Observable<Dish> {
    return this.http.put<Dish>(
      `${this.apiUrl}${id}`,
      dish
    );
  }

  deleteDish(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}${id}`
    );
  }
}

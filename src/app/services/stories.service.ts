import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StoriesResponse } from '../models/story';

const API_BASE = 'https://localhost:7043';

@Injectable({ providedIn: 'root' })
export class StoriesService {
  private http = inject(HttpClient);

  getNewest(params: {
    page: number;
    pageSize: number;
    search?: string;
  }): Observable<StoriesResponse> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.search?.trim()) {
      httpParams = httpParams.set('search', params.search.trim());
    }

    return this.http.get<StoriesResponse>(`${API_BASE}/api/Stories/newest`, {
      params: httpParams,
    });
  }
}

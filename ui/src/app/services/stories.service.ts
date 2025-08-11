import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StoriesResponse } from '../models/story';

const API_BASE =
  'https://newsappbackend-hnc2ehchh0gdggaz.centralus-01.azurewebsites.net';

@Injectable({ providedIn: 'root' })
export class StoriesService {
  private http = inject(HttpClient);

  getNewest(params: {
    page: number;
    pageSize: number;
    search?: string;
  }): Observable<StoriesResponse> {
    let data = {
      page: params.page,
      pageSize: params.pageSize,
      search: params.search ? params.search.trim() : null,
    };

    return this.http.post<StoriesResponse>(
      `${API_BASE}/api/NewsStories/newest`,
      data
    );
  }
}

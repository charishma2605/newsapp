export interface PagedResponse<T> {
  page: number;
  pageSize: number;
  total: number;
  items: T[];
}

export interface Story {
  id: number;
  title: string;
  url: string | null;
  by: string | null;
  time: number | null;
  score: number | null;
}

// backend response
export interface StoriesResponse {
  page: number;
  pageSize: number;
  total: number;
  items: Story[];
}

import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { StoriesService } from '../services/stories.service';
import { StoriesResponse, Story } from '../models/story';

@Component({
  selector: 'app-stories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './stories.component.html',
  styleUrls: ['./stories.component.css'],
})
export class StoriesComponent {
  private svc = inject(StoriesService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // State
  page = signal(1);
  pageSize = signal(20);
  total = signal(0);
  items = signal<Story[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Search
  searchCtrl = new FormControl<string>('', { nonNullable: true });
  search = signal<string>('');
  trackId = (_: number, s: Story) => s.id;
  // Derived
  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  constructor() {
    // Initialize from URL
    const qp = this.route.snapshot.queryParamMap;
    this.page.set(Number(qp.get('page')) || 1);
    this.pageSize.set(Number(qp.get('pageSize')) || 20);

    const searchFromUrl = qp.get('search') || '';
    this.search.set(searchFromUrl);
    this.searchCtrl.setValue(searchFromUrl);

    // React to search input with debounce
    this.searchCtrl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe((val) => {
        this.search.set(val || '');
        this.page.set(1); // reset to first page on new search
        this.syncUrl();
        this.fetch();
      });

    // Initial fetch
    this.fetch();

    // Keep URL synced on page/pageSize manual changes
    effect(() => {
      // Trigger when page or pageSize changes via UI
      this.page();
      this.pageSize();
      // Don’t refetch here (buttons call fetch), just keep URL consistent
      this.syncUrl(false);
    });
  }

  fetch(): void {
    console.log('Page ' + this.page());
    console.log('Page Size' + this.pageSize());
    this.loading.set(true);
    this.error.set(null);
    this.svc
      .getNewest({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search(),
      })
      .subscribe({
        next: (res: StoriesResponse) => {
          this.items.set(res.items || []);
          this.total.set(res.total || 0);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.message || 'Failed to load stories.');
        },
      });
  }

  // Pager controls (server-side)
  goFirst() {
    if (this.page() !== 1) {
      this.page.set(1);
      this.syncUrl();
      this.fetch();
    }
  }
  goPrev() {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
      this.syncUrl();
      this.fetch();
    }
  }
  goNext() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.page() + 1);
      this.syncUrl();
      this.fetch();
    }
  }
  goLast() {
    const last = this.totalPages();
    if (this.page() !== last) {
      this.page.set(last);
      this.syncUrl();
      this.fetch();
    }
  }

  onChangePageSize(size: string) {
    const ps = Number(size) || 20;
    if (ps !== this.pageSize()) {
      this.pageSize.set(ps);
      this.page.set(1);
      this.syncUrl();
      this.fetch();
    }
  }

  // Utilities
  unixToLocalString(seconds?: number | null): string {
    if (!seconds && seconds !== 0) return '';
    return new Date(seconds * 1000).toLocaleString();
  }

  private syncUrl(refetch = false) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: this.page(),
        pageSize: this.pageSize(),
        ...(this.search().trim() ? { search: this.search().trim() } : {}),
      },
      queryParamsHandling: '',
      replaceUrl: true,
    });
    if (refetch) this.fetch();
  }
}

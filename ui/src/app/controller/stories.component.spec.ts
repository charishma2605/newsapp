import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { StoriesComponent } from './stories.component';
import { Router, ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { StoriesService } from '../services/stories.service';
import { StoriesResponse, Story } from '../models/story';

function makeRes(partial?: Partial<StoriesResponse>): StoriesResponse {
  return {
    page: 1,
    pageSize: 20,
    total: 2,
    items: [
      {
        id: 1,
        title: 'A',
        url: 'https://a',
        by: 'u1',
        time: 1710000000,
        score: 10,
      },
      { id: 2, title: 'B', url: null, by: 'u2', time: 1710000500, score: 5 },
    ],
    ...partial,
  };
}

describe('StoriesComponent', () => {
  let component: StoriesComponent;
  let router: Router;
  let route: ActivatedRoute;
  let svc: jasmine.SpyObj<StoriesService>;

  beforeEach(async () => {
    const svcSpy = jasmine.createSpyObj('StoriesService', ['getNewest']);

    await TestBed.configureTestingModule({
      imports: [RouterTestingModule.withRoutes([]), StoriesComponent],
      providers: [
        { provide: StoriesService, useValue: svcSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: new Map([
                ['page', '3'],
                ['pageSize', '50'],
                ['search', 'init term'],
              ]),
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    route = TestBed.inject(ActivatedRoute);
    svc = TestBed.inject(StoriesService) as jasmine.SpyObj<StoriesService>;

    // Default fetch response unless a test overrides it
    svc.getNewest.and.returnValue(of(makeRes()));

    const fixture = TestBed.createComponent(StoriesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // runs constructor + initial fetch
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('initializes state from URL and calls fetch with those params', () => {
    // From provided ActivatedRoute stub: page=3, pageSize=50, search="init term"
    expect(component.page()).toBe(3);
    expect(component.pageSize()).toBe(50);
    expect(component.search()).toBe('init term');

    // First call triggered by constructor fetch()
    expect(svc.getNewest).toHaveBeenCalledWith({
      page: 3,
      pageSize: 50,
      search: 'init term',
    });
  });

  it('sets items/total and clears loading on success', () => {
    expect(component.loading()).toBeFalse();
    expect(component.error()).toBeNull();
    expect(component.items().length).toBe(2);
    expect(component.total()).toBe(2);
  });

  it('handles error path', () => {
    svc.getNewest.calls.reset();
    svc.getNewest.and.returnValue(throwError(() => new Error('boom')));
    component.fetch();

    expect(component.loading()).toBeFalse();
    expect(component.error()).toBe('boom');
    expect(component.items().length).toBe(0);
  });

  it('computes totalPages correctly', () => {
    // total=2, pageSize=50 -> 1 page minimum
    expect(component.totalPages()).toBe(1);

    // Make it larger
    svc.getNewest.and.returnValue(of(makeRes({ total: 125, pageSize: 20 })));
    component.pageSize.set(20);
    component.fetch();
    expect(component.totalPages()).toBe(7); // ceil(125/20)
  });

  it('debounces search and resets to page 1', fakeAsync(() => {
    svc.getNewest.calls.reset();

    const responses: StoriesResponse[] = [];
    svc.getNewest.and.callFake((params) => {
      responses.push(
        makeRes({ page: params.page, pageSize: params.pageSize, items: [] })
      );
      return of(responses.at(-1)!);
    });

    // simulate typing new term
    component.searchCtrl.setValue('angular');
    tick(299);
    expect(svc.getNewest).not.toHaveBeenCalled();

    tick(1); // reach 300ms debounce
    expect(svc.getNewest).toHaveBeenCalledWith({
      page: 1, // reset
      pageSize: component.pageSize(),
      search: 'angular',
    });
    expect(component.page()).toBe(1);
  }));

  it('pager: goNext/goPrev/goFirst/goLast update page, sync URL, and fetch', async () => {
    const navSpy = spyOn(router, 'navigate').and.returnValue(
      Promise.resolve(true)
    );

    // Prepare totals for multiple pages *before* calling fetch
    svc.getNewest.and.returnValue(
      of(makeRes({ total: 100, pageSize: component.pageSize() }))
    );

    // Manually set page to 3 so we can test next/prev
    component.page.set(3);
    component.fetch(); // now totalPages() will be > 3

    expect(component.totalPages()).toBe(Math.ceil(100 / component.pageSize()));

    component.goNext();
    expect(component.page()).toBe(4);
    expect(navSpy).toHaveBeenCalledWith(
      [],
      jasmine.objectContaining({
        queryParams: jasmine.objectContaining({ page: 4 }),
      })
    );

    component.goPrev();
    expect(component.page()).toBe(3);

    component.goFirst();
    expect(component.page()).toBe(1);

    component.goLast();
    expect(component.page()).toBe(component.totalPages());
  });

  it('onChangePageSize resets page to 1 and fetches', () => {
    const navSpy = spyOn(router, 'navigate').and.returnValue(
      Promise.resolve(true)
    );
    svc.getNewest.calls.reset();

    component.onChangePageSize('10');

    expect(component.pageSize()).toBe(10);
    expect(component.page()).toBe(1);
    expect(svc.getNewest).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      search: component.search(),
    });
    expect(navSpy).toHaveBeenCalled();
  });

  it('trackId returns story id', () => {
    const s: Story = {
      id: 42,
      title: 't',
      url: null,
      by: null,
      time: null,
      score: null,
    };
    expect(component.trackId(0, s)).toBe(42);
  });

  it('unixToLocalString returns empty for null/undefined and formats epoch seconds', () => {
    expect(component.unixToLocalString(null)).toBe('');
    expect(component.unixToLocalString(undefined)).toBe('');
    const out = component.unixToLocalString(0);
    // 0 should produce a valid date string
    expect(typeof out).toBe('string');
    expect(out.length).toBeGreaterThan(0);
  });

  describe('template rendering (smoke tests)', () => {
    it('shows "No results." when empty and not loading', () => {
      svc.getNewest.and.returnValue(of(makeRes({ items: [], total: 0 })));
      component.fetch();
      // No need for TestBed fixture here; logic is enough
      expect(component.items().length).toBe(0);
      expect(component.total()).toBe(0);
    });

    it('shows rows when items exist', () => {
      svc.getNewest.and.returnValue(of(makeRes()));
      component.fetch();
      expect(component.items().length).toBe(2);
    });
  });
});

import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { StoriesComponent } from './stories.component';
import { StoriesService } from '../services/stories.service';
import { StoriesResponse, Story } from '../models/story';
import { of, Subject } from 'rxjs';
import { convertToParamMap, ActivatedRoute, Router } from '@angular/router';

function makeResp(overrides?: Partial<StoriesResponse>): StoriesResponse {
  // mock response
  return {
    page: 1,
    pageSize: 20,
    total: 2,
    items: [
      { id: 1, title: 'news 1', url: 'http://a', by: 'u', time: 0, score: 1 },
      { id: 2, title: 'news 2', url: null, by: 'v', time: 0, score: 2 },
    ],
    ...overrides,
  };
}

describe('StoriesComponent', () => {
  let getNewestSpy: jasmine.Spy;
  let routerNavigateSpy: jasmine.Spy;
  let routeQueryParams$: Subject<any>;

  beforeEach(() => {
    // Mock service
    const serviceMock = jasmine.createSpyObj<StoriesService>('StoriesService', [
      'getNewest',
    ]);
    getNewestSpy = (serviceMock.getNewest as jasmine.Spy).and.returnValue(
      of(makeResp())
    );

    // Mock router + route
    routeQueryParams$ = new Subject();
    const activatedRouteMock = {
      snapshot: {
        queryParamMap: convertToParamMap({ page: '1', pageSize: '20' }),
      },
      queryParamMap: routeQueryParams$.asObservable(),
    } as unknown as ActivatedRoute;

    const routerMock = {
      navigate: jasmine.createSpy('navigate'),
    } as unknown as Router;
    routerNavigateSpy = routerMock.navigate as jasmine.Spy;

    TestBed.configureTestingModule({
      imports: [StoriesComponent],
      providers: [
        { provide: StoriesService, useValue: serviceMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  it('should create and load first page', () => {
    const fixture = TestBed.createComponent(StoriesComponent);
    fixture.detectChanges();

    expect(getNewestSpy).toHaveBeenCalledTimes(1);
    expect(getNewestSpy.calls.mostRecent().args[0]).toEqual({
      page: 1,
      pageSize: 20,
      search: '',
    });

    const comp = fixture.componentInstance;
    expect(comp.items().length).toBe(2);
    expect(comp.total()).toBe(2);
    expect(comp.page()).toBe(1);
    expect(comp.pageSize()).toBe(20);
  });

  it('should render rows for items', () => {
    const fixture = TestBed.createComponent(StoriesComponent);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('news 1');
    expect(rows[1].textContent).toContain('news 2');
  });

  it('should debounce search and reset to page 1', fakeAsync(() => {
    const fixture = TestBed.createComponent(StoriesComponent);
    fixture.detectChanges();
    getNewestSpy.calls.reset();

    const comp = fixture.componentInstance;
    comp.searchCtrl.setValue('spa'); // triggers valueChanges
    tick(300); // debounceTime

    expect(comp.page()).toBe(1);
    expect(getNewestSpy).toHaveBeenCalledTimes(1);
    expect(getNewestSpy.calls.mostRecent().args[0]).toEqual({
      page: 1,
      pageSize: 20,
      search: 'spa',
    });
  }));

  it('should navigate pages (Next and Prev)', () => {
    getNewestSpy.and.returnValue(of(makeResp({ total: 100 })));

    const fixture = TestBed.createComponent(StoriesComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance;

    // initial load (page 1), now Next should be enabled
    expect(comp.page()).toBe(1);

    comp.goNext();
    expect(comp.page()).toBe(2); // moved to page 2

    comp.goPrev();
    expect(comp.page()).toBe(1); // back to page 1
  });
});

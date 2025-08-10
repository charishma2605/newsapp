import { Routes } from '@angular/router';
import { StoriesComponent } from './controller/stories.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'stories' },
  { path: 'stories', component: StoriesComponent },
];

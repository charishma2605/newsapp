// src/main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { StoriesComponent } from './app/controller/stories.component';

bootstrapApplication(StoriesComponent, {
  providers: [provideRouter(routes), provideHttpClient(withFetch())],
}).catch((err) => console.error(err));

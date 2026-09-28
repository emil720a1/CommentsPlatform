import { Routes } from '@angular/router';

import { CommentsPage } from './features/comments/pages/comments-page/comments-page';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'comments',
  },
  {
    path: 'comments',
    component: CommentsPage,
  },
  {
    path: '**',
    redirectTo: 'comments',
  },
];

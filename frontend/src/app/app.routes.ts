import { Routes } from '@angular/router';
import { CatalogAdmin } from './admin/catalog-admin';
import { SystemAdmin } from './admin/system-admin';
import { PriceAnalyzer } from './analysis/price-analyzer';
import { adminGuard, businessGuard } from './auth/auth.guard';
import { SignIn } from './auth/sign-in';
import { PriceList } from './price-list/price-list';

export const routes: Routes = [
  { path: 'prijava', component: SignIn, title: 'Prijava' },

  // Business-facing screens. The market corpus is what the application offers, so it is
  // not exposed to anonymous visitors; administrators are redirected to their own screens.
  { path: 'analiza', component: PriceAnalyzer, canActivate: [businessGuard], title: 'Analiza cijene' },
  { path: 'cjenik', component: PriceList, canActivate: [businessGuard], title: 'Moj cjenik' },

  // Admin screens change how every entry is interpreted, so they sit behind a role check
  // here and behind [Authorize(Roles = "Admin")] on the server.
  { path: 'admin/katalog', component: CatalogAdmin, canActivate: [adminGuard], title: 'Katalog usluga' },
  { path: 'admin/sustav', component: SystemAdmin, canActivate: [adminGuard], title: 'Sustav' },

  // Both guards redirect onwards, so an administrator landing here ends up on the catalog.
  { path: '', pathMatch: 'full', redirectTo: 'analiza' },
  { path: '**', redirectTo: 'analiza' },
];

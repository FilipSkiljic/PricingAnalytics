import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isSignedIn = this.authService.isSignedIn;
  protected readonly isAdmin = this.authService.isAdmin;
  protected readonly businessName = computed(() => this.authService.currentBusiness()?.businessName ?? '');

  protected signOut(): void {
    this.authService.logout();
    this.router.navigate(['/prijava']);
  }
}

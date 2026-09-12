import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

type Mode = 'login' | 'register';

@Component({
  selector: 'app-sign-in',
  imports: [ReactiveFormsModule],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.css',
})
export class SignIn {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly mode = signal<Mode>('login');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    // Only required in register mode; toggled in switchMode().
    businessName: [''],
  });

  protected switchMode(mode: Mode): void {
    this.mode.set(mode);
    this.errorMessage.set(null);

    const businessName = this.form.controls.businessName;
    if (mode === 'register') {
      businessName.setValidators([Validators.required, Validators.minLength(2)]);
    } else {
      businessName.clearValidators();
    }

    businessName.updateValueAndValidity();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { email, password, businessName } = this.form.getRawValue();
    const request$ =
      this.mode() === 'register'
        ? this.authService.register({ email, password, businessName })
        : this.authService.login({ email, password });

    request$.subscribe({
      next: (result) => {
        this.isSubmitting.set(false);
        // Administrators have no price list of their own; send them to the screens they use.
        this.router.navigate([result.role === 'Admin' ? '/admin/katalog' : '/cjenik']);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error.error?.error ?? 'Prijava nije uspjela. Provjerite radi li poslužitelj.',
        );
      },
    });
  }
}

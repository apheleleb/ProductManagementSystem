import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'Something went wrong. Please try again.';

      if (error.status === 401) {
        message = 'Your session has expired. Please log in again.';
        authService.logout();
        router.navigate(['/login']);
      } else if (error.status === 403) {
        message = "You don't have permission to do that.";
      } else if (error.error?.errors?.length) {
        message = error.error.errors.join(' ');
      } else if (error.error?.message) {
        message = error.error.message;
      } else if (error.status === 0) {
        message = 'Cannot reach the server. Check your connection.';
      }

      snackBar.open(message, 'Dismiss', { duration: 5000, panelClass: 'cat-snack-error' });
      return throwError(() => error);
    })
  );
};

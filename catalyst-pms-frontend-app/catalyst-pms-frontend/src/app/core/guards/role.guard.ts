import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { CanActivateFn, Router } from '@angular/router';
import { MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { filter, map, switchMap, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Usage in routes: data: { roles: ['ProductManager'] }
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const msalBroadcastService = inject(MsalBroadcastService);
  const router = inject(Router);

  const allowedRoles = route.data?.['roles'] as string[] | undefined;

  // Same NG0203 fix as auth.guard.ts — must call toObservable() synchronously
  // here, not inside a later async callback.
  const userResolved$ = toObservable(authService.userResolved);

  return msalBroadcastService.inProgress$.pipe(
    filter((status) => status === InteractionStatus.None),
    take(1),
    switchMap(() => userResolved$.pipe(filter(Boolean), take(1))),
    map(() => {
      const user = authService.currentUser();

      if (!user) {
        router.navigate(['/login']);
        return false;
      }

      if (!allowedRoles || allowedRoles.length === 0 || allowedRoles.includes(user.role)) {
        return true;
      }

      router.navigate(['/dashboard']);
      return false;
    })
  );
};

import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { CanActivateFn, Router } from '@angular/router';
import { MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { filter, map, switchMap, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const msalBroadcastService = inject(MsalBroadcastService);
  const router = inject(Router);

  // toObservable() MUST be called synchronously here, in the guard's own
  // injection context — calling it later inside switchMap() (as a previous
  // version of this file did) throws NG0203, since by the time that callback
  // runs, Angular's injection context is no longer active.
  const userResolved$ = toObservable(authService.userResolved);

  // Wait for MSAL to finish any in-flight redirect/login processing, THEN wait
  // for AuthService's /api/auth/me fetch to settle, before making a decision.
  return msalBroadcastService.inProgress$.pipe(
    filter((status) => status === InteractionStatus.None),
    take(1),
    switchMap(() => userResolved$.pipe(filter(Boolean), take(1))),
    map(() => {
      if (authService.isAuthenticated()) return true;
      router.navigate(['/login']);
      return false;
    })
  );
};

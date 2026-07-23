import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AccountInfo, EventType, InteractionStatus } from '@azure/msal-browser';
import { filter } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { CurrentUser } from '../models/auth.model';

/**
 * Wraps MsalService so the rest of the app (guards, layout, dashboard, etc.)
 * keeps using the same currentUser()/isManager()/isCapturer()/isAuthenticated
 * API it always has.
 *
 * IMPORTANT: the user's role (ProductCapturer/ProductManager) is assigned in
 * Entra ID against the API's Enterprise Application, so it shows up in the
 * ACCESS TOKEN issued for the API — not in the SPA's own ID token. Rather than
 * try to read it from the wrong token, this service asks the backend directly
 * via GET /api/auth/me, which validates the access token and reflects back
 * whatever roles Entra ID actually granted for that resource. This is also
 * a good sanity check that your backend's token validation is configured
 * correctly, independent of anything the frontend assumes.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly msalService = inject(MsalService);
  private readonly msalBroadcastService = inject(MsalBroadcastService);
  private readonly http = inject(HttpClient);

  private readonly accountSignal = signal<AccountInfo | null>(this.msalService.instance.getActiveAccount());
  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  // True once we've attempted to resolve the current user at least once
  // (successfully or not) since the last account change. Guards wait on this
  // so they don't make an authorization decision while /me is still in flight.
  private readonly userResolvedSignal = signal(false);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly userResolved = this.userResolvedSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.accountSignal() !== null);
  readonly isManager = computed(() => this.currentUser()?.role === 'ProductManager');
  readonly isCapturer = computed(() => this.currentUser()?.role === 'ProductCapturer');

  constructor() {
    // Re-sync once MSAL has fully finished processing any in-flight
    // redirect/login (this is what fixes the "bounces back to /login" race —
    // guards wait on userResolved, which only flips true after this settles).
    this.msalBroadcastService.inProgress$
      .pipe(filter((status) => status === InteractionStatus.None))
      .subscribe(() => this.syncActiveAccount());

    this.msalBroadcastService.msalSubject$
      .pipe(
        filter(
          (msg) => msg.eventType === EventType.LOGIN_SUCCESS || msg.eventType === EventType.LOGOUT_SUCCESS
        )
      )
      .subscribe(() => this.syncActiveAccount());
  }

  // login(): void {
  //   // Requesting the API scope up front means MSAL can silently acquire an
  //   // access token for it right after login without an extra round trip.
  //   this.msalService.loginRedirect({
  //     scopes: [environment.entraId.apiScope]
  //   });
  // }

  login(loginHint?: string): void {
  // Requesting the API scope up front means MSAL can silently acquire an
  // access token for it right after login without an extra round trip.
  // loginHint (when provided) pre-fills the email on Entra ID's hosted
  // login page so the user doesn't have to retype it.
  this.msalService.loginRedirect({
    scopes: [environment.entraId.apiScope],
    ...(loginHint ? { loginHint } : {})
  });
}

  logout(): void {
    this.currentUserSignal.set(null);
    this.userResolvedSignal.set(false);
    this.msalService.logoutRedirect();
  }

  private syncActiveAccount(): void {
    const accounts = this.msalService.instance.getAllAccounts();
    if (accounts.length > 0 && !this.msalService.instance.getActiveAccount()) {
      this.msalService.instance.setActiveAccount(accounts[0]);
    }

    const account = this.msalService.instance.getActiveAccount();
    this.accountSignal.set(account);

    if (!account) {
      this.currentUserSignal.set(null);
      this.userResolvedSignal.set(true);
      return;
    }

    this.fetchCurrentUser();
  }

  private fetchCurrentUser(): void {
    this.userResolvedSignal.set(false);

    // MsalInterceptor attaches the access token automatically for any call
    // matching the protectedResourceMap configured in app.config.ts.
    this.http.get<ApiResponse<CurrentUser>>(`${environment.apiUrl}/auth/me`).subscribe({
      next: (res) => {
        this.currentUserSignal.set(res.data);
        this.userResolvedSignal.set(true);
      },
      error: () => {
        // Most commonly: the signed-in user has no ProductCapturer/ProductManager
        // role assigned in Entra ID yet (backend returns 400 in that case — see
        // AuthController.Me()). Treat as "no usable identity" rather than crash.
        this.currentUserSignal.set(null);
        this.userResolvedSignal.set(true);
      }
    });
  }
}

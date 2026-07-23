import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`
})
export class App implements OnInit {
  private readonly msalService = inject(MsalService);

  ngOnInit(): void {
    // Processes the auth response after Entra ID redirects back to the app
    // (e.g. after a successful login). Must run once on every app load —
    // it's a no-op if there's nothing to process.
    //
    // IMPORTANT: explicitly set the active account from THIS result, rather
    // than letting AuthService fall back to "whichever account happens to be
    // first in getAllAccounts()". MSAL caches every account ever signed in
    // with in this browser (e.g. a personal Microsoft account tried earlier,
    // alongside an organizational test user) — without this, the wrong
    // cached account can silently become "active" and every API call
    // authenticates as the wrong identity.
    this.msalService.handleRedirectObservable().subscribe((result) => {
      if (result?.account) {
        this.msalService.instance.setActiveAccount(result.account);
      }
    });
  }
}

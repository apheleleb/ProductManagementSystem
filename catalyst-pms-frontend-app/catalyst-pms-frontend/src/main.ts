import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { msalInstance } from './app/core/auth/msal-instance';

// MSAL must finish initializing (it reads any auth response in the URL/cache)
// before Angular bootstraps, otherwise the redirect-back-from-login flow can
// race with route guards evaluating on the first navigation.
msalInstance
  .initialize()
  .then(() => bootstrapApplication(App, appConfig))
  .catch((err) => console.error(err));

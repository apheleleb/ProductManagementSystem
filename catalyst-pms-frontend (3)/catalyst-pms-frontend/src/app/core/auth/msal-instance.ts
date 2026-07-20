import { BrowserCacheLocation, LogLevel, PublicClientApplication } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

/**
 * A single shared MSAL instance, created once at module load time and used both:
 *  - here, in main.ts, to call `.initialize()` before Angular bootstraps
 *  - in app.config.ts's MSAL_INSTANCE provider factory, so the whole app shares
 *    exactly one PublicClientApplication rather than accidentally creating two.
 */
export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: environment.entraId.spaClientId,
    authority: `https://login.microsoftonline.com/${environment.entraId.tenantId}`,
    redirectUri: environment.entraId.redirectUri,
    postLogoutRedirectUri: environment.entraId.redirectUri
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage // survives tab refresh
  },
  system: {
    loggerOptions: {
      loggerCallback: (level: LogLevel, message: string) => {
        if (!environment.production) {
          console.log(message);
        }
      },
      logLevel: LogLevel.Warning,
      piiLoggingEnabled: false
    }
  }
});

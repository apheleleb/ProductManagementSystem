import {
  ApplicationConfig,
  provideAppInitializer, //newley added
  inject, //also new
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { InteractionType } from '@azure/msal-browser';
import {
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalService
} from '@azure/msal-angular';
import { firstValueFrom } from 'rxjs';//

import { routes } from './app.routes';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { msalInstance } from './core/auth/msal-instance';
import { environment } from '../environments/environment';

function msalGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: [environment.entraId.apiScope]
    }
  };
}

function msalInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  // Any call to our own API requires the API's scope. Using a wildcard so it
  // covers every path under /api without listing each endpoint individually.
  protectedResourceMap.set(`${environment.apiUrl}/*`, [environment.entraId.apiScope]);

  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),

    // withInterceptorsFromDi() lets MsalInterceptor (a class-based interceptor)
    // run alongside our own functional errorInterceptor in the same pipeline.
    provideHttpClient(withInterceptors([errorInterceptor]), withInterceptorsFromDi()),

    { provide: MSAL_INSTANCE, useValue: msalInstance },
    { provide: MSAL_GUARD_CONFIG, useFactory: msalGuardConfigFactory },
    { provide: MSAL_INTERCEPTOR_CONFIG, useFactory: msalInterceptorConfigFactory },
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },

    MsalService,
    MsalGuard,
    MsalBroadcastService
  ]
};

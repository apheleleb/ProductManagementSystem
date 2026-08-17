// import { Component, inject } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { MatCardModule } from '@angular/material/card';
// import { MatButtonModule } from '@angular/material/button';
// import { MatIconModule } from '@angular/material/icon';

// import { AuthService } from '../../../core/services/auth.service';

// @Component({
//   selector: 'app-login',
//   standalone: true,
//   imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
//   templateUrl: './login.component.html',
//   styleUrl: './login.component.scss'
// })
// export class LoginComponent {
//   private readonly authService = inject(AuthService);

//   signIn(): void {
//     // Redirects the whole browser tab to Entra ID's hosted login page.
//     // On success, Entra ID redirects back to this app's redirectUri, and
//     // App.ngOnInit()'s handleRedirectObservable() picks up the result.
//     this.authService.login();
//   }
// }

import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);

  signIn(): void {
    // Redirects the whole browser tab to Entra ID's hosted login page.
    // On success, Entra ID redirects back to this app's redirectUri, and
    // App.ngOnInit()'s handleRedirectObservable() picks up the result.
    console.log('signIn fired')
    this.authService.login();
  }
}
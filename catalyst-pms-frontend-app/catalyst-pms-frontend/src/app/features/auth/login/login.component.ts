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
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly preLoginForm: FormGroup = this.fb.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]]
  });

  signIn(): void {
    if (this.preLoginForm.invalid) {
      this.preLoginForm.markAllAsTouched();
      return;
    }

    const { email } = this.preLoginForm.value;

    // Redirects the whole browser tab to Entra ID's hosted login page,
    // pre-filling the email as a login hint. On success, Entra ID redirects
    // back to this app's redirectUri, and App.ngOnInit()'s
    // handleRedirectObservable() picks up the result.
    this.authService.login(email);
  }
}
/**
 * The app's local representation of "who is signed in", derived from the
 * active MSAL account's ID token claims (see AuthService.mapAccount()).
 * Login/registration are no longer handled by our own API — they happen
 * entirely through the Entra ID redirect flow.
 */
export interface CurrentUser {
  userId: string;
  fullName: string;
  email: string;
  role: string;
}

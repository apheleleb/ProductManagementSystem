# Catalyst PMS Frontend

Angular 20 standalone frontend for the Catalyst Product Management System (part of the
MOYO Online Order Solution). Built against the existing ASP.NET Core 8 API — no backend
endpoints were changed or invented.

**This project was scaffolded with the real Angular CLI (`ng new` + `ng add @angular/material`)
rather than a hand-written `package.json`, then verified with a real `npm install` and both
dev and production `ng build` runs, so the dependency versions here are genuinely
resolved and compatible — not guessed.**

## Requirements

- **Node.js**: `^20.19.0 || ^22.12.0 || >=24.0.0` (this is Angular 20's actual requirement —
  check yours with `node -v`; anything older, e.g. 20.18 or earlier, will not work)
- **npm**: 8.x or later (bundled with modern Node)

If you need to manage multiple Node versions, use [nvm](https://github.com/nvm-sh/nvm)
(macOS/Linux) or [nvm-windows](https://github.com/coreybutler/nvm-windows):

```bash
nvm install 22.12.0
nvm use 22.12.0
```

## Getting started

```bash
npm install
npm start
```

The app runs on `http://localhost:4200` by default and expects the API at the URL
configured in `src/environments/environment.ts` (`https://localhost:7001/api` for local
development — update this to match your backend's launch profile).

For production builds:

```bash
npm run build
```

This uses `src/environments/environment.prod.ts` via Angular's `fileReplacements`
(`apiUrl: '/api'` — adjust to your deployed API's base path, or configure a reverse proxy).

## Verified versions (as resolved by npm at build time)

| Package | Version |
|---|---|
| @angular/core, common, forms, router, compiler, platform-browser | ^20.3.0 |
| @angular/cli, @angular/build, @angular/compiler-cli | ^20.3.31 |
| @angular/material, @angular/cdk | ^20.2.14 |
| @angular/animations | ^20.3.25 (see note below) |
| typescript | ~5.9.2 |
| rxjs | ~7.8.0 |
| zone.js | ~0.15.0 |

### A note on `@angular/animations`

As of Angular 20.2, `@angular/animations` and `provideAnimations()` are **deprecated** in
favor of native CSS animations (`animate.enter` / `animate.leave`), with removal targeted
for Angular v23. However, Angular Material's components (menus, dialogs, selects, sidenav,
ripples) still depend on the legacy animations module at this Angular version, so
`provideAnimations()` is still wired up in `app.config.ts` and will keep working through
v20–v22. When Material fully migrates off the legacy animation engine, this can be removed
in favor of the native API — worth revisiting on your next Angular major upgrade.

### A note on font loading in production builds

`ng build --configuration production` normally tries to inline Google Fonts CSS at build
time, which requires the build machine to reach `fonts.googleapis.com`. This is disabled
here (`optimization.fonts: false` in `angular.json`) so the build doesn't fail on networks
without that access (common in CI runners or locked-down corporate environments). Fonts
still load fine at runtime via the `<link>` tags in `index.html` — this only affects
build-time inlining/optimization, not whether fonts display.

## Folder structure

```
src/app/
  core/
    models/         DTOs matching the backend's Shared/DTOs and Feature DTOs
    services/        One HttpClient service per controller (Auth, Products, Categories,
                     Notifications, AuditLogs, Workflow)
    interceptors/    JWT attach + centralized error handling / snackbar
    guards/          authGuard (must be logged in), roleGuard (role-based route access)
  shared/
    components/
      layout/        Sidenav + toolbar shell, role-aware navigation
      confirm-dialog/ Reusable confirm/reject dialog (supports a required comment field)
  features/
    auth/login/       Login + registration (toggle in one screen)
    dashboard/        Stats tiles + recent activity feed
    products/
      product-list/    Search, category/status filters, pagination
      product-details/ Full detail view, approval history, audit log, workflow actions
      product-form/     Shared create/edit form: dynamic specifications, image upload/preview
    categories/       Manager-only CRUD (soft delete via deactivate)
    workflow/         Manager approval queue: pending / approved / published tabs
    notifications/    Notification inbox with mark-as-read
    audit-log/        Look up audit history by product ID
```

## Roles

- **ProductCapturer** — creates/edits products, saves drafts, submits for approval.
- **ProductManager** — approves/rejects, publishes/unpublishes, archives, manages categories.

Role-gated routes and actions read the role from the JWT-derived `CurrentUser` stored in
`AuthService` (backed by `localStorage`).

## Notes on the backend contract

- All endpoints return `ApiResponse<T>` (`success`, `message`, `data`, `errors`); list
  endpoints from `ProductsController.Search` return `PagedResponseDto<T>` inside `data`.
- Product create/update use `multipart/form-data` (`CreateProductRequest` /
  `UpdateProductRequest`), including indexed `Specifications[i].Key` / `Specifications[i].Value`
  fields and an optional `Image` file — handled by `ProductService`'s `toFormData()`.
- Product images are served from `GET /api/products/{id}/image` and rendered directly as
  `<img>` `src` (this endpoint is `[AllowAnonymous]` on the backend).

## If `ng update` moves you past Angular 20 later

Re-run this same process rather than hand-editing versions: `ng update @angular/cli
@angular/core`, then `ng update @angular/material`, then re-run `npm install` and `ng build`
to catch anything the update didn't handle automatically.

# Chuka University Portal (frontend)

Next.js + React + TypeScript + Tailwind. Talks to the ASP.NET portal API through
a same-origin proxy, so the JWT only ever lives in an httpOnly cookie.

## Run it

```bash
npm install
cp .env.example .env.local     # set API_BASE_URL
npm run dev                    # http://localhost:3000
```

| Script | What it does |
|---|---|
| `npm run dev` | Development server |
| `npm run build` / `npm start` | Production build and server |
| `npm run lint` | ESLint |
| `npm run typecheck` | TypeScript check |
| `npm run api:types` | Regenerate `src/types/api.d.ts` from the running API's Swagger |

After any backend DTO change: start the API, run `npm run api:types`, then `npm run typecheck`.

## How it fits together

```
browser ──> /api/*  (Next route handler)  ──> ASP.NET API
             adds "Authorization: Bearer <cookie>"
```

- `src/proxy.ts`: no session cookie, no portal pages (quick gate).
- `src/lib/server/session.ts`, `guards.ts`: the real check, on the server, on every request (`getCurrentUser`, `requireRole`).
- `src/app/api/auth/login|logout`: set and clear the cookie. `src/app/api/[...path]`: the proxy.
- `src/lib/api/`: browser client, services per area, and React Query hooks.
- `src/types/index.ts`: friendly aliases over the generated `api.d.ts`.
- `src/components/ui/`: the design system. Colours and fonts are tokens in `src/app/globals.css`.

## Roles and routes

| Role | Home | Pages |
|---|---|---|
| Student | `/student` | invoices, invoice detail, payments, statement |
| FinanceOfficer | `/officer` | overview, invoices (list, new, detail), payments, outstanding |
| Administrator | `/admin` | users (create, reset password, activate/deactivate) |

Navigation per role lives in `src/components/shell/nav-config.ts`.

## Configuration

| Variable | Purpose |
|---|---|
| `API_BASE_URL` | API address. Required in production. Read at runtime. |
| `COOKIE_SECURE` | Optional. Cookies are Secure in production; `false` only for testing a production build over plain http. |

For production, set `API_BASE_URL`, build with `npm run build`, and start with
`npm start`. Serve it over HTTPS (reverse proxy or ingress) and add HSTS there.
Login rate limiting and lockout belong on the API.

# Vercel + Render Deployment

This guide documents the working free-tier deployment setup for this repository:

- frontend on Vercel
- backend on Render
- PostgreSQL on Render

It is intended as a public reference for deploying a .NET + Angular project without Azure.

## Architecture

- Frontend host: Vercel
- Backend host: Render Web Service
- Database: Render PostgreSQL
- Source control: GitHub
- Production branch: `main`

## Why This Split Works

The Angular client uses relative API paths such as `/api/auth` and `/api/admin/...`.

Instead of rewriting the frontend services to hardcode a backend base URL, Vercel handles that routing with [AngularApp4/ClientApp/vercel.json](/E:/Nexus/AngularApp4/ClientApp/vercel.json):

- SPA routes fall back to `index.html`
- `/api/*` is proxied to the Render backend

That keeps the Angular codebase simple and lets the frontend and backend live on different hosts.

## Current Production URLs

- Frontend: `https://nexus-hospital.vercel.app`
- Backend: `https://nexus-app-scn1.onrender.com`

## Important Files

- [render.yaml](/E:/Nexus/render.yaml)
- [AngularApp4/Dockerfile](/E:/Nexus/AngularApp4/Dockerfile)
- [AngularApp4/ClientApp/vercel.json](/E:/Nexus/AngularApp4/ClientApp/vercel.json)
- [AngularApp4/Program.cs](/E:/Nexus/AngularApp4/Program.cs)
- [AngularApp4/Data/DatabaseInitializer.cs](/E:/Nexus/AngularApp4/Data/DatabaseInitializer.cs)

## Backend Changes Required For PostgreSQL

This repository was originally wired for SQL Server and was adapted to PostgreSQL.

Key changes:

- EF Core provider switched from SQL Server to PostgreSQL
- startup accepts `postgres://` and `postgresql://` connection URLs
- Npgsql legacy timestamp behavior is enabled for compatibility with existing seed data
- a fresh PostgreSQL migration baseline was generated
- the app can bind to Render’s dynamic `PORT`
- forwarded headers support was enabled for hosted environments

## 1. Create the Render PostgreSQL Database

Create a PostgreSQL database on Render.

Use the external connection string in this format:

```text
postgresql://<user>:<password>@<host>/<database>
```

Render provides this directly in the database dashboard.

## 2. Configure Render Web Service

This repo includes [render.yaml](/E:/Nexus/render.yaml), which defines:

- a free web service named `nexus-app`
- a free PostgreSQL database named `nexus-db`
- backend environment variable wiring from the database connection string

Required backend environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<Render PostgreSQL connection string>
Jwt__Issuer=HospitalManagementSystem
Jwt__Audience=HospitalManagementSystem.Client
Jwt__Key=<strong random secret>
Jwt__ExpiresInMinutes=120
```

## 3. Configure the Backend Docker Build

Render builds the backend from [AngularApp4/Dockerfile](/E:/Nexus/AngularApp4/Dockerfile).

That Dockerfile:

- installs Node 18
- installs Angular dependencies
- runs `dotnet publish`
- serves the published ASP.NET Core app from the final image

This works because the ASP.NET publish step also builds the Angular client into the backend artifact for the Render-hosted backend build.

## 4. Configure the Vercel Frontend Project

Create a Vercel project from the GitHub repository with:

- framework preset: `Angular`
- root directory: `AngularApp4/ClientApp`

Vercel should build the Angular client from that directory.

The frontend routing behavior comes from [AngularApp4/ClientApp/vercel.json](/E:/Nexus/AngularApp4/ClientApp/vercel.json):

```json
{
  "$schema": "https://openapi.vercel.sh/vercel.json",
  "routes": [
    {
      "src": "/api/(.*)",
      "dest": "https://nexus-app-scn1.onrender.com/api/$1"
    },
    {
      "handle": "filesystem"
    },
    {
      "src": "/(.*)",
      "dest": "/index.html"
    }
  ]
}
```

Notes:

- `/api/*` is proxied to Render
- normal Angular routes such as `/auth/login` and `/admin/dashboard` resolve through SPA fallback

## 5. Frontend Production Domain

Vercel assigned a custom `vercel.app` alias to the live deployment:

```text
https://nexus-hospital.vercel.app
```

If you rename the Vercel project and the new built-in `vercel.app` hostname does not appear automatically, you can alias it with the Vercel CLI:

```powershell
npx vercel alias set <deployment-url> nexus-hospital.vercel.app --scope <your-team-slug>
```

## 6. Auto-Deploy Setup

This deployment model is already wired for push-based deployment from GitHub.

Behavior:

- pushing to `main` triggers a new Vercel production deployment
- pushing to `main` triggers a new Render backend deployment

You do not need a separate deploy branch unless you want one for your own workflow.

## 7. Local Verification Before Deploy

Recommended local checks:

```powershell
dotnet build --no-restore AngularApp4/AngularApp4.csproj
dotnet publish --no-restore AngularApp4/AngularApp4.csproj
```

Frontend-only build:

```powershell
cd AngularApp4/ClientApp
npm run build
```

## 8. Operational Notes

- Render free services can spin down after inactivity, so the first backend request can be slow.
- Vercel serves the frontend immediately, but API-backed pages depend on the Render backend being awake.
- If you change the backend public URL, update [AngularApp4/ClientApp/vercel.json](/E:/Nexus/AngularApp4/ClientApp/vercel.json) and redeploy Vercel.
- If you rotate the database or JWT secret, update Render environment variables and redeploy the backend.

## 9. Typical Workflow

1. Make code changes locally.
2. Run local checks.
3. Commit changes.
4. Push to `main`.
5. Wait for Vercel and Render to finish deploying.
6. Verify the hosted app at `https://nexus-hospital.vercel.app`.

## 10. Public Reuse Checklist

If you want to reuse this pattern for another .NET + Angular repository:

1. Move the Angular client into a clear frontend directory.
2. Ensure the backend can build in Docker.
3. Use PostgreSQL if you want a practical free-hosting path.
4. Add a Vercel routing file to proxy `/api/*` to the backend.
5. Connect both services to the same GitHub repo.
6. Deploy both from `main`.

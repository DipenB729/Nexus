# Nexus

Public reference project for deploying an ASP.NET Core + Angular app with:

- backend on Render
- frontend on Vercel
- PostgreSQL as the application database

## Deployment Guides

- [Vercel + Render Deployment](./docs/vercel-render-deploy.md)
- [Azure App Service Deployment](./docs/azure-app-service-deploy.md)

## Production URLs

- Frontend: `https://nexus-hospital.vercel.app`
- Backend: `https://nexus-app-scn1.onrender.com`

## Auto-Deploy Behavior

Both platforms are connected to the public GitHub repository and deploy from `main`.

Typical workflow:

1. Make changes locally.
2. Commit them.
3. Push to `main`.

Vercel redeploys the Angular frontend automatically. Render redeploys the backend automatically.

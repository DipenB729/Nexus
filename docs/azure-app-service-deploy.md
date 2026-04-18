# Azure Deployment

This project is set up to deploy to Azure App Service as a single ASP.NET Core app that serves the Angular frontend and API together.

## Architecture

- App host: Azure App Service
- Database: Azure SQL Database
- CI/CD: GitHub Actions via `.github/workflows/ci-cd.yml`

## What is already configured

- `dotnet publish` builds the Angular app during publish and includes the static files in the ASP.NET output.
- `.github/workflows/ci-cd.yml` builds, publishes, uploads an artifact, and can deploy to Azure Web App by publish profile.
- Startup now applies the checked-in phase 5 inventory SQL script if the historical EF migration chain is missing that schema on a fresh database.

## 1. Create Azure SQL Database

Create a SQL Server and a SQL Database in Azure.

Recommended connection string format:

```text
Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Make sure the Azure SQL server firewall allows connections from Azure services, or explicitly allow your App Service outbound access.

## 2. Create Azure App Service

Create:

- Resource type: `Web App`
- Runtime stack: `.NET 6 (LTS)`
- Operating system: `Windows`

Free tier is acceptable for a demo deployment. For anything real, use a paid tier.

## 3. Configure App Settings in Azure

In the App Service `Environment variables` / `Application settings`, add:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<your Azure SQL connection string>
Jwt__Issuer=HospitalManagementSystem
Jwt__Audience=HospitalManagementSystem.Client
Jwt__Key=<a long random secret at least 32 characters>
Jwt__ExpiresInMinutes=120
```

Notes:

- Do not use the demo JWT key from `appsettings.json` in production.
- Azure App Service maps `ConnectionStrings__DefaultConnection` correctly into the ASP.NET configuration system.

## 4. Configure GitHub Secrets

This repo's workflow expects these repository or environment secrets:

```text
AZURE_WEBAPP_NAME
AZURE_WEBAPP_PUBLISH_PROFILE
```

How to get them:

1. Open the Azure Web App.
2. Download the publish profile.
3. In GitHub, open `Settings -> Secrets and variables -> Actions`.
4. Add:
- `AZURE_WEBAPP_NAME`: your Azure Web App name
- `AZURE_WEBAPP_PUBLISH_PROFILE`: full contents of the downloaded publish profile XML

If you want separate staging and production values, create GitHub environments named `staging` and `production` and store the secrets there.

## 5. Deploy from GitHub Actions

The workflow deploys only on manual dispatch.

Use:

1. Push your branch to GitHub.
2. Open `Actions`.
3. Open the `CI-CD` workflow.
4. Click `Run workflow`.
5. Choose the branch and `production` environment.

The workflow will:

- install Node 18
- build Angular
- build and publish the ASP.NET app
- deploy the publish output to Azure App Service

## 6. First Startup Behavior

On first startup, the app will:

1. connect to Azure SQL
2. run EF Core migrations
3. if the migration chain hits the known missing phase 5 inventory schema, apply `Data/Sql/Phase5InventoryInfrastructure.sql`
4. retry migrations
5. run seed data

That recovery path is required for this repo because the checked-in migration history is incomplete around the inventory module.

## 7. Verify Deployment

After deployment, verify:

- `/swagger`
- login flow
- inventory pages
- doctor workspace pages

If startup fails, check:

- App Service `Log stream`
- App Service `Diagnose and solve problems`
- connection string correctness
- Azure SQL firewall settings

## Local Publish Check

This repo was verified with:

```powershell
dotnet publish AngularApp4/AngularApp4.csproj --configuration Release
```

## Current Known Risk

The publish currently reports frontend dependency vulnerabilities from `npm audit`. That does not block deployment, but it should be addressed separately.

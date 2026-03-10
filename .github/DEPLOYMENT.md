# Deployment Environments

This repository uses GitHub Actions workflow `.github/workflows/ci-cd.yml`.

## Environments
The workflow supports two GitHub deployment environments:
- `staging`
- `production`

They are selected when manually running the workflow (`workflow_dispatch`).

## Required repository secrets (for Azure deployment)
To enable real deployment to Azure App Service, configure:
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

If these are not set, the deploy job will still run but only report that deployment target is not configured.

## What the pipeline does
1. Installs Node.js + npm dependencies.
2. Builds Angular in production mode.
3. Restores/builds .NET backend.
4. Runs .NET tests if any test projects exist.
5. Publishes app artifacts.
6. Deploys on manual trigger to selected environment.

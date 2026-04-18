# Nexus.Hosting.AspNetCore

Small helpers for ASP.NET Core apps deployed behind proxies and on hosts like Render.

Features:

- bind to Render's `PORT`
- normalize `postgres://` and `postgresql://` URLs into Npgsql connection strings
- enable forwarded headers for reverse-proxy deployments

## Install

```bash
dotnet add package Nexus.Hosting.AspNetCore
```

## Usage

```csharp
using Nexus.Hosting.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseRenderPortBinding();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    .NormalizePostgresConnectionString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseCommonForwardedHeaders();
```

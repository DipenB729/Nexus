const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7230';

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api/todo",
      "/api/auth",
      "/api/services",
      "/api/hospital-services",
      "/api/appointments",
      "/api/dashboard",
      "/api/admin",
      "/api/roles",
      "/api/schedules",
      "/api/settings",
      "/api/departments",
      "/api/doctors",
      "/api/staff",
      "/api/patient-categories",
      "/api/wards",
      "/api/beds"
    ],
    proxyTimeout: 10000,
    target: target,
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    }
  }
]

module.exports = PROXY_CONFIG;

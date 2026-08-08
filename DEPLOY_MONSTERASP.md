# MEDIC-AI deployment to MonsterASP.NET

This version serves the React/Vite frontend and ASP.NET Core 8 API from the same MonsterASP.NET website. The React build is copied into `MedicAI.Api/wwwroot` during GitHub Actions deployment.

## MonsterASP settings

1. Website runtime: .NET 8 / x86.
2. Configure environment variables under Website > Scripting > Environment Variables:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection=<Neon Npgsql connection string>`
   - `Jwt__Key=<long random secret, at least 32 characters>`
   - `Jwt__Issuer=MEDIC-AI`
   - `Jwt__Audience=MEDIC-AI-Web`
3. Enable WebDeploy under Website > Deploy > WebDeploy access.
4. Add GitHub repository secrets from the WebDeploy details:
   - `WEBSITE_NAME`
   - `SERVER_COMPUTER_NAME`
   - `SERVER_USERNAME`
   - `SERVER_PASSWORD`
5. Push this project to the `main` branch or manually run the GitHub Actions workflow.

## Neon connection string format

Use Npgsql keyword/value format, for example:

`Host=YOUR_HOST;Port=5432;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=Require`

Never commit the real password or JWT key to GitHub.

## Test after deployment

- Website: `http://medic-ai.runasp.net/`
- Health: `http://medic-ai.runasp.net/health`
- Swagger: `http://medic-ai.runasp.net/swagger`

Use only synthetic/demo data on free hosting.

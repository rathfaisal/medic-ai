# Deploy MEDIC-AI Free for Testing: Render + Neon

This deployment is for demonstration and testing with fictional or synthetic patient data only. Do not enter real patient or confidential clinic data.

## Hosting layout

- React frontend: Render Static Site
- ASP.NET Core API: Render Free Web Service using Docker
- PostgreSQL: Neon Free PostgreSQL

## 1. Upload the project to GitHub

1. Extract the project.
2. Create a new GitHub repository.
3. Upload the contents of the `MEDIC-AI-Full-System` folder to the repository root.
4. Do not upload real passwords, patient records, or `.env` files.

## 2. Create the Neon PostgreSQL database

1. Create a free Neon account and project.
2. Open the project dashboard and copy the complete PostgreSQL connection string.
3. Keep the connection string private. It will be added to Render as an environment variable.

The API calls `EnsureCreatedAsync` during startup, so it creates the schema and demo accounts when the database is empty.

## 3. Deploy the ASP.NET Core API on Render

Create a new **Web Service** and connect the GitHub repository.

Use these settings:

```text
Language: Docker
Root Directory: src/MedicAI.Api
Dockerfile Path: Dockerfile
Instance Type: Free
Health Check Path: /health
```

Add these environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<paste the complete Neon connection string>
Jwt__Key=<a random secret containing at least 32 characters>
Jwt__Issuer=MEDIC-AI
Jwt__Audience=MEDIC-AI-Web
Cors__AllowedOrigins=https://REPLACE-WITH-FRONTEND.onrender.com
```

For the first deployment, the frontend URL does not exist yet. Temporarily use:

```text
Cors__AllowedOrigins=http://localhost:5173
```

Deploy the service. When deployment finishes, copy the API URL, for example:

```text
https://medic-ai-api.onrender.com
```

Test:

```text
https://medic-ai-api.onrender.com/health
https://medic-ai-api.onrender.com/swagger
```

The first request after inactivity can take about one minute because the free service sleeps.

## 4. Deploy the React frontend on Render

Create a new **Static Site** from the same repository.

Use these settings:

```text
Root Directory: src/medic-ai-web
Build Command: npm install && npm run build
Publish Directory: dist
```

Add this build environment variable:

```text
VITE_API_URL=https://REPLACE-WITH-YOUR-API.onrender.com
```

Add this rewrite rule so React routes work when refreshed:

```text
Source: /*
Destination: /index.html
Action: Rewrite
```

Deploy the site and copy its URL, for example:

```text
https://medic-ai-web.onrender.com
```

## 5. Update API CORS

Return to the API Web Service and change:

```text
Cors__AllowedOrigins=https://medic-ai-web.onrender.com
```

Save the variable and redeploy the API.

For multiple permitted sites, separate URLs with commas:

```text
Cors__AllowedOrigins=https://medic-ai-web.onrender.com,http://localhost:5173
```

## 6. Test demo accounts

```text
Administrator: admin@medic-ai.local / Admin123!
Doctor:        doctor@medic-ai.local / Doctor123!
Receptionist:  staff@medic-ai.local / Staff123!
Patient:       patient@medic-ai.local / Patient123!
```

Change these passwords before showing the app publicly.

## 7. Test the complete flow

1. Sign in as the patient.
2. Book or open an appointment.
3. Submit a non-emergency pre-triage form.
4. Verify that an urgency result or manual-review fallback is stored.
5. Submit a red-flag scenario and verify the emergency warning.
6. Sign in as clinic staff and check the queue and alerts.
7. Sign in as the doctor and override an urgency level with a reason.
8. Verify that the queue changes and an audit record is created.
9. Sign in as the administrator and inspect users and logs.

## Important free-tier limitations

- The Render API sleeps after inactivity, so the first request may be slow.
- The generated ML.NET model is stored on the API container's temporary filesystem. A restart may cause the model to train again from the included synthetic dataset.
- Neon free storage is limited, but sufficient for an FYP demonstration database.
- This deployment is not suitable for real healthcare operations or real patient information.

## Common errors

### CORS error in browser

Confirm that `Cors__AllowedOrigins` exactly matches the frontend URL, including `https://` and without a trailing slash.

### Database connection error

Paste the complete Neon connection string into `ConnectionStrings__DefaultConnection`. Do not remove its SSL settings.

### Frontend calls `/api` instead of Render API

Confirm `VITE_API_URL` was configured before the frontend build, then trigger a new deployment.

### React page shows 404 after refresh

Add the Render rewrite rule from `/*` to `/index.html`.

### API takes a long time to open

Wait for the free Render service to wake and initialise the database and ML model, then refresh `/health`.

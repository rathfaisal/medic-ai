# MEDIC-AI Windows Development Setup

## Required software

- Visual Studio 2022 with ASP.NET and web development workload
- .NET 8 SDK
- Node.js 20 or newer
- PostgreSQL 16 or newer
- pgAdmin 4
- Git

## 1. Create the database

Create a PostgreSQL database named `medic_ai` and a user that can create tables.

Example connection string:

```text
Host=localhost;Port=5432;Database=medic_ai;Username=postgres;Password=YOUR_PASSWORD
```

Update `src/MedicAI.Api/appsettings.Development.json`.

## 2. Start the API

```powershell
cd src\MedicAI.Api
dotnet restore
dotnet run
```

The API creates the schema and seed data automatically. Swagger is available at the URL shown in the console, normally `http://localhost:5080/swagger`.

## 3. Start the React frontend

Open a second terminal:

```powershell
cd src\medic-ai-web
npm install
npm run dev
```

Open `http://localhost:5173`.

## 4. Run tests

```powershell
cd src\MedicAI.Tests
dotnet test
```

## 5. Reset the demonstration database

Stop the API, delete and recreate the `medic_ai` database, and start the API again. The seed process is idempotent and recreates the demonstration data.


## Independent doctor-validation evaluation

1. Start the API once to generate `src\MedicAI.Api\MachineLearning\Artifacts\triage-model.zip`.
2. Ask the clinic reviewer to fill only `DoctorLabel` and `DoctorNotes` in `src\MedicAI.Api\MachineLearning\Data\doctor-validation-template.csv` without seeing model predictions.
3. Run:

```powershell
dotnet run --project src\MedicAI.ModelEvaluation
```

4. Review `docs\doctor-validation-results.json`. Report only the actual observed agreement metrics.

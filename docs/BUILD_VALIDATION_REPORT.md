# MEDIC-AI Build Validation Report

## Package scope

This package contains the complete source-code MVP for the MEDIC-AI FYP prototype:

- ASP.NET Core Web API backend in C#
- PostgreSQL persistence through Entity Framework Core and Npgsql
- React.js frontend using JavaScript, HTML and CSS
- ML.NET multiclass pre-triage model
- Deterministic red-flag safety engine
- Automated tests, synthetic datasets, doctor-validation utility, Docker configuration and Chapter 6 evidence guides

## Static validation completed

The following checks were completed on the packaged source:

- All JSON configuration files parsed successfully.
- All `.csproj` XML files parsed successfully.
- `docker-compose.yml` parsed successfully as YAML.
- Every project path listed in `MEDIC-AI.sln` exists.
- C# source files passed lexical delimiter and unterminated-string/comment checks.
- All 21 JavaScript/JSX files passed TypeScript parser syntax checks.
- All relative frontend imports resolve to files in the project.
- CSS braces are balanced.
- `triage-training-data.csv` contains 1,500 valid rows with 11 columns.
- `doctor-validation-template.csv` contains 120 valid rows with 13 columns.
- The ZIP package is tested for archive integrity before delivery.

## Safety checks represented in source and tests

- Rule-based red flags execute before the ML model.
- ML classifications cover only Routine, Priority and Urgent.
- A machine-learning result cannot downgrade a rule-based emergency warning.
- Emergency-warning cases cannot enter the normal clinic queue.
- Low-confidence classifications are escalated and flagged for human review.
- If the model is unavailable, no AI prediction is fabricated; a transparent manual-review fallback is stored.
- Doctors can override urgency only with a recorded reason.
- Queue changes, emergency actions, patient-record access and overrides are audited.
- Unacknowledged emergency alerts are escalated internally to Doctor/Admin.
- The application does not claim to contact an ambulance or external emergency service.

## Runtime validation still required on the student's computer

The active artifact environment did not provide the .NET SDK, Docker, PostgreSQL, or access to the public npm registry. Therefore, the following commands could not be executed here:

```powershell
dotnet restore
dotnet build
dotnet test
npm install
npm run build
docker compose up --build
```

Run those commands using `SETUP_WINDOWS.md` before treating the system as presentation-ready. Fix any environment-specific issue found there, complete the tests in `TEST_CASES.md`, and report only observed results in Chapter 6.

## Recommended acceptance gate before Chapter 6

1. `dotnet build MEDIC-AI.sln` succeeds with zero errors.
2. `dotnet test MEDIC-AI.sln` passes.
3. `npm run build` succeeds.
4. `docker compose up --build` starts database, API and frontend.
5. `/health` reports database availability and either `Healthy` or a transparent `Degraded` ML state.
6. All four demo accounts can log in.
7. Routine, low-confidence, urgent and emergency scenarios behave as documented.
8. Doctor override updates the queue and audit log.
9. Emergency alert acknowledgement and resolution work.
10. Screenshots in `CHAPTER_6_SCREENSHOT_CHECKLIST.md` are captured from the running system.

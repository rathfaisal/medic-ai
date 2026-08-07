# MEDIC-AI Full System

MEDIC-AI is a web-based patient pre-triage and clinic-management prototype for Klinik Medic 24-7. It combines:

- React.js, JavaScript, HTML and CSS frontend
- C# ASP.NET Core Web API backend
- PostgreSQL database through Entity Framework Core and Npgsql
- ML.NET multiclass urgency model
- Doctor-approved rule-based emergency safety layer
- JWT authentication and role-based access control

> **Safety statement:** MEDIC-AI provides preliminary urgency guidance only. It does not diagnose disease and does not replace a doctor. Emergency red-flag rules always run before the machine-learning model, and the model cannot downgrade an emergency warning.

## Implemented roles

- Patient
- Receptionist / Clinic Staff
- Doctor
- Administrator

## Implemented core functions

- Patient registration, login and profile management
- Appointment booking, rescheduling and cancellation
- Structured symptom pre-triage submission
- Red-flag emergency detection
- ML.NET classification into Routine, Priority or Urgent
- Confidence-based escalation and staff-review flag
- Transparent manual-review fallback when the ML model is unavailable
- Emergency-alert creation, timed escalation, acknowledgement and resolution
- Smart digital queue ordered by urgency and arrival time
- Doctor patient review and urgency override
- Consultation, diagnosis, treatment and follow-up recording
- User and role administration
- In-app notifications
- Operational reports and audit logs

## Fastest setup: Docker Desktop

1. Install Docker Desktop.
2. Copy `.env.example` to `.env` and change the passwords/secrets.
3. Open a terminal in the project root.
4. Run:

```bash
docker compose up --build
```

5. Open:

- Web application: `http://localhost:3000`
- API Swagger page: `http://localhost:5080/swagger`
- API health endpoint: `http://localhost:5080/health`

The database schema, demo accounts, red-flag rules, sample appointments and synthetic ML model are created automatically on first startup. Unacknowledged emergency alerts are escalated inside the clinic dashboard after the configured acknowledgement period; no external emergency service is contacted.

## Demo accounts

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@medic-ai.local` | `Admin123!` |
| Doctor | `doctor@medic-ai.local` | `Doctor123!` |
| Staff | `staff@medic-ai.local` | `Staff123!` |
| Patient | `patient@medic-ai.local` | `Patient123!` |

Change these passwords before any non-demo deployment.

## Manual development setup

See [SETUP_WINDOWS.md](SETUP_WINDOWS.md).

## ML model behaviour

When the model artifact does not exist, the API creates a synthetic non-emergency training dataset, trains an ML.NET multiclass SDCA Maximum Entropy model, evaluates it and saves the model to `MachineLearning/Artifacts/triage-model.zip`. If the model cannot initialise, the system clearly records a `ManualFallback` assessment, assigns a conservative temporary priority only for routing, and requires staff or doctor review; it does not claim that an AI prediction was produced. The independent doctor-validation dataset must still be labelled by Dr. Prem Rayan before the final evaluation is reported.

## Important limitations

- The included training data is synthetic and is not clinical evidence.
- The model is designed for an FYP demonstration, not autonomous clinical use.
- No hospital EHR, insurance, billing, ambulance, SMS, WhatsApp or wearable integration is included.
- Use only fictional/sample patient records during development and presentation.

## Independent doctor-validation evaluation

The project includes `src/MedicAI.ModelEvaluation`, a C# console utility that compares the trained ML.NET model with blind labels entered in `doctor-validation-template.csv`.

1. Ask the doctor to complete only `DoctorLabel` and `DoctorNotes` without seeing model predictions.
2. Start the API once so `triage-model.zip` is generated.
3. Run:

```powershell
dotnet run --project src\MedicAI.ModelEvaluation
```

The utility produces `docs/doctor-validation-results.json` containing exact agreement, weighted F1-score, Cohen's kappa, a confusion matrix and disagreement examples. Emergency-warning rules must still be evaluated separately through the safety test cases.

## Before Chapter 6

Run the system locally, complete the test cases in `docs/TEST_CASES.md`, obtain the independent doctor labels, and capture the screenshots listed in `docs/CHAPTER_6_SCREENSHOT_CHECKLIST.md`. Only actual observed results should be reported in Chapter 6.

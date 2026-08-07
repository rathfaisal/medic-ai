# MEDIC-AI Demonstration Script

## Startup

1. Start PostgreSQL, API and web application.
2. Open `http://localhost:5080/health` and confirm `Healthy`.
3. Open `http://localhost:3000`.
4. Keep the backup video and screenshots available.

## Scenario A — Routine workflow

1. Sign in as patient.
2. Book an appointment.
3. Submit mild symptoms without red flags.
4. Show the preliminary category, confidence and explanation.
5. Sign in as staff and check in the patient.
6. Show the smart queue.
7. Sign in as doctor and review the patient.
8. Save consultation notes and complete the visit.

## Scenario B — Doctor override

1. Open a patient assessment.
2. Change Priority to Urgent.
3. Enter a clinical reason.
4. Show that queue order changes.
5. Sign in as administrator and show the audit record.

## Scenario C — Emergency warning

1. Sign in as patient and submit severe breathing difficulty.
2. Show that the red-flag rule runs before ML.
3. Show the immediate warning and explain there is no diagnosis.
4. Sign in as staff and open Emergency Alerts.
5. Acknowledge and resolve the alert with notes.
6. Confirm the patient was not placed in the normal queue.

## Scenario D — Security

1. Demonstrate a patient cannot open `/admin/users`.
2. Demonstrate role-based navigation.
3. Explain password hashing, JWT, validation and audit logs.

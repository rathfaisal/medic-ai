# MEDIC-AI API Endpoints

Base URL during local development: `http://localhost:5080/api`

All protected endpoints require `Authorization: Bearer <JWT>`.

## Authentication

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| POST | `/auth/register` | Public | Register a patient account |
| POST | `/auth/login` | Public | Authenticate and obtain JWT |
| GET | `/auth/me` | Any authenticated | Read current account |
| POST | `/auth/logout` | Any authenticated | Record logout event |

## Profiles and directories

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET/PUT | `/profile` | Patient | View/update own patient profile |
| GET | `/doctors` | Authenticated | List available doctors |
| GET | `/patients?search=` | Staff/Doctor/Admin | Search patients |

## Appointments

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET | `/appointments` | Authenticated | Role-filtered appointment list |
| POST | `/appointments` | Patient/Staff/Admin | Book appointment |
| PUT | `/appointments/{id}` | Patient/Staff/Admin | Reschedule/update |
| DELETE | `/appointments/{id}` | Patient/Staff/Admin | Cancel appointment |

## Pre-triage

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| POST | `/triage` | Patient | Submit symptom information |
| GET | `/triage/mine` | Patient | View own assessments |
| GET | `/triage/submissions/{id}` | Authenticated, authorised | View assessment by submission |
| GET | `/triage/appointment/{id}` | Authenticated, authorised | View assessment by appointment |

## Queue

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET | `/queue` | Staff/Doctor/Admin | View active priority queue |
| GET | `/queue/mine` | Patient | View own current status |
| POST | `/queue/check-in` | Staff/Admin | Check in appointment |
| POST | `/queue/check-in-walk-in` | Staff/Admin | Add a walk-in using an assessment or manual urgency |
| PATCH | `/queue/{id}/status` | Staff/Doctor/Admin | Update queue state |
| POST | `/queue/call-next` | Staff/Doctor/Admin | Call highest-priority waiting patient |

## Doctor workflow

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET | `/doctor/patients/{id}/review` | Staff/Doctor/Admin | Combined patient review |
| POST | `/doctor/assessments/{id}/override` | Doctor | Override urgency with reason |
| POST | `/doctor/consultations` | Doctor | Record consultation |
| GET | `/consultations/mine` | Patient | View own consultation history |

## Emergency alerts

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET | `/emergency-alerts` | Staff/Doctor/Admin | View alerts |
| POST | `/emergency-alerts/{id}/acknowledge` | Staff/Doctor/Admin | Acknowledge alert |
| POST | `/emergency-alerts/{id}/resolve` | Staff/Doctor/Admin | Resolve with notes |
| POST | `/emergency-alerts/{id}/false-alarm` | Doctor/Admin | Document false alarm |

## Administration and reporting

| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| GET/POST | `/admin/users` | Admin | List/create clinic users |
| PATCH | `/admin/users/{id}` | Admin | Change status/role/availability |
| GET | `/reports/summary` | Doctor/Admin | Dashboard summary |
| GET | `/reports/audit-logs` | Admin | Audit trail |
| GET | `/notifications` | Authenticated | List own notifications |
| POST | `/notifications/{id}/read` | Authenticated | Mark message as read |


## Triage safety notes

- Every assessment response includes `source`: `RuleEngine`, `MachineLearning`, or `ManualFallback`.
- A rule-engine emergency is excluded from the normal queue and creates an emergency alert.
- A low-confidence ML result is escalated one level and marked `reviewRequired`.
- If the model is unavailable, no AI prediction is fabricated. The API records `ManualFallback`, assigns a conservative temporary Priority only for routing, and requires human review.
- `GET /health` reports database and ML-model availability. A missing model produces a degraded state rather than a false healthy claim.
- Unacknowledged emergency alerts are escalated internally to Doctor/Admin after the configured acknowledgement window. No external emergency service is contacted.

# MEDIC-AI Core Test Cases

| ID | Module | Test | Expected Result |
|---|---|---|---|
| AUTH-01 | Registration | Register valid patient | Account and profile created; JWT returned |
| AUTH-02 | Registration | Duplicate email | Conflict response; no duplicate user |
| AUTH-03 | Login | Wrong password repeatedly | Invalid response; rate limiter eventually applies |
| SEC-01 | Access | Patient requests admin users | Access denied |
| SEC-02 | Access | Patient requests another patient's review URL | Access denied |
| APPT-01 | Appointment | Book future available slot | Appointment saved |
| APPT-02 | Appointment | Book same patient/same time | Conflict rejected |
| APPT-03 | Appointment | Book doctor already occupied | Conflict rejected |
| TRI-01 | Triage | Mild non-red-flag combination | Routine/Priority/Urgent ML result stored |
| TRI-02 | Triage | Severe breathing difficulty | Emergency warning; ML cannot downgrade |
| TRI-03 | Triage | Low-confidence output | One-level escalation and review flag |
| TRI-04 | Triage | No consent | Submission rejected |
| QUE-01 | Queue | Check in assessed appointment | Queue entry created |
| QUE-02 | Queue | Emergency warning check-in | Rejected from normal queue |
| QUE-03 | Queue | Call next | Highest urgency/wait score called first |
| DOC-01 | Override | Doctor overrides urgency with reason | Assessment, queue and audit updated |
| DOC-02 | Consultation | Doctor records consultation | Record saved and appointment completed |
| ALERT-01 | Alert | Staff acknowledges open alert | User/time recorded |
| ALERT-02 | Alert | Resolve without notes | Rejected |
| ALERT-03 | Alert | Doctor marks false alarm | Status and reason recorded |
| ADMIN-01 | Users | Admin creates doctor | Doctor profile created |
| ADMIN-02 | Users | Disable final active admin | Rejected |
| FAIL-01 | AI failure | Model unavailable | Submission saved; no fabricated prediction |
| PRIV-01 | Data | Demo uses fictional records | No real ID or confidential clinic record stored |

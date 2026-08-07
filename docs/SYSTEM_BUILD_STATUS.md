# MEDIC-AI System Build Status

## Completed modules

### Patient

- Registration, login and logout
- Patient profile management
- Appointment booking, rescheduling and cancellation
- Structured pre-triage submission
- Preliminary urgency guidance with source, confidence and explanation
- Own queue status
- Consultation and follow-up history
- In-app notifications

### Receptionist / Clinic Staff

- Appointment administration
- Patient search
- Appointment check-in
- Walk-in queue entry
- Manual urgency when no assessment exists
- Smart queue monitoring and status updates
- Emergency alert acknowledgement and resolution
- Limited patient review without confidential consultation history

### Doctor

- Assigned and unassigned appointment view
- Priority queue monitoring
- Full patient review
- AI/rule assessment review
- Urgency override with mandatory reason
- Consultation, diagnosis, treatment and follow-up recording
- Emergency alert handling
- Operational summary reports

### Administrator

- User creation and status management
- Staff/Admin role changes
- Doctor availability control
- Appointment and queue access
- Emergency alert access
- Dashboard reports
- Audit-log review

### AI and safety

- Synthetic ambiguous scenario generator
- ML.NET multiclass model for Routine, Priority and Urgent
- Rule-based emergency layer
- Confidence-based escalation
- Manual-review fallback
- Independent doctor-label evaluation utility
- Emergency-alert acknowledgement watchdog

### Technical and documentation

- PostgreSQL reference schema
- EF Core entity model
- JWT authentication and role-based authorization
- Active-account and role revalidation for issued JWTs
- Login rate limiting
- CORS configuration
- Docker Compose deployment
- Automated tests
- API endpoint reference
- Test cases and demo script
- Chapter 6 screenshot checklist

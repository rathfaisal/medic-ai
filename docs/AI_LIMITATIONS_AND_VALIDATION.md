# AI Safety, Limitations and Validation Plan

## Intended role

MEDIC-AI gives preliminary urgency guidance. It is not a diagnostic system and does not replace a doctor.

## Safety hierarchy

1. Validate submitted data.
2. Execute deterministic red-flag rules.
3. If a red flag is present, create `EmergencyWarning` and an emergency alert.
4. Never send an emergency-warning case to the ordinary digital queue.
5. For non-emergency cases, run the ML.NET multiclass model.
6. If confidence is below 0.65, escalate one level and require staff review.
7. Allow a doctor to override urgency with a documented reason.
8. Recalculate the queue and create an audit record after override.

## Model scope

The ML.NET model classifies only:

- Routine
- Priority
- Urgent

Emergency warnings are not learned by the model. They are controlled by approved red-flag rules.

## Circularity control

Internal synthetic accuracy must not be described as clinical truth. The included generator intentionally introduces overlapping combinations, label uncertainty, missing information and conflicting signals.

The file `MachineLearning/Data/doctor-validation-template.csv` contains 120 scenarios without generated labels. Dr. Prem Rayan should label these independently and blindly.

Report separately:

- Internal synthetic accuracy, precision, recall, F1 and confusion matrix
- Model-to-doctor exact agreement
- Per-class agreement
- Weighted F1
- Cohen's kappa, where feasible
- Emergency-rule sensitivity and false-warning results
- Number of emergency cases downgraded by ML, which must be zero

## Prototype limitations

- Synthetic training and demonstration data only
- No hospital EHR integration
- No ambulance or emergency-service integration
- No payment, insurance, telemedicine or wearable-device integration
- No guarantee of clinical accuracy
- Doctor and clinic staff retain final responsibility

## Running the independent agreement utility

After the doctor has filled the `DoctorLabel` column in `src/MedicAI.Api/MachineLearning/Data/doctor-validation-template.csv`, run:

```powershell
dotnet run --project src\MedicAI.ModelEvaluation
```

The generated `docs/doctor-validation-results.json` reports model-to-doctor agreement separately from synthetic hold-out performance. Do not copy any percentage into the report until the doctor-labelled file has genuinely been completed and the utility has run successfully.

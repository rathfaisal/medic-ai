import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { Alert, Card, Field, Loading, PageHeader } from '../components/UI'

export default function ProfilePage() {
  const [form, setForm] = useState(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => { api.get('/profile').then(({ data }) => setForm(data)).catch((err) => setError(errorMessage(err))) }, [])
  if (!form) return error ? <Alert type="danger">{error}</Alert> : <Loading />

  const change = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  const submit = async (event) => {
    event.preventDefault(); setBusy(true); setError(''); setMessage('')
    try {
      const payload = { ...form, patientId: undefined, userId: undefined, email: undefined }
      const { data } = await api.put('/profile', payload)
      setForm(data); setMessage('Profile updated successfully.')
    } catch (err) { setError(errorMessage(err)) }
    finally { setBusy(false) }
  }

  return <>
    <PageHeader title="My profile" description="Maintain contact, allergy and existing-condition information used by authorised clinic personnel." />
    {message && <Alert type="success">{message}</Alert>}{error && <Alert type="danger">{error}</Alert>}
    <Card title="Patient information">
      <form onSubmit={submit} className="form-stack">
        <div className="form-grid two">
          <Field label="Full name" required><input value={form.fullName || ''} onChange={change('fullName')} required /></Field>
          <Field label="Email"><input value={form.email || ''} disabled /></Field>
          <Field label="Date of birth" required><input type="date" value={form.dateOfBirth || ''} onChange={change('dateOfBirth')} required /></Field>
          <Field label="Gender"><select value={form.gender || ''} onChange={change('gender')}><option value="">Prefer not to say</option><option>Male</option><option>Female</option><option>Other</option></select></Field>
          <Field label="Phone" required><input value={form.phone || ''} onChange={change('phone')} required /></Field>
          <Field label="Emergency contact"><input value={form.emergencyContact || ''} onChange={change('emergencyContact')} /></Field>
        </div>
        <Field label="Address"><textarea rows="2" value={form.address || ''} onChange={change('address')} /></Field>
        <div className="form-grid two">
          <Field label="Allergies"><textarea rows="4" value={form.allergies || ''} onChange={change('allergies')} /></Field>
          <Field label="Existing conditions"><textarea rows="4" value={form.existingConditions || ''} onChange={change('existingConditions')} /></Field>
        </div>
        <div><button className="button button-primary" disabled={busy}>{busy ? 'Saving…' : 'Save profile'}</button></div>
      </form>
    </Card>
  </>
}

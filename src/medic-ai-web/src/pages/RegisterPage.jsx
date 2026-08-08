import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../api/client'
import { Alert, Field } from '../components/UI'

const initial = { email: '', password: '', fullName: '', dateOfBirth: '', phone: '', gender: '' }

export default function RegisterPage() {
  const [form, setForm] = useState(initial)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const { register } = useAuth()
  const navigate = useNavigate()

  const change = (key) => (event) => setForm({ ...form, [key]: event.target.value })

  const submit = async (event) => {
    event.preventDefault()
    setBusy(true)
    setError('')

    try {
      await register({ ...form, gender: form.gender || null })
      navigate('/dashboard')
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-panel auth-intro">
        <Link className="auth-home-link" to="/">← Back to homepage</Link>
        <div className="brand large">
          <div className="brand-mark">M+</div>
          <div><strong>MEDIC-AI</strong><span>Patient Registration</span></div>
        </div>
        <h1>Create your secure clinic account.</h1>
        <p>Book appointments and provide structured symptom information before consultation.</p>
        <div className="safety-box">
          <strong>Use fictional data for the FYP demo</strong>
          <span>Do not enter real identification numbers or confidential medical records during development.</span>
        </div>
      </div>

      <div className="auth-panel auth-form-panel">
        <form className="auth-form wide" onSubmit={submit}>
          <div><div className="eyebrow">Patient account</div><h2>Register</h2></div>

          {error && <Alert type="danger">{error}</Alert>}

          <div className="form-grid two">
            <Field label="Full name" required>
              <input value={form.fullName} onChange={change('fullName')} required maxLength="150" />
            </Field>

            <Field label="Phone number" required>
              <input value={form.phone} onChange={change('phone')} required maxLength="30" />
            </Field>

            <Field label="Date of birth" required>
              <input type="date" value={form.dateOfBirth} onChange={change('dateOfBirth')} required />
            </Field>

            <Field label="Gender (optional)">
              <select value={form.gender} onChange={change('gender')}>
                <option value="">Prefer not to say</option>
                <option>Male</option>
                <option>Female</option>
                <option>Other</option>
              </select>
            </Field>

            <Field label="Email address" required>
              <input type="email" value={form.email} onChange={change('email')} required />
            </Field>

            <Field label="Password" required hint="At least 8 characters">
              <input type="password" value={form.password} onChange={change('password')} minLength="8" required />
            </Field>
          </div>

          <button className="button button-primary full" disabled={busy}>
            {busy ? 'Creating account…' : 'Create account'}
          </button>

          <p className="auth-link">Already registered? <Link to="/login">Sign in</Link></p>
        </form>
      </div>
    </div>
  )
}

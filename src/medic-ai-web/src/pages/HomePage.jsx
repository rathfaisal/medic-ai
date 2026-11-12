import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function HomePage() {
  const { user } = useAuth()

  return (
    <div className="public-home">
      <header className="public-nav">
        <Link className="public-brand" to="/">
          <span className="brand-mark" aria-hidden="true">M+</span>
          <span><strong>MEDIC-AI</strong><small>Klinik Medic 24-7</small></span>
        </Link>
        <nav className="public-nav-actions" aria-label="Account">
          {user ? <Link className="button button-primary" to="/dashboard">Open dashboard</Link>
            : <Link className="button button-ghost public-ghost" to="/login">Sign in</Link>}
        </nav>
      </header>

      <main>
        <section className="public-hero">
          <div className="public-hero-copy">
            <p className="public-kicker">Patient portal</p>
            <h1>Manage your clinic visit online.</h1>
            <p className="public-lead">
              Book an appointment, share your symptoms before your visit and check your queue status at Klinik Medic 24-7.
            </p>
            <div className="public-hero-actions">
              {user ? <Link className="button button-primary public-large-button" to="/dashboard">Open dashboard</Link> : <>
                <Link className="button button-primary public-large-button" to="/register">Create patient account</Link>
                <Link className="button button-ghost public-ghost public-large-button" to="/login">I have an account</Link>
              </>}
            </div>
          </div>

          <div className="visit-guide">
            <h2>Your visit, step by step</h2>
            <ol>
              <li><strong>Book an appointment</strong><span>Choose a time for your clinic visit.</span></li>
              <li><strong>Share your symptoms</strong><span>Complete the pre-triage form before your visit.</span></li>
              <li><strong>Follow your queue status</strong><span>Check your place in the queue after checking in.</span></li>
            </ol>
          </div>
        </section>

        <section className="public-access">
          <div>
            <h2>For clinic staff</h2>
            <p>Manage appointments, monitor the queue and review pre-triage submissions.</p>
            <Link className="text-link" to={user ? '/dashboard' : '/login'}>
              {user ? 'Open dashboard' : 'Staff sign in'} →
            </Link>
          </div>
          <div>
            <h2>How pre-triage is used</h2>
            <p>Pre-triage provides preliminary urgency guidance. Clinic staff review the result and clinicians make the final assessment.</p>
          </div>
        </section>
      </main>
      <footer className="public-footer"><strong>MEDIC-AI</strong><p>Klinik Medic 24-7 · FYP prototype</p></footer>
    </div>
  )
}

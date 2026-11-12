import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const navByRole = {
  Patient: [
    ['Dashboard', '/dashboard'], ['My Profile', '/profile'], ['Appointments', '/appointments'],
    ['Pre-Triage', '/pre-triage'], ['Queue Status', '/queue'], ['Consultations', '/consultations'],
    ['Notifications', '/notifications']
  ],
  Staff: [
    ['Dashboard', '/dashboard'], ['Appointments', '/appointments'], ['Digital Queue', '/queue'],
    ['Emergency Alerts', '/emergency-alerts'], ['Patient Review', '/patient-review'], ['Notifications', '/notifications']
  ],
  Doctor: [
    ['Dashboard', '/dashboard'], ['Appointments', '/appointments'], ['Digital Queue', '/queue'],
    ['Emergency Alerts', '/emergency-alerts'], ['Patient Review', '/patient-review'],
    ['Reports', '/reports'], ['Notifications', '/notifications']
  ],
  Admin: [
    ['Dashboard', '/dashboard'], ['Appointments', '/appointments'], ['Digital Queue', '/queue'],
    ['Emergency Alerts', '/emergency-alerts'], ['Patient Review', '/patient-review'],
    ['Users & Roles', '/admin/users'], ['Reports & Audit', '/reports'], ['Notifications', '/notifications']
  ]
}

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const signOut = async () => {
    await logout()
    navigate('/')
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#workspace">Skip to content</a>
      <aside className="sidebar">
        <div className="brand">
          <img className="brand-mark" src="/medic-ai.svg" alt="" />
          <div><strong>MEDIC-AI</strong><span>Clinic Management</span></div>
        </div>

        <nav aria-label="Workspace">
          {(navByRole[user?.role] || []).map(([label, path]) => (
            <NavLink
              key={path}
              to={path}
              className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}
            >
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-chip">
            <strong>{user?.displayName}</strong>
            <span>{user?.role}</span>
          </div>
          <button className="button button-ghost full" onClick={signOut}>Log out</button>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div>
            <div className="eyebrow">Klinik Medic 24-7</div>
            <strong>{user?.role === 'Patient' ? 'Patient portal' : 'Clinic workspace'}</strong>
          </div>
          <div className="safety-pill">AI guidance is not a diagnosis</div>
        </header>

        <section className="page-area" id="workspace"><Outlet /></section>
      </main>
    </div>
  )
}

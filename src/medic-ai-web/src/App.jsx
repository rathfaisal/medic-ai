import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from './components/ProtectedRoute'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import DashboardPage from './pages/DashboardPage'
import ProfilePage from './pages/ProfilePage'
import AppointmentsPage from './pages/AppointmentsPage'
import PreTriagePage from './pages/PreTriagePage'
import QueuePage from './pages/QueuePage'
import PatientReviewPage from './pages/PatientReviewPage'
import EmergencyAlertsPage from './pages/EmergencyAlertsPage'
import AdminUsersPage from './pages/AdminUsersPage'
import ReportsPage from './pages/ReportsPage'
import NotificationsPage from './pages/NotificationsPage'
import ConsultationsPage from './pages/ConsultationsPage'
import { useAuth } from './auth/AuthContext'

function PublicOnly({ children }) {
  const { user } = useAuth()
  return user ? <Navigate to="/" replace /> : children
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<PublicOnly><LoginPage /></PublicOnly>} />
      <Route path="/register" element={<PublicOnly><RegisterPage /></PublicOnly>} />
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="appointments" element={<AppointmentsPage />} />
          <Route path="queue" element={<QueuePage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route element={<ProtectedRoute roles={['Patient']} />}>
            <Route path="profile" element={<ProfilePage />} />
            <Route path="pre-triage" element={<PreTriagePage />} />
            <Route path="consultations" element={<ConsultationsPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={['Staff', 'Doctor', 'Admin']} />}>
            <Route path="patient-review" element={<PatientReviewPage />} />
            <Route path="emergency-alerts" element={<EmergencyAlertsPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={['Admin']} />}>
            <Route path="admin/users" element={<AdminUsersPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={['Doctor', 'Admin']} />}>
            <Route path="reports" element={<ReportsPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

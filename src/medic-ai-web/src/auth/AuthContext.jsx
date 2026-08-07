import { createContext, useContext, useMemo, useState } from 'react'
import api from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try { return JSON.parse(localStorage.getItem('medic-ai-user')) } catch { return null }
  })

  const saveSession = (session) => {
    localStorage.setItem('medic-ai-token', session.token)
    const safeUser = { id: session.id, email: session.email, role: session.role, displayName: session.displayName }
    localStorage.setItem('medic-ai-user', JSON.stringify(safeUser))
    setUser(safeUser)
  }

  const login = async (email, password) => {
    const { data } = await api.post('/auth/login', { email, password })
    saveSession(data)
    return data
  }

  const register = async (payload) => {
    const { data } = await api.post('/auth/register', payload)
    saveSession(data)
    return data
  }

  const logout = async () => {
    try { await api.post('/auth/logout') } catch { /* token may already be expired */ }
    localStorage.removeItem('medic-ai-token')
    localStorage.removeItem('medic-ai-user')
    setUser(null)
  }

  const value = useMemo(() => ({ user, login, register, logout, isAuthenticated: Boolean(user) }), [user])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}

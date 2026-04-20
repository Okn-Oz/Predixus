import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Users, BarChart3, Activity, ShieldCheck, ShieldOff, UserCog, RefreshCw } from 'lucide-react'
import { getAdminStats, getAdminUsers, toggleUserActive, setUserRole, forceSync } from '../api'
import type { AdminStats, UserSummary } from '../types'
import { cn } from '../lib/utils'

export default function AdminPage() {
  const navigate = useNavigate()
  const [stats, setStats] = useState<AdminStats | null>(null)
  const [users, setUsers] = useState<UserSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState<string | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [syncMsg, setSyncMsg] = useState<string | null>(null)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    setLoading(true)
    try {
      const [s, u] = await Promise.all([getAdminStats(), getAdminUsers()])
      setStats(s)
      setUsers(u)
    } finally {
      setLoading(false)
    }
  }

  async function handleToggle(userId: string) {
    setActionLoading(userId + ':toggle')
    try {
      await toggleUserActive(userId)
      setUsers(prev => prev.map(u => u.id === userId ? { ...u, isActive: !u.isActive } : u))
      if (stats) {
        const user = users.find(u => u.id === userId)
        if (user) {
          setStats(s => s ? {
            ...s,
            activeUsers: user.isActive ? s.activeUsers - 1 : s.activeUsers + 1
          } : s)
        }
      }
    } finally {
      setActionLoading(null)
    }
  }

  async function handleRoleChange(userId: string, currentRole: string) {
    const newRole = currentRole === 'Admin' ? 'User' : 'Admin'
    setActionLoading(userId + ':role')
    try {
      await setUserRole(userId, newRole)
      setUsers(prev => prev.map(u => u.id === userId ? { ...u, role: newRole } : u))
    } finally {
      setActionLoading(null)
    }
  }

  return (
    <div className="min-h-screen bg-[#060B14] relative overflow-hidden">
      <div className="absolute inset-0 login-grid pointer-events-none" />
      <div className="absolute -top-40 -left-40 w-[500px] h-[500px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(43,123,228,0.12) 0%, transparent 70%)' }} />
      <div className="absolute -bottom-40 -right-40 w-[500px] h-[500px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(20,184,166,0.10) 0%, transparent 70%)' }} />

      <div className="relative z-10 max-w-5xl mx-auto px-4 py-10">

        {/* Header */}
        <div className="flex items-center justify-between mb-8 animate-fade-up">
          <button
            onClick={() => navigate('/')}
            className="flex items-center gap-2 text-gray-500 hover:text-gray-300 text-sm transition-colors group"
          >
            <ArrowLeft className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform" />
            Dashboard'a dön
          </button>
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 rounded-full bg-amber-400 animate-pulse" />
            <span className="text-amber-400 text-xs font-medium">Admin Paneli</span>
          </div>
        </div>

        <div className="flex items-end justify-between mb-8 animate-fade-up">
          <div>
            <h1 className="text-white font-bold text-2xl mb-1">Yönetim Paneli</h1>
            <p className="text-gray-500 text-sm">Kullanıcıları ve sistem durumunu buradan yönet.</p>
          </div>
          <div className="flex flex-col items-end gap-1">
            <button
              onClick={async () => {
                setSyncing(true)
                setSyncMsg(null)
                try {
                  const result = await forceSync()
                  setSyncMsg(
                    result.newRecords > 0
                      ? `${result.newRecords} yeni kayıt eklendi.`
                      : 'Veriler zaten güncel.'
                  )
                } catch {
                  setSyncMsg('Senkronizasyon başarısız.')
                } finally {
                  setSyncing(false)
                  setTimeout(() => setSyncMsg(null), 4000)
                }
              }}
              disabled={syncing}
              className="flex items-center gap-2 px-4 py-2 rounded-xl bg-white/5 border border-white/10 text-sm text-gray-300 hover:bg-white/8 hover:text-white transition-all disabled:opacity-50"
            >
              <RefreshCw className={cn('w-3.5 h-3.5', syncing && 'animate-spin')} />
              {syncing ? 'Güncelleniyor...' : 'Veriyi Senkronize Et'}
            </button>
            {syncMsg && (
              <span className={cn('text-xs', syncMsg.includes('başarısız') ? 'text-red-400' : 'text-teal-400')}>
                {syncMsg}
              </span>
            )}
          </div>
        </div>

        {/* Stats */}
        {loading ? (
          <div className="grid grid-cols-3 gap-4 mb-8">
            {[0, 1, 2].map(i => (
              <div key={i} className="glass-card rounded-2xl p-5 animate-pulse">
                <div className="h-4 w-20 bg-white/10 rounded mb-3" />
                <div className="h-8 w-14 bg-white/10 rounded" />
              </div>
            ))}
          </div>
        ) : stats && (
          <div className="grid grid-cols-3 gap-4 mb-8 animate-fade-up">
            <StatCard
              icon={<Users className="w-4 h-4" />}
              label="Toplam Kullanıcı"
              value={stats.totalUsers}
              color="blue"
            />
            <StatCard
              icon={<Activity className="w-4 h-4" />}
              label="Aktif Kullanıcı"
              value={stats.activeUsers}
              color="teal"
            />
            <StatCard
              icon={<BarChart3 className="w-4 h-4" />}
              label="Toplam Tahmin"
              value={stats.totalPredictions}
              color="purple"
            />
          </div>
        )}

        {/* Users Table */}
        <div className="glass-card rounded-2xl overflow-hidden animate-fade-up-2">
          <div className="px-6 py-4 border-b border-white/8 flex items-center gap-3">
            <UserCog className="w-4 h-4 text-gray-400" />
            <h2 className="text-white font-semibold text-sm">Kullanıcılar</h2>
            <span className="ml-auto text-xs text-gray-500">{users.length} kullanıcı</span>
          </div>

          {loading ? (
            <div className="p-6 space-y-3">
              {[0, 1, 2].map(i => (
                <div key={i} className="h-12 bg-white/5 rounded-xl animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="divide-y divide-white/5">
              {users.map(user => (
                <UserRow
                  key={user.id}
                  user={user}
                  actionLoading={actionLoading}
                  onToggle={handleToggle}
                  onRoleChange={handleRoleChange}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function StatCard({ icon, label, value, color }: {
  icon: React.ReactNode
  label: string
  value: number
  color: 'blue' | 'teal' | 'purple'
}) {
  const colors = {
    blue: 'text-blue-400',
    teal: 'text-teal-400',
    purple: 'text-purple-400',
  }
  return (
    <div className="glass-card rounded-2xl p-5">
      <div className={cn('flex items-center gap-2 mb-3', colors[color])}>
        {icon}
        <span className="text-xs font-medium uppercase tracking-wide text-gray-500">{label}</span>
      </div>
      <p className={cn('text-3xl font-bold', colors[color])}>{value.toLocaleString('tr-TR')}</p>
    </div>
  )
}

function UserRow({ user, actionLoading, onToggle, onRoleChange }: {
  user: UserSummary
  actionLoading: string | null
  onToggle: (id: string) => void
  onRoleChange: (id: string, role: string) => void
}) {
  const isTogglingActive = actionLoading === user.id + ':toggle'
  const isTogglingRole = actionLoading === user.id + ':role'

  return (
    <div className="px-6 py-4 flex items-center gap-4 hover:bg-white/5 transition-colors">
      {/* Avatar */}
      <div className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-xs font-bold shrink-0"
        style={{ background: user.role === 'Admin' ? 'linear-gradient(135deg, #f59e0b, #d97706)' : 'linear-gradient(135deg, #2B7BE4, #14B8A6)' }}>
        {user.email.charAt(0).toUpperCase()}
      </div>

      {/* Email */}
      <div className="flex-1 min-w-0">
        <p className="text-white text-sm font-medium truncate">{user.email}</p>
        <p className="text-gray-600 text-xs">{user.predictionCount} tahmin · {new Date(user.createdAt).toLocaleDateString('tr-TR')}</p>
      </div>

      {/* Role badge */}
      <span className={cn(
        'text-xs font-medium px-2.5 py-1 rounded-lg border shrink-0',
        user.role === 'Admin'
          ? 'bg-amber-500/10 border-amber-500/20 text-amber-400'
          : 'bg-white/5 border-white/10 text-gray-400'
      )}>
        {user.role}
      </span>

      {/* Status badge */}
      <span className={cn(
        'text-xs font-medium px-2.5 py-1 rounded-lg border shrink-0',
        user.isActive
          ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'
          : 'bg-red-500/10 border-red-500/20 text-red-400'
      )}>
        {user.isActive ? 'Aktif' : 'Pasif'}
      </span>

      {/* Actions */}
      <div className="flex items-center gap-2 shrink-0">
        <button
          onClick={() => onRoleChange(user.id, user.role)}
          disabled={isTogglingRole || isTogglingActive}
          title={user.role === 'Admin' ? 'User yap' : 'Admin yap'}
          className="w-7 h-7 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center text-gray-400 hover:text-amber-400 hover:border-amber-400/30 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {isTogglingRole
            ? <span className="w-3 h-3 border border-current border-t-transparent rounded-full animate-spin" />
            : <ShieldCheck className="w-3.5 h-3.5" />
          }
        </button>
        <button
          onClick={() => onToggle(user.id)}
          disabled={isTogglingActive || isTogglingRole}
          title={user.isActive ? 'Pasife al' : 'Aktive et'}
          className="w-7 h-7 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center text-gray-400 hover:text-red-400 hover:border-red-400/30 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {isTogglingActive
            ? <span className="w-3 h-3 border border-current border-t-transparent rounded-full animate-spin" />
            : <ShieldOff className="w-3.5 h-3.5" />
          }
        </button>
      </div>
    </div>
  )
}

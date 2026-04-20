import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, KeyRound, User, CheckCircle } from 'lucide-react'
import { useAuthStore } from '../store/useAuthStore'
import { changePassword } from '../api'
import { cn } from '../lib/utils'

export default function ProfilePage() {
  const navigate = useNavigate()
  const { email, logout } = useAuthStore()

  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess(false)

    if (next !== confirm) {
      setError('Yeni şifreler eşleşmiyor.')
      return
    }
    if (next.length < 6) {
      setError('Yeni şifre en az 6 karakter olmalı.')
      return
    }

    setLoading(true)
    try {
      await changePassword(current, next)
      setSuccess(true)
      setCurrent('')
      setNext('')
      setConfirm('')
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'response' in err
          ? ((err as { response?: { data?: { message?: string } } }).response?.data?.message ?? 'Bir hata oluştu.')
          : 'Bir hata oluştu.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-[#060B14] relative overflow-hidden">

      {/* Arka plan */}
      <div className="absolute inset-0 login-grid pointer-events-none" />
      <div className="absolute -top-40 -left-40 w-[500px] h-[500px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(43,123,228,0.12) 0%, transparent 70%)' }} />
      <div className="absolute -bottom-40 -right-40 w-[500px] h-[500px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(20,184,166,0.10) 0%, transparent 70%)' }} />

      {/* İçerik */}
      <div className="relative z-10 max-w-lg mx-auto px-4 py-12">

        {/* Geri */}
        <button
          onClick={() => navigate('/')}
          className="flex items-center gap-2 text-gray-500 hover:text-gray-300 text-sm mb-8 transition-colors group"
        >
          <ArrowLeft className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform" />
          Dashboard'a dön
        </button>

        {/* Profil başlık */}
        <div className="flex items-center gap-4 mb-8 animate-fade-up">
          <div className="w-14 h-14 rounded-2xl flex items-center justify-center text-white font-bold text-xl shrink-0"
            style={{ background: 'linear-gradient(135deg, #2B7BE4 0%, #14B8A6 100%)' }}>
            {email?.charAt(0).toUpperCase() ?? 'U'}
          </div>
          <div>
            <h1 className="text-white font-bold text-xl leading-none mb-1">Profil</h1>
            <p className="text-gray-500 text-sm">{email}</p>
          </div>
        </div>

        {/* Email kartı */}
        <div className="glass-card rounded-2xl p-5 mb-4 animate-fade-up">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-white/5 border border-white/8 flex items-center justify-center shrink-0">
              <User className="w-4 h-4 text-gray-400" />
            </div>
            <div>
              <p className="text-gray-500 text-xs uppercase tracking-wide font-medium mb-0.5">E-posta</p>
              <p className="text-white text-sm font-medium">{email}</p>
            </div>
          </div>
        </div>

        {/* Şifre değiştir kartı */}
        <div className="glass-card rounded-2xl p-6 animate-fade-up-2">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-8 h-8 rounded-lg bg-white/5 border border-white/8 flex items-center justify-center shrink-0">
              <KeyRound className="w-4 h-4 text-gray-400" />
            </div>
            <div>
              <h2 className="text-white font-semibold text-sm leading-none mb-0.5">Şifre Değiştir</h2>
              <p className="text-gray-500 text-xs">Güvenliğin için güçlü bir şifre seç.</p>
            </div>
          </div>

          {success && (
            <div className="flex items-center gap-2 bg-emerald-500/10 border border-emerald-500/20 rounded-xl px-4 py-3 mb-5">
              <CheckCircle className="w-4 h-4 text-emerald-400 shrink-0" />
              <p className="text-emerald-400 text-sm">Şifren başarıyla değiştirildi.</p>
            </div>
          )}

          <form onSubmit={(e) => { void handleSubmit(e) }} className="space-y-4">
            <div>
              <label className="text-gray-400 text-xs font-medium uppercase tracking-wide block mb-2">
                Mevcut Şifre
              </label>
              <input
                type="password"
                value={current}
                onChange={(e) => { setCurrent(e.target.value); setError(''); setSuccess(false) }}
                required
                placeholder="••••••••"
                className={cn(
                  'w-full bg-white/5 border rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 transition-all',
                  error && error.includes('Mevcut')
                    ? 'border-red-400/50 focus:border-red-400/60 focus:ring-red-400/20'
                    : 'border-white/8 focus:border-primary/60 focus:ring-primary/30'
                )}
              />
              {error && error.includes('Mevcut') && (
                <p className="text-red-400 text-xs mt-1.5 pl-1">{error}</p>
              )}
            </div>

            <div>
              <label className="text-gray-400 text-xs font-medium uppercase tracking-wide block mb-2">
                Yeni Şifre
              </label>
              <input
                type="password"
                value={next}
                onChange={(e) => { setNext(e.target.value); setError(''); setSuccess(false) }}
                required
                placeholder="••••••••"
                className={cn(
                  'w-full bg-white/5 border rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 transition-all',
                  error && (error.includes('karakter') || error.includes('eşleşmiyor'))
                    ? 'border-red-400/50 focus:border-red-400/60 focus:ring-red-400/20'
                    : 'border-white/8 focus:border-primary/60 focus:ring-primary/30'
                )}
              />
            </div>

            <div>
              <label className="text-gray-400 text-xs font-medium uppercase tracking-wide block mb-2">
                Yeni Şifre Tekrar
              </label>
              <input
                type="password"
                value={confirm}
                onChange={(e) => { setConfirm(e.target.value); setError(''); setSuccess(false) }}
                required
                placeholder="••••••••"
                className={cn(
                  'w-full bg-white/5 border rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 transition-all',
                  error && error.includes('eşleşmiyor')
                    ? 'border-red-400/50 focus:border-red-400/60 focus:ring-red-400/20'
                    : 'border-white/8 focus:border-primary/60 focus:ring-primary/30'
                )}
              />
              {error && !error.includes('Mevcut') && (
                <p className="text-red-400 text-xs mt-1.5 pl-1">{error}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full relative flex items-center justify-center gap-2 rounded-xl py-3 text-sm font-semibold text-white transition-all disabled:opacity-50 disabled:cursor-not-allowed overflow-hidden group mt-2"
              style={{ background: 'linear-gradient(135deg, #2B7BE4 0%, #1a68d4 100%)' }}
            >
              <span className="absolute inset-0 bg-white/0 group-hover:bg-white/8 transition-colors" />
              <span className="relative">{loading ? 'Kaydediliyor...' : 'Şifreyi Güncelle'}</span>
            </button>
          </form>
        </div>

        {/* Çıkış */}
        <div className="mt-4 animate-fade-up-3">
          <button
            onClick={() => { logout(); navigate('/login') }}
            className="w-full glass-card rounded-2xl py-3 text-sm font-medium text-red-400 hover:text-red-300 hover:bg-red-400/5 transition-all"
          >
            Çıkış Yap
          </button>
        </div>

      </div>
    </div>
  )
}

import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/useAuthStore'
import { login, register } from '../api'
import { TrendingUp, Shield, Zap, ArrowRight } from 'lucide-react'

export default function LoginPage() {
  const navigate = useNavigate()
  const loginStore = useAuthStore((s) => s.login)

  const [isRegister, setIsRegister] = useState(false)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = isRegister ? await register(email, password) : await login(email, password)
      loginStore(res)
      navigate('/')
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
    <div className="min-h-screen bg-[#060B14] flex items-center justify-center overflow-hidden relative">

      {/* Arka plan */}
      <div className="absolute inset-0 login-grid pointer-events-none" />
      <div className="animate-float-slow absolute -top-40 -left-40 w-[600px] h-[600px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(43,123,228,0.18) 0%, transparent 70%)' }} />
      <div className="animate-float-medium absolute -bottom-60 -right-40 w-[700px] h-[700px] rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(20,184,166,0.14) 0%, transparent 70%)' }} />
      <div className="animate-float-fast absolute top-1/2 left-1/2 w-[400px] h-[400px] -translate-x-1/2 -translate-y-1/2 rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(139,92,246,0.07) 0%, transparent 70%)' }} />

      {/* İçerik */}
      <div className="relative z-10 flex flex-col lg:flex-row items-center gap-16 px-6 w-full max-w-4xl mx-auto">

        {/* Sol — Marka */}
        <div className="flex-1 flex flex-col items-center lg:items-start text-center lg:text-left animate-fade-up">

          {/* Logo */}
          <div className="flex items-center gap-4 mb-10">
            <div className="logo-glow w-14 h-14 rounded-2xl flex items-center justify-center text-white font-bold text-xl shrink-0"
              style={{ background: 'linear-gradient(135deg, #2B7BE4 0%, #14B8A6 100%)' }}>
              PX
            </div>
            <div>
              <div className="text-white font-bold text-2xl leading-none">Predixus</div>
              <div className="text-gray-500 text-sm mt-1">BIST 100 Tahmin Platformu</div>
            </div>
          </div>

          {/* Başlık */}
          <h1 className="text-gradient text-4xl xl:text-5xl font-bold leading-tight mb-4">
            Borsa tahminini<br />
            <span className="text-gradient-blue">akıllıca</span> yap.
          </h1>
          <p className="text-gray-500 text-base leading-relaxed max-w-sm mb-8">
            Yapay zeka destekli analizlerle BIST 100 endeksinin
            yarınki hareketini önceden gör.
          </p>

          {/* Özellikler */}
          <div className="animate-fade-up-2 space-y-3">
            {[
              { icon: TrendingUp, label: 'Günlük tahmin',      sub: 'Her gün otomatik güncellenir' },
              { icon: Zap,        label: 'Anlık sonuç',        sub: 'Önbellek ile hızlı yanıt' },
              { icon: Shield,     label: 'Güvenli hesap',      sub: 'Kişisel veriler şifreli' },
            ].map(({ icon: Icon, label, sub }) => (
              <div key={label} className="flex items-start gap-3 group">
                <div className="w-8 h-8 mt-0.5 rounded-lg bg-white/5 border border-white/8 flex items-center justify-center shrink-0 group-hover:bg-primary/10 group-hover:border-primary/20 transition-colors">
                  <Icon className="w-4 h-4 text-gray-500 group-hover:text-primary transition-colors" />
                </div>
                <div className="text-left">
                  <p className="text-gray-300 text-sm font-medium leading-snug">{label}</p>
                  <p className="text-gray-600 text-xs leading-snug">{sub}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Sağ — Form */}
        <div className="w-full max-w-sm animate-fade-up-2 shrink-0">
          <div className="glass-card rounded-2xl p-8">

            <div className="mb-7">
              <h2 className="text-white text-xl font-bold mb-1">
                {isRegister ? 'Hesap Oluştur' : 'Hoşgeldin'}
              </h2>
              <p className="text-gray-500 text-sm">
                {isRegister
                  ? 'Birkaç saniyede hesabını oluştur.'
                  : 'Hesabına giriş yap, tahminlere devam et.'}
              </p>
            </div>

            <form onSubmit={(e) => { void handleSubmit(e) }} className="space-y-4">
              <div>
                <label className="text-gray-400 text-xs font-medium uppercase tracking-wide block mb-2">
                  E-posta
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  placeholder="siz@ornek.com"
                  className={`w-full bg-white/5 border rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 transition-all ${
                    error ? 'border-red-400/50 focus:border-red-400/60 focus:ring-red-400/20' : 'border-white/8 focus:border-primary/60 focus:ring-primary/30'
                  }`}
                />
                {error && (
                  <p className="text-red-400 text-xs mt-1.5 pl-1">{error}</p>
                )}
              </div>

              <div>
                <label className="text-gray-400 text-xs font-medium uppercase tracking-wide block mb-2">
                  Şifre
                </label>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  placeholder="••••••••"
                  className="w-full bg-white/5 border border-white/8 rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:border-primary/60 focus:ring-1 focus:ring-primary/30 transition-all"
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full relative flex items-center justify-center gap-2 rounded-xl py-3 text-sm font-semibold text-white transition-all disabled:opacity-50 disabled:cursor-not-allowed overflow-hidden group"
                style={{ background: 'linear-gradient(135deg, #2B7BE4 0%, #1a68d4 100%)' }}
              >
                <span className="absolute inset-0 bg-white/0 group-hover:bg-white/8 transition-colors" />
                <span className="relative">
                  {loading ? 'Yükleniyor...' : isRegister ? 'Kayıt Ol' : 'Giriş Yap'}
                </span>
                {!loading && <ArrowRight className="relative w-4 h-4 group-hover:translate-x-0.5 transition-transform" />}
              </button>
            </form>

            <div className="mt-6 pt-6 border-t border-white/6 text-center">
              <button
                onClick={() => { setIsRegister(!isRegister); setError('') }}
                className="text-gray-500 hover:text-gray-300 text-sm transition-colors"
              >
                {isRegister ? (
                  <>Zaten hesabın var mı? <span className="text-primary font-medium">Giriş yap</span></>
                ) : (
                  <>Hesabın yok mu? <span className="text-primary font-medium">Kayıt ol</span></>
                )}
              </button>
            </div>
          </div>

          <p className="text-gray-700 text-xs text-center mt-5">
            Yatırım tavsiyesi değildir.
          </p>
        </div>

      </div>
    </div>
  )
}

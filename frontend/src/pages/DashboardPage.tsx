import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import {
  ComposedChart,
  Area,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,

  AreaChart,
  BarChart,
  Bar,
  Cell,
  ReferenceLine,
} from 'recharts'
import {
  TrendingUp,
  TrendingDown,
  RefreshCw,
  LogOut,
  Sun,
  Moon,
  Database,
  Clock,
  BarChart2,
  Activity,
  Brain,
  LineChart as LineIcon,
  Target,
  ChevronRight,
  Cpu,
  ShieldCheck,
  BellPlus,
  Bell,
  Trash2,
  Download,
} from 'lucide-react'
import { useAuthStore } from '../store/useAuthStore'
import { predict, getHistory, getBist100Prices, getAlerts, createAlert, deleteAlert } from '../api'
import type { PredictionResponse, StockPricePoint, AlertItem } from '../types'
import { cn, formatPrice, formatDate, formatShortDate, formatVolume } from '../lib/utils'

// ── Sabitler ──────────────────────────────────────────────────────────────────

type Tab = 'overview' | 'analytics' | 'models'
type ModelId = 'lstm' | 'gru' | 'xgboost'

const MODELS: {
  id: ModelId
  label: string
  desc: string
  mape: string | null
  rmse: string | null
  mae: string | null
  active: boolean
}[] = [
  {
    id: 'lstm',
    label: 'LSTM + Attention',
    desc: 'Uzun-kısa vadeli hafıza ağı ile dikkat mekanizması. Zaman serisi tahmini için optimize edilmiştir.',
    mape: '0.19',
    rmse: '26.90',
    mae: '26.90',
    active: true,
  },
  {
    id: 'gru',
    label: 'GRU',
    desc: 'Gated Recurrent Unit — LSTM\'e göre daha hızlı, daha az parametre ile benzer performans.',
    mape: null,
    rmse: null,
    mae: null,
    active: false,
  },
  {
    id: 'xgboost',
    label: 'XGBoost',
    desc: 'Gradient Boosted Trees tabanlı model. Teknik indikatörlerle birlikte kullanılır.',
    mape: null,
    rmse: null,
    mae: null,
    active: false,
  },
]

// ── Hooks ─────────────────────────────────────────────────────────────────────

function useMarketStatus() {
  const [isOpen, setIsOpen] = useState(false)
  const [countdown, setCountdown] = useState('')

  useEffect(() => {
    function update() {
      const istanbul = new Date(
        new Date().toLocaleString('en-US', { timeZone: 'Europe/Istanbul' })
      )
      const day = istanbul.getDay()
      const total = istanbul.getHours() * 60 + istanbul.getMinutes()
      const isWeekday = day >= 1 && day <= 5
      const open = isWeekday && total >= 600 && total < 1080
      setIsOpen(open)
      if (!open) {
        const next = new Date(istanbul)
        next.setSeconds(0, 0)
        if (total >= 1080 || !isWeekday) {
          next.setDate(next.getDate() + 1)
          next.setHours(10, 0, 0, 0)
          while (next.getDay() === 0 || next.getDay() === 6) {
            next.setDate(next.getDate() + 1)
          }
        } else {
          next.setHours(10, 0, 0, 0)
        }
        const diff = next.getTime() - istanbul.getTime()
        const dh = Math.floor(diff / 3_600_000)
        const dm = Math.floor((diff % 3_600_000) / 60_000)
        const ds = Math.floor((diff % 60_000) / 1_000)
        setCountdown(
          `${String(dh).padStart(2, '0')}:${String(dm).padStart(2, '0')}:${String(ds).padStart(2, '0')}`
        )
      }
    }
    update()
    const t = setInterval(update, 1000)
    return () => clearInterval(t)
  }, [])

  return { isOpen, countdown }
}

// ── Skeleton ──────────────────────────────────────────────────────────────────

function Skeleton({ className, dark }: { className?: string; dark: boolean }) {
  return (
    <div
      className={cn(
        'animate-pulse rounded-xl',
        dark ? 'bg-gray-800' : 'bg-gray-200',
        className
      )}
    />
  )
}

function SkeletonCard({ dark }: { dark: boolean }) {
  return (
    <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-start justify-between">
        <div className="space-y-2 flex-1 mr-3">
          <Skeleton dark={dark} className="h-3 w-20" />
          <Skeleton dark={dark} className="h-6 w-32" />
          <Skeleton dark={dark} className="h-4 w-16" />
        </div>
        <Skeleton dark={dark} className="w-9 h-9 rounded-xl shrink-0" />
      </div>
    </div>
  )
}

// ── Topbar ────────────────────────────────────────────────────────────────────

function Topbar({
  email, role, dark, onThemeToggle, onLogout, latestPrice,
}: {
  email: string; role: string | null; dark: boolean; onThemeToggle: () => void; onLogout: () => void
  latestPrice: StockPricePoint | null
}) {
  const { isOpen, countdown } = useMarketStatus()
  const navigate = useNavigate()
  const changeUp = (latestPrice?.dailyChangePercent ?? 0) >= 0

  return (
    <header className={cn(
      'h-16 flex items-center justify-between px-6 border-b shrink-0 z-10',
      dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200'
    )}>

      {/* Logo */}
      <div className="flex items-center gap-3">
        <div
          className="w-9 h-9 rounded-xl flex items-center justify-center text-white font-bold text-sm shrink-0"
          style={{
            background: 'linear-gradient(135deg, #2B7BE4 0%, #14B8A6 100%)',
            boxShadow: '0 0 16px rgba(43,123,228,0.45), 0 0 32px rgba(43,123,228,0.15)',
          }}
        >
          PX
        </div>
        <div>
          <div className={cn('font-bold text-sm leading-none', dark ? 'text-white' : 'text-gray-900')}>Predixus</div>
          <div className="text-gray-500 text-xs mt-0.5">BIST 100 Tahmin</div>
        </div>
      </div>

      {/* BIST100 canlı widget */}
      <div className={cn(
        'hidden md:flex items-center gap-4 px-5 py-2.5 rounded-xl border',
        dark ? 'bg-dark-bg border-dark-border' : 'bg-gray-50 border-gray-200'
      )}>
        {/* Durum */}
        <div className="flex items-center gap-2">
          {isOpen ? (
            <span className="relative flex h-2 w-2">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
              <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
            </span>
          ) : (
            <span className="h-2 w-2 rounded-full bg-gray-600" />
          )}
          <span className={cn('text-xs font-medium', isOpen ? 'text-emerald-400' : 'text-gray-500')}>
            {isOpen ? 'Açık' : 'Kapalı'}
          </span>
        </div>

        {/* Dikey ayraç */}
        <div className={cn('w-px h-6', dark ? 'bg-dark-border' : 'bg-gray-200')} />

        {/* Fiyat */}
        {latestPrice ? (
          <div className="flex items-center gap-3">
            <div>
              <div className="text-gray-400 text-xs leading-none mb-0.5">BIST 100</div>
              <div className={cn('font-bold text-sm tabular-nums leading-none', dark ? 'text-white' : 'text-gray-900')}>
                {formatPrice(latestPrice.close)} ₺
              </div>
            </div>
            <div className={cn(
              'flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded-full',
              changeUp ? 'bg-emerald-400/10 text-emerald-400' : 'bg-red-400/10 text-red-400'
            )}>
              {changeUp ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
              {changeUp ? '+' : ''}{latestPrice.dailyChangePercent.toFixed(2)}%
            </div>
          </div>
        ) : (
          <div>
            <div className="text-gray-400 text-xs leading-none mb-0.5">Açılışa kalan</div>
            <div className={cn('font-mono font-semibold text-sm tabular-nums', dark ? 'text-white' : 'text-gray-900')}>
              {countdown || '—'}
            </div>
          </div>
        )}

        {/* Kapalıysa countdown da göster */}
        {!isOpen && latestPrice && (
          <>
            <div className={cn('w-px h-6', dark ? 'bg-dark-border' : 'bg-gray-200')} />
            <div>
              <div className="text-gray-400 text-xs leading-none mb-0.5">Açılışa kalan</div>
              <div className={cn('font-mono font-semibold text-sm tabular-nums', dark ? 'text-white' : 'text-gray-900')}>
                {countdown || '—'}
              </div>
            </div>
          </>
        )}
      </div>

      {/* Sağ aksiyonlar */}
      <div className="flex items-center gap-2">
        {role === 'Admin' && (
          <button
            onClick={() => navigate('/admin')}
            title="Yönetim Paneli"
            className={cn('w-8 h-8 rounded-lg flex items-center justify-center transition-colors', dark ? 'text-amber-400 hover:text-amber-300 hover:bg-dark-bg' : 'text-amber-500 hover:text-amber-600 hover:bg-gray-100')}
          >
            <ShieldCheck className="w-4 h-4" />
          </button>
        )}
        <button onClick={onThemeToggle} className={cn('w-8 h-8 rounded-lg flex items-center justify-center transition-colors', dark ? 'text-gray-400 hover:text-gray-200 hover:bg-dark-bg' : 'text-gray-500 hover:text-gray-700 hover:bg-gray-100')}>
          {dark ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </button>
        <button
          onClick={() => navigate('/profile')}
          title={email}
          className="w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold transition-opacity hover:opacity-80"
          style={{ background: 'linear-gradient(135deg, #2B7BE4 0%, #14B8A6 100%)' }}
        >
          {email.charAt(0).toUpperCase()}
        </button>
        <button onClick={onLogout} className={cn('w-8 h-8 rounded-lg flex items-center justify-center transition-colors', dark ? 'text-gray-400 hover:text-red-400 hover:bg-dark-bg' : 'text-gray-500 hover:text-red-500 hover:bg-gray-100')}>
          <LogOut className="w-4 h-4" />
        </button>
      </div>
    </header>
  )
}

// ── Sidebar ───────────────────────────────────────────────────────────────────

function Sidebar({ tab, onTab, dark }: { tab: Tab; onTab: (t: Tab) => void; dark: boolean }) {
  const items: { id: Tab; label: string; icon: React.ComponentType<{ className?: string }> }[] = [
    { id: 'overview', label: 'Genel Bakış', icon: BarChart2 },
    { id: 'analytics', label: 'Analiz', icon: LineIcon },
    { id: 'models', label: 'Modeller', icon: Cpu },
  ]
  return (
    <aside className={cn('hidden md:flex w-52 shrink-0 flex-col border-r pt-6 pb-4', dark ? 'bg-dark-sidebar border-dark-border' : 'bg-white border-gray-200')}>
      <nav className="px-3 space-y-1">
        {items.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => onTab(id)}
            className={cn(
              'w-full flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors text-left',
              tab === id
                ? 'bg-primary/10 text-primary'
                : dark
                ? 'text-gray-400 hover:text-gray-200 hover:bg-dark-bg/60'
                : 'text-gray-500 hover:text-gray-800 hover:bg-gray-50'
            )}
          >
            <Icon className="w-4 h-4 shrink-0" />
            {label}
          </button>
        ))}
      </nav>

      <div className="mt-auto px-4">
        <div className={cn('rounded-xl border p-3 text-xs', dark ? 'border-dark-border' : 'border-gray-200')}>
          <p className="font-medium text-gray-400 mb-0.5">Aktif Model</p>
          <p className={dark ? 'text-gray-300' : 'text-gray-600'}>LSTM + Attention</p>
          <p className="font-medium text-gray-400 mt-1.5 mb-0.5">Son Test Hatası</p>
          <p className="text-emerald-400 font-semibold">%0.19</p>
        </div>
      </div>
    </aside>
  )
}

// ── KPI Card ──────────────────────────────────────────────────────────────────

function KpiCard({
  label, value, badge, badgeColor, trend, icon: Icon, dark,
}: {
  label: string; value: string; badge?: string
  badgeColor?: 'green' | 'amber' | 'blue' | 'gray' | 'red'
  trend?: { value: string; up: boolean } | null
  icon: React.ComponentType<{ className?: string }>; dark: boolean
}) {
  const bc = {
    green: 'bg-emerald-400/15 text-emerald-400',
    amber: 'bg-amber-400/15 text-amber-400',
    blue: 'bg-primary/15 text-primary',
    red: 'bg-red-400/15 text-red-400',
    gray: dark ? 'bg-gray-700 text-gray-400' : 'bg-gray-100 text-gray-500',
  }
  return (
    <div className={cn('rounded-2xl border p-5 flex items-start justify-between', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="space-y-1 min-w-0 flex-1 mr-3">
        <p className="text-gray-400 text-xs font-medium">{label}</p>
        <p className={cn('text-xl font-semibold truncate', dark ? 'text-white' : 'text-gray-900')}>{value}</p>
        {trend ? (
          <div className={cn('flex items-center gap-1 text-xs font-medium', trend.up ? 'text-emerald-400' : 'text-red-400')}>
            {trend.up ? <TrendingUp className="w-3.5 h-3.5" /> : <TrendingDown className="w-3.5 h-3.5" />}
            {trend.value} önceki tahminden
          </div>
        ) : badge ? (
          <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium inline-block', bc[badgeColor ?? 'gray'])}>{badge}</span>
        ) : null}
      </div>
      <div className={cn('w-9 h-9 rounded-xl flex items-center justify-center shrink-0', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
        <Icon className="w-4 h-4 text-primary" />
      </div>
    </div>
  )
}

// ── Model Selector ────────────────────────────────────────────────────────────

function ModelSelector({
  selected, onSelect, dark,
}: {
  selected: ModelId; onSelect: (id: ModelId) => void; dark: boolean
}) {
  return (
    <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-center justify-between mb-4">
        <div>
          <p className="text-gray-400 text-xs font-medium uppercase tracking-wide">Model Seçimi</p>
          <h3 className={cn('font-semibold mt-0.5', dark ? 'text-white' : 'text-gray-900')}>Tahmin Modeli</h3>
        </div>
        <span className="text-xs text-gray-400">1 model aktif</span>
      </div>
      <div className="grid grid-cols-3 gap-3">
        {MODELS.map((m) => (
          <button
            key={m.id}
            onClick={() => {
              if (!m.active) {
                toast(`${m.label} henüz hazır değil`, { icon: '🔜', duration: 2500 })
                return
              }
              onSelect(m.id)
            }}
            className={cn(
              'relative rounded-xl border p-3 text-left transition-all',
              selected === m.id && m.active
                ? 'border-primary bg-primary/5'
                : dark
                ? 'border-dark-border bg-dark-bg hover:border-gray-600'
                : 'border-gray-200 bg-gray-50 hover:border-gray-300',
              !m.active && 'opacity-60 cursor-not-allowed'
            )}
          >
            {m.active && (
              <span className="absolute top-2 right-2 w-2 h-2 rounded-full bg-emerald-400" />
            )}
            <div className={cn('font-semibold text-sm mb-1', dark ? 'text-white' : 'text-gray-900')}>{m.label}</div>
            {m.active ? (
              <>
                <div className="text-xs text-gray-400">MAPE</div>
                <div className="text-emerald-400 font-mono text-sm font-semibold">%{m.mape}</div>
              </>
            ) : (
              <div className="text-xs text-gray-500 mt-1">Yakında</div>
            )}
          </button>
        ))}
      </div>
    </div>
  )
}

// ── Grafik Tooltip ────────────────────────────────────────────────────────────

function ChartTooltip({ active, payload, label, dark, fmt }: {
  active?: boolean
  payload?: Array<{ name: string; value: number; color: string }>
  label?: string
  dark: boolean
  fmt?: (v: number, name: string) => string
}) {
  if (!active || !payload?.length) return null
  return (
    <div style={{
      background: dark ? 'rgba(6,11,20,0.94)' : 'rgba(255,255,255,0.97)',
      border: `1px solid ${dark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)'}`,
      borderRadius: 12,
      padding: '10px 14px',
      boxShadow: '0 12px 40px rgba(0,0,0,0.35)',
      minWidth: 140,
    }}>
      <p style={{ color: '#6B7280', marginBottom: 8, fontSize: 11, fontWeight: 500 }}>{label}</p>
      {payload.map((p, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: i < payload.length - 1 ? 5 : 0 }}>
          <span style={{ width: 8, height: 8, borderRadius: '50%', background: p.color, display: 'inline-block', flexShrink: 0, boxShadow: `0 0 8px ${p.color}88` }} />
          <span style={{ color: dark ? '#9CA3AF' : '#6B7280', fontSize: 12 }}>{p.name}</span>
          <span style={{ color: dark ? '#F9FAFB' : '#111827', fontWeight: 700, fontSize: 13, marginLeft: 'auto' }}>
            {fmt ? fmt(p.value, p.name) : p.value}
          </span>
        </div>
      ))}
    </div>
  )
}

// ── BIST100 + Tahmin Grafiği ──────────────────────────────────────────────────

type PriceRange = 7 | 30 | 60 | 90

function PriceAndPredictionChart({
  prices, prediction, dark, priceDays, onPriceDaysChange,
}: {
  prices: StockPricePoint[]; prediction: PredictionResponse | null; dark: boolean
  priceDays: PriceRange; onPriceDaysChange: (d: PriceRange) => void
}) {
  const chartData = useMemo(() => {
    const sorted = [...prices]
      .sort((a, b) => new Date(a.date + 'T12:00:00Z').getTime() - new Date(b.date + 'T12:00:00Z').getTime())
      .slice(-priceDays)

    const data: { date: string; gerçek: number | null; tahmin: number | null }[] = sorted.map((p) => ({
      date: formatShortDate(p.date),
      gerçek: p.close,
      tahmin: null,
    }))

    if (prediction && data.length > 0) {
      data[data.length - 1] = { ...data[data.length - 1], tahmin: data[data.length - 1].gerçek }
      data.push({ date: 'Yarın', gerçek: null, tahmin: prediction.predictedPrice })
    }
    return data
  }, [prices, prediction])

  const avg = useMemo(() => {
    const vals = chartData.filter(d => d.gerçek !== null).map(d => d.gerçek as number)
    return vals.length ? vals.reduce((a, b) => a + b, 0) / vals.length : null
  }, [chartData])

  const textColor = dark ? '#4B5563' : '#9CA3AF'
  const gridColor = dark ? 'rgba(255,255,255,0.035)' : '#F3F4F6'

  if (prices.length === 0) {
    return (
      <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
        <div className="h-60 flex flex-col items-center justify-center gap-2 text-gray-500 text-sm">
          <BarChart2 className="w-8 h-8 text-gray-600" />
          <p>Fiyat verisi yükleniyor...</p>
        </div>
      </div>
    )
  }

  return (
    <div className={cn('rounded-2xl border overflow-hidden', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-center justify-between px-6 pt-5 pb-3 flex-wrap gap-3">
        <div>
          <p className="text-gray-500 text-xs font-medium uppercase tracking-widest mb-1">Fiyat Geçmişi</p>
          <h3 className={cn('font-bold', dark ? 'text-white' : 'text-gray-900')}>BIST 100 · Son {priceDays} Gün</h3>
        </div>
        <div className="flex items-center gap-3 flex-wrap">
          {/* Tarih aralığı seçici */}
          <div className={cn('flex rounded-lg overflow-hidden border text-xs font-medium', dark ? 'border-dark-border' : 'border-gray-200')}>
            {([7, 30, 60, 90] as PriceRange[]).map(d => (
              <button
                key={d}
                onClick={() => onPriceDaysChange(d)}
                className={cn(
                  'px-2.5 py-1 transition-colors',
                  priceDays === d
                    ? 'bg-primary text-white'
                    : dark ? 'text-gray-400 hover:text-gray-200 hover:bg-dark-bg' : 'text-gray-500 hover:bg-gray-50'
                )}
              >
                {d}G
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2">
            <span className="w-4 h-0.5 rounded-full inline-block bg-blue-400" />
            <span className="text-gray-500 text-xs">Kapanış</span>
          </div>
          {prediction && (
            <div className="flex items-center gap-2">
              <span className="w-4 border-t-2 border-dashed border-teal-400 inline-block" />
              <span className="text-gray-500 text-xs">Tahmin</span>
            </div>
          )}
        </div>
      </div>
      <ResponsiveContainer width="100%" height={300}>
        <ComposedChart data={chartData} margin={{ top: 8, right: 24, left: 0, bottom: 4 }}>
          <defs>
            <linearGradient id="priceGradQ" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3B8EF3" stopOpacity={0.28} />
              <stop offset="55%" stopColor="#3B8EF3" stopOpacity={0.07} />
              <stop offset="100%" stopColor="#3B8EF3" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="1 6" stroke={gridColor} vertical={false} />
          <XAxis dataKey="date" tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
          <YAxis tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} width={72} tickFormatter={(v: number) => v.toLocaleString('tr-TR')} domain={['auto', 'auto']} />
          <Tooltip content={(p) => <ChartTooltip {...p} dark={dark} fmt={(v) => `${formatPrice(v)} ₺`} />} />
          {avg && (
            <ReferenceLine y={avg} stroke={dark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.08)'} strokeDasharray="4 4"
              label={{ value: `ort. ${formatPrice(avg)}`, position: 'insideTopRight', fill: textColor, fontSize: 10, dy: -6 }} />
          )}
          <Area type="monotone" dataKey="gerçek" name="Kapanış" stroke="#3B8EF3" strokeWidth={2} fill="url(#priceGradQ)"
            dot={false} activeDot={{ r: 5, fill: '#3B8EF3', stroke: '#fff', strokeWidth: 2 }} connectNulls={false} />
          <Line type="monotone" dataKey="tahmin" name="Tahmin" stroke="#14B8A6" strokeWidth={2.5} strokeDasharray="6 4"
            dot={(props: { cx: number; cy: number; payload: { date: string } }) => {
              const { cx, cy, payload } = props
              if (payload.date !== 'Yarın') return <g key={cx} />
              return (
                <g key={cx}>
                  <circle cx={cx} cy={cy} r={14} fill="#14B8A6" opacity={0.1} />
                  <circle cx={cx} cy={cy} r={8} fill="#14B8A6" opacity={0.2} />
                  <circle cx={cx} cy={cy} r={4} fill="#14B8A6" stroke="#fff" strokeWidth={2} />
                </g>
              )
            }}
            connectNulls />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}

// ── Hacim Grafiği ─────────────────────────────────────────────────────────────

function VolumeChart({ prices, dark }: { prices: StockPricePoint[]; dark: boolean }) {
  const data = useMemo(() =>
    [...prices]
      .sort((a, b) => new Date(a.date + 'T12:00:00Z').getTime() - new Date(b.date + 'T12:00:00Z').getTime())
      .slice(-20)
      .map(p => ({ date: formatShortDate(p.date), hacim: p.volume, up: p.close >= p.open })),
    [prices]
  )

  const textColor = dark ? '#4B5563' : '#9CA3AF'
  const gridColor = dark ? 'rgba(255,255,255,0.035)' : '#F3F4F6'

  return (
    <div className={cn('rounded-2xl border overflow-hidden', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-center justify-between px-6 pt-5 pb-3">
        <div>
          <p className="text-gray-500 text-xs font-medium uppercase tracking-widest mb-1">İşlem Hacmi</p>
          <h3 className={cn('font-bold', dark ? 'text-white' : 'text-gray-900')}>Son 20 Gün</h3>
        </div>
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-sm inline-block" style={{ background: 'linear-gradient(180deg,#3B8EF3,#2B7BE4)' }} />
            <span className="text-gray-500 text-xs">Yükseliş</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-sm inline-block" style={{ background: 'linear-gradient(180deg,#EF4444,#DC2626)' }} />
            <span className="text-gray-500 text-xs">Düşüş</span>
          </div>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={190}>
        <BarChart data={data} margin={{ top: 4, right: 24, left: 0, bottom: 4 }} barCategoryGap="38%">
          <defs>
            <linearGradient id="volUp" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3B8EF3" stopOpacity={0.9} />
              <stop offset="100%" stopColor="#2B7BE4" stopOpacity={0.45} />
            </linearGradient>
            <linearGradient id="volDown" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#EF4444" stopOpacity={0.9} />
              <stop offset="100%" stopColor="#DC2626" stopOpacity={0.45} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="1 6" stroke={gridColor} vertical={false} />
          <XAxis dataKey="date" tick={{ fill: textColor, fontSize: 10 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
          <YAxis tick={{ fill: textColor, fontSize: 10 }} axisLine={false} tickLine={false} width={52} tickFormatter={(v: number) => formatVolume(v)} />
          <Tooltip content={(p) => <ChartTooltip {...p} dark={dark} fmt={(v) => formatVolume(v)} />} />
          <Bar dataKey="hacim" name="Hacim" radius={[4, 4, 0, 0]}>
            {data.map((entry, i) => (
              <Cell key={i} fill={entry.up ? 'url(#volUp)' : 'url(#volDown)'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

// ── Tahmin Trendi ─────────────────────────────────────────────────────────────

function PredictionTrendChart({ predictions, dark }: { predictions: PredictionResponse[]; dark: boolean }) {
  const data = useMemo(() =>
    [...predictions]
      .sort((a, b) => new Date(a.predictedAt).getTime() - new Date(b.predictedAt).getTime())
      .map(p => ({ date: formatShortDate(p.predictedAt), tahmin: Math.round(p.predictedPrice) })),
    [predictions]
  )

  const avg = data.length ? Math.round(data.reduce((s, d) => s + d.tahmin, 0) / data.length) : null
  const textColor = dark ? '#4B5563' : '#9CA3AF'
  const gridColor = dark ? 'rgba(255,255,255,0.035)' : '#F3F4F6'

  if (data.length < 2) {
    return (
      <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
        <div className="h-44 flex flex-col items-center justify-center gap-2 text-gray-500 text-sm">
          <LineIcon className="w-7 h-7 text-gray-600" />
          <p>Trend için en az 2 tahmin gerekli.</p>
        </div>
      </div>
    )
  }

  return (
    <div className={cn('rounded-2xl border overflow-hidden', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="px-6 pt-5 pb-3">
        <p className="text-gray-500 text-xs font-medium uppercase tracking-widest mb-1">Tahmin Trendi</p>
        <h3 className={cn('font-bold', dark ? 'text-white' : 'text-gray-900')}>Tahmin Edilen Açılışlar</h3>
      </div>
      <ResponsiveContainer width="100%" height={210}>
        <AreaChart data={data} margin={{ top: 8, right: 24, left: 0, bottom: 4 }}>
          <defs>
            <linearGradient id="trendGradQ" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#14B8A6" stopOpacity={0.28} />
              <stop offset="55%" stopColor="#14B8A6" stopOpacity={0.06} />
              <stop offset="100%" stopColor="#14B8A6" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="1 6" stroke={gridColor} vertical={false} />
          <XAxis dataKey="date" tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
          <YAxis tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} width={72} tickFormatter={(v: number) => v.toLocaleString('tr-TR')} domain={['auto', 'auto']} />
          <Tooltip content={(p) => <ChartTooltip {...p} dark={dark} fmt={(v) => `${formatPrice(v)} ₺`} />} />
          {avg && (
            <ReferenceLine y={avg} stroke={dark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.08)'} strokeDasharray="4 4"
              label={{ value: `ort. ${formatPrice(avg)}`, position: 'insideTopRight', fill: textColor, fontSize: 10, dy: -6 }} />
          )}
          <Area type="monotone" dataKey="tahmin" name="Tahmin" stroke="#14B8A6" strokeWidth={2.5}
            fill="url(#trendGradQ)" dot={false} activeDot={{ r: 5, fill: '#14B8A6', stroke: '#fff', strokeWidth: 2 }} />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  )
}

// ── Değişim Grafiği ───────────────────────────────────────────────────────────

function DailyChangeChart({ prices, dark }: { prices: StockPricePoint[]; dark: boolean }) {
  const data = useMemo(() =>
    [...prices]
      .sort((a, b) => new Date(a.date + 'T12:00:00Z').getTime() - new Date(b.date + 'T12:00:00Z').getTime())
      .slice(-20)
      .map(p => ({ date: formatShortDate(p.date), değişim: p.dailyChangePercent })),
    [prices]
  )

  const textColor = dark ? '#4B5563' : '#9CA3AF'
  const gridColor = dark ? 'rgba(255,255,255,0.035)' : '#F3F4F6'

  return (
    <div className={cn('rounded-2xl border overflow-hidden', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="px-6 pt-5 pb-3">
        <p className="text-gray-500 text-xs font-medium uppercase tracking-widest mb-1">Günlük Değişim</p>
        <h3 className={cn('font-bold', dark ? 'text-white' : 'text-gray-900')}>Kapanış % Değişim</h3>
      </div>
      <ResponsiveContainer width="100%" height={210}>
        <BarChart data={data} margin={{ top: 4, right: 24, left: 0, bottom: 4 }} barCategoryGap="38%">
          <defs>
            <linearGradient id="chgUp" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#14B8A6" stopOpacity={0.95} />
              <stop offset="100%" stopColor="#0D9488" stopOpacity={0.45} />
            </linearGradient>
            <linearGradient id="chgDown" x1="0" y1="1" x2="0" y2="0">
              <stop offset="0%" stopColor="#EF4444" stopOpacity={0.95} />
              <stop offset="100%" stopColor="#DC2626" stopOpacity={0.45} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="1 6" stroke={gridColor} vertical={false} />
          <XAxis dataKey="date" tick={{ fill: textColor, fontSize: 10 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
          <YAxis tick={{ fill: textColor, fontSize: 10 }} axisLine={false} tickLine={false} width={44} tickFormatter={(v: number) => `${v.toFixed(1)}%`} />
          <ReferenceLine y={0} stroke={dark ? 'rgba(255,255,255,0.18)' : 'rgba(0,0,0,0.12)'} strokeWidth={1} />
          <Tooltip content={(p) => <ChartTooltip {...p} dark={dark} fmt={(v) => `${v >= 0 ? '+' : ''}${v.toFixed(2)}%`} />} />
          <Bar dataKey="değişim" name="Değişim" radius={[4, 4, 0, 0]}>
            {data.map((entry, i) => (
              <Cell key={i} fill={entry.değişim >= 0 ? 'url(#chgUp)' : 'url(#chgDown)'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

// ── Tahmin vs Gerçek ─────────────────────────────────────────────────────────

function PredictionVsActualChart({ predictions, prices, dark }: {
  predictions: PredictionResponse[]
  prices: StockPricePoint[]
  dark: boolean
}) {
  const data = useMemo(() => {
    const sortedPrices = [...prices].sort((a, b) =>
      new Date(a.date + 'T12:00:00Z').getTime() - new Date(b.date + 'T12:00:00Z').getTime()
    )

    return [...predictions]
      .sort((a, b) => new Date(a.predictedAt).getTime() - new Date(b.predictedAt).getTime())
      .map(pred => {
        const predTime = new Date(pred.predictedAt).getTime()
        // Tahmin yapılan günden sonraki ilk işlem gününü bul
        const actual = sortedPrices.find(p =>
          new Date(p.date + 'T12:00:00Z').getTime() > predTime
        )
        if (!actual) return null
        const diff = ((pred.predictedPrice - actual.open) / actual.open) * 100
        return {
          date: formatShortDate(actual.date),
          tahmin: Math.round(pred.predictedPrice),
          gerçek: Math.round(actual.open),
          fark: Math.round(diff * 100) / 100,
        }
      })
      .filter(Boolean) as { date: string; tahmin: number; gerçek: number; fark: number }[]
  }, [predictions, prices])

  const textColor = dark ? '#4B5563' : '#9CA3AF'
  const gridColor = dark ? 'rgba(255,255,255,0.035)' : '#F3F4F6'

  if (data.length === 0) {
    return (
      <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
        <div className="h-44 flex flex-col items-center justify-center gap-2 text-gray-500 text-sm">
          <Target className="w-7 h-7 text-gray-600" />
          <p>Karşılaştırma için ertesi gün verisi bekleniyor.</p>
        </div>
      </div>
    )
  }

  return (
    <div className={cn('rounded-2xl border overflow-hidden', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-center justify-between px-6 pt-5 pb-3">
        <div>
          <p className="text-gray-500 text-xs font-medium uppercase tracking-widest mb-1">Doğruluk Analizi</p>
          <h3 className={cn('font-bold', dark ? 'text-white' : 'text-gray-900')}>Tahmin vs Gerçek Açılış</h3>
        </div>
        <div className="flex items-center gap-5">
          <div className="flex items-center gap-2">
            <span className="w-3 h-0.5 rounded-full inline-block bg-blue-400" />
            <span className="text-gray-500 text-xs">Tahmin</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="w-3 h-0.5 rounded-full inline-block bg-teal-400" />
            <span className="text-gray-500 text-xs">Gerçek</span>
          </div>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={260}>
        <ComposedChart data={data} margin={{ top: 8, right: 24, left: 0, bottom: 4 }}>
          <defs>
            <linearGradient id="actualGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#14B8A6" stopOpacity={0.2} />
              <stop offset="100%" stopColor="#14B8A6" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="1 6" stroke={gridColor} vertical={false} />
          <XAxis dataKey="date" tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
          <YAxis tick={{ fill: textColor, fontSize: 11 }} axisLine={false} tickLine={false} width={72}
            tickFormatter={(v: number) => v.toLocaleString('tr-TR')} domain={['auto', 'auto']} />
          <Tooltip content={(p) => <ChartTooltip {...p} dark={dark} fmt={(v) => `${formatPrice(v)} ₺`} />} />
          <Area type="monotone" dataKey="gerçek" name="Gerçek" stroke="#14B8A6" strokeWidth={2}
            fill="url(#actualGrad)" dot={false} activeDot={{ r: 4, fill: '#14B8A6', stroke: '#fff', strokeWidth: 2 }} />
          <Line type="monotone" dataKey="tahmin" name="Tahmin" stroke="#3B8EF3" strokeWidth={2}
            strokeDasharray="5 4" dot={false} activeDot={{ r: 4, fill: '#3B8EF3', stroke: '#fff', strokeWidth: 2 }} />
        </ComposedChart>
      </ResponsiveContainer>

      {/* Sapma özeti */}
      <div className={cn('mx-6 mb-5 rounded-xl p-4 grid grid-cols-3 gap-4', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
        {(() => {
          const abs = data.map(d => Math.abs(d.fark))
          const avgErr = abs.reduce((a, b) => a + b, 0) / abs.length
          const best = Math.min(...abs)
          const worst = Math.max(...abs)
          return [
            { label: 'Ort. Sapma', value: `%${avgErr.toFixed(2)}`, color: 'text-blue-400' },
            { label: 'En İyi', value: `%${best.toFixed(2)}`, color: 'text-teal-400' },
            { label: 'En Kötü', value: `%${worst.toFixed(2)}`, color: 'text-red-400' },
          ].map(s => (
            <div key={s.label} className="text-center">
              <div className={cn('font-bold text-sm', s.color)}>{s.value}</div>
              <div className="text-gray-500 text-xs mt-0.5">{s.label}</div>
            </div>
          ))
        })()}
      </div>
    </div>
  )
}

// ── History Table ─────────────────────────────────────────────────────────────

function HistoryTable({ data, prices, dark }: { data: PredictionResponse[]; prices: StockPricePoint[]; dark: boolean }) {
  const sorted = [...data].sort((a, b) => new Date(b.predictedAt).getTime() - new Date(a.predictedAt).getTime())

  const sortedPrices = useMemo(() =>
    [...prices].sort((a, b) => new Date(a.date + 'T12:00:00Z').getTime() - new Date(b.date + 'T12:00:00Z').getTime()),
    [prices]
  )

  function getActualError(pred: PredictionResponse): number | null {
    const predTime = new Date(pred.predictedAt).getTime()
    const actual = sortedPrices.find(p => new Date(p.date + 'T12:00:00Z').getTime() > predTime)
    if (!actual) return null
    return Math.abs((pred.predictedPrice - actual.open) / actual.open) * 100
  }

  function downloadCsv() {
    const header = 'Tarih,Tahmin (₺),Gerçek Hata (%)\n'
    const rows = sorted.map(p => {
      const err = getActualError(p)
      return `${formatDate(p.predictedAt)},${p.predictedPrice.toFixed(2)},${err !== null ? err.toFixed(2) : ''}`
    }).join('\n')
    const blob = new Blob([header + rows], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `predixus_tahminler_${new Date().toISOString().slice(0,10)}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className={cn('rounded-2xl border', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="p-5 border-b flex items-center justify-between" style={{ borderColor: dark ? '#1F2937' : '#E5E7EB' }}>
        <div>
          <p className="text-gray-400 text-xs font-medium uppercase tracking-wide">Geçmiş Tahminler</p>
          <h3 className={cn('font-semibold mt-0.5', dark ? 'text-white' : 'text-gray-900')}>Son {sorted.length} Kayıt</h3>
        </div>
        {sorted.length > 0 && (
          <button
            onClick={downloadCsv}
            title="CSV olarak indir"
            className={cn('flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg border transition-colors', dark ? 'border-dark-border text-gray-400 hover:text-gray-200 hover:border-gray-600' : 'border-gray-200 text-gray-500 hover:text-gray-700 hover:border-gray-300')}
          >
            <Download className="w-3.5 h-3.5" />
            CSV
          </button>
        )}
      </div>
      <div className="overflow-x-auto scrollbar-thin">
        <table className="w-full text-sm">
          <thead>
            <tr className={dark ? 'border-b border-dark-border' : 'border-b border-gray-100'}>
              <th className="text-left text-gray-400 font-medium text-xs px-5 py-3">#</th>
              <th className="text-left text-gray-400 font-medium text-xs px-5 py-3">Tarih</th>
              <th className="text-right text-gray-400 font-medium text-xs px-5 py-3">Tahmin (Açılış)</th>
              <th className="text-center text-gray-400 font-medium text-xs px-5 py-3">Gerçek Hata</th>
            </tr>
          </thead>
          <tbody>
            {sorted.length === 0 && (
              <tr><td colSpan={4} className="px-5 py-12 text-center text-gray-500 text-sm">Henüz tahmin yok. "Yeni Tahmin Al" butonuna tıklayın.</td></tr>
            )}
            {sorted.map((p, i) => {
              const err = getActualError(p)
              return (
                <tr key={p.predictionId} className={cn('transition-colors', i !== sorted.length - 1 ? dark ? 'border-b border-dark-border' : 'border-b border-gray-50' : '', dark ? 'hover:bg-dark-bg/60' : 'hover:bg-gray-50')}>
                  <td className="px-5 py-3.5 text-gray-500 text-xs">{sorted.length - i}</td>
                  <td className={cn('px-5 py-3.5', dark ? 'text-gray-300' : 'text-gray-700')}>{formatDate(p.predictedAt)}</td>
                  <td className={cn('px-5 py-3.5 text-right font-semibold tabular-nums', dark ? 'text-white' : 'text-gray-900')}>{formatPrice(p.predictedPrice)} ₺</td>
                  <td className="px-5 py-3.5 text-center">
                    {err !== null ? (
                      <span className={cn(
                        'text-xs px-2 py-0.5 rounded-full font-medium',
                        err <= 0.5 ? 'bg-teal-400/10 text-teal-400' : err <= 1.5 ? 'bg-amber-400/10 text-amber-400' : 'bg-red-400/10 text-red-400'
                      )}>
                        %{err.toFixed(2)}
                      </span>
                    ) : (
                      <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-white/5 text-gray-500 border border-white/10">
                        Bekleniyor
                      </span>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── Alert Widget ─────────────────────────────────────────────────────────────

function AlertWidget({ dark, latestPrice }: { dark: boolean; latestPrice: StockPricePoint | null }) {
  const [alerts, setAlerts] = useState<AlertItem[]>([])
  const [condition, setCondition] = useState<'ABOVE' | 'BELOW'>('ABOVE')
  const [targetPrice, setTargetPrice] = useState('')
  const [isAdding, setIsAdding] = useState(false)
  const [isOpen, setIsOpen] = useState(false)

  useEffect(() => {
    if (!isOpen) return
    getAlerts().then(setAlerts).catch(() => {})
  }, [isOpen])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    const price = parseFloat(targetPrice)
    if (!price || price <= 0) return
    setIsAdding(true)
    try {
      const newAlert = await createAlert(condition, price)
      setAlerts(prev => [newAlert, ...prev])
      setTargetPrice('')
      toast.success(`Alarm oluşturuldu: ${condition === 'ABOVE' ? '↑' : '↓'} ${formatPrice(price)} ₺`)
    } catch {
      toast.error('Alarm oluşturulamadı.')
    } finally {
      setIsAdding(false)
    }
  }

  async function handleDelete(id: string) {
    try {
      await deleteAlert(id)
      setAlerts(prev => prev.filter(a => a.id !== id))
    } catch {
      toast.error('Alarm silinemedi.')
    }
  }

  return (
    <div className={cn('rounded-2xl border', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <button
        onClick={() => setIsOpen(o => !o)}
        className="w-full flex items-center justify-between px-5 py-4"
      >
        <div className="flex items-center gap-2.5">
          <div className={cn('w-8 h-8 rounded-xl flex items-center justify-center', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
            <Bell className="w-4 h-4 text-amber-400" />
          </div>
          <div className="text-left">
            <p className={cn('font-semibold text-sm', dark ? 'text-white' : 'text-gray-900')}>Fiyat Alarmları</p>
            <p className="text-gray-500 text-xs">{alerts.length > 0 ? `${alerts.length} aktif alarm` : 'Fiyat hedefi belirle'}</p>
          </div>
        </div>
        <ChevronRight className={cn('w-4 h-4 text-gray-400 transition-transform', isOpen && 'rotate-90')} />
      </button>

      {isOpen && (
        <div className="px-5 pb-5 space-y-3 border-t" style={{ borderColor: dark ? '#1F2937' : '#E5E7EB' }}>
          {/* Form */}
          <form onSubmit={(e) => void handleCreate(e)} className="flex gap-2 mt-4">
            <select
              value={condition}
              onChange={e => setCondition(e.target.value as 'ABOVE' | 'BELOW')}
              className={cn('rounded-lg px-2.5 py-2 text-sm border outline-none', dark ? 'bg-dark-bg border-dark-border text-white' : 'bg-gray-50 border-gray-200 text-gray-900')}
            >
              <option value="ABOVE">↑ Üzerine çıkarsa</option>
              <option value="BELOW">↓ Altına inerse</option>
            </select>
            <input
              type="number"
              step="10"
              min="1"
              placeholder={latestPrice ? `örn. ${Math.round(latestPrice.close)}` : 'Hedef fiyat (₺)'}
              value={targetPrice}
              onChange={e => setTargetPrice(e.target.value)}
              className={cn('flex-1 rounded-lg px-3 py-2 text-sm border outline-none min-w-0', dark ? 'bg-dark-bg border-dark-border text-white placeholder-gray-600' : 'bg-gray-50 border-gray-200 text-gray-900 placeholder-gray-400')}
            />
            <button
              type="submit"
              disabled={isAdding || !targetPrice}
              className="flex items-center gap-1.5 bg-amber-400/10 hover:bg-amber-400/20 text-amber-400 text-sm font-medium px-3 py-2 rounded-lg transition-colors disabled:opacity-50"
            >
              <BellPlus className="w-4 h-4" />
            </button>
          </form>

          {/* Liste */}
          {alerts.length === 0 ? (
            <p className="text-gray-500 text-xs text-center py-3">Henüz alarm yok.</p>
          ) : (
            <div className="space-y-2">
              {alerts.map(a => (
                <div
                  key={a.id}
                  className={cn('flex items-center justify-between rounded-xl px-3 py-2.5', dark ? 'bg-dark-bg' : 'bg-gray-50')}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <span className={cn('text-xs font-bold px-2 py-0.5 rounded-full', a.condition === 'ABOVE' ? 'bg-emerald-400/10 text-emerald-400' : 'bg-red-400/10 text-red-400')}>
                      {a.condition === 'ABOVE' ? '↑' : '↓'}
                    </span>
                    <span className={cn('font-semibold text-sm tabular-nums', dark ? 'text-white' : 'text-gray-900')}>
                      {formatPrice(a.targetPrice)} ₺
                    </span>
                    {a.isTriggered && (
                      <span className="text-xs text-amber-400 font-medium">Tetiklendi</span>
                    )}
                  </div>
                  <button
                    onClick={() => void handleDelete(a.id)}
                    className="text-gray-500 hover:text-red-400 transition-colors ml-2"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ── Model Detail Card ─────────────────────────────────────────────────────────

function ModelDetailCard({ model, dark }: { model: typeof MODELS[number]; dark: boolean }) {
  return (
    <div className={cn('rounded-2xl border p-5 flex flex-col', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <div className="flex items-start justify-between mb-3">
        <div className={cn('w-10 h-10 rounded-xl flex items-center justify-center', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
          <Brain className="w-5 h-5 text-primary" />
        </div>
        <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium', model.active ? 'bg-emerald-400/15 text-emerald-400' : dark ? 'bg-gray-700 text-gray-500' : 'bg-gray-100 text-gray-400')}>
          {model.active ? 'Aktif' : 'Yakında'}
        </span>
      </div>

      <h3 className={cn('font-semibold mb-1', dark ? 'text-white' : 'text-gray-900')}>{model.label}</h3>
      <p className="text-gray-400 text-sm mb-4 flex-1 leading-relaxed">{model.desc}</p>

      <div className={cn('rounded-xl p-3 space-y-2', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
        {model.active ? (
          <>
            <div className="flex items-center justify-between">
              <span className="text-gray-400 text-xs">MAPE</span>
              <span className="text-emerald-400 font-mono text-sm font-semibold">%{model.mape}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-gray-400 text-xs">MAE (₺)</span>
              <span className={cn('font-mono text-sm font-semibold', dark ? 'text-white' : 'text-gray-900')}>{model.mae}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-gray-400 text-xs">RMSE (₺)</span>
              <span className={cn('font-mono text-sm font-semibold', dark ? 'text-white' : 'text-gray-900')}>{model.rmse}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-gray-400 text-xs">Test (14.322 → 14.349)</span>
              <span className="text-emerald-400 text-xs font-medium">✓ Doğrulandı</span>
            </div>
          </>
        ) : (
          <div className="text-center py-2 text-gray-500 text-sm">
            Bu model henüz hazır değil.
            <br />
            <span className="text-xs">Eklenince metrikler burada görünecek.</span>
          </div>
        )}
      </div>

      {model.active && (
        <button className="mt-3 w-full bg-primary/10 hover:bg-primary/15 text-primary text-sm font-medium rounded-xl py-2 transition-colors flex items-center justify-center gap-1.5">
          <Target className="w-3.5 h-3.5" />
          Seçili Model
        </button>
      )}
    </div>
  )
}

// ── Doğruluk Halkası ─────────────────────────────────────────────────────────

function AccuracyRing({ pct, dark }: { pct: number; dark: boolean }) {
  const r = 48
  const circ = 2 * Math.PI * r
  const filled = circ * (pct / 100)
  return (
    <svg viewBox="0 0 120 120" className="w-32 h-32 shrink-0">
      {/* track */}
      <circle cx="60" cy="60" r={r} fill="none" stroke={dark ? '#1F2937' : '#E5E7EB'} strokeWidth="10" />
      {/* error arc (red, tiny) */}
      <circle
        cx="60" cy="60" r={r} fill="none"
        stroke="#EF4444" strokeWidth="10" strokeOpacity="0.5"
        strokeDasharray={`${circ - filled} ${filled}`}
        strokeDashoffset={-(filled)}
        strokeLinecap="round"
        transform="rotate(-90 60 60)"
      />
      {/* accuracy arc (teal) */}
      <circle
        cx="60" cy="60" r={r} fill="none"
        stroke="#14B8A6" strokeWidth="10"
        strokeDasharray={`${filled} ${circ}`}
        strokeLinecap="round"
        transform="rotate(-90 60 60)"
        style={{ filter: 'drop-shadow(0 0 6px rgba(20,184,166,0.5))' }}
      />
      <text x="60" y="56" textAnchor="middle" dominantBaseline="middle"
        fill={dark ? '#F9FAFB' : '#111827'} fontSize="17" fontWeight="bold" fontFamily="sans-serif">
        %{pct.toFixed(2)}
      </text>
      <text x="60" y="72" textAnchor="middle" dominantBaseline="middle"
        fill="#9CA3AF" fontSize="9" fontFamily="sans-serif">
        isabetli
      </text>
    </svg>
  )
}

// ── Güven Kartı ───────────────────────────────────────────────────────────────

function AccuracyCard({ prediction, dark }: { prediction: PredictionResponse | null; dark: boolean }) {
  const accuracy = 99.81
  const mape = 0.19
  const mae = 26.90
  const examplePrice = prediction?.predictedPrice ?? 14350

  const low = examplePrice * (1 - mape / 100)
  const high = examplePrice * (1 + mape / 100)

  return (
    <div className={cn('rounded-2xl border p-6', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
      <p className="text-gray-400 text-xs font-medium uppercase tracking-widest mb-5">Model Performansı</p>

      <div className="flex flex-col sm:flex-row gap-6 items-start">
        {/* Ring */}
        <div className="flex flex-col items-center gap-2 shrink-0">
          <AccuracyRing pct={accuracy} dark={dark} />
          <span className="text-xs text-gray-500 text-center">doğruluk oranı</span>
        </div>

        {/* Açıklama */}
        <div className="flex-1 space-y-4">
          <div>
            <h3 className={cn('font-bold text-lg mb-1', dark ? 'text-white' : 'text-gray-900')}>
              Model Performans Raporu
            </h3>
            <p className={cn('text-sm leading-relaxed', dark ? 'text-gray-400' : 'text-gray-600')}>
              Model, geçmiş veriler üzerinde test edildi. Her 1.000 ₺'lik tahminde ortalama
              <span className="text-teal-400 font-semibold"> {mae.toFixed(0)} ₺</span> sapma ölçüldü.
              Aşağıda test setinden hesaplanan hata aralığı gösterilmektedir.
            </p>
          </div>

          {/* Örnek aralık */}
          <div className={cn('rounded-xl p-4', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
            <p className="text-gray-500 text-xs mb-3 uppercase tracking-wide">Örnek · bugünkü tahmin</p>
            <div className="flex items-center gap-2 mb-2">
              <span className="text-gray-500 text-xs tabular-nums w-24 text-right">{formatPrice(low)} ₺</span>
              <div className="flex-1 relative h-4 flex items-center">
                <div className={cn('absolute inset-0 rounded-full', dark ? 'bg-gray-800' : 'bg-gray-200')} />
                <div className="absolute left-0 right-0 mx-4 h-1.5 rounded-full bg-teal-400/30" />
                <div className="absolute left-1/2 -translate-x-1/2 w-2 h-4 rounded-sm bg-teal-400"
                  style={{ boxShadow: '0 0 8px rgba(20,184,166,0.6)' }} />
              </div>
              <span className="text-gray-500 text-xs tabular-nums w-24">{formatPrice(high)} ₺</span>
            </div>
            <div className="text-center">
              <span className={cn('text-sm font-bold tabular-nums', dark ? 'text-white' : 'text-gray-900')}>
                {formatPrice(examplePrice)} ₺
              </span>
              <span className="text-gray-500 text-xs ml-2">tahmin edilen değer</span>
            </div>
            <p className="text-gray-600 text-xs text-center mt-1">
              Gerçek değer genellikle bu aralık içinde kalır
            </p>
          </div>

          {/* 3 mini stat */}
          <div className="grid grid-cols-3 gap-3">
            {[
              { label: 'Hata Payı', value: `%${mape}`, color: 'text-teal-400', hint: 'yüzde' },
              { label: 'Ort. Sapma', value: `${mae} ₺`, color: 'text-blue-400', hint: 'tutar' },
              { label: 'Test Sonucu', value: '✓ Geçti', color: 'text-emerald-400', hint: 'doğrulandı' },
            ].map(s => (
              <div key={s.label} className={cn('rounded-xl p-3 text-center', dark ? 'bg-dark-bg border border-dark-border' : 'bg-gray-50 border border-gray-100')}>
                <div className={cn('font-bold text-sm', s.color)}>{s.value}</div>
                <div className="text-gray-500 text-xs mt-0.5">{s.label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}

// ── Analiz İstatistik Satırı ──────────────────────────────────────────────────

function AnalyticsStats({ predictions, prices, dark }: {
  predictions: PredictionResponse[]
  prices: StockPricePoint[]
  dark: boolean
}) {
  const avg = predictions.length > 0
    ? predictions.reduce((s, p) => s + p.predictedPrice, 0) / predictions.length
    : null

  const sorted30 = useMemo(() =>
    [...prices]
      .sort((a, b) => new Date(b.date + 'T12:00:00Z').getTime() - new Date(a.date + 'T12:00:00Z').getTime())
      .slice(0, 30),
    [prices])

  const bestDay = sorted30.reduce((best, p) => p.dailyChangePercent > (best?.dailyChangePercent ?? -Infinity) ? p : best, null as StockPricePoint | null)
  const worstDay = sorted30.reduce((worst, p) => p.dailyChangePercent < (worst?.dailyChangePercent ?? Infinity) ? p : worst, null as StockPricePoint | null)

  const stats: { label: string; value: string; sub: string; color: string; icon: React.ComponentType<{ className?: string }> }[] = [
    { label: 'Ortalama Tahmin', value: avg !== null ? `${formatPrice(avg)} ₺` : '—', sub: 'Tüm tahminlerin ortalaması', color: 'text-blue-400', icon: BarChart2 },
    { label: 'En İyi Gün', value: bestDay ? `+${bestDay.dailyChangePercent.toFixed(2)}%` : '—', sub: bestDay ? formatShortDate(bestDay.date) : 'Son 30 gün', color: 'text-emerald-400', icon: TrendingUp },
    { label: 'En Zor Gün', value: worstDay ? `${worstDay.dailyChangePercent.toFixed(2)}%` : '—', sub: worstDay ? formatShortDate(worstDay.date) : 'Son 30 gün', color: 'text-red-400', icon: TrendingDown },
    { label: 'Analiz Edilen', value: `${predictions.length}`, sub: 'Toplam tahmin kaydı', color: 'text-teal-400', icon: Database },
  ]

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {stats.map(s => (
        <div key={s.label} className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
          <div className="flex items-start justify-between mb-3">
            <div className={cn('w-9 h-9 rounded-xl flex items-center justify-center', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
              <s.icon className={cn('w-4 h-4', s.color)} />
            </div>
          </div>
          <div className={cn('text-2xl font-bold tabular-nums mb-1', s.color)}>{s.value}</div>
          <div className={cn('text-sm font-medium mb-0.5', dark ? 'text-gray-300' : 'text-gray-700')}>{s.label}</div>
          <div className="text-gray-500 text-xs">{s.sub}</div>
        </div>
      ))}
    </div>
  )
}

// ── DASHBOARD PAGE ─────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const { email, role, logout } = useAuthStore()
  const [dark, setDark] = useState(true)
  const [tab, setTab] = useState<Tab>('overview')
  const [selectedModel, setSelectedModel] = useState<ModelId>('lstm')
  const [predictions, setPredictions] = useState<PredictionResponse[]>([])
  const [prices, setPrices] = useState<StockPricePoint[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isPredicting, setIsPredicting] = useState(false)
  const [priceDays, setPriceDays] = useState<PriceRange>(60)
  const [predictionFlash, setPredictionFlash] = useState(false)

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark)
  }, [dark])

  const loadData = useCallback(async (days: PriceRange = priceDays) => {
    try {
      const [histData, priceData] = await Promise.all([getHistory(30), getBist100Prices(days)])
      setPredictions(histData)
      setPrices(priceData)
    } catch {
      toast.error('Veriler yüklenemedi.')
    } finally {
      setIsLoading(false)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => { void loadData() }, [loadData])

  useEffect(() => {
    if (isLoading) return
    getBist100Prices(priceDays).then(setPrices).catch(() => {})
  }, [priceDays, isLoading])

  async function handleNewPrediction() {
    setIsPredicting(true)
    const toastId = toast.loading('Tahmin hesaplanıyor...')
    try {
      const result = await predict()
      setPredictions((prev) => prev.some(p => p.predictionId === result.predictionId) ? prev : [result, ...prev])
      if (result.fromCache) {
        toast('Bugün için tahmin zaten alınmış', { id: toastId, icon: '⚡', duration: 3000 })
      } else {
        toast.success(`Tahmin alındı: ${formatPrice(result.predictedPrice)} ₺`, { id: toastId, duration: 4000 })
        setPredictionFlash(true)
        setTimeout(() => setPredictionFlash(false), 1800)
      }
    } catch {
      toast.error('Tahmin alınamadı. ML servisi çalışıyor mu?', { id: toastId })
    } finally {
      setIsPredicting(false)
    }
  }

  function handleLogout() {
    document.documentElement.classList.remove('dark')
    logout()
  }

  // Derived
  const sorted = useMemo(() => [...predictions].sort((a, b) => new Date(b.predictedAt).getTime() - new Date(a.predictedAt).getTime()), [predictions])
  const latestPrediction = sorted[0] ?? null
  const previousPrediction = sorted[1] ?? null
  const trend = latestPrediction && previousPrediction ? (() => {
    const diff = latestPrediction.predictedPrice - previousPrediction.predictedPrice
    const pct = (diff / previousPrediction.predictedPrice) * 100
    return { value: `${pct > 0 ? '+' : ''}${pct.toFixed(2)}%`, up: pct >= 0 }
  })() : null

  // En güncel fiyat istatistikleri
  const latestPrice = useMemo(() => {
    if (!prices.length) return null
    return [...prices].sort((a, b) => new Date(b.date + 'T12:00:00Z').getTime() - new Date(a.date + 'T12:00:00Z').getTime())[0]
  }, [prices])

  const newPredictionBtn = (
    <button
      onClick={() => { void handleNewPrediction() }}
      disabled={isPredicting}
      className="shrink-0 flex items-center gap-2 bg-primary hover:bg-primary/90 disabled:opacity-60 disabled:cursor-not-allowed text-white text-sm font-medium px-4 py-2.5 rounded-xl transition-all shadow-lg shadow-primary/20"
    >
      <RefreshCw className={cn('w-4 h-4', isPredicting && 'animate-spin')} />
      {isPredicting ? 'Hesaplanıyor...' : 'Yeni Tahmin Al'}
    </button>
  )

  return (
    <div className={cn('min-h-screen flex flex-col', dark ? 'bg-dark-bg' : 'bg-gray-50')}>
      <Topbar email={email ?? ''} role={role} dark={dark} onThemeToggle={() => setDark(d => !d)} onLogout={handleLogout} latestPrice={latestPrice} />

      <div className="flex flex-1 overflow-hidden">
        <Sidebar tab={tab} onTab={setTab} dark={dark} />

        <main className="flex-1 overflow-y-auto">
          <div className="max-w-6xl mx-auto px-4 sm:px-6 py-6 pb-20 md:pb-6 space-y-5">

            {/* Page header */}
            <div className="flex items-start justify-between gap-4">
              <div>

                <h1 className={cn('text-2xl font-bold mt-1', dark ? 'text-white' : 'text-gray-900')}>
                  {{
                    overview: 'Genel Bakış',
                    analytics: 'Analiz',
                    models: 'Modeller',
                  }[tab]}
                </h1>
              </div>
              {tab === 'overview' && newPredictionBtn}
            </div>

            {/* Loading */}
            {isLoading ? (
              <div className="space-y-5">
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">{[1,2,3,4].map(n => <SkeletonCard key={n} dark={dark} />)}</div>
                <Skeleton dark={dark} className="h-72 rounded-2xl" />
              </div>
            ) : (
              <>
                {/* ── GENEL BAKIŞ ── */}
                {tab === 'overview' && (
                  <div className="space-y-5">
                    {/* KPI grid */}
                    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                      <div className={cn('transition-all duration-500', predictionFlash && 'ring-2 ring-teal-400 ring-offset-2 ring-offset-transparent rounded-2xl scale-[1.02]')}>
                        <KpiCard
                          label="Yarınki BIST 100 Açılışı"
                          value={latestPrediction ? `${formatPrice(latestPrediction.predictedPrice)} ₺` : '—'}
                          trend={trend}
                          badge={!trend && latestPrediction ? 'LSTM · 1 Gün' : undefined}
                          badgeColor="blue"
                          icon={TrendingUp}
                          dark={dark}
                        />
                      </div>
                      <KpiCard
                        label="Son Kapanış"
                        value={latestPrice ? `${formatPrice(latestPrice.close)} ₺` : '—'}
                        badge={latestPrice ? `${latestPrice.dailyChangePercent >= 0 ? '+' : ''}${latestPrice.dailyChangePercent.toFixed(2)}%` : undefined}
                        badgeColor={latestPrice ? (latestPrice.dailyChangePercent >= 0 ? 'green' : 'red') : 'gray'}
                        icon={Activity}
                        dark={dark}
                      />
                      <KpiCard
                        label="Toplam Tahmin"
                        value={String(predictions.length)}
                        badge={predictions.length > 0 ? 'Kayıtlı' : 'Henüz yok'}
                        badgeColor={predictions.length > 0 ? 'green' : 'gray'}
                        icon={Database}
                        dark={dark}
                      />
                      <KpiCard
                        label="Son Güncelleme"
                        value={latestPrediction ? new Date(latestPrediction.predictedAt).toLocaleDateString('tr-TR') : '—'}
                        badge={latestPrediction
                          ? new Date(latestPrediction.predictedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
                          : undefined}
                        badgeColor="gray"
                        icon={Clock}
                        dark={dark}
                      />
                    </div>

                    {/* Ana fiyat grafiği */}
                    <PriceAndPredictionChart prices={prices} prediction={latestPrediction} dark={dark} priceDays={priceDays} onPriceDaysChange={setPriceDays} />

                    {/* Model seçici */}
                    <ModelSelector selected={selectedModel} onSelect={setSelectedModel} dark={dark} />

                    {/* Alarm widget */}
                    <AlertWidget dark={dark} latestPrice={latestPrice} />

                    {/* Tablo */}
                    <HistoryTable data={predictions} prices={prices} dark={dark} />
                  </div>
                )}

                {/* ── ANALİZ ── */}
                {tab === 'analytics' && (
                  <div className="space-y-5">
                    {/* İstatistik satırı */}
                    <AnalyticsStats predictions={predictions} prices={prices} dark={dark} />

                    {/* Güven kartı */}
                    <AccuracyCard prediction={latestPrediction} dark={dark} />

                    {/* Tahmin vs Gerçek */}
                    <PredictionVsActualChart predictions={predictions} prices={prices} dark={dark} />

                    {/* Fiyat grafiği */}
                    <PriceAndPredictionChart prices={prices} prediction={latestPrediction} dark={dark} priceDays={priceDays} onPriceDaysChange={setPriceDays} />

                    {/* 2 kolon grafik */}
                    <div className="grid lg:grid-cols-2 gap-5">
                      <PredictionTrendChart predictions={predictions} dark={dark} />
                      <DailyChangeChart prices={prices} dark={dark} />
                    </div>

                    {/* Hacim */}
                    <VolumeChart prices={prices} dark={dark} />

                    {/* Tablo */}
                    <HistoryTable data={predictions} prices={prices} dark={dark} />
                  </div>
                )}

                {/* ── MODELLER ── */}
                {tab === 'models' && (
                  <div className="space-y-5">
                    <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
                      <p className="text-gray-400 text-sm leading-relaxed">
                        Şu an <span className="text-primary font-medium">LSTM + Attention</span> modeli aktif.
                        GRU ve XGBoost modelleri ekip arkadaşı tarafından eğitilip sisteme eklenecek.
                        Yeni model eklendiğinde aynı CSV kontratı üzerinden çalışacak — backend değişmeyecek.
                      </p>
                    </div>

                    <div className="grid md:grid-cols-3 gap-4">
                      {MODELS.map(m => <ModelDetailCard key={m.id} model={m} dark={dark} />)}
                    </div>

                    {/* Teknik detaylar */}
                    <div className={cn('rounded-2xl border p-5', dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200')}>
                      <p className="text-gray-400 text-xs font-medium uppercase tracking-wide mb-4">Sistem Mimarisi</p>
                      <div className="grid sm:grid-cols-2 gap-4 text-sm">
                        {[
                          ['Veri Kaynağı', 'Yahoo Finance XU100.IS (v8 API)'],
                          ['Güncelleme Sıklığı', 'Otomatik — her 24 saatte bir'],
                          ['Minimum Veri', '24 gün OHLCV geçmişi'],
                          ['Tahmin Ufku', 'Ertesi gün açılış fiyatı (1 gün)'],
                          ['Cache Stratejisi', 'Redis — gün sonuna kadar önbellekleme'],
                          ['ML Kontrat', 'POST /predict, CSV multipart form'],
                          ['Hata Yönetimi', '2 retry, Redis fail-open'],
                          ['Son Test Sonucu', '14.322,35 ₺ tahmin — 14.349,25 ₺ gerçek (%0.19)'],
                        ].map(([k, v]) => (
                          <div key={k} className={cn('flex gap-2', dark ? 'text-gray-300' : 'text-gray-600')}>
                            <span className="text-gray-500 shrink-0 w-40">{k}</span>
                            <span className="font-medium">{v}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </main>
      </div>

      {/* Mobil alt navigasyon */}
      <nav className={cn(
        'md:hidden fixed bottom-0 left-0 right-0 z-20 flex border-t',
        dark ? 'bg-dark-card border-dark-border' : 'bg-white border-gray-200'
      )}>
        {([
          { id: 'overview' as Tab, label: 'Genel Bakış', icon: BarChart2 },
          { id: 'analytics' as Tab, label: 'Analiz', icon: LineIcon },
          { id: 'models' as Tab, label: 'Modeller', icon: Cpu },
        ] as const).map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => setTab(id)}
            className={cn(
              'flex-1 flex flex-col items-center gap-1 py-3 text-xs font-medium transition-colors',
              tab === id ? 'text-primary' : dark ? 'text-gray-500' : 'text-gray-400'
            )}
          >
            <Icon className="w-5 h-5" />
            {label}
          </button>
        ))}
      </nav>
    </div>
  )
}

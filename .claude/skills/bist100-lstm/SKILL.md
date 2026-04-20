---
name: bist100-lstm
description: |
  BIST100 hisse senedi tahmin sistemi için kapsamlı proje rehberi ve kod üretici.
  Bu skill'i kullan her zaman:
  - "BIST100", "tahmin", "hisse", "LSTM", "bist-api", "bist-ml" gibi kelimeler geçtiğinde
  - .NET Clean Architecture projesi için entity, service, controller, repository, DTO kodu yazılacaksa
  - EF Core migration, Redis cache, Docker Compose, JWT auth ile ilgili sorular sorulduğunda
  - Yahoo Finance client, ML prediction client, background job konuları geçtiğinde
  - Projenin mimari kararları, katman yapısı veya servis haritası sorulduğunda
  - "prediction servisi", "stock controller", "AppDbContext", "PredictionService" gibi proje bileşenleri bahsedildiğinde
  Tek cümle ile: Bu proje hakkında herhangi bir şey yapılacaksa bu skill MUTLAKA okunmalıdır.
---

# BIST100 LSTM Tahmin Sistemi — Proje Skill

## Projenin Özeti

Yahoo Finance'tan çekilen BIST100 hisse senedi verileri üzerinde LSTM modeli ile fiyat tahmini yapan full-stack sistem. İki ana geliştirici: sen (.NET backend) ve ekip arkadaşın (Python ML servisi).

**Tech Stack:**
- Backend: `.NET 9` + `ASP.NET Core` + `Clean Architecture`
- Veritabanı: `PostgreSQL 16` (EF Core 9)
- Cache: `Redis 7` (StackExchange.Redis) — henüz eklenmedi
- ML Servisi: `Python FastAPI` + `PyTorch LSTM` — henüz eklenmedi
- Veri Kaynağı: `Yahoo Finance` (HTTP API) — henüz eklenmedi
- Container: `Docker Compose` — henüz eklenmedi
- Auth: `JWT Bearer` + Refresh Token ✅

---

## Sistem Mimarisi

```
Kullanıcı → .NET API (5000) → Redis Cache?
                                ├── HIT  → Direkt dön
                                └── MISS → Python ML (8000) → /predict
                                              ↓
                                         PostgreSQL (5432)
```

**Container Haritası (planlandı, henüz yazılmadı):**

| Servis | Image | Port | Görev |
|--------|-------|------|-------|
| predixus-api | Proje Dockerfile | 5000 | .NET 9 Web API |
| predixus-ml | ml/Dockerfile | 8000 | FastAPI + LSTM inference |
| predixus-db | postgres:16-alpine | 5432 | Ana veritabanı |
| predixus-redis | redis:7-alpine | 6379 | Cache + rate limiting |

---

## Clean Architecture Katmanları

```
Predixus.API          → Controllers, Middleware, Program.cs, Swagger, JWT
Predixus.Application  → Services, DTOs, Interfaces, Exceptions
Predixus.Domain       → Entities, Repository Interfaces (SIFIR dış bağımlılık)
Predixus.Infrastructure → EF Core, PostgreSQL, Security, (Redis, Yahoo, ML — gelecek)
```

**Namespace formatı:** `Predixus.{Katman}.{Klasör}` (ör: `Predixus.Application.Services`)

**Bağımlılık yönü (sadece aşağı gidebilir):**
- API → Application + Infrastructure
- Application → Domain
- Infrastructure → Application (interface impl.)
- Domain → Hiçbir şey

---

## Mevcut Proje Klasör Yapısı

```
Predixus/
├── src/
│   ├── Predixus.Domain/
│   │   ├── BaseEntity.cs                          ✅
│   │   ├── Entities/
│   │   │   ├── Stock.cs                           ✅
│   │   │   ├── StockPrice.cs                      ✅
│   │   │   ├── Prediction.cs                      ✅
│   │   │   ├── PredictionPoint.cs                 ✅
│   │   │   ├── User.cs                            ✅
│   │   │   ├── RefreshToken.cs                    ✅
│   │   │   └── Alert.cs                           ❌ (henüz yazılmadı)
│   │   └── Interfaces/
│   │       ├── IUserRepository.cs                 ✅
│   │       ├── IStockRepository.cs                ❌
│   │       ├── IStockPriceRepository.cs           ❌
│   │       └── IPredictionRepository.cs           ❌
│   ├── Predixus.Application/
│   │   ├── Services/
│   │   │   ├── AuthService.cs                     ✅
│   │   │   ├── StockDataService.cs                ❌
│   │   │   ├── PredictionService.cs               ❌
│   │   │   └── AlertService.cs                    ❌
│   │   ├── DTOs/
│   │   │   ├── AuthDto.cs                         ✅
│   │   │   ├── StockDto.cs                        ❌
│   │   │   ├── PredictionRequestDto.cs            ❌
│   │   │   └── PredictionResponseDto.cs           ❌
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs                    ✅
│   │   │   ├── IPasswordHasher.cs                 ✅
│   │   │   ├── IJwtTokenService.cs                ✅
│   │   │   ├── IStockDataService.cs               ❌
│   │   │   └── IPredictionService.cs              ❌
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs               ✅
│   │   │   ├── ConflictException.cs               ✅
│   │   │   └── UnauthorizedException.cs           ✅
│   │   └── BackgroundJobs/
│   │       └── StockDataFetchJob.cs               ❌
│   ├── Predixus.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                    ✅
│   │   │   ├── Configurations/
│   │   │   │   ├── UserConfiguration.cs           ✅
│   │   │   │   ├── RefreshTokenConfiguration.cs   ✅
│   │   │   │   ├── StockConfiguration.cs          ❌
│   │   │   │   ├── StockPriceConfiguration.cs     ❌
│   │   │   │   ├── PredictionConfiguration.cs     ❌
│   │   │   │   └── PredictionPointConfiguration.cs ❌
│   │   │   ├── Repositories/
│   │   │   │   ├── UserRepository.cs              ✅
│   │   │   │   ├── StockRepository.cs             ❌
│   │   │   │   └── PredictionRepository.cs        ❌
│   │   │   └── Migrations/                        ✅ (InitialCreate)
│   │   ├── Security/
│   │   │   ├── JwtTokenService.cs                 ✅
│   │   │   └── PasswordHasher.cs                  ✅
│   │   ├── Cache/
│   │   │   ├── RedisCacheService.cs               ❌
│   │   │   └── RedisRateLimiter.cs                ❌
│   │   └── ExternalServices/
│   │       ├── YahooFinanceClient.cs              ❌
│   │       └── MlPredictionClient.cs              ❌
│   └── Predixus.API/
│       ├── Controllers/
│       │   ├── AuthController.cs                  ✅
│       │   ├── StocksController.cs                ❌
│       │   └── PredictionsController.cs           ❌
│       ├── Middleware/
│       │   ├── ErrorHandlingMiddleware.cs         ✅
│       │   └── RateLimitingMiddleware.cs          ❌
│       ├── Program.cs                             ✅
│       ├── appsettings.json                       ✅
│       └── Dockerfile                             ❌
├── ml-service/                                    ❌ (ekip arkadaşının sorumluluğu)
├── docker-compose.yml                             ❌
└── Predixus.sln                                   ✅
```

---

## NuGet Paketleri (Kurulu)

**Predixus.Infrastructure:**
- `BCrypt.Net-Next 4.1.0`
- `Microsoft.EntityFrameworkCore 9.0.5`
- `Microsoft.EntityFrameworkCore.Design 9.0.5`
- `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4`
- `System.IdentityModel.Tokens.Jwt 8.0.1`
- `Microsoft.Extensions.Logging.Abstractions 9.0.5`

**Predixus.Application:**
- `Microsoft.Extensions.Logging.Abstractions 9.0.5`

**Predixus.API:**
- `Microsoft.AspNetCore.Authentication.JwtBearer 9.0.5`
- `Microsoft.AspNetCore.OpenApi 9.0.5`
- `Microsoft.EntityFrameworkCore.Design 9.0.5`
- `Swashbuckle.AspNetCore 7.3.1`

**Henüz eklenmeyenler:**
- `StackExchange.Redis` (Redis için)
- `Serilog.AspNetCore` (structured logging)

---

## PostgreSQL Şema (Tablolar)

| Tablo | Önemli Sütunlar | Durum |
|-------|----------------|-------|
| users | id, email, password_hash, is_active, created_at | ✅ Migration'da var |
| refresh_tokens | id, user_id, token, expires_at, is_revoked | ✅ Migration'da var |
| stocks | id, symbol, name, sector, is_active | ✅ Entity var, config yok |
| stock_prices | id, stock_id, date, open, high, low, close, volume | ✅ Entity var, config yok |
| predictions | id, stock_id, user_id, predicted_at, forecast_days, confidence_score | ✅ Entity var, config yok |
| prediction_points | id, prediction_id, day_offset, predicted_price, actual_price | ✅ Entity var, config yok |
| alerts | id, user_id, stock_id, condition, target_price, is_triggered | ❌ Entity yok |

---

## Domain Entity Kuralları

Tüm entity'ler `BaseEntity`'den türer:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
```

**Kurallar:**
- Property setter'lar `private set` — dışarıdan direkt atama yasak
- Nesne oluşturmak için `static Create(...)` factory method kullan
- Domain validasyonu Create() içinde yapılır (`ArgumentException` fırlat)
- EF Core için `private Entity() { }` parametresiz constructor zorunlu
- Navigation property'ler `ICollection<T>` olarak tanımlanır, `= new List<T>()` ile init edilir
- Hesaplanmış property'ler (ör. `DailyChangePercent`, `IsExpired`) DB'ye yazılmaz — config'de `builder.Ignore()` ile işaretlenir

---

## DTO Kuralları

- DTO'lar `record` tipinde tanımlanır (immutable)
- `Application/DTOs/` altına konur
- Auth DTO'ları: `RegisterRequest`, `LoginRequest`, `AuthResponse`, `RefreshTokenRequest`
- Prediction DTO'ları: `PredictionRequestDto(string Symbol, int ForecastDays)`, `PredictionResponseDto` (FromCache bool içerir)
- ML servisine giden: `MlPredictionRequest(string Symbol, int ForecastDays, List<OhlcvPoint> HistoricalData)`
- ML servisinden gelen: `MlPredictionResponse(string Symbol, string ModelVersion, decimal Confidence, List<decimal> PredictedPrices)`

---

## Auth Akışı (Tamamlandı)

```
Register: email unique? → BCrypt.Hash(password) → User.Create() → DB kaydet → JWT + RefreshToken üret
Login:    kullanıcı var? → BCrypt.Verify() → eski tokenları revoke → JWT + RefreshToken üret
Refresh:  token geçerli? (IsValid = !IsRevoked && !IsExpired) → eski revoke → yeni JWT + RefreshToken
```

**JWT Claims:** `sub` (userId), `email`, `jti` (unique token id)
**RefreshToken:** `RandomNumberGenerator.GetBytes(64)` → Base64 string, 7 gün geçerli

---

## Cache-Aside Pattern (PredictionService — Henüz Yazılmadı)

```
1. Redis'te cacheKey var mı? → Varsa dön (FromCache: true)
2. Hisse DB'de var mı? → Yoksa NotFoundException fırlat
3. Son 60 günlük fiyatı DB'den çek (minimum 30 gün şartı)
4. ML servisine HTTP POST /predict → MlPredictionResponse al
5. Prediction entity'si oluştur, DB'ye kaydet
6. Response DTO oluştur
7. Redis'e TTL ile cache'le (TTL = gün sonu - şu an)
8. Response dön
```

**Cache Key Şeması:**

| Key | TTL | Açıklama |
|-----|-----|----------|
| `prediction:{SYMBOL}:{DAYS}d` | Gün sonu | Tahmin cache |
| `stock:prices:{SYMBOL}:latest` | 1 saat | Son fiyat |
| `stock:prices:{SYMBOL}:history:{DAYS}` | 4 saat | Geçmiş fiyat dizisi |
| `ratelimit:{IP}:{ENDPOINT}` | 1 dakika | Rate limiting |

---

## Redis Kuralları (Henüz Uygulanmadı)

- Redis çökerse uygulama çalışmaya devam etmeli — her Redis çağrısı try/catch ile sarılır, exception loglanır ama fırlatılmaz
- `GetAsync<T>` / `SetAsync<T>` generic, JSON serialize/deserialize yapar

---

## API Endpoint'leri

**AuthController** (`[AllowAnonymous]`) ✅
- `POST /api/auth/register`
- `POST /api/auth/login` → JWT + Refresh Token döner
- `POST /api/auth/refresh`

**PredictionsController** (`[Authorize]`) ❌
- `POST /api/predictions` → Tahmin al (Cache-Aside akışı)
- `GET /api/predictions/{symbol}/history?count=10` → Tahmin geçmişi
- `GET /api/predictions/{predictionId}/accuracy` → Doğruluk hesapla

**StocksController** ❌
- `GET /api/stocks` → Tüm aktif hisseler
- `GET /api/stocks/{symbol}` → Hisse detayı
- `GET /api/stocks/{symbol}/prices` → Fiyat geçmişi

Cache HIT durumunda response header'a `X-Cache: HIT` eklenir.

---

## Hata Hiyerarşisi

`ErrorHandlingMiddleware` şu exception tiplerini yakalar:
- `NotFoundException` → 404
- `ConflictException` → 409 (ör: email zaten kayıtlı)
- `UnauthorizedException` → 401 (ör: şifre yanlış, token geçersiz)
- `Exception` → 500

**Henüz eklenmeyenler:**
- `InsufficientDataException` → 400 (ML için min 30 gün veri şartı)
- `ExternalServiceException` → 503 (ML veya Yahoo Finance erişilemez)

---

## Program.cs Middleware Sırası (Değiştirme!)

```
ErrorHandlingMiddleware → Swagger → HttpsRedirection
→ Authentication → Authorization → MapControllers
```

---

## Yahoo Finance Notu (Henüz Yazılmadı)

- BIST hisseleri için Yahoo sembolü: `THYAO` → `THYAO.IS` (`.IS` eklenir)
- v8 API kullanılıyor: `query1.finance.yahoo.com/v8/finance/chart/{symbol}`
- Unix timestamp parametreleri: `period1`, `period2`
- İstekler arası 500ms beklenir (rate limiting)

---

## ML Servisi Kontrat

Python servisi bu endpoint'i sunar:
- `GET /health` → `{ status, model_loaded, model_version }`
- `POST /predict` → Body: `PredictionRequest`, Response: `PredictionResponse`

**Kısıtlar:**
- `forecast_days`: sadece 5, 10 veya 30 kabul edilir
- `historical_data`: minimum 30 kayıt gerekli (ideal 60 gün)
- Model sliding window ile tahmin üretir (Close fiyatlar üzerinden MinMaxScaler)

---

## appsettings.json Yapısı (Mevcut)

```json
{
  "ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=predixus;Username=postgres;Password=YOUR_PASSWORD" },
  "Jwt": { "SecretKey": "YOUR_SECRET_KEY_MIN_32_CHARS", "Issuer": "predixus-api", "Audience": "predixus-client", "ExpirationMinutes": 60, "RefreshTokenExpirationDays": 7 }
}
```

**Eklenecekler:**
```json
{
  "MlService": { "BaseUrl": "http://predixus-ml:8000", "TimeoutSeconds": 30, "RetryCount": 2 },
  "YahooFinance": { "BaseUrl": "https://query1.finance.yahoo.com", "RequestDelayMs": 500 },
  "Cache": { "PredictionTtlHours": 24, "StockPriceTtlMinutes": 60, "HistoryTtlHours": 4 }
}
```

---

## EF Core Migration Komutları

```bash
# Yeni migration oluştur
dotnet ef migrations add <MigrationName> \
  --project src/Predixus.Infrastructure \
  --startup-project src/Predixus.API

# Uygula (PostgreSQL çalışıyor olmalı)
dotnet ef database update \
  --project src/Predixus.Infrastructure \
  --startup-project src/Predixus.API
```

---

## Geliştirme Başlatma

```bash
# PostgreSQL'i başlat (docker varsa)
docker compose up predixus-db -d

# .NET API'yi hot reload ile başlat
dotnet watch run --project src/Predixus.API

# Swagger: http://localhost:5000/swagger
```

---

## Kod Üretirken Uyulacak Kurallar

1. **Her yeni entity:** `BaseEntity`'den türet, `private set`, `private Entity() { }`, `static Create()` factory ekle
2. **Her yeni servis:** Önce `Application/Interfaces/` altına interface yaz, sonra implement et
3. **Her yeni repository:** `Domain/Interfaces/` altında interface, `Infrastructure/Persistence/Repositories/` altında impl
4. **Her yeni DTO:** `record` tipi kullan, `Application/DTOs/` altına koy
5. **Controller:** `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]` standart
6. **EF Config:** Hesaplanmış property'ler için `builder.Ignore()` kullan
7. **Redis erişimi:** Her zaman try/catch, exception fırlatma, sadece logla
8. **ML/Yahoo Finance çağrısı:** `ExternalServiceException` ile wrap et
9. **Async:** Tüm metotlar `async Task<T>`, parametre olarak `CancellationToken ct = default`
10. **Log:** `ILogger<T>` inject et, structured logging kullan (`{Symbol}`, `{Days}` gibi)
11. **Adım adım ilerle:** Her dosyayı açıkla, onay bekle, sonra yaz

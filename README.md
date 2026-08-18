# Proje: Buduns Social Platform

## Kısa Özet

Buduns, blog altyapısı üzerine kurulan sosyal platform API'sidir. Proje; kullanıcıların içerik üretmesini, birbirini takip etmesini, postlara yorum/like/bookmark bırakmasını, bildirim almasını ve uygunsuz içerikleri raporlamasını sağlar. Backend tarafında CQRS + MediatR, JWT tabanlı kimlik doğrulama, refresh-token oturum yönetimi, role-based authorization, PostgreSQL , Serilog logging ve test edilebilir katmanlı mimari birlikte uygulanmıştır.

## Projenin Özellikleri

Teknolojiler: .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, Redis, ASP.NET Core Identity, MediatR, CQRS, FluentValidation, JWT Bearer Authentication, Serilog, Seq, Swagger, xUnit, Testcontainers

Mimari: Onion Architecture / katmanlı backend yapısı: Domain, Application, Persistence, Infrastructure ve WebAPI

Uygulama Akışı:

- CQRS tabanlı command/query yapısı
- MediatR ile controller -> handler akışı
- FluentValidation ile feature bazlı validator yapısı
- MediatR pipeline içinde validation, logging, current user ve account status kontrolleri
- İstisnasız her cevap ortak `ApiResponse` zarfında döner: `{ isSuccess, data, error, traceId }`
- Açılışta seeder'lar çalışır: sistem rolleri, ilk admin, endpoint yetki kataloğu ve mail şablonları
- `daily-top50` gibi ağır sorgular ve endpoint yetkileri Redis'te önbelleklenir

Kimlik ve Güvenlik:

- ASP.NET Core Identity
- JWT access token
- Refresh token ve oturum yönetimi
- Email doğrulama, email değiştirme ve şifre sıfırlama akışları
- Admin, Moderator ve kullanıcı rolleri
- Yetki tanımı ve endpoint bazlı rol eşleştirme
- Hassas endpointler için rate limiting

Sosyal Platform Özellikleri:

- Kullanıcı kayıt, giriş ve profil yönetimi
- Post oluşturma, güncelleme, silme, listeleme ve detay getirme
- Tag yönetimi ve tag'e göre post listeleme
- Yorum oluşturma, güncelleme, silme ve listeleme
- Like, bookmark ve takip sistemi
- Takip edilen kullanıcıların post akışı
- Günlük popüler post listesi
- Kullanıcı bildirimleri ve okunma durumu yönetimi
- Post, yorum ve kullanıcı raporlama
- Admin/Moderator için rapor inceleme ve moderasyon aksiyonları

Logging ve Gözlemlenebilirlik:

- Serilog ile dosya loglama
- PostgreSQL logs tablosuna warning/error seviyesinde log yazma
- Seq entegrasyonu
- Kullanıcı adı enrichment
- Global exception middleware ile standart hata cevapları



## Depo Yapısı

- `buduns-server/` — .NET 8 API (bu dokümanın konusu)
- `buduns-web/` — Vite + React 19 istemci
- `docker-compose.yml` — PostgreSQL, Redis, Seq ve API'yi birlikte ayağa kaldırır. Backend kodu değiştiyse `--build` ile çalıştırılmalı

## Katmanlar

### WebAPI

- Dış dünyaya açılan HTTP API katmanıdır.
- Controller'lar üzerinden client isteklerini alır.
- İstekleri doğrudan iş mantığına sokmak yerine MediatR command/query handler'larına yönlendirir.
- Swagger, JWT Bearer authentication, CORS, authorization, rate limiting, request logging ve global exception middleware burada yapılandırılır.

### Application

- Projenin use-case katmanıdır.
- CQRS feature klasörleri bu katmanda bulunur.
- Command, query, handler, response ve validator yapıları burada organize edilir.
- MediatR pipeline davranışları bu katmanda çalışır:
  - `ValidationBehavior`
  - `LoggingBehavior`
  - `CurrentUserBehavior`
  - `AccountStatusBehavior`
- DTO'lar, elle yazılan mapping'ler, servis arayüzleri, repository arayüzleri ve uygulama exception'ları bu katmanda yer alır.
- Bu katmana EF Core referansı eklenmez; veri erişimi `IUnitOfWork` ve repository soyutlamaları üzerinden yapılır.

### Domain

- Projenin temel entity ve enum modellerini içerir.
- User, Role, Post, Comment, Like, Follower, Bookmark, Tag, Notification, Report, ModerationAction, AuthSession ve VerificationChallenge gibi domain modelleri burada bulunur.
- İş alanının temel veri yapısını framework bağımlılığı olmadan temsil eder.

### Persistence

- PostgreSQL veritabanı erişim katmanıdır.
- `BudunsDbContext`, EF Core entity konfigürasyonları ve migration dosyaları burada bulunur.
- Repository implementasyonları, Unit of Work, Identity servisleri, auth session yönetimi, notification servisi ve verification challenge servisi bu katmanda yer alır.
- ASP.NET Core Identity, PostgreSQL provider ile bu katmanda yapılandırılır.

### Infrastructure

- Dış dünya ile ilişkili teknik servisleri içerir.
- JWT token üretimi, mail gönderimi ve uygulama endpoint/authorization tanımlarını okuyan servisler burada bulunur.

### Tests

- Unit test ve integration test projelerini içerir.
- Unit testlerde handler, validator, middleware, behavior ve filter davranışları test edilir.
- Integration testlerde API, auth, kullanıcı, moderasyon ve veritabanı davranışları doğrulanır.
- Integration testler PostgreSQL için Testcontainers kullanır.

## Ana Modüller

### Auth

- Kullanıcı girişi yapar.
- Refresh token ile access token yeniler.
- Oturumları listeler, tek oturumu veya tüm oturumları sonlandırır.
- Şifre sıfırlama, email doğrulama ve email değiştirme akışlarını yönetir.
- JWT içindeki kullanıcı ve session bilgisi aktif oturumla eşleşmediğinde isteği reddeder.

Öne çıkan endpointler:

- `POST /api/Auth/login`
- `POST /api/Auth/refreshTokenLogin`
- `POST /api/Auth/forgotPassword`
- `POST /api/Auth/mailVerify`
- `POST /api/Auth/emailChange`
- `POST /api/Auth/logout`
- `POST /api/Auth/logoutAll`
- `GET /api/Auth/sessions`
- `DELETE /api/Auth/sessions/{sessionId}`

### User

- Kullanıcı kaydı, profil güncelleme, email güncelleme ve şifre güncelleme işlemlerini yapar.
- Kullanıcıları id veya kullanıcı adına göre getirir.
- Admin kullanıcılar için kullanıcı listeleme, role bakma ve role atama işlemleri sağlar.

Öne çıkan endpointler:

- `POST /api/User/register`
- `POST /api/User/updatePassword`
- `POST /api/User/updateMailVerify`
- `POST /api/User/updateUserProfile`
- `POST /api/User/updateUserEmail`
- `GET /api/User/getUserById/{userId}`
- `GET /api/User/getUserByUsername/{userName}`
- `GET /api/User/getAllUsers`
- `POST /api/User/assignRoleToUser`

### Post

- Post oluşturma, güncelleme, silme, listeleme ve detay getirme işlemlerini yapar.
- Kullanıcının kendi postlarını, belirli kullanıcının postlarını, tag'e bağlı postları ve takip edilen kullanıcıların postlarını listeleyebilir.
- Günlük en popüler postlar için ayrı endpoint sunar.

Öne çıkan endpointler:

- `POST /api/Post/create`
- `PUT /api/Post/update`
- `DELETE /api/Post/delete`
- `GET /api/Post/getAll`
- `GET /api/Post/getById/{id}`
- `GET /api/Post/tag/{tagId}`
- `GET /api/Post/me`
- `GET /api/Post/user/{userId}`
- `GET /api/Post/following`
- `GET /api/Post/daily-top50`

### Comment

- Postlara yorum ekler.
- Yorum güncelleme ve silme işlemlerinde yetkili kullanıcı kontrolü uygular.
- Yorumu id, post veya kullanıcı bazlı listeleyebilir.

Öne çıkan endpointler:

- `POST /api/Comment`
- `PUT /api/Comment/{id}`
- `DELETE /api/Comment/{id}`
- `GET /api/Comment/{id}`
- `GET /api/Comment/post/{postId}`
- `GET /api/Comment/user/{userId}`

### Like, Bookmark ve Follower

- Kullanıcıların post beğenme, post kaydetme ve diğer kullanıcıları takip etme davranışlarını yönetir.
- Tekrarlı like/bookmark/follow durumları veritabanı seviyesindeki unique indexlerle korunur.
- Client tarafı için status endpointleri sağlar.

Öne çıkan endpointler:

- `POST /api/Like/{postId}`
- `DELETE /api/Like/{postId}`
- `GET /api/Like/status/{postId}`
- `GET /api/Like/me`
- `POST /api/Bookmark`
- `DELETE /api/Bookmark/{postId}`
- `GET /api/Bookmark`
- `GET /api/Bookmark/status/{postId}`
- `POST /api/Follower/{userId}`
- `DELETE /api/Follower/{userId}`
- `GET /api/Follower/{userId}/followers`
- `GET /api/Follower/{userId}/followings`
- `GET /api/Follower/status/{userId}`

### Notification

- Kullanıcı bildirimlerini listeler.
- Okunmamış bildirim sayısını getirir.
- Tek bildirimi veya tüm bildirimleri okundu olarak işaretler.
- Bildirim silme işlemini destekler.

Öne çıkan endpointler:

- `GET /api/Notification`
- `GET /api/Notification/unread-count`
- `PATCH /api/Notification/{id}/read`
- `PATCH /api/Notification/read-all`
- `DELETE /api/Notification/{id}`

### Report ve Moderasyon

- Kullanıcılar post, yorum veya kullanıcı raporlayabilir.
- Admin ve Moderator rolleri raporları listeleyebilir, detaylarını görebilir ve inceleme sonucunu işleyebilir.
- Rapor tekrarları, hedef türü doğruluğu ve moderasyon state'i veritabanı kurallarıyla desteklenir.

Öne çıkan endpointler:

- `POST /api/Report/createPostReport`
- `POST /api/Report/createCommentReport`
- `POST /api/Report/createUserReport`
- `GET /api/Report`
- `GET /api/Report/getById/{reportId}`
- `POST /api/Report/review`

### Role ve Authorization Endpoints

- Admin kullanıcılar rol CRUD işlemlerini yapabilir.
- Uygulamadaki authorization tanımları okunabilir.
- Endpointlere rol ataması yapılabilir.

Öne çıkan endpointler:

- `GET /api/Role/getAll`
- `GET /api/Role/getById/{id}`
- `POST /api/Role/create`
- `PUT /api/Role/update`
- `DELETE /api/Role/delete/{id}`
- `GET /api/ApplicationService`
- `POST /api/AuthorizationEndpoints/getRolesToEndpoint`
- `POST /api/AuthorizationEndpoints`







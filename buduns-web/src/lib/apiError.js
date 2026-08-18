// Backend'in ApiResponse zarfindaki error nesnesini, uygulamanin her yerinde
// ayni sekilde kullanilan tek tip bir nesneye cevirir.

export const ApiErrorCodes = {
  Validation: 'VALIDATION_ERROR',
  BadRequest: 'BAD_REQUEST',
  RegisterFailed: 'REGISTER_FAILED',
  PasswordChangeFailed: 'PASSWORD_CHANGE_FAILED',
  MailVerifyFailed: 'MAIL_VERIFY_FAILED',
  EmailChangeFailed: 'EMAIL_CHANGE_FAILED',
  Unauthorized: 'UNAUTHORIZED',
  UnauthorizedAccess: 'UNAUTHORIZED_ACCESS',
  InvalidRefreshToken: 'INVALID_REFRESH_TOKEN',
  Forbidden: 'FORBIDDEN',
  EmailVerificationRequired: 'EMAIL_VERIFICATION_REQUIRED',
  ResourceNotFound: 'RESOURCE_NOT_FOUND',
  NotFound: 'NOT_FOUND',
  MethodNotAllowed: 'METHOD_NOT_ALLOWED',
  ConcurrencyConflict: 'CONCURRENCY_CONFLICT',
  RateLimitExceeded: 'RATE_LIMIT_EXCEEDED',
  TooManyRequests: 'TOO_MANY_REQUESTS',
  InternalServerError: 'INTERNAL_SERVER_ERROR',

  // Backend'den gelmez, istemcide uretilir.
  Network: 'NETWORK_ERROR',
  Canceled: 'REQUEST_CANCELED',
  Unknown: 'UNKNOWN_ERROR',
}

const MESSAGES = {
  [ApiErrorCodes.Validation]: 'Girdiginiz bilgilerde hata var.',
  [ApiErrorCodes.BadRequest]: 'Istek islenemedi.',
  [ApiErrorCodes.RegisterFailed]: 'Kayit tamamlanamadi.',
  [ApiErrorCodes.PasswordChangeFailed]: 'Sifre degistirilemedi.',
  [ApiErrorCodes.MailVerifyFailed]: 'E-posta dogrulanamadi.',
  [ApiErrorCodes.EmailChangeFailed]: 'E-posta degistirilemedi.',
  [ApiErrorCodes.Unauthorized]: 'Bu islem icin giris yapmalisiniz.',
  [ApiErrorCodes.UnauthorizedAccess]: 'Bu islem icin yetkiniz yok.',
  [ApiErrorCodes.InvalidRefreshToken]: 'Oturumunuz sona erdi. Tekrar giris yapin.',
  [ApiErrorCodes.Forbidden]: 'Bu islem icin yetkiniz yok.',
  [ApiErrorCodes.EmailVerificationRequired]: 'Bu islem icin e-posta adresinizi dogrulamalisiniz.',
  [ApiErrorCodes.ResourceNotFound]: 'Aradiginiz kayit bulunamadi.',
  [ApiErrorCodes.NotFound]: 'Aradiginiz sayfa bulunamadi.',
  [ApiErrorCodes.MethodNotAllowed]: 'Bu istek desteklenmiyor.',
  [ApiErrorCodes.ConcurrencyConflict]: 'Kayit baska bir islem tarafindan guncellendi. Yenileyin.',
  [ApiErrorCodes.RateLimitExceeded]: 'Cok fazla istek gonderdiniz. Biraz bekleyin.',
  [ApiErrorCodes.TooManyRequests]: 'Cok fazla istek gonderdiniz. Biraz bekleyin.',
  [ApiErrorCodes.InternalServerError]: 'Beklenmeyen bir hata olustu.',
  [ApiErrorCodes.Network]: 'Sunucuya ulasilamadi. Baglantinizi kontrol edin.',
  [ApiErrorCodes.Canceled]: 'Istek iptal edildi.',
  [ApiErrorCodes.Unknown]: 'Beklenmeyen bir hata olustu.',
}

export function messageForCode(code, fallback) {
  const known = MESSAGES[code]
  if (known) return known

  if (import.meta.env?.DEV) {
    console.warn(`[apiError] Sozlukte olmayan hata kodu: ${code}`)
  }
  return fallback || MESSAGES[ApiErrorCodes.Unknown]
}

// Redux'ta saklanabilmesi icin duz nesne dondurur; Error sinifi degil.
function createApiError({ code, message, validationErrors, traceId, status }) {
  return {
    code: code || ApiErrorCodes.Unknown,
    message: messageForCode(code, message),
    validationErrors: validationErrors || null,
    traceId: traceId || null,
    status: status ?? null,
  }
}

// ApiResponse zarfindan (HTTP 200 gelse bile isSuccess=false ise) hata uretir.
export function apiErrorFromEnvelope(envelope, status) {
  return createApiError({
    code: envelope?.error?.code,
    message: envelope?.error?.message,
    validationErrors: envelope?.error?.validationErrors,
    traceId: envelope?.traceId,
    status,
  })
}

export function toApiError(error) {
  if (error?.code && error?.message && 'traceId' in error) return error

  if (error?.name === 'CanceledError' || error?.code === 'ERR_CANCELED') {
    return createApiError({ code: ApiErrorCodes.Canceled })
  }

  // Cevap hic gelmediyse (backend kapali, DNS, CORS reddi) response yoktur.
  if (!error?.response) {
    return createApiError({ code: ApiErrorCodes.Network })
  }

  const { status, data } = error.response

  // Zarfsiz bir cevap geldiyse (proxy hatasi, HTML sayfasi) koda dusulur.
  if (!data || typeof data !== 'object' || !('isSuccess' in data)) {
    return createApiError({ code: statusToCode(status), status })
  }

  return apiErrorFromEnvelope(data, status)
}

function statusToCode(status) {
  if (status === 401) return ApiErrorCodes.Unauthorized
  if (status === 403) return ApiErrorCodes.Forbidden
  if (status === 404) return ApiErrorCodes.NotFound
  if (status === 405) return ApiErrorCodes.MethodNotAllowed
  if (status === 409) return ApiErrorCodes.ConcurrencyConflict
  if (status === 429) return ApiErrorCodes.RateLimitExceeded
  if (status >= 500) return ApiErrorCodes.InternalServerError
  return ApiErrorCodes.BadRequest
}

export function isCanceled(error) {
  return error?.code === ApiErrorCodes.Canceled
}

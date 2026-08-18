using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.Auth;
using buduns_server.Application.Exceptions;
using buduns_server.Domain.Entities;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace buduns_server.Persistence.Services
{
    public class AuthSessionService : IAuthSessionService
    {
        private readonly BudunsDbContext _context;
        private readonly IClientContext _clientContext;
        private readonly ILogger<AuthSessionService> _logger;

        public AuthSessionService(BudunsDbContext context, IClientContext clientContext, ILogger<AuthSessionService> logger)
        {
            _context = context;
            _clientContext = clientContext;
            _logger = logger;
        }

        public async Task CreateSessionAsync(int userId, Guid sessionId, Guid tokenFamilyId, string refreshToken, DateTime expiresAt, CancellationToken cancellationToken)
        {
            var session = CreateSession(userId, sessionId, tokenFamilyId, refreshToken, expiresAt);
            await _context.AuthSessions.AddAsync(session, cancellationToken);
            await _context.SaveTranslatedAsync(cancellationToken);
        }

        public async Task<AuthSessionRotationResult> RotateSessionAsync(string currentRefreshToken, Guid replacementSessionId, string replacementRefreshToken, DateTime replacementExpiresAt, CancellationToken cancellationToken)
        {
            var currentHash = HashRefreshToken(currentRefreshToken);
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var currentSession = await _context.AuthSessions.FirstOrDefaultAsync(session => session.RefreshTokenHash == currentHash, cancellationToken);

            if (currentSession == null)
            {
                throw new InvalidRefreshTokenException("Refresh token gecersiz veya suresi dolmus.");
            }

            if (currentSession.RevokedAt.HasValue)
            {
                await RevokeTokenFamilyAsync(currentSession.TokenFamilyId, "Refresh token reuse detected", cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogWarning(
                    "Refresh token reuse detected. UserId: {UserId}, TokenFamilyId: {TokenFamilyId}",
                    currentSession.UserId,
                    currentSession.TokenFamilyId);

                throw new InvalidRefreshTokenException("Refresh token gecersiz veya daha once kullanilmis.");
            }

            if (currentSession.ExpiresAt <= DateTime.UtcNow)
            {
                currentSession.RevokedAt = DateTime.UtcNow;
                currentSession.RevokedReason = "Expired";
                await _context.SaveTranslatedAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new InvalidRefreshTokenException("Refresh token gecersiz veya suresi dolmus.");
            }

            var now = DateTime.UtcNow;
            currentSession.LastUsedAt = now;
            currentSession.RevokedAt = now;
            currentSession.RevokedReason = "Rotated";
            currentSession.ReplacedBySessionId = replacementSessionId;

            var replacement = CreateSession(currentSession.UserId, replacementSessionId, currentSession.TokenFamilyId, replacementRefreshToken, replacementExpiresAt);

            await _context.AuthSessions.AddAsync(replacement, cancellationToken);
            await _context.SaveTranslatedAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AuthSessionRotationResult(currentSession.UserId, replacementSessionId);
        }

        public Task<bool> IsSessionActiveAsync(int userId, Guid sessionId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            return _context.AuthSessions.AsNoTracking().AnyAsync( session => 
                session.Id == sessionId &&
                session.UserId == userId &&
                session.RevokedAt == null &&
                session.ExpiresAt > now,
                cancellationToken);
        }

        public async Task<IReadOnlyList<AuthSessionDto>> GetActiveSessionsAsync(int userId, Guid currentSessionId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            return await _context.AuthSessions
                .AsNoTracking()
                .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > now)
                .OrderByDescending(session => session.LastUsedAt ?? session.CreatedAt)
                .Select(session => new AuthSessionDto
                {
                    Id = session.Id,
                    DeviceName = session.DeviceName,
                    IpAddress = session.IpAddress,
                    CreatedAt = session.CreatedAt,
                    LastUsedAt = session.LastUsedAt,
                    ExpiresAt = session.ExpiresAt,
                    IsCurrent = session.Id == currentSessionId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> RevokeSessionAsync(int userId, Guid sessionId, string reason, CancellationToken cancellationToken)
        {
            var session = await _context.AuthSessions.FirstOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);

            if (session == null)
            {
                return false;
            }

            if (!session.RevokedAt.HasValue)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedReason = Limit(reason, 200);
                await _context.SaveTranslatedAsync(cancellationToken);

                _logger.LogInformation(
                    "Auth session revoked. UserId: {UserId}, SessionId: {SessionId}, Reason: {Reason}",
                    userId,
                    sessionId,
                    reason);
            }

            return true;
        }

        public async Task RevokeAllSessionsAsync(int userId, string reason, CancellationToken cancellationToken)
        {
            var activeSessions = await _context.AuthSessions
                .Where(session => session.UserId == userId && session.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
                session.RevokedReason = Limit(reason, 200);
            }

            if (activeSessions.Count > 0)
            {
                await _context.SaveTranslatedAsync(cancellationToken);

                _logger.LogInformation(
                    "All auth sessions revoked. UserId: {UserId}, SessionCount: {SessionCount}, Reason: {Reason}",
                    userId,
                    activeSessions.Count,
                    reason);
            }
        }

        private AuthSession CreateSession(int userId, Guid sessionId, Guid tokenFamilyId, string refreshToken, DateTime expiresAt) => new()
        {
            Id = sessionId,
            TokenFamilyId = tokenFamilyId,
            UserId = userId,
            RefreshTokenHash = HashRefreshToken(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            DeviceName = Limit(_clientContext.DeviceName ?? _clientContext.UserAgent, 100),
            UserAgent = Limit(_clientContext.UserAgent, 500),
            IpAddress = Limit(_clientContext.IpAddress, 64)
        };

        private async Task RevokeTokenFamilyAsync(Guid tokenFamilyId, string reason, CancellationToken cancellationToken)
        {
            var activeSessions = await _context.AuthSessions
                .Where(session => session.TokenFamilyId == tokenFamilyId && session.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
                session.RevokedReason = reason;
            }

            await _context.SaveTranslatedAsync(cancellationToken);
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }

        private static string? Limit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}

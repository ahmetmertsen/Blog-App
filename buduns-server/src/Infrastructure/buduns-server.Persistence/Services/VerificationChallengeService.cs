using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Exceptions;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace buduns_server.Persistence.Services
{
    public class VerificationChallengeService : IVerificationChallengeService
    {
        private readonly BudunsDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly JwtTokenOptions _tokenOptions;

        public VerificationChallengeService(BudunsDbContext context, IConfiguration configuration, IOptions<JwtTokenOptions> tokenOptions)
        {
            _context = context;
            _configuration = configuration;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<string> CreateCodeAsync(int userId, VerificationPurpose purpose, string? targetEmail, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var normalizedTargetEmail = NormalizeEmail(targetEmail);
            var activeChallenges = await _context.VerificationChallenges
                .Where(challenge => challenge.UserId == userId && challenge.Purpose == purpose && challenge.TargetEmail == normalizedTargetEmail && challenge.ConsumedAt == null && challenge.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            foreach (var activeChallenge in activeChallenges)
            {
                activeChallenge.ConsumedAt = now;
                activeChallenge.UpdateAt = now;
            }

            var code = GenerateCode();
            var challenge = new VerificationChallenge
            {
                UserId = userId,
                Purpose = purpose,
                TargetEmail = normalizedTargetEmail,
                CodeHash = HashCode(userId, purpose, normalizedTargetEmail, code),
                ExpiresAt = now.AddMinutes(GetExpirationMinutes()),
                MaxAttempts = GetMaxAttempts(),
                CreatedAt = now,
                UpdateAt = now,
                LastSentAt = now,
                isActive = true,
                isDeleted = false
            };

            await _context.VerificationChallenges.AddAsync(challenge, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return code;
        }

        public async Task ValidateCodeAsync(int userId, VerificationPurpose purpose, string? targetEmail, string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new BadRequestException("Doğrulama kodu boş olamaz.");
            }

            var now = DateTime.UtcNow;
            var normalizedTargetEmail = NormalizeEmail(targetEmail);
            var challenge = await _context.VerificationChallenges
                .Where(challenge => challenge.UserId == userId && challenge.Purpose == purpose && challenge.TargetEmail == normalizedTargetEmail && challenge.ConsumedAt == null && !challenge.isDeleted)
                .OrderByDescending(challenge => challenge.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (challenge == null || challenge.ExpiresAt <= now)
            {
                throw new BadRequestException("Doğrulama kodu geçersiz veya süresi dolmuş.");
            }

            if (challenge.AttemptCount >= challenge.MaxAttempts)
            {
                throw new TooManyRequestsException("Doğrulama kodu için maksimum deneme hakkı aşıldı.");
            }

            var expectedHash = HashCode(userId, purpose, normalizedTargetEmail, code.Trim());
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(challenge.CodeHash), Convert.FromHexString(expectedHash)))
            {
                challenge.AttemptCount++;
                challenge.UpdateAt = now;
                await _context.SaveChangesAsync(cancellationToken);
                throw new BadRequestException("Doğrulama kodu hatalı.");
            }

            challenge.ConsumedAt = now;
            challenge.UpdateAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ValidateEmailChangeCodesAsync(int userId, string targetEmail, string oldEmailCode, string newEmailCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(oldEmailCode) || string.IsNullOrWhiteSpace(newEmailCode))
            {
                throw new BadRequestException("Doğrulama kodları boş olamaz.");
            }

            var now = DateTime.UtcNow;
            var normalizedTargetEmail = NormalizeEmail(targetEmail);
            var oldEmailChallenge = await GetActiveChallengeAsync(userId, VerificationPurpose.EmailChangeOld, normalizedTargetEmail, cancellationToken);
            var newEmailChallenge = await GetActiveChallengeAsync(userId, VerificationPurpose.EmailChangeNew, normalizedTargetEmail, cancellationToken);

            EnsureChallengeIsUsable(oldEmailChallenge, now);
            EnsureChallengeIsUsable(newEmailChallenge, now);

            var oldEmailCodeMatches = IsCodeMatch(oldEmailChallenge, userId, VerificationPurpose.EmailChangeOld, normalizedTargetEmail, oldEmailCode.Trim());
            var newEmailCodeMatches = IsCodeMatch(newEmailChallenge, userId, VerificationPurpose.EmailChangeNew, normalizedTargetEmail, newEmailCode.Trim());

            if (!oldEmailCodeMatches || !newEmailCodeMatches)
            {
                MarkFailedAttempt(oldEmailChallenge, oldEmailCodeMatches, now);
                MarkFailedAttempt(newEmailChallenge, newEmailCodeMatches, now);
                await _context.SaveChangesAsync(cancellationToken);
                throw new BadRequestException("Doğrulama kodları hatalı.");
            }

            oldEmailChallenge.ConsumedAt = now;
            oldEmailChallenge.UpdateAt = now;
            newEmailChallenge.ConsumedAt = now;
            newEmailChallenge.UpdateAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string GenerateCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString("D6");
        }

        private string HashCode(int userId, VerificationPurpose purpose, string? targetEmail, string code)
        {
            var secret = _configuration["VerificationCode:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = _tokenOptions.SecurityKey;
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("VerificationCode:Secret veya Token:SecurityKey yapılandırması tanımlı olmalıdır.");
            }

            var payload = $"{userId}:{purpose}:{targetEmail ?? string.Empty}:{code}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private async Task<VerificationChallenge> GetActiveChallengeAsync(int userId, VerificationPurpose purpose, string? targetEmail, CancellationToken cancellationToken)
        {
            var challenge = await _context.VerificationChallenges
                .Where(challenge => challenge.UserId == userId && challenge.Purpose == purpose && challenge.TargetEmail == targetEmail && challenge.ConsumedAt == null && !challenge.isDeleted)
                .OrderByDescending(challenge => challenge.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (challenge == null)
            {
                throw new BadRequestException("Doğrulama kodu geçersiz veya süresi dolmuş.");
            }

            return challenge;
        }

        private static void EnsureChallengeIsUsable(VerificationChallenge challenge, DateTime now)
        {
            if (challenge.ExpiresAt <= now)
            {
                throw new BadRequestException("Doğrulama kodu geçersiz veya süresi dolmuş.");
            }

            if (challenge.AttemptCount >= challenge.MaxAttempts)
            {
                throw new TooManyRequestsException("Doğrulama kodu için maksimum deneme hakkı aşıldı.");
            }
        }

        private bool IsCodeMatch(VerificationChallenge challenge, int userId, VerificationPurpose purpose, string? targetEmail, string code)
        {
            var expectedHash = HashCode(userId, purpose, targetEmail, code);
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(challenge.CodeHash), Convert.FromHexString(expectedHash));
        }

        private static void MarkFailedAttempt(VerificationChallenge challenge, bool codeMatches, DateTime now)
        {
            if (codeMatches)
            {
                return;
            }

            challenge.AttemptCount++;
            challenge.UpdateAt = now;
        }

        private int GetExpirationMinutes()
        {
            return int.TryParse(_configuration["VerificationCode:ExpirationMinutes"], out var minutes) && minutes > 0 ? minutes : 10;
        }

        private int GetMaxAttempts()
        {
            return int.TryParse(_configuration["VerificationCode:MaxAttempts"], out var attempts) && attempts > 0 ? attempts : 5;
        }

        private static string? NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        }
    }
}

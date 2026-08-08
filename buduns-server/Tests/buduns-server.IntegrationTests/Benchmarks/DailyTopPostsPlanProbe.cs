using buduns_server.Domain.Enums;
using buduns_server.Persistence.Context;
using buduns_server.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using Testcontainers.PostgreSql;

namespace buduns_server.IntegrationTests.Benchmarks;

/// <summary>
/// Dogrulama testi degil, olcum aracidir. Kendi Postgres container'ini kurar,
/// sentetik veri yukler, EF'in urettigi gercek SQL'i yakalar ve indeks
/// varyantlarini EXPLAIN ANALYZE ile karsilastirir.
///
/// Normal test kosusunda calismaz; ~35 saniye surer ve bir seyi dogrulamaz,
/// olcer. Calistirmak icin cikti yolunu vermek gerekir:
///
///   PLAN_PROBE_OUTPUT=/tmp/plan.txt dotnet test \
///     --filter "FullyQualifiedName~DailyTopPostsPlanProbe"
///
/// Faz 2 kararlari bu araci n ciktisina dayaniyor; Faz 3'te sorgu yeniden
/// yazildiginda ayni olcum tekrarlanmali.
/// </summary>
public sealed class DailyTopPostsPlanProbe : IAsyncLifetime
{
    private static readonly string? OutputPath = Environment.GetEnvironmentVariable("PLAN_PROBE_OUTPUT");
    private static bool Enabled => !string.IsNullOrWhiteSpace(OutputPath);

    private const int UserCount = 5_000;
    private const int PostCount = 20_000;
    private const int LikeCount = 200_000;
    private const int CommentCount = 60_000;
    private const int BookmarkCount = 40_000;
    private const int TodayLikeCount = 3_000;
    private const int TodayCommentCount = 1_000;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("buduns_plan_probe")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (!Enabled)
        {
            return;
        }

        await _postgres.StartAsync();
        await using var context = CreateContext(interceptor: null);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Enabled ? _postgres.DisposeAsync().AsTask() : Task.CompletedTask;

    [Fact]
    public async Task Compare_index_variants()
    {
        if (!Enabled)
        {
            return;
        }

        var outputPath = OutputPath!;
        var report = new StringBuilder();

        var (startDateUtc, endDateUtc) = TurkeyDayWindowUtc();
        await SeedAsync(startDateUtc);

        var legacyCommands = await CaptureAsync(LegacyImplementation, startDateUtc, endDateUtc);
        var currentCommands = await CaptureAsync(CurrentImplementation, startDateUtc, endDateUtc);

        var scenarios = new (string Name, List<CapturedCommand> Commands, string Setup, bool JitEnabled)[]
        {
            ("1. Faz 1 oncesi: eski sorgu, mig_23/24 yok, jit=on", legacyCommands, PreFaz2Schema, true),
            ("2. Faz 2 sonu: eski sorgu, mig_23, jit=off", legacyCommands, WithoutMig24, false),
            ("3. Faz 3 SEVK EDILEN: yeni sorgu, mig_23+24, jit=off", currentCommands, "", false),
            ("4. yeni sorgu, mig_23+24, jit=on", currentCommands, "", true),
            ("5. yeni sorgu, mig_24 yok, jit=off", currentCommands, WithoutMig24, false),
            ("6. yeni sorgu, mig_23 yok, jit=off", currentCommands, WithoutMig23, false),
            ("7. yeni sorgu, mig_23/24 yok, jit=off", currentCommands, PreFaz2Schema, false)
        };

        var summary = new StringBuilder();
        summary.AppendLine("| Senaryo | Komut | Toplam yurutme | JIT | Toplam buffer | Kullanilan indeksler |");
        summary.AppendLine("|---|---|---|---|---|---|");

        foreach (var (name, commands, setup, jitEnabled) in scenarios)
        {
            await ResetSchemaAsync();
            if (!string.IsNullOrEmpty(setup))
            {
                await ExecuteAsync(setup);
            }

            // VACUUM transaction icinde calismaz; Npgsql cok ifadeli komutu
            // transaction'a sardigi icin her biri ayri gonderiliyor.
            await ExecuteAsync("""VACUUM (ANALYZE) "Likes";""");
            await ExecuteAsync("""VACUUM (ANALYZE) "Comments";""");
            await ExecuteAsync("""VACUUM (ANALYZE) "Posts";""");

            double totalExecution = 0;
            double totalJit = 0;
            var totalBuffers = 0;
            var usedIndexes = new SortedSet<string>();

            report.AppendLine($"########## {name} ##########");
            foreach (var command in commands)
            {
                var plan = await ExplainAsync(command, jitEnabled);

                totalExecution += ParseDouble(plan, @"Execution Time: ([\d.]+) ms");
                totalJit += ParseDouble(plan, @"Timing: .*Total ([\d.]+) ms");
                totalBuffers += plan.Split('\n')
                    .Select(line => Regex.Match(line, @"Buffers: shared hit=(\d+)"))
                    .Where(match => match.Success)
                    .Select(match => int.Parse(match.Groups[1].Value))
                    .DefaultIfEmpty(0)
                    .Max();

                foreach (Match match in Regex.Matches(plan, @"(?:Index (?:Only )?Scan using|Bitmap Index Scan on) ""?((?:IX|UX|PK)_[^""\s]*)"))
                {
                    usedIndexes.Add(match.Groups[1].Value);
                }

                report.AppendLine(Summarize(plan));
                report.AppendLine("---");
            }

            report.AppendLine();

            var relevantIndexes = usedIndexes.Where(index => index.Contains("Likes") || index.Contains("Comments"));
            summary.AppendLine($"| {name} | {commands.Count} | {totalExecution:F1} ms | {(totalJit > 0 ? $"{totalJit:F1} ms" : "-")} | {totalBuffers} | {string.Join(", ", relevantIndexes)} |");
        }

        var final = new StringBuilder();
        final.AppendLine("=== OZET ===");
        final.AppendLine(summary.ToString());
        final.AppendLine();
        final.AppendLine($"=== YENI SORGU: {currentCommands.Count} komut ===");
        foreach (var command in currentCommands)
        {
            final.AppendLine(command.Text);
            final.AppendLine("---");
        }

        final.AppendLine();
        final.AppendLine($"=== ESKI SORGU: {legacyCommands.Count} komut ===");
        foreach (var command in legacyCommands)
        {
            final.AppendLine(command.Text);
        }

        final.AppendLine();
        final.Append(report);

        await File.WriteAllTextAsync(outputPath, final.ToString());
    }

    private Task CurrentImplementation(BudunsDbContext context, DateTime startDateUtc, DateTime endDateUtc) =>
        new PostRepository(context).GetDailyTopPostsAsync(startDateUtc, endDateUtc, 50, CancellationToken.None);

    /// <summary>
    /// Faz 3 oncesi implementasyon. Karsilastirmanin durust olmasi icin SQL
    /// elle kopyalanmadi; ayni LINQ burada tutulup EF'e yeniden cevirtiliyor.
    /// </summary>
    private static async Task LegacyImplementation(BudunsDbContext context, DateTime startDateUtc, DateTime endDateUtc)
    {
        await context.Posts
            .Where(post => post.Status == PostStatus.Published && post.isPublished && post.isActive && !post.isDeleted && post.User.Status != UserStatus.Banned)
            .AsNoTracking()
            .Select(post => new
            {
                Post = post,
                DailyLikeCount = post.Likes.Count(like => like.UserId != post.UserId && like.User.Status != UserStatus.Banned && like.CreatedAt >= startDateUtc && like.CreatedAt < endDateUtc && like.isActive && !like.isDeleted),
                DailyCommentCount = post.Comments.Count(comment => comment.User.Status != UserStatus.Banned && comment.CreatedAt >= startDateUtc && comment.CreatedAt < endDateUtc && comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted),
                LikeCount = post.Likes.Count(like => like.User.Status != UserStatus.Banned && like.isActive && !like.isDeleted),
                CommentCount = post.Comments.Count(comment => comment.User.Status != UserStatus.Banned && comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted),
                BookmarkCount = post.Bookmarks.Count(bookmark => bookmark.isActive && !bookmark.isDeleted)
            })
            .Where(item => item.DailyLikeCount > 0 || item.DailyCommentCount > 0)
            .OrderByDescending(item => (item.DailyLikeCount * 0.4) + (item.DailyCommentCount * 0.6))
            .ThenByDescending(item => item.DailyCommentCount)
            .ThenByDescending(item => item.DailyLikeCount)
            .ThenByDescending(item => item.Post.CreatedAt)
            .Take(50)
            .Select(item => new { item.Post.Id, item.DailyLikeCount, item.DailyCommentCount, item.LikeCount, item.CommentCount, item.BookmarkCount })
            .ToListAsync();
    }

    private static double ParseDouble(string text, string pattern)
    {
        var value = Match(text, pattern);
        return value == null ? 0 : double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Plani kisaltir: sadece ust dugumler ve en pahali alt planlar.</summary>
    private static string Summarize(string plan)
    {
        var interesting = plan.Split('\n')
            .Where(line => Regex.IsMatch(line, @"(Execution Time|Planning Time|Timing:|Functions:|SubPlan \d+|Seq Scan|Bitmap Heap Scan on|Index Scan using|Index Only Scan using|Heap Blocks|Rows Removed by Filter|^\s*->\s+(Limit|Sort|Nested Loop|Result))"))
            .Select(line => line.TrimEnd());

        return string.Join('\n', interesting);
    }

    private static string? Match(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>mig_24'un ekledigi CreatedAt indekslerini kaldirir.</summary>
    private const string WithoutMig24 = """
        DROP INDEX IF EXISTS "IX_Likes_CreatedAt";
        DROP INDEX IF EXISTS "IX_Comments_CreatedAt";
        """;

    /// <summary>mig_23'u geri alir: genis indeks yerine tek kolonlu FK indeksi.</summary>
    private const string WithoutMig23 = """
        DROP INDEX IF EXISTS "IX_Likes_PostId_CreatedAt";
        CREATE INDEX IF NOT EXISTS "IX_Likes_PostId" ON "Likes" ("PostId");
        """;

    private const string PreFaz2Schema = WithoutMig24 + WithoutMig23;

    /// <summary>Her senaryo oncesi semayi sevk edilen haline geri getirir.</summary>
    private async Task ResetSchemaAsync() => await ExecuteAsync("""
        DROP INDEX IF EXISTS "IX_Likes_PostId";
        CREATE INDEX IF NOT EXISTS "IX_Likes_PostId_CreatedAt" ON "Likes" ("PostId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_Likes_CreatedAt" ON "Likes" ("CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_Comments_CreatedAt" ON "Comments" ("CreatedAt");
        """);

    private static (DateTime StartUtc, DateTime EndUtc) TurkeyDayWindowUtc()
    {
        var turkey = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul");
        var todayStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkey).Date;
        return (TimeZoneInfo.ConvertTimeToUtc(todayStart, turkey), TimeZoneInfo.ConvertTimeToUtc(todayStart.AddDays(1), turkey));
    }

    private BudunsDbContext CreateContext(CommandCapturingInterceptor? interceptor)
    {
        var builder = new DbContextOptionsBuilder<BudunsDbContext>().UseNpgsql(ConnectionString);
        if (interceptor != null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new BudunsDbContext(builder.Options);
    }

    private async Task<List<CapturedCommand>> CaptureAsync(Func<BudunsDbContext, DateTime, DateTime, Task> implementation, DateTime startDateUtc, DateTime endDateUtc)
    {
        var interceptor = new CommandCapturingInterceptor();
        await using var context = CreateContext(interceptor);

        await implementation(context, startDateUtc, endDateUtc);

        Assert.NotEmpty(interceptor.Commands);
        return interceptor.Commands;
    }

    private async Task<string> ExplainAsync(CapturedCommand captured, bool jitEnabled)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var jitCommand = new NpgsqlCommand($"SET jit = {(jitEnabled ? "on" : "off")};", connection))
        {
            await jitCommand.ExecuteNonQueryAsync();
        }

        // Ilk kosu onbellek isitir, olcum ikinci kosudan alinir.
        string plan = string.Empty;
        for (var run = 0; run < 2; run++)
        {
            await using var command = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, TIMING) " + captured.Text, connection) { CommandTimeout = 600 };
            foreach (var (name, value) in captured.Parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            var builder = new StringBuilder();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                builder.AppendLine(reader.GetString(0));
            }

            plan = builder.ToString();
        }

        return plan;
    }

    private async Task SeedAsync(DateTime startDateUtc)
    {
        await ExecuteAsync($"""
            INSERT INTO "AspNetUsers" (
                "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed",
                "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed",
                "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "FullName", "Status")
            SELECT
                'user' || i, 'USER' || i, 'user' || i || '@probe.test', 'USER' || i || '@PROBE.TEST', true,
                'hash', gen_random_uuid()::text, gen_random_uuid()::text, false,
                false, true, 0, 'User ' || i,
                CASE WHEN i % 50 = 0 THEN '{nameof(UserStatus.Banned)}' ELSE '{nameof(UserStatus.Active)}' END
            FROM generate_series(1, {UserCount}) AS i;

            INSERT INTO "Posts" ("Content", "isPublished", "Status", "UserId", "CreatedAt", "UpdateAt", "isActive", "isDeleted")
            SELECT
                'Probe post ' || i, true, {(int)PostStatus.Published}, (i % {UserCount}) + 1,
                now() - ((i % 700) + 1) * interval '1 day', now(), true, false
            FROM generate_series(1, {PostCount}) AS i;
            """);

        // Benzersiz (UserId, PostId) kisiti var: her paylasima farkli bir
        // kullanici blogu dusecek sekilde uretiliyor.
        await ExecuteAsync($"""
            INSERT INTO "Likes" ("PostId", "UserId", "CreatedAt", "UpdateAt", "isActive", "isDeleted")
            SELECT
                (i % {PostCount}) + 1,
                (((i / {PostCount}) * 499) % {UserCount}) + 1,
                CASE WHEN i < {TodayLikeCount}
                     THEN @todayStart + ((i % 20) * interval '1 hour')
                     ELSE now() - ((i % 700) + 1) * interval '1 day' END,
                now(), true, false
            FROM generate_series(0, {LikeCount - 1}) AS i;

            INSERT INTO "Comments" ("PostId", "UserId", "Content", "Status", "CreatedAt", "UpdateAt", "isActive", "isDeleted")
            SELECT
                (i % {PostCount}) + 1,
                (((i / {PostCount}) * 331) % {UserCount}) + 1,
                'Probe comment ' || i,
                {(int)CommentStatus.Published},
                CASE WHEN i < {TodayCommentCount}
                     THEN @todayStart + ((i % 20) * interval '1 hour')
                     ELSE now() - ((i % 700) + 1) * interval '1 day' END,
                now(), true, false
            FROM generate_series(0, {CommentCount - 1}) AS i;

            INSERT INTO "Bookmarks" ("PostId", "UserId", "CreatedAt", "UpdateAt", "isActive", "isDeleted")
            SELECT
                (i % {PostCount}) + 1,
                (((i / {PostCount}) * 277) % {UserCount}) + 1,
                now() - ((i % 700) + 1) * interval '1 day', now(), true, false
            FROM generate_series(0, {BookmarkCount - 1}) AS i;
            """, ("todayStart", startDateUtc));
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 600 };
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private sealed record CapturedCommand(string Text, List<(string Name, object? Value)> Parameters);

    private sealed class CommandCapturingInterceptor : DbCommandInterceptor
    {
        public List<CapturedCommand> Commands { get; } = new();

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Capture(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Capture(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Capture(DbCommand command)
        {
            var parameters = new List<(string Name, object? Value)>();
            foreach (DbParameter parameter in command.Parameters)
            {
                parameters.Add((parameter.ParameterName, parameter.Value));
            }

            Commands.Add(new CapturedCommand(command.CommandText, parameters));
        }
    }
}

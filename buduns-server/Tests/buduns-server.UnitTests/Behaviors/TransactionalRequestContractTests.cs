using System.Reflection;
using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using MediatR;

namespace buduns_server.UnitTests.Behaviors;

/// <summary>
/// Transaction sinirinin varsayilani komutlarda aciktir. Bu sozlesmeyi kod
/// zorlamiyor: yeni bir komut yazip <see cref="ITransactionalRequest"/>
/// eklemeyi unutmak sessizce transaction'siz bir komut uretirdi. Bu testler
/// o sessizligi kaldiriyor; muafiyet istisna degil, yazili karar oluyor.
/// </summary>
public class TransactionalRequestContractTests
{
    /// <summary>
    /// Muaf komutlar ve gerekceleri. Buraya bir sey eklemek, o komutun
    /// yazmalarinin atomik olmadigini kabul etmek demektir.
    /// <para>
    /// En tehlikeli grup ucuncusu: <b>basarisizlik yolunda</b> yazan sayaclar.
    /// Bu komutlar once sayaci artirip sonra istisna firlatiyor; transaction
    /// istisnayla birlikte sayaci da geri alir ve kaba kuvvet korumasi
    /// sessizce devre disi kalir.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> ExemptCommands = new()
    {
        // 1) Kendi transaction'ini yoneten akis
        ["RefreshTokenLoginCommand"] =
            "AuthSessionService.RotateSessionAsync kendi Serializable transaction'ini aciyor; " +
            "ambient bir transaction varken ikincisini acmak InvalidOperationException uretir.",

        // 2) Icinde dis I/O (SMTP) olan akislar: transaction mail gonderimi
        //    boyunca acik kalir, baglanti ve kilitler bosuna tutulur.
        ["RegisterUserCommand"] = "Akis icinde SMTP var.",
        ["MailVerifyCommand"] = "Akis icinde SMTP var.",
        ["ForgotPasswordCommand"] = "Akis icinde SMTP var.",
        ["ChangeEmailCommand"] = "Akis icinde SMTP var; ayrica dogrulama denemesi sayaci yaziyor.",
        ["UpdateUserEmailCommand"] = "Akis icinde SMTP var; ayrica dogrulama denemesi sayaci yaziyor.",

        // 3) Basarisizlik yolunda sayac yazanlar
        ["LoginUserCommand"] =
            "CheckPasswordSignInAsync(lockoutOnFailure: true) hatali girişte AccessFailedCount'u " +
            "artirip kaydediyor, ardindan handler istisna firlatiyor. Transaction bu sayaci geri " +
            "alsaydi hesap hicbir zaman kilitlenmezdi.",
        ["UpdateUserPasswordCommand"] =
            "VerificationChallengeService.ValidateCodeAsync hatali kodda AttemptCount'u artirip " +
            "kaydediyor, sonra istisna firlatiyor. Geri alinirsa kod MaxAttempts'e hic ulasmaz.",
        ["UpdateUserMailVerifyCommand"] = "Ayni dogrulama denemesi sayaci yolu.",
    };

    private static IReadOnlyList<Type> AllCommands =>
        typeof(CreateBookmarksCommand).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.Name.EndsWith("Command", StringComparison.Ordinal))
            .Where(type => type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToList();

    [Fact]
    public void Every_command_should_be_transactional_unless_explicitly_exempt()
    {
        var missing = AllCommands
            .Where(type => !typeof(ITransactionalRequest).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Where(name => !ExemptCommands.ContainsKey(name))
            .OrderBy(name => name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"ITransactionalRequest tasimayan komutlar: {string.Join(", ", missing)}. " +
            "Ya isaretleyin ya da ExemptCommands'a gerekcesiyle ekleyin.");
    }

    /// <summary>
    /// Muafiyet listesi bayatlarsa fark edilmeli: silinen ya da sonradan
    /// isaretlenen bir komut listede kalirsa, liste gercegi anlatmayi birakir.
    /// </summary>
    [Fact]
    public void Exemption_list_should_not_contain_stale_entries()
    {
        var names = AllCommands.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        var vanished = ExemptCommands.Keys.Where(name => !names.Contains(name)).OrderBy(name => name).ToList();
        Assert.True(vanished.Count == 0, $"Artik var olmayan komutlar muafiyet listesinde: {string.Join(", ", vanished)}");

        var alreadyMarked = AllCommands
            .Where(type => ExemptCommands.ContainsKey(type.Name) && typeof(ITransactionalRequest).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToList();
        Assert.True(alreadyMarked.Count == 0, $"Hem isaretli hem muaf sayilan komutlar: {string.Join(", ", alreadyMarked)}");
    }

    [Fact]
    public void Every_exemption_should_carry_a_reason()
    {
        Assert.All(ExemptCommands, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value)));
    }
}

using System.Text.RegularExpressions;
using buduns_server.Application.Common.Consts;
using buduns_server.Persistence.MailTemplates;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Sablonlardaki yer tutucularla MailService'in doldurduklari birbirini
/// tutmak zorunda. Tutmazsa hata hicbir yerde patlamaz; kullaniciya gonderilen
/// e-postada duz metin olarak "{reset_link}" gorunur.
/// </summary>
public class MailTemplateContractTests
{
    /// <summary>MailService'in her sablonda doldurdugu yer tutucular.</summary>
    private static readonly string[] CommonPlaceholders = { "full_name", "verification_code", "app_name" };

    /// <summary>Yalnizca belirli sablonlarda doldurulan yer tutucular.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> TemplateSpecificPlaceholders = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [MailTemplateKeys.ChangeEmailOld] = new[] { "new_email" }
    };

    private static readonly Regex PlaceholderPattern = new(@"\{(?<name>[a-z0-9_]+)\}", RegexOptions.Compiled);

    [Theory]
    [MemberData(nameof(TemplateKeys))]
    public void Every_declared_key_should_resolve_to_a_body(string key)
    {
        // Gomulu kaynak adi kayarsa ya da .html dosyasi EmbeddedResource olarak
        // isaretlenmezse burasi kirilir; acilista fail-fast'e kalmaz.
        Assert.False(string.IsNullOrWhiteSpace(MailTemplateCatalog.GetBody(key)));
    }

    [Theory]
    [MemberData(nameof(TemplateKeys))]
    public void Every_placeholder_should_be_filled_by_the_mail_service(string key)
    {
        var allowed = CommonPlaceholders
            .Concat(TemplateSpecificPlaceholders.TryGetValue(key, out var extra) ? extra : Array.Empty<string>())
            .ToHashSet(StringComparer.Ordinal);

        var used = PlaceholderPattern.Matches(MailTemplateCatalog.GetBody(key))
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var unfilled = used.Where(name => !allowed.Contains(name)).ToArray();

        Assert.True(unfilled.Length == 0, $"{key} sablonunda MailService'in doldurmadigi yer tutucu var: {string.Join(", ", unfilled)}");
    }

    [Theory]
    [MemberData(nameof(TemplateKeys))]
    public void Every_template_should_use_the_common_placeholders(string key)
    {
        var body = MailTemplateCatalog.GetBody(key);

        foreach (var placeholder in CommonPlaceholders)
        {
            Assert.Contains($"{{{placeholder}}}", body);
        }
    }

    [Fact]
    public void Change_email_old_template_should_name_the_new_address()
    {
        Assert.Contains("{new_email}", MailTemplateCatalog.GetBody(MailTemplateKeys.ChangeEmailOld));
    }

    /// <summary>
    /// Faz 1.5'te kaynak dosyalarin ANSI olmasi 401 Turkce karakteri bozmustu.
    /// Sablonlar da metin dosyasi; ayni tuzaga karsi kanarya.
    /// </summary>
    [Theory]
    [MemberData(nameof(TemplateKeys))]
    public void Every_template_should_keep_its_turkish_characters(string key)
    {
        Assert.Contains("geçerlidir", MailTemplateCatalog.GetBody(key));
    }

    [Fact]
    public void Catalog_should_expose_exactly_the_declared_keys()
    {
        Assert.Equal(MailTemplateKeys.All.OrderBy(key => key, StringComparer.Ordinal), MailTemplateCatalog.GetAll().Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    public static TheoryData<string> TemplateKeys()
    {
        var data = new TheoryData<string>();
        foreach (var key in MailTemplateKeys.All)
        {
            data.Add(key);
        }

        return data;
    }
}

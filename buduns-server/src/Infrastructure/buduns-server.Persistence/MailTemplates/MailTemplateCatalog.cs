using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using buduns_server.Application.Common.Consts;

namespace buduns_server.Persistence.MailTemplates
{
    /// <summary>
    /// Kodda tanimli mail sablonlarinin kaynagi. Govdeler bu klasordeki
    /// .html dosyalarindan gelir; dosyalar assembly'ye gomulu oldugu icin
    /// yayinda ayrica tasinmalari gerekmez.
    /// </summary>
    public static class MailTemplateCatalog
    {
        private const string ResourceNamespace = "buduns_server.Persistence.MailTemplates";

        private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

        /// <summary>
        /// Anahtarin karsiligi olan HTML govde. Kaynak bulunamazsa bu bir
        /// yapilandirma hatasi degil, eksik derleme girdisidir; sessizce
        /// gecilmez.
        /// </summary>
        public static string GetBody(string key) => Cache.GetOrAdd(key, ReadResource);

        public static IReadOnlyDictionary<string, string> GetAll() =>
            MailTemplateKeys.All.ToDictionary(key => key, GetBody, StringComparer.Ordinal);

        private static string ReadResource(string key)
        {
            var resourceName = $"{ResourceNamespace}.{key}.html";

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"'{resourceName}' gomulu mail sablonu bulunamadi.");

            using var reader = new StreamReader(stream, Encoding.UTF8);

            return reader.ReadToEnd().Trim();
        }
    }
}

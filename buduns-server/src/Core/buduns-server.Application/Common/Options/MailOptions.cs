namespace buduns_server.Application.Common.Options
{
    public class MailOptions
    {
        public const string SectionName = "Mail";

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string FromName { get; set; } = "Buduns";

        /// <summary>
        /// SMTP bilgileri eksikse uygulama acilir ama mail gonderemez. Acilista
        /// uyari verilir, gonderim denendiginde de anlamli bir hata firlatilir;
        /// zorunlu tutulsaydi SMTP hesabi olmayan bir gelistirici uygulamayi
        /// hic calistiramazdi.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(Host);

        /// <summary>Eksik olan ayarlarin adlari; log ve hata mesajlarinda kullanilir.</summary>
        public IReadOnlyList<string> GetMissingSettings()
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(Username))
            {
                missing.Add($"{SectionName}:{nameof(Username)}");
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                missing.Add($"{SectionName}:{nameof(Password)}");
            }

            if (string.IsNullOrWhiteSpace(Host))
            {
                missing.Add($"{SectionName}:{nameof(Host)}");
            }

            return missing;
        }
    }
}

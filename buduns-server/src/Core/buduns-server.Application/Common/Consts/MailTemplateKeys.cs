namespace buduns_server.Application.Common.Consts
{
    /// <summary>
    /// Mail sablonlarinin <c>Utilities.Name</c> kolonundaki anahtarlari.
    /// Hem sablonu arayan <c>MailService</c> hem de acilista sablonlari yazan
    /// seeder buradan okur; ikisinin ayrisamamasi icin liste de burada.
    /// </summary>
    public static class MailTemplateKeys
    {
        public const string MailVerify = "MAIL_VERIFY";
        public const string ForgotPassword = "FORGOT_PASSWORD";
        public const string ChangeEmail = "CHANGE_EMAIL";
        public const string ChangeEmailOld = "CHANGE_EMAIL_OLD";

        public static readonly IReadOnlyList<string> All = new[] { MailVerify, ForgotPassword, ChangeEmail, ChangeEmailOld };
    }
}

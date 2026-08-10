namespace buduns_server.Application.Common.Options
{
    public class BootstrapAdminOptions
    {
        public const string SectionName = "BootstrapAdmin";

        /// <summary>
        /// Sistemde hic admin yokken Admin rolune yukseltilecek, halihazirda
        /// kayitli olan hesabin e-postasi. Bos birakilirsa yukseltme yapilmaz.
        /// </summary>
        public string? Email { get; set; }
    }
}

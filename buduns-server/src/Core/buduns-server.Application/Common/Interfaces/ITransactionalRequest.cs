namespace buduns_server.Application.Common.Interfaces
{
    /// <summary>
    /// Bu istegin yazmalari tek bir veritabani transaction'i icinde calisir:
    /// ya hepsi kalici olur ya hicbiri. <c>TransactionBehavior</c> siniri
    /// handler'in etrafinda kurar, handler transaction'dan habersizdir.
    /// <para>
    /// Komutlarda varsayilan budur; isaretlenmeyen her komut
    /// <c>TransactionalRequestContractTests</c> tarafindan yakalanir ve orada
    /// gerekcesiyle birlikte muaf tutulmasi gerekir.
    /// </para>
    /// </summary>
    public interface ITransactionalRequest
    {
    }
}

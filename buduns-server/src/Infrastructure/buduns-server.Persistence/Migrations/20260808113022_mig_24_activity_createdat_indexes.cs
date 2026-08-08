using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buduns_server.Persistence.Migrations
{
    /// <summary>
    /// daily-top50 Faz 3'te yeniden yazildi: artik paylasimdan degil gunun
    /// begeni/yorum olaylarindan basliyor. Birinci asama iki aktivite
    /// tablosuna CreatedAt araligiyla giriyor, dolayisiyla o kolon indeks
    /// istiyor.
    ///
    /// Olcum (20 bin paylasim / 200 bin begeni / 60 bin yorum):
    ///   yeni sorgu, indekssiz         42,2 ms / 4.525 buffer
    ///   + Likes(CreatedAt)            27,8 ms / 2.726 buffer
    ///   + Comments(CreatedAt)         21,8 ms / 2.120 buffer
    ///
    /// Ayni indeks Faz 2'de olculmus ve ise yaramamisti; o zaman sorgu tarih
    /// araligiyla degil paylasim basina calisiyordu. Dogru indeks tabloya
    /// degil sorguya baglidir.
    ///
    /// mig_23'teki gibi CONCURRENTLY: Likes ve Comments aktivite tablolari,
    /// normal CREATE INDEX ikisini de yazmaya kapatirdi. Yarida kalirsa
    /// arkasinda INVALID indeks birakir; elle DROP edip tekrar calistirmak
    /// gerekir, IF NOT EXISTS bunu guvenli kilar.
    /// </summary>
    public partial class mig_24_activity_createdat_indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Likes_CreatedAt" ON "Likes" ("CreatedAt");""",
                suppressTransaction: true);

            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Comments_CreatedAt" ON "Comments" ("CreatedAt");""",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """DROP INDEX CONCURRENTLY IF EXISTS "IX_Likes_CreatedAt";""",
                suppressTransaction: true);

            migrationBuilder.Sql(
                """DROP INDEX CONCURRENTLY IF EXISTS "IX_Comments_CreatedAt";""",
                suppressTransaction: true);
        }
    }
}

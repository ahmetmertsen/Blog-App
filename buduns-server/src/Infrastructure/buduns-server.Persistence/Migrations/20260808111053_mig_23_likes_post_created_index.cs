using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buduns_server.Persistence.Migrations
{
    /// <summary>
    /// daily-top50, begenileri paylasim basina ve gun araliginda sayiyor.
    /// Tek kolonlu IX_Likes_PostId tarih filtresini heap'e biraktigi icin her
    /// paylasim icin gereksiz sayfa okunuyordu; olcumde toplam buffer 417.854
    /// -> 196.593, sorgu 324 ms -> 104 ms.
    ///
    /// EF'in urettigi DROP + CREATE elle CONCURRENTLY'ye cevrildi: Likes buyuk
    /// bir aktivite tablosu ve normal CREATE INDEX indeks kurulurken tabloyu
    /// yazmaya kapatir. CONCURRENTLY transaction icinde calismadigi icin
    /// suppressTransaction: true veriliyor.
    ///
    /// DIKKAT: CONCURRENTLY yarida kalirsa arkasinda INVALID bir indeks
    /// birakir; bu durumda indeksi elle DROP edip migration tekrar
    /// calistirilmali. IF NOT EXISTS bu tekrari guvenli kilar.
    /// </summary>
    public partial class mig_23_likes_post_created_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Once yeni indeks kurulur: PostId'siz kalan bir an olmasin.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Likes_PostId_CreatedAt" ON "Likes" ("PostId", "CreatedAt");""",
                suppressTransaction: true);

            // Yeni indeksin ilk kolonu PostId oldugu icin eski indeks gereksiz.
            migrationBuilder.Sql(
                """DROP INDEX CONCURRENTLY IF EXISTS "IX_Likes_PostId";""",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Likes_PostId" ON "Likes" ("PostId");""",
                suppressTransaction: true);

            migrationBuilder.Sql(
                """DROP INDEX CONCURRENTLY IF EXISTS "IX_Likes_PostId_CreatedAt";""",
                suppressTransaction: true);
        }
    }
}

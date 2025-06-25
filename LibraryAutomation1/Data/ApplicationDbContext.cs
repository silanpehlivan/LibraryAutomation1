using LibraryAutomation1.Models; // Model sınıflarınızın bulunduğu namespace
using Microsoft.EntityFrameworkCore; // Entity Framework Core için gerekli temel sınıflar

namespace LibraryAutomation1.Data
{
    public class ApplicationDbContext : DbContext // DbContext sınıfından miras alarak veritabanı bağlamını tanımlarız.
    {
        // Yapıcı metot: DbContextOptions'ı temel sınıfa iletir.
        // Bu, veritabanı sağlayıcısı (SQL Server, SQLite vb.) ve bağlantı dizesi gibi yapılandırma seçeneklerinin dışarıdan gelmesini sağlar.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet'ler: Uygulamanızdaki her bir model sınıfı için bir DbSet özelliği tanımlar.
        // Bu özellikler, veritabanındaki ilgili tablolara erişim noktalarıdır.
        public DbSet<Book> Books { get; set; } // Kitaplar tablosunu temsil eder.
        public DbSet<Member> Members { get; set; } // Üyeler tablosunu temsil eder.
        public DbSet<Loan> Loans { get; set; } // Ödünç alma işlemleri tablosunu temsil eder.
        public DbSet<BookRating> BookRatings { get; set; } // Kitap puanlamaları tablosunu temsil eder.

        // OnModelCreating metodu: Model oluşturulurken (veritabanı şeması belirlenirken) çağrılır.
        // Bu metodda, Entity Framework'ün varsayılan davranışlarını geçersiz kılabilir ve özel konfigürasyonlar yapabiliriz.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Temel sınıfın (DbContext) OnModelCreating metodunu çağırırız.

            // --- Book varlığı (entity) konfigürasyonu ---
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id); // 'Id' özelliğini birincil anahtar (Primary Key) olarak belirler.
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200); // 'Title' alanını zorunlu ve maksimum 200 karakter olarak ayarlar.
                entity.Property(e => e.Author).IsRequired().HasMaxLength(100); // 'Author' alanını zorunlu ve maksimum 100 karakter olarak ayarlar.
                entity.Property(e => e.Genre).HasMaxLength(50); // 'Genre' alanını maksimum 50 karakter olarak ayarlar.
                entity.Property(e => e.Description).HasMaxLength(1000); // 'Description' alanını maksimum 1000 karakter olarak ayarlar.
                entity.Property(e => e.ISBN).HasMaxLength(20); // 'ISBN' alanını maksimum 20 karakter olarak ayarlar.
                entity.Property(e => e.CoverImageUrl).HasMaxLength(500); // 'CoverImageUrl' alanını maksimum 500 karakter olarak ayarlar.
                entity.Property(e => e.Category).HasMaxLength(50); // 'Category' alanını maksimum 50 karakter olarak ayarlar.
                entity.Property(e => e.ShelfNumber).HasMaxLength(20); // 'ShelfNumber' alanını maksimum 20 karakter olarak ayarlar.
                entity.Property(e => e.Publisher).HasMaxLength(100); // 'Publisher' alanını maksimum 100 karakter olarak ayarlar.
                entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)"); // 'AverageRating' alanını ondalık tipinde ve belirli bir hassasiyetle (toplam 3 basamak, virgülden sonra 2 basamak) veritabanına eşler.
            });

            // --- Member varlığı (entity) konfigürasyonu ---
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id); // 'Id' özelliğini birincil anahtar olarak belirler.
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50); // 'FirstName' zorunlu, max 50 karakter.
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50); // 'LastName' zorunlu, max 50 karakter.
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100); // 'Email' zorunlu, max 100 karakter.
                entity.Property(e => e.Phone).HasMaxLength(15); // 'Phone' max 15 karakter.
                entity.Property(e => e.Address).HasMaxLength(200); // 'Address' max 200 karakter.
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255); // 'PasswordHash' zorunlu, max 255 karakter.
                entity.HasIndex(e => e.Email).IsUnique(); // 'Email' alanında benzersiz bir indeks oluşturur. Bu, aynı e-posta adresine sahip birden fazla üyenin olamayacağı anlamına gelir.
            });

            // --- Loan varlığı (entity) konfigürasyonu ---
            modelBuilder.Entity<Loan>(entity =>
            {
                entity.HasKey(e => e.Id); // 'Id' özelliğini birincil anahtar olarak belirler.
                entity.Property(e => e.LateFee).HasColumnType("decimal(10,2)"); // 'LateFee' (gecikme ücreti) alanını ondalık tipinde ve belirli bir hassasiyetle ayarlar.
                entity.Property(e => e.Notes).HasMaxLength(500); // 'Notes' alanı max 500 karakter.

                // Kitap ile Ödünç Arasındaki İlişki (One-to-Many)
                entity.HasOne(d => d.Book) // Bir ödünç kaydının bir kitaba ait olduğunu belirtir.
                    .WithMany(p => p.Loans) // Bir kitabın birden fazla ödünç kaydı olabileceğini belirtir.
                    .HasForeignKey(d => d.BookId) // 'Loan' tablosundaki 'BookId'nin foreign key olduğunu belirtir.
                    .OnDelete(DeleteBehavior.Restrict); // İlişkili bir kitap silindiğinde, bu kitaba ait ödünç kayıtlarının silinmesini ENGELLER. (Ödünç varken kitap silinemez.)

                // Üye ile Ödünç Arasındaki İlişki (One-to-Many)
                entity.HasOne(d => d.Member) // Bir ödünç kaydının bir üyeye ait olduğunu belirtir.
                    .WithMany(p => p.Loans) // Bir üyenin birden fazla ödünç kaydı olabileceğini belirtir.
                    .HasForeignKey(d => d.MemberId) // 'Loan' tablosundaki 'MemberId'nin foreign key olduğunu belirtir.
                    .OnDelete(DeleteBehavior.Restrict); // İlişkili bir üye silindiğinde, bu üyeye ait ödünç kayıtlarının silinmesini ENGELLER. (Ödünç varken üye silinemez.)
            });

            // --- BookRating varlığı (entity) konfigürasyonu ---
            modelBuilder.Entity<BookRating>(entity =>
            {
                entity.HasKey(e => e.Id); // 'Id' özelliğini birincil anahtar olarak belirler.
                entity.Property(e => e.Comment).HasMaxLength(500); // 'Comment' alanı max 500 karakter.

                // Kitap ile Puanlama Arasındaki İlişki (One-to-Many)
                entity.HasOne(d => d.Book) // Bir puanlamanın bir kitaba ait olduğunu belirtir.
                    .WithMany(p => p.BookRatings) // Bir kitabın birden fazla puanlaması olabileceğini belirtir.
                    .HasForeignKey(d => d.BookId) // 'BookRating' tablosundaki 'BookId'nin foreign key olduğunu belirtir.
                    .OnDelete(DeleteBehavior.Cascade); // İlişkili bir kitap silindiğinde, bu kitaba ait tüm puanlamaları da SİLİNİR.

                // Üye ile Puanlama Arasındaki İlişki (One-to-Many)
                entity.HasOne(d => d.Member) // Bir puanlamanın bir üyeye ait olduğunu belirtir.
                    .WithMany(p => p.BookRatings) // Bir üyenin birden fazla puanlama yapabileceğini belirtir.
                    .HasForeignKey(d => d.MemberId) // 'BookRating' tablosundaki 'MemberId'nin foreign key olduğunu belirtir.
                    .OnDelete(DeleteBehavior.Cascade); // İlişkili bir üye silindiğinde, bu üyenin yaptığı tüm puanlamalar da SİLİNİR.

                // Kompozit Benzersiz İndeks: Her üye her kitaba sadece bir kez puan verebilir.
                entity.HasIndex(e => new { e.BookId, e.MemberId }).IsUnique();
            });

            // --- Seed Data (Başlangıç Verileri) ---
            // Veritabanı ilk oluşturulduğunda veya migrate edildiğinde otomatik olarak eklenecek örnek veriler.
            SeedData(modelBuilder);
        }

        // SeedData metodu: Örnek verileri veritabanına eklemek için kullanılır.
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Örnek kitap verileri
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    Title = "Suç ve Ceza",
                    Author = "Fyodor Dostoyevski",
                    PublicationYear = 1866,
                    Genre = "Klasik Edebiyat",
                    Description = "Dostoyevski'nin ünlü psikolojik roman eseri.",
                    ISBN = "978-0-14-044913-6",
                    Category = "Edebiyat",
                    ShelfNumber = "A-001",
                    PageCount = 671,
                    Publisher = "İş Bankası Kültür Yayınları",
                    IsAvailable = true
                },
                new Book
                {
                    Id = 2,
                    Title = "1984",
                    Author = "George Orwell",
                    PublicationYear = 1949,
                    Genre = "Distopya",
                    Description = "Totaliter bir toplumda geçen distopik roman.",
                    ISBN = "978-0-452-28423-4",
                    Category = "Bilim Kurgu",
                    ShelfNumber = "B-015",
                    PageCount = 328,
                    Publisher = "Can Yayınları",
                    IsAvailable = true
                },
                new Book
                {
                    Id = 3,
                    Title = "Simyacı",
                    Author = "Paulo Coelho",
                    PublicationYear = 1988,
                    Genre = "Felsefe",
                    Description = "Kişisel efsaneyi bulma yolculuğu hakkında felsefi roman.",
                    ISBN = "978-0-06-112241-5",
                    Category = "Felsefe",
                    ShelfNumber = "C-032",
                    PageCount = 163,
                    Publisher = "Epsilon Yayınları",
                    IsAvailable = true
                }
            );

            // Örnek üye verileri (parolaların gerçek uygulamada hash'lenmesi gerektiğini unutmayın!)
            modelBuilder.Entity<Member>().HasData(
                new Member
                {
                    Id = 1,
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Email = "ahmet.yilmaz@email.com",
                    Phone = "0532-123-4567",
                    Address = "İstanbul, Türkiye",
                    Role = UserRole.Admin, // UserRole, muhtemelen ayrı bir Enum'da tanımlıdır.
                    PasswordHash = "AQAAAAEAACcQAAAAEJ..." // **ÖNEMLİ: Gerçek uygulamada güvenli hash'leme kullanılmalıdır!**
                },
                new Member
                {
                    Id = 2,
                    FirstName = "Ayşe",
                    LastName = "Kaya",
                    Email = "ayse.kaya@email.com",
                    Phone = "0533-987-6543",
                    Address = "Ankara, Türkiye",
                    Role = UserRole.Member,
                    PasswordHash = "AQAAAAEAACcQAAAAEK..." // **ÖNEMLİ: Gerçek uygulamada güvenli hash'leme kullanılmalıdır!**
                }
            );
        }
    }
}

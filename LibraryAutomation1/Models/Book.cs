using System.ComponentModel.DataAnnotations; // Veri doğrulama (validation) öznitelikleri için
using System.ComponentModel.DataAnnotations.Schema; // Veritabanı eşleme (mapping) öznitelikleri için
using Microsoft.AspNetCore.Http; // IFormFile için gerekli (resim yüklemeleri için)

namespace LibraryAutomation1.Models
{
    public class Book
    {
        // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        // Kitap Adı
        [Required(ErrorMessage = "Kitap adı zorunludur.")] // Bu alanın doldurulması zorunludur.
        [StringLength(200)] // Maksimum 200 karakter uzunluğunda olabilir.
        public string Title { get; set; }

        // Yazar Adı
        [Required(ErrorMessage = "Yazar adı zorunludur.")] // Bu alanın doldurulması zorunludur.
        [StringLength(100)] // Maksimum 100 karakter uzunluğunda olabilir.
        public string Author { get; set; }

        // Yayın Yılı
        [Display(Name = "Yayın Yılı")] // Kullanıcı arayüzünde "Yayın Yılı" olarak görüntülenir.
        [Range(1000, 2100, ErrorMessage = "Geçerli bir yayın yılı giriniz.")] // Yılın 1000 ile 2100 arasında olmasını sağlar.
        public int PublicationYear { get; set; }

        // Kitap Türü (Opsiyonel)
        [Display(Name = "Tür")]
        [StringLength(50)] // Maksimum 50 karakter.
        public string? Genre { get; set; } // '?' nullable olduğunu (boş geçilebilir) belirtir.

        // Kitap Açıklaması (Opsiyonel)
        [Display(Name = "Açıklama")]
        [StringLength(1000)] // Maksimum 1000 karakter.
        public string? Description { get; set; }

        // ISBN Numarası (Opsiyonel)
        [Display(Name = "ISBN")]
        [StringLength(20)] // Maksimum 20 karakter.
        public string? ISBN { get; set; }

        // Kapak Resmi URL'si (Opsiyonel)
        [Display(Name = "Kapak Resmi URL")]
        [StringLength(500)] // Maksimum 500 karakter.
        public string? CoverImageUrl { get; set; }

        // Kategori (Opsiyonel)
        [Display(Name = "Kategori")]
        [StringLength(50)] // Maksimum 50 karakter.
        public string? Category { get; set; }

        // Raf Numarası (Opsiyonel)
        [Display(Name = "Raf Numarası")]
        [StringLength(20)] // Maksimum 20 karakter.
        public string? ShelfNumber { get; set; }

        // Sayfa Sayısı (Opsiyonel)
        [Display(Name = "Sayfa Sayısı")]
        public int? PageCount { get; set; } // '?' nullable olduğunu belirtir.

        // Yayınevi (Opsiyonel)
        [Display(Name = "Yayınevi")]
        [StringLength(100)] // Maksimum 100 karakter.
        public string? Publisher { get; set; }

        // Kitabın Kütüphanede Mevcut Olup Olmadığı
        [Display(Name = "Müsait")]
        public bool IsAvailable { get; set; } = true; // Varsayılan değeri 'true' (müsait) olarak ayarlanır.

        // Ortalama Puan (Puanlama Sistemi İçin)
        [Display(Name = "Ortalama Puan")]
        public double AverageRating { get; set; } = 0.0; // Varsayılan değeri 0.0 olarak ayarlanır.

        // Puan Sayısı (Puanlama Sistemi İçin)
        [Display(Name = "Puan Sayısı")]
        public int RatingCount { get; set; } = 0; // Varsayılan değeri 0 olarak ayarlanır.

        // Resim Yüklemesi İçin Geçici Alan (Veritabanına Haritalanmaz)
        [NotMapped] // Bu öznitelik, Entity Framework Core'a bu özelliğin veritabanında bir sütuna karşılık gelmediğini bildirir.
        public IFormFile? ImageFile { get; set; } // Kullanıcının bir dosya yüklemesi için kullanılır, doğrudan veritabanında saklanmaz.

        // --- Navigasyon Özellikleri (İlişkiler) ---

        // Bu kitaba ait tüm ödünç alma kayıtları koleksiyonu.
        // Bir kitap birden fazla kez ödünç alınabilir (One-to-Many ilişkisi).
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

        // Bu kitaba ait tüm puanlama kayıtları koleksiyonu.
        // Bir kitap birden fazla kişi tarafından puanlanabilir (One-to-Many ilişkisi).
        public virtual ICollection<BookRating> BookRatings { get; set; } = new List<BookRating>();
    }
}



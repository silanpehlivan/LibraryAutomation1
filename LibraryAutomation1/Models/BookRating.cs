using System.ComponentModel.DataAnnotations; // Veri doğrulama (validation) öznitelikleri için

namespace LibraryAutomation1.Models
{
    public class BookRating
    {
        // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        // Kitap Kimliği (Foreign Key)
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        public int BookId { get; set; } // Hangi kitaba puan verildiğini gösterir.

        // Üye Kimliği (Foreign Key)
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        public int MemberId { get; set; } // Puanlamayı yapan üyeyi gösterir.

        // Puan Değeri
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")] // Puanın 1 ile 5 arasında bir değer olmasını sağlar.
        [Display(Name = "Puan")] // Kullanıcı arayüzünde "Puan" olarak görüntülenir.
        public int Rating { get; set; }

        // Yorum (Opsiyonel)
        [Display(Name = "Yorum")] // Kullanıcı arayüzünde "Yorum" olarak görüntülenir.
        [StringLength(500)] // Maksimum 500 karakter uzunluğunda olabilir.
        public string? Comment { get; set; } // '?' nullable olduğunu (boş geçilebilir) belirtir.

        // Puanlama Tarihi
        [Display(Name = "Puanlama Tarihi")] // Kullanıcı arayüzünde "Puanlama Tarihi" olarak görüntülenir.
        public DateTime RatingDate { get; set; } = DateTime.Now; // Puanlamanın yapıldığı tarihi otomatik olarak atar.

        // --- Navigasyon Özellikleri (İlişkiler) ---

        // Puanlamanın ait olduğu Kitap nesnesi.
        // Entity Framework Core, BookId ile bu Book nesnesi arasındaki ilişkiyi kurar.
        public virtual Book Book { get; set; }

        // Puanlamayı yapan Üye nesnesi.
        // Entity Framework Core, MemberId ile bu Member nesnesi arasındaki ilişkiyi kurar.
        public virtual Member Member { get; set; }
    }
}


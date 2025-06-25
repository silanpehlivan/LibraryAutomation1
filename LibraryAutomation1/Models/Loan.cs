using System.ComponentModel.DataAnnotations; // Veri doğrulama (validation) ve görüntüleme (display) öznitelikleri için

namespace LibraryAutomation1.Models
{
    public class Loan
    {
        // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        // Kitap Kimliği (Foreign Key)
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        public int BookId { get; set; } // Hangi kitabın ödünç alındığını gösterir.

        // Üye Kimliği (Foreign Key)
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        public int MemberId { get; set; } // Kitabı ödünç alan üyeyi gösterir.

        // Ödünç Alma Tarihi
        [Display(Name = "Ödünç Alma Tarihi")] // Kullanıcı arayüzünde "Ödünç Alma Tarihi" olarak görüntülenir.
        public DateTime LoanDate { get; set; }

        // Teslim Tarihi (Beklenen İade Tarihi)
        [Display(Name = "Teslim Tarihi")] // Kullanıcı arayüzünde "Teslim Tarihi" olarak görüntülenir.
        public DateTime DueDate { get; set; }

        // İade Tarihi
        [Display(Name = "İade Tarihi")] // Kullanıcı arayüzünde "İade Tarihi" olarak görüntülenir.
        public DateTime? ReturnDate { get; set; } // '?' nullable olduğunu belirtir. Kitap henüz iade edilmediyse bu değer null (boş) olacaktır.

        // Gecikme Ücreti
        [Display(Name = "Gecikme Ücreti")] // Kullanıcı arayüzünde "Gecikme Ücreti" olarak görüntülenir.
        [Range(0, double.MaxValue)] // Gecikme ücretinin 0 veya pozitif bir değer olmasını sağlar.
        public decimal LateFee { get; set; } = 0; // Varsayılan değeri 0 olarak ayarlanır.

        // Notlar (Opsiyonel)
        [Display(Name = "Notlar")] // Kullanıcı arayüzünde "Notlar" olarak görüntülenir.
        [StringLength(500)] // Maksimum 500 karakter uzunluğunda olabilir.
        public string? Notes { get; set; } // '?' nullable olduğunu (boş geçilebilir) belirtir.

        // Ödünç Alma İşleminin Durumu
        [Display(Name = "Durum")] // Kullanıcı arayüzünde "Durum" olarak görüntülenir.
        public LoanStatus Status { get; set; } = LoanStatus.Active; // Varsayılan olarak 'Aktif' durumunda başlar.

        // --- Navigasyon Özellikleri (İlişkiler) ---

        // Bu ödünç alma kaydının ait olduğu Kitap nesnesi.
        // Entity Framework Core, BookId ile bu Book nesnesi arasındaki ilişkiyi kurar.
        public virtual Book Book { get; set; }

        // Bu ödünç alma kaydını yapan Üye nesnesi.
        // Entity Framework Core, MemberId ile bu Member nesnesi arasındaki ilişkiyi kurar.
        public virtual Member Member { get; set; }

        // --- Hesaplanan Özellikler ---
        // Bu özellikler doğrudan veritabanında saklanmaz, programatik olarak hesaplanır.

        // Gecikme Günü Sayısı
        [Display(Name = "Gecikme Günü")] // Kullanıcı arayüzünde "Gecikme Günü" olarak görüntülenir.
        public int DaysLate
        {
            get
            {
                // Eğer kitap iade edilmişse, iade tarihi ile teslim tarihi arasındaki farkı hesaplarız.
                if (ReturnDate.HasValue)
                {
                    return Math.Max(0, (ReturnDate.Value.Date - DueDate.Date).Days);
                }
                // Eğer kitap henüz iade edilmemişse, bugünün tarihi ile teslim tarihi arasındaki farkı hesaplarız.
                else
                {
                    return Math.Max(0, (DateTime.Now.Date - DueDate.Date).Days);
                }
            }
        }

        // Kitabın Gecikmeli Olup Olmadığı
        [Display(Name = "Gecikmeli")] // Kullanıcı arayüzünde "Gecikmeli" olarak görüntülenir.
        // Kitap iade edilmemişse (ReturnDate == null) VE bugünün tarihi teslim tarihinden büyükse (DateTime.Now.Date > DueDate.Date) gecikmelidir.
        public bool IsOverdue => !ReturnDate.HasValue && DateTime.Now.Date > DueDate.Date;
    }

    // --- LoanStatus Enum'u ---
    // Bir ödünç alma işleminin olası durumlarını tanımlayan bir numaralandırma (enum).
    public enum LoanStatus
    {
        [Display(Name = "Aktif")] // Henüz iade edilmemiş ve teslim tarihi geçmemiş.
        Active = 0,
        [Display(Name = "İade Edildi")] // Kitap iade edilmiş.
        Returned = 1,
        [Display(Name = "Gecikmeli")] // Teslim tarihi geçmiş ama henüz iade edilmemiş.
        Overdue = 2,
        [Display(Name = "Kayıp")] // Kitap kaybolmuş.
        Lost = 3
    }
}

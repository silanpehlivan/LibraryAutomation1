using System.ComponentModel.DataAnnotations; // Veri doğrulama (validation) ve görüntüleme (display) öznitelikleri için
using LibraryAutomation1.Models; // UserRole enum'ı için (eğer ayrı bir dosyadaysa bu using gerekli olmayabilir)

namespace LibraryAutomation1.Models
{
    public class Member
    {
        // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        // Ad
        [Required(ErrorMessage = "İsim zorunludur.")] // Bu alanın doldurulması zorunludur.
        [StringLength(50)] // Maksimum 50 karakter uzunluğunda olabilir.
        [Display(Name = "Ad")] // Kullanıcı arayüzünde "Ad" olarak görüntülenir.
        public string FirstName { get; set; }

        // Soyad
        [Required(ErrorMessage = "Soyisim zorunludur.")] // Bu alanın doldurulması zorunludur.
        [StringLength(50)] // Maksimum 50 karakter uzunluğunda olabilir.
        [Display(Name = "Soyad")] // Kullanıcı arayüzünde "Soyad" olarak görüntülenir.
        public string LastName { get; set; }

        // E-posta Adresi
        [Required(ErrorMessage = "E-posta zorunludur.")] // Bu alanın doldurulması zorunludur.
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")] // Geçerli bir e-posta formatı olmasını sağlar.
        [StringLength(100)] // Maksimum 100 karakter uzunluğunda olabilir.
        [Display(Name = "E-posta")] // Kullanıcı arayüzünde "E-posta" olarak görüntülenir.
        public string Email { get; set; }

        // Telefon Numarası (Opsiyonel)
        [StringLength(15)] // Maksimum 15 karakter uzunluğunda olabilir.
        [Display(Name = "Telefon")] // Kullanıcı arayüzünde "Telefon" olarak görüntülenir.
        public string? Phone { get; set; } // '?' nullable olduğunu (boş geçilebilir) belirtir.

        // Adres (Opsiyonel)
        [StringLength(200)] // Maksimum 200 karakter uzunluğunda olabilir.
        [Display(Name = "Adres")] // Kullanıcı arayüzünde "Adres" olarak görüntülenir.
        public string? Address { get; set; } // '?' nullable olduğunu (boş geçilebilir) belirtir.

        // Üyelik Tarihi
        [Display(Name = "Üyelik Tarihi")] // Kullanıcı arayüzünde "Üyelik Tarihi" olarak görüntülenir.
        public DateTime MembershipDate { get; set; } = DateTime.Now; // Üyeliğin başladığı tarihi otomatik olarak atar.

        // Üyenin Aktif Olup Olmadığı
        [Display(Name = "Aktif")] // Kullanıcı arayüzünde "Aktif" olarak görüntülenir.
        public bool IsActive { get; set; } = true; // Varsayılan olarak 'true' (aktif) olarak ayarlanır.

        // Üyenin Rolü
        [Display(Name = "Rol")] // Kullanıcı arayüzünde "Rol" olarak görüntülenir.
        public UserRole Role { get; set; } = UserRole.Member; // Varsayılan olarak 'Member' (normal üye) rolünde başlar.

        // Parola Hash'i
        // **ÖNEMLİ:** Gerçek uygulamalarda bu alanın güvenli bir şekilde yönetilmesi gerekir.
        // Parolalar asla düz metin olarak saklanmamalıdır; her zaman hash'lenmelidir.
        [Required] // Bu alanın zorunlu olduğunu belirtir.
        [StringLength(255)] // Hash'lenmiş parolanın uzunluğu için yeterli alan sağlar.
        public string PasswordHash { get; set; }

        // Tam Ad (Hesaplanan Özellik)
        [Display(Name = "Tam Ad")] // Kullanıcı arayüzünde "Tam Ad" olarak görüntülenir.
        // Bu, FirstName ve LastName özelliklerini birleştirerek otomatik olarak oluşturulan bir özelliktir.
        // Veritabanında ayrı bir sütun olarak saklanmaz.
        public string FullName => $"{FirstName} {LastName}";

        // --- Navigasyon Özellikleri (İlişkiler) ---

        // Bu üyenin yaptığı tüm ödünç alma kayıtları koleksiyonu.
        // Bir üye birden fazla kitap ödünç alabilir (One-to-Many ilişkisi).
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

        // Bu üyenin yaptığı tüm kitap puanlama kayıtları koleksiyonu.
        // Bir üye birden fazla kitap puanlayabilir (One-to-Many ilişkisi).
        public virtual ICollection<BookRating> BookRatings { get; set; } = new List<BookRating>();
    }

    // --- UserRole Enum'u ---
    // Bir kullanıcının/üyenin sistemdeki farklı rollerini tanımlayan bir numaralandırma (enum).
    public enum UserRole
    {
        [Display(Name = "Üye")] // Kütüphane hizmetlerinden yararlanan standart kullanıcı.
        Member = 0,
        [Display(Name = "Personel")] // Kütüphane operasyonlarını yöneten personel.
        Staff = 1,
        [Display(Name = "Yönetici")] // Sistem üzerinde tam yetkiye sahip yönetici.
        Admin = 2
    }
}


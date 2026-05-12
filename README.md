📚 Kütüphane Otomasyon Sistemi (Library Management System)

Bu proje, modern web teknolojileri kullanılarak geliştirilmiş, kütüphane operasyonlarını dijitalleştirirmeyi amaçlayan kapsamlı bir ASP.NET Core 8.0 MVC uygulamasıdır. Kitap takibinden üye yönetimine, ödünç alma süreçlerinden detaylı istatistiksel raporlamalara kadar bir kütüphanenin ihtiyaç duyabileceği tüm temel fonksiyonları bünyesinde barındırır.




🎯 Projenin Amacı

Bu projenin temel amacı, kütüphane yönetim süreçlerini optimize etmek ve kullanıcı dostu bir arayüz ile hem personel hem de üyeler için verimli bir deneyim sunmaktır. Bu kapsamda:

•
Kitap envanterinin dijital ortamda merkezi olarak yönetilmesi

•
Üye kayıtlarının ve yetkilendirme süreçlerinin (Admin, Personel, Üye) takibi

•
Ödünç alma ve iade süreçlerinin otomatize edilmesi

•
Geciken kitapların ve popüler içeriklerin raporlanması

•
Modern yazılım mimarisi ve veritabanı yönetim prensiplerinin uygulanması




📚 Temel Özellikler

📖 Kitap Yönetimi

•
Yeni kitap ekleme, güncelleme ve silme işlemleri (CRUD).

•
Kitap kapak resmi yükleme desteği ve resim yönetimi.

•
Kitapların müsaitlik durumunun gerçek zamanlı takibi.

•
Tür, yazar ve yayınevi bazlı detaylı filtreleme ve arama.

👥 Üye ve Yetki Yönetimi

•
Çoklu rol desteği: Admin, Personel ve Üye.

•
Güvenli kullanıcı doğrulama ve parola hashleme sistemleri.

•
Üye profil yönetimi ve bireysel okuma geçmişi takibi.

🔄 Ödünç Alma ve Etkileşim

•
Kitap ödünç verme ve iade alma süreçlerinin yönetimi.

•
Teslim tarihi takibi ve gecikme durumlarının belirlenmesi.

•
Kitaplar için 1-5 arası puanlama ve yorum yapma sistemi.

📊 Gelişmiş Raporlama

•
En çok okunan kitaplar ve en aktif üyeler analizi.

•
Geciken kitaplar raporu ve kategori bazlı istatistikler.

•
Puanlama verileri üzerinden popüler içerik takibi.




⚙️ Teknik Detaylar

Özellik
Açıklama
Dil
C#
Framework
ASP.NET Core 8.0 MVC
Veritabanı
SQLite / MS SQL Server (EF Core 8.0)
Frontend
HTML5, CSS3 (Bootstrap 5), JavaScript (jQuery)
Güvenlik
Password Hashing, Role-Based Authorization
Mimari
Model-View-Controller (MVC)







💻 Implementasyon Detayları

Proje, Entity Framework Core 8.0'ın modern yaklaşımları kullanılarak geliştirilmiştir. Veritabanı şeması, C# sınıfları üzerinden yönetilmekte ve ilişkisel veri modeli başarıyla uygulanmaktadır.

C# Kodu (Örnek - Kitap Modeli):

C#


public class Book
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Kitap adı zorunludur.")]
    public string Title { get; set; }
    
    public string Author { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    // İlişkisel veriler (Navigasyon Özellikleri)
    public virtual ICollection<Loan> Loans { get; set; }
    public virtual ICollection<BookRating> BookRatings { get; set; }
}



Uygulama, Program.cs içerisinde yapılandırılan gelişmiş bir servis mimarisine sahiptir ve oturum (Session) yönetimi ile kullanıcı deneyimi en üst düzeye çıkarılmıştır.




🚀 Kurulum ve Çalıştırma

1.
Projeyi indirin ve bir klasöre çıkarın.

2.
LibraryAutomation1.sln dosyasını Visual Studio 2022 ile açın.

3.
Package Manager Console üzerinden Update-Database komutunu çalıştırın.

4.
Gerekli bağımlılıkların (NuGet paketleri) yüklendiğinden emin olun.

5.
F5 tuşuna basarak projeyi başlatın ve tarayıcı üzerinden erişim sağlayın.




📂 Proje Yapısı

Plain Text


LibraryAutomation1-master/
├── LibraryAutomation1/
│   ├── Controllers/      # İş mantığının yönetildiği kontrolcüler
│   ├── Models/           # Veritabanı nesneleri ve veri modelleri
│   ├── Views/            # Kullanıcı arayüzü (Razor Pages) dosyaları
│   ├── Data/             # DbContext ve veritabanı yapılandırması
│   ├── wwwroot/          # CSS, JS, Resimler ve kütüphaneler
│   └── Program.cs        # Uygulama başlangıç ve servis yapılandırması
├── LibraryAutomation1.sln
└── LICENSE






📜 Lisans

Bu proje MIT Lisansı kapsamında lisanslanmıştır. Detaylar LICENSE dosyasında yer almaktadır.




👩‍💻 Yazar

Şilan Pehlivan


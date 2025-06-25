using LibraryAutomation1.Data;
using LibraryAutomation1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LibraryAutomation1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger; 
        // HomeController sýnýfýna özel günlük (log) kaydý, uyarý ve hata mesajlarýný kaydetmek için kullanýlan bir günlükleyici (logger) nesnesi.

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Kontrol paneli (dashboard) için gerekli çeþitli istatistikler ve veriler toplanýyor.
            // Anonim bir nesne oluþturularak tüm bu veriler tek bir yapý altýnda birleþtiriliyor.
            var dashboardData = new
            {
                // Toplam kitap sayýsýný veritabanýndan asenkron olarak alýr.
                TotalBooks = await _context.Books.CountAsync(),

                // Mevcut (ödünç alýnmamýþ) kitap sayýsýný veritabanýndan asenkron olarak alýr.
                AvailableBooks = await _context.Books.CountAsync(b => b.IsAvailable),

                // Aktif üye sayýsýný veritabanýndan asenkron olarak alýr (IsActive = true olan üyeler).
                TotalMembers = await _context.Members.CountAsync(m => m.IsActive),

                // Henüz iade edilmemiþ aktif ödünç alma iþlemlerinin sayýsýný alýr.
                ActiveLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null),

                // Vadesi geçmiþ (iade tarihi geçmiþ ve henüz iade edilmemiþ) ödünç alma iþlemlerinin sayýsýný alýr.
                OverdueLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date),

                // Toplam kitap puanlama sayýsýný alýr.
                TotalRatings = await _context.BookRatings.CountAsync(),

                // Son eklenen 5 kitabý ID'ye göre azalan sýrada (en yeniden en eskiye) getirir.
                RecentBooks = await _context.Books
                    .OrderByDescending(b => b.Id) // Kitaplarý ID'ye göre azalan sýrada sýrala
                    .Take(5) // Ýlk 5 kaydý al
                    .ToListAsync(), // Sorguyu çalýþtýr ve liste olarak al

                // En yüksek puanlý 5 kitabý getirir.
                // Sadece en az 3 puanlamasý olan kitaplarý dikkate alýr.
                // Ortalama puana göre azalan, puan sayýsýna göre azalan þekilde sýralar.
                TopRatedBooks = await _context.Books
                    .Where(b => b.RatingCount >= 3) // En az 3 puanlamasý olan kitaplarý filtrele
                    .OrderByDescending(b => b.AverageRating) // Ortalama puana göre azalan sýrada sýrala
                    .ThenByDescending(b => b.RatingCount) // Puanlar eþitse, puan sayýsýna göre azalan sýrada sýrala
                    .Take(5) // Ýlk 5 kaydý al
                    .ToListAsync(), // Sorguyu çalýþtýr ve liste olarak al

                // Son 30 gün içinde en çok ödünç alýnan 5 kitabý getirir.
                PopularBooks = await _context.Loans
                    .Include(l => l.Book) // Ýliþkili Kitap verilerini de dahil et
                    .Where(l => l.LoanDate >= DateTime.Now.AddDays(-30)) // Son 30 gündeki ödünç alma iþlemlerini filtrele
                    .GroupBy(l => l.Book) // Kitap bazýnda grupla
                    .Select(g => new { Book = g.Key, Count = g.Count() }) // Her kitap için ödünç alma sayýsýný say
                    .OrderByDescending(x => x.Count) // Sayýya göre azalan sýrada sýrala (en popülerden)
                    .Take(5) // Ýlk 5 kaydý al
                    .ToListAsync(), // Sorguyu çalýþtýr ve liste olarak al

                // En son yapýlan 10 ödünç alma iþlemini getirir.
                // Kitap ve Üye bilgilerini de içerir.
                RecentLoans = await _context.Loans
                    .Include(l => l.Book) // Ýliþkili Kitap verilerini dahil et
                    .Include(l => l.Member) // Ýliþkili Üye verilerini dahil et
                    .OrderByDescending(l => l.LoanDate) // Ödünç alma tarihine göre azalan sýrada sýrala
                    .Take(10) // Ýlk 10 kaydý al
                    .ToListAsync(), // Sorguyu çalýþtýr ve liste olarak al

                // Gecikmiþ 5 kitabý getirir.
                // Henüz iade edilmemiþ ve vadesi geçmiþ ödünç alma iþlemlerini içerir.
                OverdueBooks = await _context.Loans
                    .Include(l => l.Book) // Ýliþkili Kitap verilerini dahil et
                    .Include(l => l.Member) // Ýliþkili Üye verilerini dahil et
                    .Where(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date) // Gecikmiþ olanlarý filtrele
                    .OrderBy(l => l.DueDate) // Vade tarihine göre artan sýrada sýrala (en eski gecikmiþten)
                    .Take(5) // Ýlk 5 kaydý al
                    .ToListAsync() // Sorguyu çalýþtýr ve liste olarak al
            };

            // Hazýrlanan tüm dashboard verilerini içeren anonim nesneyi View'e (görünüme) gönderir.
            // View, bu nesnedeki verileri kullanarak kontrol panelini oluþturacaktýr.
            return View(dashboardData);
        }

        // Arama sayfasý için action metodu. Kullanýcýnýn girdiði 'query' parametresine göre kitaplar ve üyeler arasýnda arama yapar.
        public async Task<IActionResult> Search(string query)
        {
            // 1. Arama sorgusunun boþ veya null olup olmadýðýný kontrol et.
            if (string.IsNullOrEmpty(query))
            {
                // Eðer sorgu boþsa, boþ kitap ve üye listeleriyle birlikte arama sayfasýný döndür.
                // Bu, kullanýcýnýn henüz bir þey aramadýðý durumlar için baþlangýç veya varsayýlan bir görünüm saðlar.
                return View(new { Books = new List<Book>(), Members = new List<Member>(), Query = "" });
            }

            // 2. Kitaplar arasýnda arama yap.
            // 'query' içinde geçen baþlýk, yazar, ISBN veya tür alanlarýna göre kitaplarý filtreler.
            // Ýlk 20 eþleþen kitabý alýr.
            var books = await _context.Books
                .Where(b => b.Title.Contains(query) ||      // Kitap baþlýðýnda ara
                            b.Author.Contains(query) ||     // Yazar adýnda ara
                            b.ISBN.Contains(query) ||       // ISBN numarasýnda ara
                            b.Genre.Contains(query))        // Tür (genre) adýnda ara
                .Take(20) // Sadece ilk 20 sonucu al (performans için limit)
                .ToListAsync(); // Sorguyu asenkron olarak çalýþtýr ve liste olarak al

            // 3. Üyeler arasýnda arama yap.
            // 'query' içinde geçen ad, soyad veya e-posta alanlarýna göre üyeleri filtreler.
            // Ýlk 10 eþleþen üyeyi alýr.
            var members = await _context.Members
                .Where(m => m.FirstName.Contains(query) ||  // Üye adýnda ara
                            m.LastName.Contains(query) ||   // Üye soyadýnda ara
                            m.Email.Contains(query))        // Üye e-postasýnda ara
                .Take(10) // Sadece ilk 10 sonucu al (performans için limit)
                .ToListAsync(); // Sorguyu asenkron olarak çalýþtýr ve liste olarak al

            // 4. Arama sonuçlarýný tek bir anonim nesnede birleþtir.
            // Bu nesne, hem kitap hem de üye arama sonuçlarýný ve orijinal sorguyu içerir.
            var searchResults = new
            {
                Books = books,   // Kitap arama sonuçlarý
                Members = members, // Üye arama sonuçlarý
                Query = query    // Orijinal arama sorgusu
            };

            // 5. Birleþtirilmiþ arama sonuçlarýný View'e göndererek arama sayfasýný render et.
            // View, bu verileri kullanarak arama sonuçlarýný kullanýcýya gösterecektir.
            return View(searchResults);
        }


        // Hakkýnda sayfasý
        public IActionResult About()
        {
            return View();
        }

        // Ýletiþim sayfasý
        public IActionResult Contact()
        {
            return View();
        }

        // Gizlilik sayfasý
        public IActionResult Privacy()
        {
            return View();
        }

        // Hata sayfasý
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // API uç noktasý - Dashboard verileri (özellikle AJAX çaðrýlarý için tasarlanmýþtýr).
        // Bu metod, kütüphanenin temel istatistiklerini JSON formatýnda döndürür.
        [HttpGet] // Bu metodun sadece HTTP GET istekleriyle çaðrýlabileceðini belirtir.
        public async Task<IActionResult> GetDashboardStats()
        {
            // Anonim bir nesne oluþturarak çeþitli istatistikleri toplar.
            // Bu istatistikler, veritabanýndan çekilir.
            var stats = new
            {
                // Toplam kitap sayýsýný alýr.
                totalBooks = await _context.Books.CountAsync(),

                // Mevcut (ödünç alýnabilir) kitap sayýsýný alýr.
                availableBooks = await _context.Books.CountAsync(b => b.IsAvailable),

                // Toplam aktif üye sayýsýný alýr.
                totalMembers = await _context.Members.CountAsync(m => m.IsActive),

                // Devam eden (henüz iade edilmemiþ) ödünç alma iþlemlerinin sayýsýný alýr.
                activeLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null),

                // Gecikmiþ (iade tarihi geçmiþ ve henüz iade edilmemiþ) ödünç alma iþlemlerinin sayýsýný alýr.
                overdueLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date),

                // Yapýlan toplam puanlama sayýsýný alýr.
                totalRatings = await _context.BookRatings.CountAsync(),

                // Tüm kitap puanlamalarýnýn ortalamasýný hesaplar.
                // Eðer hiç puanlama yoksa '?? 0' ile varsayýlan olarak 0 deðeri atanýr.
                averageRating = await _context.BookRatings.AverageAsync(br => (double?)br.Rating) ?? 0
            };

            // Oluþturulan istatistikler nesnesini JSON formatýnda bir HTTP 200 OK yanýtý olarak döndürür.
            // Bu, istemci tarafýndaki JavaScript'in bu verilere kolayca eriþmesini saðlar.
            return Json(stats);
        }
        // API uç noktasý - Kütüphanedeki son aktiviteleri getirir.
        // Bu metod, genellikle bir kontrol paneli veya bildirim alaný için AJAX ile çaðrýlýr.
        [HttpGet] // Bu metodun yalnýzca HTTP GET isteklerine yanýt vereceðini belirtir.
        public async Task<IActionResult> GetRecentActivity()
        {
            // Veritabanýndan en son 10 ödünç alma kaydýný (loan) asenkron olarak çeker.
            var recentActivity = await _context.Loans
                .Include(l => l.Book)   // Her ödünç alma kaydýyla iliþkili Kitap verilerini de dahil et.
                .Include(l => l.Member) // Her ödünç alma kaydýyla iliþkili Üye verilerini de dahil et.
                .OrderByDescending(l => l.LoanDate) // Kayýtlarý ödünç alma tarihine göre en yeniden en eskiye doðru sýrala.
                .Take(10) // Sadece en yeni 10 kaydý al.
                .Select(l => new // Geri döndürülecek verileri anonim bir nesneye dönüþtürerek özelleþtir.
                {
                    id = l.Id, // Ödünç alma kaydýnýn ID'si.
                    bookTitle = l.Book.Title, // Ödünç alýnan kitabýn baþlýðý.
                    memberName = l.Member.FullName, // Ödünç alan üyenin tam adý.
                    loanDate = l.LoanDate.ToString("dd.MM.yyyy"), // Ödünç alma tarihi (gün.ay.yýl formatýnda).
                    dueDate = l.DueDate.ToString("dd.MM.yyyy"), // Ýade tarihi (gün.ay.yýl formatýnda).
                    isReturned = l.ReturnDate != null, // Kitabýn iade edilip edilmediðini gösteren boolean deðer.
                    isOverdue = l.ReturnDate == null && l.DueDate < DateTime.Now.Date // Kitabýn gecikmiþ olup olmadýðýný gösteren boolean deðer.
                })
                .ToListAsync(); // Oluþturulan sorguyu veritabanýnda çalýþtýr ve sonuçlarý liste olarak al.

            // Elde edilen 'recentActivity' listesini JSON formatýnda HTTP 200 OK yanýtý olarak döndürür.
            // Bu, istemci tarafýndaki JavaScript kodunun bu verilere kolayca eriþip UI'yý güncellemesini saðlar.
            return Json(recentActivity);
        }
    }

    // Hata Görünüm Modeli (Error ViewModel) sýnýfý.
    // Bu model, hata sayfalarýna iletilecek verileri (özellikle istek kimliðini) temsil eder.
    public class ErrorViewModel
    {
        // RequestId (Ýstek Kimliði):
        // Mevcut HTTP isteðinin benzersiz tanýmlayýcýsýný tutan bir string özelliðidir.
        // '?' iþareti, bu özelliðin null olabileceðini belirtir (nullable string).
        public string? RequestId { get; set; }

        // ShowRequestId (Ýstek Kimliðini Göster):
        // Bu özellik, RequestId'nin boþ veya null olup olmadýðýný kontrol eden salt okunur bir boolean (true/false) deðeridir.
        // Eðer RequestId boþ deðilse (yani bir deðeri varsa), 'true' döner ve bu da hata sayfasýnda istek kimliðinin gösterilmesi gerektiðini belirtir.
        // Kullanýcýya gereksiz veya hassas bilgileri göstermemek için bir güvenlik ve kullanýcý deneyimi kontrolü saðlar.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

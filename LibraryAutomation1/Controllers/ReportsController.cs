using LibraryAutomation1.Data;
using LibraryAutomation1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAutomation1.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context; // Veritabanı bağlamı için özel, salt okunur bir alan tanımlar.

        public ReportsController(ApplicationDbContext context) // ReportsController sınıfının yapıcı (constructor) metodu.
        {
            _context = context; // Bağımlılık enjeksiyonu ile gelen ApplicationDbContext örneğini _context alanına atar.
        }

        // Ana rapor sayfası
        public async Task<IActionResult> Index()
        {
            // Kütüphanenin genel istatistiklerini toplamak için anonim bir nesne oluştururuz.
            // 'await' anahtar kelimesi, veritabanı sorgularının eşzamansız olarak tamamlanmasını bekler,
            // böylece uygulamanız bu sırada diğer işlemleri yapabilir ve kullanıcı arayüzü donmaz.
            var stats = new
            {
                TotalBooks = await _context.Books.CountAsync(), // Kütüphanedeki toplam kitap sayısını sayar.
                TotalMembers = await _context.Members.CountAsync(m => m.IsActive), // Aktif (hesabı etkin) toplam üye sayısını sayar.
                ActiveLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null), // Henüz iade edilmemiş, aktif olarak ödünçte olan kitapların sayısını sayar.
                OverdueLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date), // İade tarihi geçmiş ve henüz iade edilmemiş ödünçlerin sayısını sayar.
                TotalRatings = await _context.BookRatings.CountAsync(), // Tüm kitaplar için yapılan toplam puanlama sayısını sayar.
                                                                        // Kitap puanlamalarının ortalamasını hesaplar. Eğer hiç puanlama yapılmamışsa (sonuç null ise), ortalama 0 olarak kabul edilir.
                AverageRating = await _context.BookRatings.AverageAsync(br => (double?)br.Rating) ?? 0
            };

            // Hazırlanan 'stats' (istatistikler) nesnesini ilgili View'e gönderir.
            // View, bu nesnedeki verilere erişerek ana rapor sayfasını dinamik olarak oluşturur.
            return View(stats);
        }

        // En çok ödünç alınan kitaplar
        public async Task<IActionResult> MostBorrowedBooks(int? months) // 'months' parametresi ile belirli bir süre içindeki verileri filtreleyebilirsiniz(örn: son 3 ay).
        {
            // Raporun başlangıç tarihini belirler.
            // Eğer 'months' parametresi bir değer içeriyorsa (örneğin 3), başlangıç tarihi şu anki tarihten o kadar ay öncesi olur.
            // Eğer 'months' null ise (yani belirtilmemişse), DateTime.MinValue kullanılır; bu da tüm zamanlardaki verilerin dikkate alınmasını sağlar.
            var startDate = months.HasValue ? DateTime.Now.AddMonths(-months.Value) : DateTime.MinValue;

            // Veritabanından en çok ödünç alınan kitapları sorgular.
            var mostBorrowedBooks = await _context.Loans // 'Loans' (ödünç alma kayıtları) tablosundan başlıyoruz.
                .Include(l => l.Book) // Her ödünç kaydıyla ilişkili 'Book' (kitap) bilgilerini de sorguya dahil ediyoruz. Bu, kitap başlığı gibi detaylara erişmemizi sağlar.
                .Where(l => l.LoanDate >= startDate) // Sadece belirlenen 'startDate' tarihinden sonraki ödünç alma kayıtlarını filtreleriz.
                .GroupBy(l => l.Book) // Kitaplara göre gruplama yaparız. Her grup, belirli bir kitaba ait tüm ödünç kayıtlarını içerir.
                .Select(g => new // Her grup için (yani her kitap için) yeni bir anonim nesne oluştururuz.
                {
                    Book = g.Key, // Grubun anahtarı olan 'Book' nesnesi (ilgili kitabın tüm bilgileri).
                    BorrowCount = g.Count(), // Bu kitabın ödünç alınma sayısını sayarız.
                    LastBorrowed = g.Max(l => l.LoanDate) // Bu kitabın en son ne zaman ödünç alındığını buluruz.
                })
                .OrderByDescending(x => x.BorrowCount) // Kitapları, ödünç alınma sayısına göre azalan (en çoktan en aza) sırada sıralarız.
                .Take(20) // Sadece ilk 20 kitabı (en çok ödünç alınan 20'yi) alırız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.


            // 'months' parametresini (eğer varsa) View'e aktarırız.
            // Bu, View'in hangi zaman filtresinin uygulandığını bilmesini sağlar.
            ViewBag.Months = months;
            return View(mostBorrowedBooks);
        }

        // En yüksek puanlı kitaplar raporu.
        // 'minRatings' parametresi, bir kitabın bu listede yer alabilmesi için alması gereken minimum puanlama sayısını belirler (varsayılan: 3).
        public async Task<IActionResult> HighestRatedBooks(int minRatings = 3)
        {
            // Veritabanından en yüksek puanlı kitapları sorgular.
            var highestRatedBooks = await _context.Books // 'Books' (kitaplar) tablosundan başlıyoruz.
                .Include(b => b.BookRatings) // Her kitapla ilişkili 'BookRatings' (kitap puanlamaları) bilgilerini de sorguya dahil ediyoruz.
                .Where(b => b.RatingCount >= minRatings) // Sadece belirlenen 'minRatings' sayısından daha fazla veya eşit puanlama almış kitapları filtreleriz. Bu, az sayıda puanla yüksek görünen kitapların dışarıda kalmasını sağlar.
                .OrderByDescending(b => b.AverageRating) // Kitapları, ortalama puanlarına göre azalan (en yüksekten en düşüğe) sırada sıralarız.
                .ThenByDescending(b => b.RatingCount) // Eğer ortalama puanlar eşitse, puanlama sayısına göre azalan sırada sıralarız. (Daha fazla oylanan kitaplar öne çıkar.)
                .Take(20) // Sadece ilk 20 kitabı (en yüksek puanlı 20'yi) alırız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // 'minRatings' parametresini View'e aktarırız. Bu, View'in hangi filtreleme kriterinin uygulandığını bilmesini sağlar.
            ViewBag.MinRatings = minRatings;
            // Hazırlanan 'highestRatedBooks' listesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(highestRatedBooks);
        }

        // En aktif okuyucular raporu.
        // 'months' parametresi ile belirli bir süre içindeki verileri filtreleyebilirsiniz (örn: son 6 ay).
        public async Task<IActionResult> MostActiveReaders(int? months)
        {
            // Raporun başlangıç tarihini belirler.
            // Eğer 'months' parametresi bir değer içeriyorsa (örneğin 6), başlangıç tarihi şu anki tarihten o kadar ay öncesi olur.
            // Eğer 'months' null ise (yani belirtilmemişse), DateTime.MinValue kullanılır; bu da tüm zamanlardaki verilerin dikkate alınmasını sağlar.
            var startDate = months.HasValue ? DateTime.Now.AddMonths(-months.Value) : DateTime.MinValue;

            // Veritabanından en aktif okuyucuları (üyeleri) sorgular.
            var mostActiveReaders = await _context.Loans // 'Loans' (ödünç alma kayıtları) tablosundan başlıyoruz.
                .Include(l => l.Member) // Her ödünç kaydıyla ilişkili 'Member' (üye) bilgilerini de sorguya dahil ediyoruz. Bu, üye adı gibi detaylara erişmemizi sağlar.
                .Where(l => l.LoanDate >= startDate) // Sadece belirlenen 'startDate' tarihinden sonraki ödünç alma kayıtlarını filtreleriz.
                .GroupBy(l => l.Member) // Üyelere göre gruplama yaparız. Her grup, belirli bir üyeye ait tüm ödünç kayıtlarını içerir.
                .Select(g => new // Her grup için (yani her üye için) yeni bir anonim nesne oluştururuz.
                {
                    Member = g.Key, // Grubun anahtarı olan 'Member' nesnesi (ilgili üyenin tüm bilgileri).
                    BorrowCount = g.Count(), // Bu üyenin toplam ödünç alma sayısını sayarız.
                    ReturnedCount = g.Count(l => l.ReturnDate != null), // Bu üyenin iade ettiği kitap sayısını sayarız.
                    OverdueCount = g.Count(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date), // Bu üyenin gecikmiş (iade tarihi geçmiş ve henüz iade edilmemiş) kitap sayısını sayarız.
                    LastActivity = g.Max(l => l.LoanDate) // Bu üyenin en son ne zaman kitap ödünç aldığını buluruz.
                })
                .OrderByDescending(x => x.BorrowCount) // Üyeleri, ödünç alma sayısına göre azalan (en çoktan en aza) sırada sıralarız.
                .Take(20) // Sadece ilk 20 aktif okuyucuyu alırız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // 'months' parametresini (eğer varsa) View'e aktarırız. Bu, View'in hangi zaman filtresinin uygulandığını bilmesini sağlar.
            ViewBag.Months = months;
            // Hazırlanan 'mostActiveReaders' listesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(mostActiveReaders);
        }

        // Aylık istatistikler raporu.
        // 'year' parametresi ile belirli bir yılın istatistiklerini görüntüleyebilirsiniz (varsayılan: mevcut yıl).
        public async Task<IActionResult> MonthlyStats(int? year)
        {
            // Hedef yılı belirler. Eğer 'year' parametresi belirtilmemişse, varsayılan olarak içinde bulunulan yıl kullanılır.
            var targetYear = year ?? DateTime.Now.Year;

            // Belirlenen yılın başlangıç ve bitiş tarihlerini tanımlarız.
            // Başlangıç tarihi: Yılın ilk günü (1 Ocak).
            // Bitiş tarihi: Bir sonraki yılın ilk günü (1 Ocak), bu sayede mevcut yılın son gününe kadar olan tüm veriler dahil edilir.
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear + 1, 1, 1);

            // Veritabanından aylık istatistikleri sorgular.
            var monthlyStats = await _context.Loans // 'Loans' (ödünç alma kayıtları) tablosundan başlıyoruz.
                .Where(l => l.LoanDate >= startDate && l.LoanDate < endDate) // Sadece belirlenen yıl aralığındaki ödünç kayıtlarını filtreleriz.
                .GroupBy(l => l.LoanDate.Month) // Ödünç alma tarihlerinin ayına göre gruplama yaparız (örn: 1 for Ocak, 2 for Şubat vb.).
                .Select(g => new // Her grup (yani her ay) için yeni bir anonim nesne oluştururuz.
                {
                    Month = g.Key, // Grubun anahtarı olan ay numarası.
                    LoanCount = g.Count(), // Bu ayda yapılan toplam ödünç alma sayısını sayarız.
                                           // Bu ayda iade edilen kitap sayısını sayarız. Dikkat: İade tarihinin de aynı yıl içinde olması koşulunu eklemek önemlidir,
                                           // aksi takdirde önceki yıllarda ödünç alınıp bu ay iade edilenler de sayılabilir.
                    ReturnCount = g.Count(l => l.ReturnDate != null && l.ReturnDate >= startDate && l.ReturnDate < endDate),
                    UniqueBooks = g.Select(l => l.BookId).Distinct().Count(), // Bu ayda ödünç alınan benzersiz kitap sayısını sayarız.
                    UniqueMembers = g.Select(l => l.MemberId).Distinct().Count() // Bu ayda kitap ödünç alan benzersiz üye sayısını sayarız.
                })
                .OrderBy(x => x.Month) // Sonuçları ay numarasına göre artan (Ocak'tan Aralık'a) sırada sıralarız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // 'targetYear' değerini View'e aktarırız. Bu, View'in hangi yılın istatistiklerinin gösterildiğini bilmesini sağlar.
            ViewBag.Year = targetYear;
            // Hazırlanan 'monthlyStats' listesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(monthlyStats);
        }

        // Tür bazlı istatistikler raporu. Kütüphanedeki kitap türlerinin popülaritesini ve genel durumunu değerlendirir.
        public async Task<IActionResult> GenreStats()
        {
            // Veritabanından tür bazlı istatistikleri sorgularız.
            var genreStats = await _context.Books // 'Books' (kitaplar) tablosundan başlıyoruz.
                .Where(b => !string.IsNullOrEmpty(b.Genre)) // Sadece 'Genre' (tür) alanı boş olmayan veya null olmayan kitapları dahil ederiz. Bu, anlamsız gruplamaların önüne geçer.
                .GroupBy(b => b.Genre) // Kitapları 'Genre' (tür) alanına göre gruplarız. Her grup, aynı türe ait tüm kitapları içerir.
                .Select(g => new // Her grup (yani her tür) için yeni bir anonim nesne oluştururuz.
                {
                    Genre = g.Key, // Grubun anahtarı olan tür adı (örn: "Bilim Kurgu", "Tarih").
                    BookCount = g.Count(), // Bu türe ait toplam kitap sayısını sayarız.
                                           // Bu türe ait tüm kitapların toplam ödünç alınma sayısını hesaplarız.
                                           // 'SelectMany' kullanarak her kitabın 'Loans' koleksiyonunu düzleştiririz ve sonra sayarız.
                    BorrowCount = g.SelectMany(b => b.Loans).Count(),
                    // Bu türe ait kitapların ortalama puanını hesaplarız.
                    // Sadece 'RatingCount'u (puanlama sayısı) 0'dan büyük olan kitapları dikkate alırız ki henüz puanlanmamış kitaplar ortalamayı etkilemesin.
                    // Eğer hiç puanlama yoksa, ortalama 0 olarak kabul edilir (?? 0).
                    AverageRating = g.Where(b => b.RatingCount > 0).Average(b => (double?)b.AverageRating) ?? 0,
                    TotalRatings = g.Sum(b => b.RatingCount) // Bu türe ait tüm kitaplar için yapılan toplam puanlama sayısını toplarız.
                })
                .OrderByDescending(x => x.BorrowCount) // Sonuçları, ödünç alınma sayısına göre azalan (en çok ödünç alınan türden en aza) sırada sıralarız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // Hazırlanan 'genreStats' listesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(genreStats);
        }

        // Gecikme raporu. İade tarihi geçmiş ve henüz iade edilmemiş tüm ödünç alma kayıtlarını gösterir.
        public async Task<IActionResult> OverdueReport()
        {
            // Veritabanından gecikmiş ödünç alma kayıtlarını sorgularız.
            var overdueReport = await _context.Loans // 'Loans' (ödünç alma kayıtları) tablosundan başlıyoruz.
                .Include(l => l.Book) // Her ödünç kaydıyla ilişkili 'Book' (kitap) bilgilerini dahil ediyoruz.
                .Include(l => l.Member) // Her ödünç kaydıyla ilişkili 'Member' (üye) bilgilerini dahil ediyoruz.
                .Where(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date) // Filtreleme: Kitap henüz iade edilmemiş OLMALIDIR (ReturnDate == null) VE iade tarihi (DueDate) bugünün tarihinden ESKİ OLMALIDIR.
                .Select(l => new // Her gecikmiş ödünç kaydı için yeni bir anonim nesne oluştururuz.
                {
                    Loan = l, // İlgili ödünç alma nesnesinin tamamı.
                              // Gecikme gün sayısını hesaplarız. EF.Functions.DateDiffDay, veritabanı seviyesinde tarih farkını gün olarak hesaplayan bir fonksiyondur.
                              // (İade tarihi - Bugünün tarihi) şeklinde hesaplanır.
                    DaysOverdue = EF.Functions.DateDiffDay(l.DueDate, DateTime.Now),
                    // Tahmini gecikme ücretini hesaplarız. Burada her gecikme günü için 2.00 TL/birim ücret alındığı varsayılmıştır.
                    EstimatedFee = EF.Functions.DateDiffDay(l.DueDate, DateTime.Now) * 2.00m
                })
                .OrderBy(x => x.Loan.DueDate) // Gecikmiş kayıtları, iade tarihine göre artan sırada (en eski gecikmeden en yeni gecikmeye) sıralarız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // Tüm gecikmiş ödünç kayıtlarının toplam tahmini gecikme ücretini hesaplarız.
            var totalOverdueFees = overdueReport.Sum(r => r.EstimatedFee);
            // Hesaplanan toplam tahmini ücreti View'e aktarırız.
            ViewBag.TotalOverdueFees = totalOverdueFees;

            // Hazırlanan 'overdueReport' listesini View'e gönderir ve gecikme raporu sayfasının oluşturulmasını sağlar.
            return View(overdueReport);
        }

        // Üyelik istatistikleri raporu. Kütüphanedeki üyeliklerin yıllık gelişimini ve rol dağılımını gösterir.
        public async Task<IActionResult> MembershipStats()
        {
            // Veritabanından üyelik istatistiklerini sorgularız.
            var membershipStats = await _context.Members // 'Members' (üyeler) tablosundan başlıyoruz.
                .GroupBy(m => m.MembershipDate.Year) // Üyeleri, 'MembershipDate' (üyelik tarihi) yıl bilgisine göre gruplarız. Her grup, aynı yılda üye olan kişileri içerir.
                .Select(g => new // Her grup (yani her üyelik yılı) için yeni bir anonim nesne oluştururuz.
                {
                    Year = g.Key, // Grubun anahtarı olan üyelik yılı (örn: 2022, 2023).
                    NewMembers = g.Count(), // Bu yılda kütüphaneye katılan toplam yeni üye sayısını sayarız.
                    ActiveMembers = g.Count(m => m.IsActive), // Bu yılda üye olanlardan **aktif** durumdaki üye sayısını sayarız.
                    AdminCount = g.Count(m => m.Role == UserRole.Admin), // Bu yılda üye olan ve rolü 'Admin' olanların sayısını sayarız.
                    StaffCount = g.Count(m => m.Role == UserRole.Staff), // Bu yılda üye olan ve rolü 'Staff' (personel) olanların sayısını sayarız.
                    RegularCount = g.Count(m => m.Role == UserRole.Member) // Bu yılda üye olan ve rolü 'Member' (normal üye) olanların sayısını sayarız.
                })
                .OrderByDescending(x => x.Year) // Sonuçları, üyelik yılına göre azalan (en yeni yıldan en eskiye) sırada sıralarız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // Hazırlanan 'membershipStats' listesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(membershipStats);
        }

        // Kitap durumu raporu. Kütüphanedeki kitapların mevcut durumlarını ve popülerliklerini özetler.
        public async Task<IActionResult> BookStatusReport()
        {
            // Kütüphanedeki kitapların çeşitli durumlarını ve özet bilgilerini toplamak için anonim bir nesne oluştururuz.
            // Her bir sorgu asenkron olarak çalışır ve veritabanından gerekli bilgileri çeker.
            var bookStatusReport = new
            {
                // Toplam mevcut (şu anda raflarda olan ve ödünç alınabilir) kitap sayısını sayar.
                AvailableBooks = await _context.Books.CountAsync(b => b.IsAvailable),

                // Toplam ödünç alınmış (şu anda bir üye tarafından tutulan) kitap sayısını sayar.
                BorrowedBooks = await _context.Books.CountAsync(b => !b.IsAvailable),

                // Kütüphaneye eklendiğinden beri hiç ödünç alınmamış kitapların sayısını sayar.
                NeverBorrowedBooks = await _context.Books.CountAsync(b => !b.Loans.Any()),

                // En çok ödünç alınan 5 kitabı bulur.
                MostPopularBooks = await _context.Books // Kitaplar tablosundan başlarız.
                    .Include(b => b.Loans) // Kitaplarla ilişkili ödünç kayıtlarını da dahil ederiz.
                    .Where(b => b.Loans.Any()) // Yalnızca en az bir kez ödünç alınmış kitapları filtreleriz.
                    .OrderByDescending(b => b.Loans.Count()) // Kitapları, toplam ödünç alınma sayılarına göre azalan sırada (en çoktan en aza) sıralarız.
                    .Take(5) // İlk 5 kitabı alırız.
                    .ToListAsync(), // Sonuçları listeye dönüştürürüz.

                // En son kütüphaneye eklenen 10 kitabı bulur.
                RecentlyAddedBooks = await _context.Books // Kitaplar tablosundan başlarız.
                    .OrderByDescending(b => b.Id) // Kitapları ID'lerine göre azalan sırada sıralarız. Genellikle, daha yüksek ID'ler daha yeni eklenen kitapları gösterir.
                    .Take(10) // İlk 10 kitabı alırız.
                    .ToListAsync() // Sonuçları listeye dönüştürürüz.
            };

            // Hazırlanan 'bookStatusReport' nesnesini View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(bookStatusReport);
        }

        // Puanlama istatistikleri raporu. Kitap puanlarının dağılımını, en beğenilen kitapları ve son yorumları gösterir.
        public async Task<IActionResult> RatingStats()
        {
            // 1. Puanlama Dağılımını Hesapla:
            // Hangi puanın (1'den 5'e kadar) kaç kez verildiğini ve toplam puanlamalar içindeki yüzdesini bulur.
            var ratingStats = await _context.BookRatings // 'BookRatings' (kitap puanlamaları) tablosundan başlıyoruz.
                .GroupBy(br => br.Rating) // Puan değerine (örneğin 1, 2, 3, 4, 5) göre gruplarız.
                .Select(g => new // Her grup (yani her puan değeri) için yeni bir anonim nesne oluştururuz.
                {
                    Rating = g.Key, // Grubun anahtarı olan puan değeri.
                    Count = g.Count(), // Bu puanın verildiği toplam puanlama sayısını sayarız.
                                       // Bu puanın toplam puanlamalar içindeki yüzdesini hesaplarız.
                                       // Not: _context.BookRatings.Count() her seferinde veritabanından çekilebilir, performans için önceden bir değişkene atamak daha iyi olabilir.
                    Percentage = (double)g.Count() / _context.BookRatings.Count() * 100
                })
                .OrderByDescending(x => x.Rating) // Puan değerine göre azalan sırada (5 yıldızdan 1 yıldıza doğru) sıralarız.
                .ToListAsync(); // Sorgu sonuçlarını asenkron olarak bir listeye dönüştürürüz.

            // 2. En Yüksek Puanlı Kitapları Getir:
            // En az 3 puanlama almış ve ortalama puanı en yüksek olan ilk 10 kitabı bulur.
            var topRatedBooks = await _context.Books // 'Books' (kitaplar) tablosundan başlıyoruz.
                .Where(b => b.RatingCount >= 3) // Sadece en az 3 puanlama almış kitapları filtreleriz. Bu, az sayıda oyla yanıltıcı yüksek puanları dışlar.
                .OrderByDescending(b => b.AverageRating) // Kitapları, ortalama puanlarına göre azalan sırada sıralarız.
                .ThenByDescending(b => b.RatingCount) // Ortalama puanları aynıysa, puanlama sayısına göre azalan sırada sıralarız (daha çok oylanan daha güvenilir kabul edilir).
                .Take(10) // İlk 10 kitabı alırız.
                .ToListAsync(); // Sonuçları listeye dönüştürürüz.

            // 3. Son Yapılan Puanlamaları Getir:
            // En son yapılmış 20 kitap puanlamasını ilgili kitap ve üye bilgileriyle birlikte bulur.
            var recentRatings = await _context.BookRatings // 'BookRatings' (kitap puanlamaları) tablosundan başlıyoruz.
                .Include(br => br.Book) // Her puanlamayla ilişkili 'Book' (kitap) bilgilerini dahil ederiz.
                .Include(br => br.Member) // Her puanlamayla ilişkili 'Member' (üye) bilgilerini dahil ederiz.
                .OrderByDescending(br => br.RatingDate) // Puanlama tarihine göre azalan sırada (en yeniden en eskiye) sıralarız.
                .Take(20) // İlk 20 puanlamayı alırız.
                .ToListAsync(); // Sonuçları listeye dönüştürürüz.

            // Hesaplanan en yüksek puanlı kitaplar ve son puanlamalar listelerini ViewBag aracılığıyla View'e aktarırız.
            // ViewBag, View'e geçici veri aktarımı için kullanılır.
            ViewBag.TopRatedBooks = topRatedBooks;
            ViewBag.RecentRatings = recentRatings;

            // Puanlama dağılımı listesini (ratingStats) ana model olarak View'e gönderir ve sayfanın oluşturulmasını sağlar.
            return View(ratingStats);
        }

        // Detaylı kitap raporu. Belirli bir kitabın tüm ödünç alma geçmişini, puanlamalarını ve mevcut durumunu gösterir.
        // 'id' parametresi, detaylarını görüntülemek istediğimiz kitabın benzersiz kimliğidir.
        public async Task<IActionResult> BookDetailReport(int id)
        {
            // Veritabanından belirtilen ID'ye sahip kitabı buluruz.
            // Kitapla ilişkili tüm ödünç alma kayıtlarını (Loans) ve her ödünç kaydıyla ilişkili üye bilgilerini (Member) dahil ederiz.
            // Aynı şekilde, kitapla ilişkili tüm puanlama kayıtlarını (BookRatings) ve her puanlamayı yapan üye bilgilerini (Member) dahil ederiz.
            var book = await _context.Books
                .Include(b => b.Loans)
                    .ThenInclude(l => l.Member) // Ödünç alan üyeleri dahil et
                .Include(b => b.BookRatings)
                    .ThenInclude(br => br.Member) // Puanlamayı yapan üyeleri dahil et
                .FirstOrDefaultAsync(b => b.Id == id); // Belirtilen ID'ye sahip ilk kitabı getir

            // Eğer belirtilen ID'ye sahip bir kitap bulunamazsa, bir 404 Not Found (Bulunamadı) hatası döndürürüz.
            if (book == null)
                return NotFound();

            // Kitabın detaylı raporunu oluşturmak için anonim bir nesne kullanırız.
            // Bu nesne, View'e gönderilecek tüm ilgili verileri içerir.
            var bookReport = new
            {
                Book = book, // Kitabın kendisi (tüm özellikleri ve dahil edilen ilişkili verilerle birlikte).
                TotalLoans = book.Loans.Count, // Kitabın toplam kaç kez ödünç alındığını sayarız.
                CurrentlyBorrowed = book.Loans.Any(l => l.ReturnDate == null), // Kitabın şu anda bir üye tarafından ödünç alınıp alınmadığını kontrol ederiz.
                AverageRating = book.AverageRating, // Kitabın ortalama puanını alırız.
                                                    // Kitap puanlamalarının dağılımını hesaplarız (örn: 5 yıldızdan kaç adet, 4 yıldızdan kaç adet vb.).
                RatingDistribution = book.BookRatings.GroupBy(br => br.Rating) // Puan değerine göre gruplarız.
                    .Select(g => new { Rating = g.Key, Count = g.Count() }) // Her puan değeri için sayısını alırız.
                    .OrderByDescending(x => x.Rating) // Puan değerine göre azalan sırada sıralarız.
                    .ToList(), // Listeye dönüştürürüz.
                RecentLoans = book.Loans.OrderByDescending(l => l.LoanDate).Take(10).ToList(), // Kitabın en son 10 ödünç alma işlemini tarihe göre azalan sırada getiririz.
                RecentRatings = book.BookRatings.OrderByDescending(br => br.RatingDate).Take(10).ToList() // Kitaba yapılan en son 10 puanlamayı tarihe göre azalan sırada getiririz.
            };

            // Hazırlanan 'bookReport' nesnesini View'e gönderir ve detaylı kitap raporu sayfasının oluşturulmasını sağlar.
            return View(bookReport);
        }
    }
}


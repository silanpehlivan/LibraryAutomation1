using LibraryAutomation1.Data; // Veritabanı bağlamını (ApplicationDbContext) içeren namespace. Veritabanı işlemleri için gereklidir.
using LibraryAutomation1.Models; // Uygulamanın model sınıflarını (Loan, Book, Member vb.) içeren namespace. Veri yapılarını tanımlar.
using Microsoft.AspNetCore.Mvc; // ASP.NET Core MVC framework'ünün temel sınıflarını (Controller, IActionResult vb.) içerir.
using Microsoft.AspNetCore.Mvc.Rendering; // HTML select listeleri oluşturmak için kullanılan sınıfları (SelectList) içerir.
using Microsoft.EntityFrameworkCore; // Entity Framework Core'un temel sınıflarını (DbSet, Include, ToListAsync vb.) içerir. Veritabanı sorguları ve işlemleri için gereklidir.

namespace LibraryAutomation1.Controllers
{
    // LoansController sınıfı, ödünç alma (Loan) işlemleriyle ilgili HTTP isteklerini yönetir.
    public class LoansController : Controller
    {
        // Veritabanı bağlamı nesnesi. Uygulamanın veritabanı ile etkileşimi bu nesne üzerinden gerçekleşir.
        private readonly ApplicationDbContext _context;
        // Günlük gecikme ücretini tanımlayan sabit. decimal tipi, para birimleri için uygun hassasiyet sağlar.
        private const decimal DailyLateFee = 2.00m;
        // Varsayılan ödünç verme süresini (gün cinsinden) tanımlayan sabit.
        private const int DefaultLoanDays = 15;

        // Constructor (yapıcı metod).
        // Dependency Injection (Bağımlılık Enjeksiyonu) kullanarak ApplicationDbContext örneğini alır.
        // Bu sayede controller, veritabanı işlemleri için gerekli bağlama sahip olur.
        public LoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tüm ödünç işlemlerini listeleyen action metodu.
        // Filtreleme seçenekleri (durum, üye adı, gecikme durumu) sunar.
        public async Task<IActionResult> Index(string statusFilter, string memberFilter, bool? overdueOnly)
        {
            // Görünüme (View) filtreleme değerlerini aktarmak için ViewData kullanılır.
            ViewData["StatusFilter"] = statusFilter;   // Mevcut durum filtresi
            ViewData["MemberFilter"] = memberFilter;   // Mevcut üye filtresi
            ViewData["OverdueOnly"] = overdueOnly;     // Sadece gecikmişleri göster filtresi

            // Tüm ödünç alma kayıtlarını (Loan) ve ilişkili Kitap (Book) ile Üye (Member) bilgilerini getirir.
            // .Include() metodu, eager loading yaparak ilişkili verilerin tek bir sorguda yüklenmesini sağlar.
            var loans = from l in _context.Loans
                                .Include(l => l.Book)   // Ödünç alınan kitap bilgilerini yükle
                                .Include(l => l.Member) // Ödünç alan üye bilgilerini yükle
                        select l;

            // Filtreleme işlemleri:
            // Durum filtresi: Eğer 'statusFilter' boş değilse ve geçerli bir LoanStatus enum değeri ise, duruma göre filtrele.
            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<LoanStatus>(statusFilter, out var status))
            {
                loans = loans.Where(l => l.Status == status);
            }

            // Üye filtresi: Eğer 'memberFilter' boş değilse, üyenin adı veya soyadında geçen kayıtlara göre filtrele.
            if (!string.IsNullOrEmpty(memberFilter))
            {
                loans = loans.Where(l => l.Member.FirstName.Contains(memberFilter) ||
                                         l.Member.LastName.Contains(memberFilter));
            }

            // Gecikmiş kitaplar filtresi: Eğer 'overdueOnly' true ise, iade edilmemiş ve vadesi geçmiş ödünç kayıtlarını filtrele.
            if (overdueOnly == true)
            {
                loans = loans.Where(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date);
            }

            // Gecikmiş ödünç kayıtlarının durumunu ve gecikme ücretlerini güncelleyen yardımcı metodu çağır.
            await UpdateOverdueLoans();

            // Filtrelenmiş ve güncellenmiş ödünç kayıtlarını ödünç alma tarihine göre azalan sırada sıralar ve görünüme gönderir.
            return View(await loans.OrderByDescending(l => l.LoanDate).ToListAsync());
        }

        // Ödünç verme formu (HTTP GET isteği için).
        // Kullanıcıya yeni bir ödünç işlemi oluşturmak için bir form sunar.
        public IActionResult Create()
        {
            // Kitap ve üye seçimi için dropdown listeleri hazırlar.
            // Sadece müsait olan kitapları ve aktif olan üyeleri listeler.
            ViewData["BookId"] = new SelectList(_context.Books.Where(b => b.IsAvailable), "Id", "Title"); // Müsait kitaplar
            ViewData["MemberId"] = new SelectList(_context.Members.Where(m => m.IsActive), "Id", "FullName"); // Aktif üyeler
            return View(); // Formu içeren View'i döndür.
        }

        // Ödünç verme işlemi (HTTP POST isteği için).
        // Formdan gelen verileri işler ve yeni bir ödünç kaydı oluşturur.
        [HttpPost] // Bu metodun sadece HTTP POST istekleriyle tetikleneceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> Create([Bind("BookId,MemberId,Notes")] Loan loan, int loanDays = DefaultLoanDays)
        {
            // ModelState'i manipüle ederek, doğrudan formdan gelmeyen ancak ilişkili olan nesneleri (Book, Member) doğrulamadan hariç tutar.
            // Bu, Entity Framework'ün ilişkili nesnelerin doğruluğunu otomatik olarak kontrol etmesini engeller,
            // çünkü bu nesneler daha sonra veritabanından yüklenecektir.
            loan.Book = await _context.Books.FindAsync(loan.BookId);
            loan.Member = await _context.Members.FindAsync(loan.MemberId);
            ModelState.Remove("Book");
            ModelState.Remove("Member");

            // Model doğrulamasının başarılı olup olmadığını kontrol eder.
            if (ModelState.IsValid)
            {
                // Kitap ve üye bilgilerini veritabanından getir.
                var book = await _context.Books.FindAsync(loan.BookId);
                var member = await _context.Members.FindAsync(loan.MemberId);

                // Kitap ve üye bulunup bulunmadığını ve durumlarının uygun olup olmadığını kontrol et.
                if (book != null && book.IsAvailable && member != null && member.IsActive)
                {
                    // Üyenin aktif ödünç aldığı kitap sayısını kontrol et (maksimum 5 limit).
                    var activeLoansCount = await _context.Loans
                        .CountAsync(l => l.MemberId == loan.MemberId && l.ReturnDate == null);

                    if (activeLoansCount >= 5)
                    {
                        // Eğer üye zaten 5 kitap ödünç almışsa hata mesajı ekle.
                        ModelState.AddModelError("", "Bir üye en fazla 5 kitap ödünç alabilir.");
                    }
                    else
                    {
                        // Ödünç alma ve iade tarihlerini ayarla.
                        loan.LoanDate = DateTime.Now; // Ödünç alma tarihi şimdiki zaman
                        loan.DueDate = DateTime.Now.AddDays(loanDays); // İade tarihi, ödünç alma tarihine ödünç süresini ekleyerek bulunur.
                        loan.ReturnDate = null; // Kitap henüz iade edilmediği için iade tarihi null.
                        loan.Status = LoanStatus.Active; // Ödünç durumunu aktif olarak ayarla.

                        // Ödünç işlemini veritabanına ekle ve kitabın durumunu müsait değil olarak güncelle.
                        _context.Add(loan); // Yeni ödünç kaydını bağlama ekle.
                        book.IsAvailable = false; // Kitabı müsait değil olarak işaretle.
                        _context.Update(book); // Kitap nesnesini güncellendi olarak işaretle.

                        await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.
                        // Başarılı mesajı TempData'ya ekle (bir sonraki HTTP isteği için).
                        TempData["Success"] = $"{book.Title} kitabı {member.FullName} adlı üyeye ödünç verildi.";
                        return RedirectToAction("Index"); // Ödünç listesi sayfasına yönlendir.
                    }
                }
                else
                {
                    // Kitap müsait değilse veya üye aktif değilse hata mesajı ekle.
                    ModelState.AddModelError("", "Bu kitap şu anda müsait değil veya üye aktif değil.");
                }
            }

            // Model doğrulama başarısız olursa veya üstteki koşullar sağlanmazsa:
            // Formdaki dropdown listelerini tekrar yükle (seçili değerleri koruyarak).
            ViewData["BookId"] = new SelectList(_context.Books.Where(b => b.IsAvailable), "Id", "Title", loan.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members.Where(m => m.IsActive), "Id", "FullName", loan.MemberId);
            return View(loan); // Hata mesajlarıyla birlikte formu tekrar göster.
        }

        // Kitap iade formu (HTTP GET isteği için).
        // Belirli bir ödünç kaydının detaylarını gösterir ve iade onayı ister.
        public async Task<IActionResult> Return(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // İlgili ödünç kaydını, kitap ve üye bilgileriyle birlikte getir.
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);

            // Ödünç kaydı yoksa veya zaten iade edilmişse NotFound döndür.
            if (loan == null || loan.ReturnDate != null)
                return NotFound();

            // Gecikme ücretini hesapla: Eğer bugünün tarihi vade tarihinden sonraysa.
            if (DateTime.Now.Date > loan.DueDate.Date)
            {
                var daysLate = (DateTime.Now.Date - loan.DueDate.Date).Days; // Gecikilen gün sayısını bul.
                loan.LateFee = daysLate * DailyLateFee; // Gecikme ücretini hesapla.
            }

            return View(loan); // Ödünç kaydını içeren View'i döndür.
        }

        // Kitap iade işlemi (HTTP POST isteği için).
        // İade işlemini onaylar, kitabın durumunu günceller ve gecikme ücretini kaydeder.
        [HttpPost] // Bu metodun sadece HTTP POST istekleriyle tetikleneceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> ConfirmReturn(int id, int? rating, string comment)
        {
            // İlgili ödünç kaydını, kitap ve üye bilgileriyle birlikte getir.
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);

            // Ödünç kaydı varsa ve henüz iade edilmemişse devam et.
            if (loan != null && loan.ReturnDate == null)
            {
                // İade tarihini ve ödünç durumunu ayarla.
                loan.ReturnDate = DateTime.Now; // İade tarihi şimdiki zaman.
                loan.Status = LoanStatus.Returned; // Durumu 'İade Edildi' olarak ayarla.

                // Gecikme ücretini tekrar hesapla (güvenlik için POST'ta da).
                if (DateTime.Now.Date > loan.DueDate.Date)
                {
                    var daysLate = (DateTime.Now.Date - loan.DueDate.Date).Days;
                    loan.LateFee = daysLate * DailyLateFee;
                }

                _context.Update(loan); // Ödünç kaydını güncellendi olarak işaretle.

                // Kitabı tekrar müsait olarak işaretle.
                loan.Book.IsAvailable = true;
                _context.Update(loan.Book); // Kitap nesnesini güncellendi olarak işaretle.

                // Eğer kullanıcı bir puanlama girdiyse, bu puanlamayı kaydet veya güncelle.
                if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
                {
                    // Mevcut puanlamayı kontrol et.
                    var existingRating = await _context.BookRatings
                        .FirstOrDefaultAsync(br => br.BookId == loan.BookId && br.MemberId == loan.MemberId);

                    if (existingRating != null)
                    {
                        // Mevcutsa güncelle.
                        existingRating.Rating = rating.Value;
                        existingRating.Comment = comment;
                        existingRating.RatingDate = DateTime.Now;
                        _context.Update(existingRating);
                    }
                    else
                    {
                        // Yoksa yeni bir puanlama oluştur.
                        var bookRating = new BookRating
                        {
                            BookId = loan.BookId,
                            MemberId = loan.MemberId,
                            Rating = rating.Value,
                            Comment = comment,
                            RatingDate = DateTime.Now
                        };
                        _context.Add(bookRating);
                    }

                    // Kitabın ortalama puanını ve puan sayısını güncelleyen yardımcı metodu çağır.
                    await UpdateBookRating(loan.BookId);
                }

                await _context.SaveChangesAsync(); // Tüm değişiklikleri veritabanına kaydet.

                // Başarı ve potansiyel uyarı mesajlarını TempData'ya ekle.
                TempData["Success"] = $"{loan.Book.Title} kitabı başarıyla iade edildi.";
                if (loan.LateFee > 0)
                {
                    TempData["Warning"] = $"Gecikme ücreti: {loan.LateFee:C}"; // Para birimi formatında gecikme ücretini göster.
                }
            }

            return RedirectToAction(nameof(Index)); // Ödünç listesi sayfasına yönlendir.
        }

        // Ödünç süresi uzatma işlemi (HTTP POST isteği için).
        // Belirli bir ödünç kaydının iade tarihini uzatır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendLoan(int id, int additionalDays = 7)
        {
            // İlgili ödünç kaydını, kitap ve üye bilgileriyle birlikte getir.
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);

            // Ödünç kaydı varsa ve henüz iade edilmemişse devam et.
            if (loan != null && loan.ReturnDate == null)
            {
                // Maksimum uzatma sayısını kontrol et (örneğin 2 kez).
                // Burada mevcut ödünç kaydının kaç kez uzatıldığını bulmak için karmaşık bir sorgu var.
                // Bu sorgu, varsayılan ödünç süresinden (DefaultLoanDays) daha uzun olan ödünçleri sayar.
                var extensionCount = await _context.Loans
                    .CountAsync(l => l.BookId == loan.BookId && l.MemberId == loan.MemberId &&
                                     l.DueDate > l.LoanDate.AddDays(DefaultLoanDays));

                if (extensionCount >= 2) // Eğer uzatma sayısı maksimuma ulaştıysa hata ver.
                {
                    TempData["Error"] = "Bu kitap için maksimum uzatma sayısına ulaşıldı.";
                }
                else
                {
                    // Vade tarihini ek günler kadar uzat.
                    loan.DueDate = loan.DueDate.AddDays(additionalDays);
                    // Notlar alanına uzatma bilgisini ekle.
                    loan.Notes += $" | {DateTime.Now:dd.MM.yyyy} tarihinde {additionalDays} gün uzatıldı.";
                    _context.Update(loan); // Ödünç kaydını güncellendi olarak işaretle.
                    await _context.SaveChangesAsync(); // Değişiklikleri kaydet.

                    // Başarı mesajı ekle.
                    TempData["Success"] = $"Ödünç süresi {additionalDays} gün uzatıldı. Yeni teslim tarihi: {loan.DueDate:dd.MM.yyyy}";
                }
            }

            // Ödünç detay sayfasına geri yönlendir.
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Ödünç işlemi detaylarını görüntüleme action metodu.
        public async Task<IActionResult> Details(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // İlgili ödünç kaydını, kitap ve üye bilgileriyle birlikte getir.
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Kayıt bulunamazsa NotFound döndür.
            if (loan == null)
                return NotFound();

            return View(loan); // Ödünç kaydını içeren View'i döndür.
        }

        // Gecikmiş kitapları listeleyen action metodu.
        public async Task<IActionResult> OverdueBooks()
        {
            // Henüz iade edilmemiş ve vadesi geçmiş tüm ödünç kayıtlarını getir.
            var overdueLoans = await _context.Loans
                .Include(l => l.Book)   // Kitap bilgilerini dahil et.
                .Include(l => l.Member) // Üye bilgilerini dahil et.
                .Where(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date) // Gecikmiş olanları filtrele.
                .OrderBy(l => l.DueDate) // Vade tarihine göre artan sırada sırala (en eski gecikmişten).
                .ToListAsync(); // Sorguyu çalıştır ve liste olarak al.

            return View(overdueLoans); // Gecikmiş ödünç kayıtlarını içeren View'i döndür.
        }

        // Üye ödünç geçmişini listeleyen action metodu.
        public async Task<IActionResult> MemberHistory(int memberId)
        {
            // Üye ID'sine göre üyeyi bul.
            var member = await _context.Members.FindAsync(memberId);
            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            // Belirli bir üyeye ait tüm ödünç kayıtlarını, kitap bilgileriyle birlikte getir.
            var memberLoans = await _context.Loans
                .Include(l => l.Book) // Kitap bilgilerini dahil et.
                .Where(l => l.MemberId == memberId) // Belirli üyeye ait olanları filtrele.
                .OrderByDescending(l => l.LoanDate) // Ödünç alma tarihine göre azalan sırada sırala.
                .ToListAsync(); // Sorguyu çalıştır ve liste olarak al.

            ViewBag.Member = member; // Üye bilgisini View'e aktar (başlık vb. için kullanılabilir).
            return View(memberLoans); // Üyenin ödünç geçmişini içeren View'i döndür.
        }

        // Yardımcı metod: Gecikmiş ödünç kayıtlarının durumunu ve ücretlerini günceller.
        // Public action metotlarından çağrılarak verilerin güncel kalmasını sağlar.
        private async Task UpdateOverdueLoans()
        {
            // Henüz iade edilmemiş, vadesi geçmiş ve henüz 'Gecikmiş' olarak işaretlenmemiş ödünç kayıtlarını bul.
            var overdueLoans = await _context.Loans
                .Where(l => l.ReturnDate == null && l.DueDate < DateTime.Now.Date && l.Status != LoanStatus.Overdue)
                .ToListAsync();

            // Bulunan her gecikmiş ödünç kaydı için durumu ve gecikme ücretini güncelle.
            foreach (var loan in overdueLoans)
            {
                loan.Status = LoanStatus.Overdue; // Durumu 'Gecikmiş' olarak ayarla.
                var daysLate = (DateTime.Now.Date - loan.DueDate.Date).Days; // Gecikilen gün sayısını hesapla.
                loan.LateFee = daysLate * DailyLateFee; // Gecikme ücretini hesapla.
            }

            // Eğer güncellenecek kayıt varsa, değişiklikleri toplu olarak kaydet.
            if (overdueLoans.Any())
            {
                _context.UpdateRange(overdueLoans); // Birden fazla nesneyi güncellendi olarak işaretle.
                await _context.SaveChangesAsync(); // Tüm değişiklikleri veritabanına kaydet.
            }
        }

        // Yardımcı metod: Kitabın ortalama puanını ve puan sayısını günceller.
        // BookRatings tablosundaki ilgili kitabın tüm puanlarını alarak hesaplama yapar.
        private async Task UpdateBookRating(int bookId)
        {
            // Güncellenecek kitabı veritabanından bul.
            var book = await _context.Books.FindAsync(bookId);
            // Kitap bulunursa devam et.
            if (book != null)
            {
                // Kitaba ait tüm puanlama kayıtlarını getir.
                var ratings = await _context.BookRatings
                    .Where(br => br.BookId == bookId)
                    .ToListAsync();

                // Eğer puanlama kayıtları varsa ortalama puanı ve sayısını hesapla.
                if (ratings.Any())
                {
                    // Ortalama puanı hesapla ve 2 ondalık basamağa yuvarla.
                    book.AverageRating = Math.Round(ratings.Average(r => r.Rating), 2);
                    // Puanlama sayısını ayarla.
                    book.RatingCount = ratings.Count;
                }
                else
                {
                    // Puanlama yoksa ortalama puanı ve sayıyı sıfırla.
                    book.AverageRating = 0;
                    book.RatingCount = 0;
                }

                _context.Update(book); // Kitap nesnesini güncellendi olarak işaretle.
                await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.
            }
        }
    }
}


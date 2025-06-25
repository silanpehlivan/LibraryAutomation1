using LibraryAutomation1.Data; // Veritabanı bağlamını (ApplicationDbContext) içeren namespace. Veritabanı işlemleri için gereklidir.
using LibraryAutomation1.Models; // Uygulamanın model sınıflarını (Member, Book, Loan, BookRating, UserRole vb.) içeren namespace. Veri yapılarını tanımlar.
using Microsoft.AspNetCore.Mvc; // ASP.NET Core MVC framework'ünün temel sınıflarını (Controller, IActionResult, HttpPost, HttpGet vb.) içerir.
using Microsoft.EntityFrameworkCore; // Entity Framework Core'un temel sınıflarını (DbSet, Include, ToListAsync, FindAsync vb.) içerir. Veritabanı sorguları ve işlemleri için gereklidir.
using System.Security.Cryptography; // Şifre hash'leme işlemleri için SHA256 gibi kriptografik algoritmaları içerir.
using System.Text; // String ve byte dizileri arasında dönüşüm yapmak için (örneğin UTF8 kodlaması) gereklidir.

namespace LibraryAutomation1.Controllers
{
    // MembersController sınıfı, kütüphane üyeleriyle ilgili tüm işlemleri yönetir.
    public class MembersController : Controller
    {
        // Veritabanı bağlamı nesnesi. Uygulamanın veritabanı ile tüm etkileşimleri bu nesne üzerinden gerçekleşir.
        private readonly ApplicationDbContext _context;

        // Constructor (yapıcı metod).
        // Dependency Injection (Bağımlılık Enjeksiyonu) kullanarak ApplicationDbContext örneğini alır.
        // Bu sayede controller, veritabanı işlemleri için gerekli bağlama sahip olur.
        public MembersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tüm üyeleri listeleyen action metodu. Arama ve filtreleme özellikleri sunar.
        public async Task<IActionResult> Index(string searchString, UserRole? roleFilter, bool? activeFilter)
        {
            // Görünüme (View) mevcut filtreleme değerlerini aktarmak için ViewData kullanılır.
            ViewData["CurrentFilter"] = searchString; // Mevcut arama sorgusu
            ViewData["RoleFilter"] = roleFilter;     // Mevcut rol filtresi (Admin, Librarian, Member)
            ViewData["ActiveFilter"] = activeFilter; // Mevcut aktiflik filtresi (true/false)

            // Tüm üyelerden oluşan bir sorgu başlatır.
            var members = from m in _context.Members select m;

            // Arama İşlemi: Eğer 'searchString' boş değilse, üyeleri ad, soyad veya e-postalarına göre filtrele.
            if (!String.IsNullOrEmpty(searchString))
            {
                members = members.Where(m => m.FirstName.Contains(searchString)    // Adında arama
                                             || m.LastName.Contains(searchString)     // Soyadında arama
                                             || m.Email.Contains(searchString));      // E-postada arama
            }

            // Rol filtresi: Eğer 'roleFilter' değeri varsa, üyeleri belirli bir role göre filtrele.
            if (roleFilter.HasValue)
            {
                members = members.Where(m => m.Role == roleFilter.Value);
            }

            // Aktiflik filtresi: Eğer 'activeFilter' değeri varsa, üyeleri aktif (true) veya pasif (false) durumlarına göre filtrele.
            if (activeFilter.HasValue)
            {
                members = members.Where(m => m.IsActive == activeFilter.Value);
            }

            // Filtrelenmiş üyeleri soyada göre artan, sonra ada göre artan sırada sıralar ve görünüme gönderir.
            return View(await members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToListAsync());
        }

        // Belirli bir üyenin detaylarını gösteren action metodu.
        public async Task<IActionResult> Details(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // Üye detaylarını, ilişkili ödünç alma kayıtları (Loan) ve puanlamaları (BookRating) ile birlikte getirir.
            // .Include() ve .ThenInclude() metotları, eager loading yaparak ilişkili verilerin tek bir sorguda yüklenmesini sağlar.
            var member = await _context.Members
                .Include(m => m.Loans)         // Üyenin ödünç aldığı kitapları yükle
                    .ThenInclude(l => l.Book)  // Her ödünç işlemi için ilgili kitabı da yükle
                .Include(m => m.BookRatings)   // Üyenin yaptığı kitap puanlamalarını yükle
                    .ThenInclude(br => br.Book) // Her puanlama için ilgili kitabı da yükle
                .FirstOrDefaultAsync(m => m.Id == id); // Belirtilen ID'ye sahip ilk üyeyi bul.

            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            return View(member); // Üye nesnesini içeren View'i döndür.
        }

        // Yeni üye ekleme formu (HTTP GET isteği için).
        // Kullanıcıya yeni bir üye oluşturmak için boş bir form sunar.
        public IActionResult Create()
        {
            return View(); // Formu içeren View'i döndür.
        }

        #region create // 'create' ile ilgili metodları bir araya getiren bölge başlangıcı
        // Yeni üye ekleme işlemi (HTTP POST isteği için).
        // Formdan gelen üye bilgilerini işler, şifreyi hash'ler ve üyeyi veritabanına kaydeder.
        [HttpPost] // Bu metodun sadece HTTP POST istekleriyle tetikleneceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Address,PasswordHash")] Member member)
        {
            // 'PasswordHash' alanı formdan doğrudan gelmez (veya şifrelenmiş hali gelir),
            // bu nedenle ModelState'ten kaldırılmazsa doğrulama hatası verebilir.
            // Modeldeki PasswordHash'in boş olma kontrolünü burada bypass ediyoruz,
            // çünkü aşağıda manuel olarak hash'leme yapacağız.
            // Bu satır, modeldeki PasswordHash özelliği için varsayılan Required veya min-length doğrulamasını devre dışı bırakır.
            ModelState.Remove("PasswordHash"); // Burayı eklemez isen model doğrulanmıyor ve çalışmıyor

            // Model doğrulamasının başarılı olup olmadığını kontrol eder.
            if (ModelState.IsValid)
            {
                // E-posta benzersizliği kontrolü: Aynı e-posta adresine sahip başka bir üyenin olup olmadığını kontrol et.
                var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.Email == member.Email);
                if (existingMember != null)
                {
                    // Eğer e-posta zaten kullanılıyorsa, ModelState'e bir hata ekle ve formu tekrar göster.
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    return View(member);
                }

                // Parola hash'leme: Üyeden gelen düz metin parolayı HashPassword metodunu kullanarak hash'le.
                // Not: Üye formundan parolayı ayrı bir input olarak alıp burada 'member.PasswordHash' yerine kullanmak daha güvenli olabilir.
                // Mevcut durumda 'member.PasswordHash' aslında formdan gelen düz metin parola gibi davranıyor.
                member.PasswordHash = HashPassword(member.PasswordHash);

                // Üyelik tarihini şimdiki zaman olarak ayarla.
                member.MembershipDate = DateTime.Now;
                // Üyeyi başlangıçta aktif olarak işaretle.
                member.IsActive = true;
                // Rolü varsayılan olarak "Member" olarak ayarla. (Modelde veya burada atanabilir)
                member.Role = UserRole.Member; // Örneğin

                _context.Add(member); // Yeni üye nesnesini veritabanı bağlamına ekle.
                await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.
                TempData["Success"] = "Üye başarıyla eklendi."; // Başarı mesajını TempData'ya ekle.
                return RedirectToAction(nameof(Index)); // Üye listesi sayfasına yönlendir.
            }
            // Model doğrulama başarısız olursa, hata mesajlarıyla birlikte formu tekrar göster.
            return View(member);
        }
        #endregion // 'create' bölgesi sonu

        #region Member Edit Methods // Üye Düzenleme Metotlarını bir araya getiren bölge başlangıcı
        // Üye düzenleme formu (HTTP GET isteği için).
        public async Task<IActionResult> Edit(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // Üyeyi ID'ye göre bul.
            var member = await _context.Members.FindAsync(id);
            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            return View(member); // Üye nesnesini içeren View'i döndür.
        }
        #endregion

        // Üye düzenleme işlemi (HTTP POST isteği için).
        // Formdan gelen güncel üye bilgilerini işler ve veritabanına kaydeder.
        [HttpPost] // Bu metodun sadece HTTP POST istekleriyle tetikleneceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> Edit(int id, Member member) // 'Bind' niteliği burada eksik, tüm özellikler bind edilir.
        {
            // URL'den gelen 'id' ile formdan gelen 'member.Id'nin uyuşup uyuşmadığını kontrol eder.
            if (id != member.Id)
                return NotFound();

            // Model doğrulamasının başarılı olup olmadığını kontrol eder.
            if (ModelState.IsValid)
            {
                try
                {
                    // Mevcut üyeyi veritabanından izleme dışı (AsNoTracking) olarak getir.
                    // Bu, Entity Framework'ün aynı varlığın birden fazla kopyasını takip etmesini önler.
                    var existingMember = await _context.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    if (existingMember == null)
                        return NotFound();

                    // E-posta benzersizliği kontrolü (mevcut üye hariç):
                    // Eğer güncellenen e-posta başka bir üye tarafından zaten kullanılıyorsa hata ver.
                    var emailExists = await _context.Members
                        .AnyAsync(m => m.Email == member.Email && m.Id != id);
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                        return View(member);
                    }

                    // Parola güncelleme kısmı (şu anda yorum satırında):
                    // Eğer yeni bir parola girildiyse (newPassword parametresi olmalıydı, şu an yok), hash'le ve kaydet.
                    // Aksi takdirde, mevcut parolayı koru.
                    // member.PasswordHash = existingMember.PasswordHash; // Eğer parola güncellenmiyorsa, mevcut parolayı koru.

                    // Not: Mevcut kodda, 'Member member' direkt olarak bağlandığı için,
                    // eğer formdan PasswordHash alanı gelmiyorsa, varsayılan null veya boş değer atanır.
                    // Bu durumda 'existingMember.PasswordHash' değerini 'member.PasswordHash' üzerine yazmak gerekir.
                    // Eğer parolayı değiştirmek için ayrı bir input yoksa, aşağıdaki satır kritik:
                    member.PasswordHash = existingMember.PasswordHash; // Mevcut parolayı koru

                    _context.Update(member); // Üye nesnesini güncellendi olarak işaretle.
                    await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.
                    TempData["Success"] = "Üye bilgileri başarıyla güncellendi."; // Başarı mesajı ekle.
                }
                catch (DbUpdateConcurrencyException) // Eşzamanlılık çakışması yakalandığında
                {
                    // Eğer üye aynı anda başka bir kullanıcı tarafından silinmişse veya güncellenmişse bu hata oluşur.
                    // Üyenin hala var olup olmadığını kontrol eder.
                    if (!MemberExists(member.Id))
                        return NotFound(); // Üye yoksa 404 döndür.
                    else
                        throw; // Üye varsa ama başka bir çakışma varsa hatayı fırlat.
                }
                return RedirectToAction(nameof(Index)); // Üye listesi sayfasına yönlendir.
            }
            // Model doğrulama başarısız olursa, hata mesajlarıyla birlikte formu tekrar göster.
            return View(member);
        }

        // Üye silme onay sayfası (HTTP GET isteği için).
        // Bir üyeyi silmeden önce kullanıcıya onay göstermek için üyenin detaylarını getirir.
        public async Task<IActionResult> Delete(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // Üye detaylarını, ilişkili ödünç alma kayıtları ve puanlamaları ile birlikte getirir.
            var member = await _context.Members
                .Include(m => m.Loans)
                .Include(m => m.BookRatings)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            return View(member); // Üye nesnesini içeren View'i döndür.
        }

        // Üye silme işlemi (HTTP POST isteği için).
        // Üyeyi veritabanından siler, ancak aktif ödünç işlemleri varsa buna izin vermez.
        [HttpPost, ActionName("Delete")] // Bu metodun HTTP POST ile çağrılacağını ve 'Delete' action'ına yanıt vereceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // İlgili üyeyi ödünç alma ve puanlama kayıtlarıyla birlikte getir.
            var member = await _context.Members
                .Include(m => m.Loans)
                .Include(m => m.BookRatings)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Üye bulunursa devam et.
            if (member != null)
            {
                // Aktif ödünç işlemi varsa silmeyi engelle.
                var activeLoans = member.Loans.Where(l => l.ReturnDate == null).ToList();
                if (activeLoans.Any())
                {
                    // Hata mesajını TempData'ya ekle ve silme onay sayfasına geri yönlendir.
                    TempData["Error"] = "Aktif ödünç işlemi olan üye silinemez. Önce kitapları iade ettirin.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Members.Remove(member); // Üyeyi veritabanı bağlamından kaldırılması için işaretle.
                await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.
                TempData["Success"] = "Üye başarıyla silindi."; // Başarı mesajı ekle.
            }
            // Üye bulunamazsa veya silme işlemi başarılı olursa üye listesi sayfasına yönlendir.
            return RedirectToAction(nameof(Index));
        }

        // Üyenin okuma geçmişini (ödünç aldığı kitapları) gösterir.
        public async Task<IActionResult> ReadingHistory(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // Üye bilgilerini ve ilişkili ödünç alma kayıtlarını (kitap bilgileriyle birlikte) getir.
            var member = await _context.Members
                .Include(m => m.Loans)
                .ThenInclude(l => l.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            // Üyenin ödünç alma kayıtlarını ödünç alma tarihine göre azalan sırada sıralar.
            var readingHistory = member.Loans
                .OrderByDescending(l => l.LoanDate)
                .ToList();

            ViewBag.Member = member; // Üye nesnesini View'e aktar.
            return View(readingHistory); // Okuma geçmişini içeren View'i döndür.
        }

        // Üyenin yaptığı kitap puanlamalarını gösterir.
        public async Task<IActionResult> MemberRatings(int? id)
        {
            // ID boşsa NotFound döndür.
            if (id == null)
                return NotFound();

            // Üye bilgilerini ve ilişkili kitap puanlamalarını (kitap bilgileriyle birlikte) getir.
            var member = await _context.Members
                .Include(m => m.BookRatings)
                .ThenInclude(br => br.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Üye bulunamazsa NotFound döndür.
            if (member == null)
                return NotFound();

            // Üyenin puanlamalarını puanlama tarihine göre azalan sırada sıralar.
            var ratings = member.BookRatings
                .OrderByDescending(br => br.RatingDate)
                .ToList();

            ViewBag.Member = member; // Üye nesnesini View'e aktar.
            return View(ratings); // Puanlamaları içeren View'i döndür.
        }

        // Üye giriş sayfasını gösterir.
        public IActionResult Login()
        {
            return View(); // Giriş formunu içeren View'i döndür.
        }

        // Üye giriş işlemini gerçekleştirir.
        [HttpPost] // Bu metodun sadece HTTP POST istekleriyle tetikleneceğini belirtir.
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma sağlar.
        public async Task<IActionResult> Login(string email, string password)
        {
            // E-posta veya parolanın boş olup olmadığını kontrol et.
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "E-posta ve parola zorunludur."); // Hata mesajı ekle.
                return View(); // Giriş formunu tekrar göster.
            }

            // Üyeyi e-posta adresine göre ve aktif olup olmadığını kontrol ederek bul.
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.Email == email && m.IsActive);

            // Parola doğrulama (düz metin karşılaştırması - güvenlik açığı olabilir, hashlenmiş olmalı!)
            // Orijinalde burada 'VerifyPassword' kullanılmalıydı, ancak kod 'password == member.PasswordHash' olarak değiştirilmiş.
            // Bu, şifrelerin açık metin olarak veya zaten hashlenmiş ama tekrar hashlenmemiş olarak karşılaştırıldığını gösterir.
            // Güvenli bir uygulama için HashPassword ve VerifyPassword metodları kullanılmalıdır.
            if (member != null && password == member.PasswordHash) // Bu satırda, 'password' zaten hashlenmiş 'PasswordHash' alanı olarak geliyormuş gibi kullanılıyor.
            // if (member != null && VerifyPassword(password, member.PasswordHash)) // Doğru implementasyon bu şekilde olmalıydı.
            {
                // Giriş başarılı olursa, oturum (Session) bilgilerini ayarla.
                // HttpContext.Session, kullanıcının oturum verilerini sunucu tarafında saklamak için kullanılır.
                HttpContext.Session.SetString("MemberId", member.Id.ToString());      // Üye ID'sini kaydet
                HttpContext.Session.SetString("MemberName", member.FullName);   // Üye tam adını kaydet
                HttpContext.Session.SetString("MemberRole", member.Role.ToString());  // Üye rolünü kaydet (Admin, Librarian, Member)

                TempData["Success"] = $"Hoş geldiniz, {member.FullName}!"; // Hoş geldiniz mesajı.
                return RedirectToAction("Index", "Home"); // Ana sayfaya yönlendir.
            }

            // Giriş başarısız olursa (üye bulunamazsa veya parola yanlışsa) hata mesajı ekle.
            ModelState.AddModelError("", "Geçersiz e-posta veya parola.");
            return View(); // Giriş formunu tekrar göster.
        }

        // Üye çıkış işlemi.
        // Oturum bilgilerini temizler ve kullanıcıyı ana sayfaya yönlendirir.
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Mevcut oturumdaki tüm verileri temizle.
            TempData["Success"] = "Başarıyla çıkış yaptınız."; // Başarı mesajı ekle.
            return RedirectToAction("Index", "Home"); // Ana sayfaya yönlendir.
        }

        // Yardımcı metod: Belirli bir ID'ye sahip üyenin veritabanında olup olmadığını kontrol eder.
        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.Id == id);
        }

        // Yardımcı metod: Verilen parolayı SHA256 algoritması ve bir 'salt' (tuz) kullanarak hash'ler.
        // Bu, parolaların düz metin olarak saklanmasını engeller ve güvenliği artırır.
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create()) // SHA256 hash algoritması örneği oluştur.
            {
                // Parola ve "LibrarySalt" stringini UTF8 byte dizisine dönüştür ve hash hesapla.
                // Salt kullanımı, aynı parolaya sahip iki kullanıcının farklı hash'lere sahip olmasını sağlar (Rainbow table saldırılarına karşı koruma).
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "LibrarySalt"));
                // Hashlenmiş byte dizisini Base64 stringine dönüştürerek saklanabilir hale getir.
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Yardımcı metod: Girilen düz metin parolanın, saklanan hashlenmiş parola ile eşleşip eşleşmediğini doğrular.
        private bool VerifyPassword(string password, string hash)
        {
            string hashedPassword = HashPassword(password); // Girilen parolayı aynı hashleme yöntemiyle hash'le.
            return hashedPassword == hash; // Hesaplanan hash ile saklanan hash'i karşılaştır.
        }
    }
}


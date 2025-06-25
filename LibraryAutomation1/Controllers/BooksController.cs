using LibraryAutomation1.Data;
using LibraryAutomation1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks; // Task kullanımı için gerekli

namespace LibraryAutomation1.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context; // Veritabanı işlemleri için kullanılan 
        private readonly IWebHostEnvironment _env; // IWebHostEnvironment için gerekli
        // Sunucu ortamı bilgisi (örneğin dosya yolu için)

        // Constructor: context ve ortam değişkenlerini alır
        public BooksController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Kitapların listelendiği sayfa (arama ve filtreleme ile)
        public async Task<IActionResult> Index(string searchString, string genreFilter, string authorFilter, int? yearFilter, string sortOrder)
        {
            // ViewBag'e filtre değerlerini gönder

            ViewData["CurrentFilter"] = searchString;
            ViewData["GenreFilter"] = genreFilter;
            ViewData["AuthorFilter"] = authorFilter;
            ViewData["YearFilter"] = yearFilter;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "Author" ? "author_desc" : "Author";
            ViewData["YearSortParm"] = sortOrder == "Year" ? "year_desc" : "Year";
            ViewData["RatingSortParm"] = sortOrder == "Rating" ? "rating_desc" : "Rating";

            var books = from b in _context.Books.Include(b => b.BookRatings)
                        select b;

            // Arama
            if (!String.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString)
                                         || b.Author.Contains(searchString)
                                         || (b.ISBN != null && b.ISBN.Contains(searchString)));
            }

            // Filtreleme
            if (!String.IsNullOrEmpty(genreFilter))
            {
                books = books.Where(b => b.Genre == genreFilter);
            }

            if (!String.IsNullOrEmpty(authorFilter))
            {
                books = books.Where(b => b.Author.Contains(authorFilter));
            }

            if (yearFilter.HasValue)
            {
                books = books.Where(b => b.PublicationYear == yearFilter.Value);
            }

            // Sıralama
            switch (sortOrder)
            {
                case "title_desc":
                    books = books.OrderByDescending(b => b.Title);
                    break;
                case "Author":
                    books = books.OrderBy(b => b.Author);
                    break;
                case "author_desc":
                    books = books.OrderByDescending(b => b.Author);
                    break;
                case "Year":
                    books = books.OrderBy(b => b.PublicationYear);
                    break;
                case "year_desc":
                    books = books.OrderByDescending(b => b.PublicationYear);
                    break;
                case "Rating":
                    books = books.OrderBy(b => b.AverageRating);
                    break;
                case "rating_desc":
                    books = books.OrderByDescending(b => b.AverageRating);
                    break;
                default: // Varsayılan olarak başlığa göre sırala
                    books = books.OrderBy(b => b.Title);
                    break;
            }

            // Dropdown listeleri için veriler
            ViewBag.Genres = await _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.Authors = await _context.Books
                .Select(b => b.Author)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            ViewBag.Years = await _context.Books
                .Select(b => b.PublicationYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return View(await books.ToListAsync());
        }

        // Kitap detay sayfası
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.BookRatings)
                .ThenInclude(br => br.Member)
                .Include(b => b.Loans)
                .ThenInclude(l => l.Member)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
                return NotFound();

            // Ortalama puanı güncelle (Details sayfasında da güncel kalsın diye)
            await UpdateBookRating(book.Id);

            return View(book);
        }

        // Yeni kitap ekleme formu (GET)
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Author,PublicationYear,Genre,Description,ISBN,CoverImageUrl,Category,ShelfNumber,PageCount,Publisher,IsAvailable,ImageFile")] Book book)
        {
            if (ModelState.IsValid)
            {
                if (book.ImageFile != null && book.ImageFile.Length > 0)
                {
                    // IWebHostEnvironment için controller'a private readonly _env ekle, constructor'da inject et
                    var wwwRootPath = _env.WebRootPath;
                    var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(book.ImageFile.FileName);
                    var path = System.IO.Path.Combine(wwwRootPath, "images", "books", fileName);

                    using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Create))
                    {
                        await book.ImageFile.CopyToAsync(stream);
                    }

                    book.CoverImageUrl = "/images/books/" + fileName;
                }

                _context.Add(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kitap başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // Kitap düzenleme formu (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        // Kitap düzenleme işlemi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,PublicationYear,Genre,Description,ISBN,CoverImageUrl,Category,ShelfNumber,PageCount,Publisher,IsAvailable,AverageRating,RatingCount")] Book book)
        {
            ModelState.Remove("ImageFile");
            // AverageRating ve RatingCount'ı bind etmeyin, bunlar otomatik hesaplanır.
            // Eğer modelden geliyorsa, bunları kaldırın veya manuel olarak güncelleyin.
            // Şimdilik Bind listesine ekledim, ancak dikkatli olunması gerekir.

            if (id != book.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Mevcut kitabı veritabanından al
                    var existingBook = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBook == null)
                    {
                        return NotFound();
                    }

                    // Güncellenmeyecek alanları (AverageRating, RatingCount) koru
                    book.AverageRating = existingBook.AverageRating;
                    book.RatingCount = existingBook.RatingCount;

                    _context.Update(book);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Kitap başarıyla güncellendi."; // Başarı mesajı eklendi
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction("Index");
            }
            return View(book);
        }
        // Kitap silme formu (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.BookRatings)
                .Include(b => b.Loans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // Kitap silme işlemi (POST)
        [HttpPost] // <-- ÖNEMLİ DÜZELTME BURADA: ActionName("Delete") kaldırıldı
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) // Metot adı hala DeleteConfirmed
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kitap başarıyla silindi."; // Başarı mesajı eklendi
            }
            else
            {
                TempData["ErrorMessage"] = "Silinecek kitap bulunamadı."; // Hata mesajı eklendi
                return NotFound(); // Kitap bulunamazsa 404 döndür
            }
            return RedirectToAction(nameof(Index));
        }

        // Kitap puanlama
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateBook(int bookId, int memberId, int rating, string comment)
        {
            // TODO: Üye kimliğini doğru şekilde almayı buraya entegre etmeniz gerekebilir (örneğin Identity kullanarak)
            // Şu an için memberId'nin nasıl geldiğini varsayıyorum.
            if (memberId == 0) // Örnek kontrol: Eğer üye kimliği yoksa
            {
                TempData["Error"] = "Puan vermek için geçerli bir üye olmalısınız.";
                return RedirectToAction("Details", new { id = bookId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Puan 1 ile 5 arasında olmalıdır.";
                return RedirectToAction("Details", new { id = bookId });
            }

            var existingRating = await _context.BookRatings
                .FirstOrDefaultAsync(br => br.BookId == bookId && br.MemberId == memberId);

            if (existingRating != null)
            {
                // Mevcut puanı güncelle
                existingRating.Rating = rating;
                existingRating.Comment = comment;
                existingRating.RatingDate = DateTime.Now;
                _context.Update(existingRating);
                TempData["InfoMessage"] = "Puanınız güncellendi.";
            }
            else
            {
                // Yeni puan ekle
                var bookRating = new BookRating
                {
                    BookId = bookId,
                    MemberId = memberId,
                    Rating = rating,
                    Comment = comment,
                    RatingDate = DateTime.Now
                };
                _context.Add(bookRating);
                TempData["SuccessMessage"] = "Kitap başarıyla puanlandı.";
            }

            await _context.SaveChangesAsync();
            await UpdateBookRating(bookId); // Ortalama puanı yeniden hesapla ve kaydet


            return RedirectToAction("Details", new { id = bookId });
        }

        // Kitap var mı kontrolü (private yardımcı fonksiyon)
        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        // Kitap ortalama puanını güncelle (refactored for clarity and reusability)
        private async Task UpdateBookRating(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book != null)
            {
                var ratings = await _context.BookRatings
                    .Where(br => br.BookId == bookId)
                    .ToListAsync();

                if (ratings.Any())
                {
                    book.AverageRating = Math.Round(ratings.Average(r => r.Rating), 2);
                    book.RatingCount = ratings.Count;
                }
                else
                {
                    book.AverageRating = 0;
                    book.RatingCount = 0;
                }

                _context.Update(book);
                await _context.SaveChangesAsync();
            }
        }

        // En yüksek puanlı kitaplar
        public async Task<IActionResult> TopRated()
        {
            // ViewData["Title"]'ı da ayarlayabilirsiniz:
            ViewData["Title"] = "En Yüksek Puanlı Kitaplar";

            var topRatedBooks = await _context.Books
                .Include(b => b.BookRatings)
                .Where(b => b.RatingCount > 0) // Sadece puanlanmış kitapları al
                .OrderByDescending(b => b.AverageRating)
                .ThenByDescending(b => b.RatingCount) // Puanlar eşitse daha çok oylanan öne gelsin
                .Take(10) // İlk 10 kitabı al
                .ToListAsync();

            return View(topRatedBooks);
        }
    }
}

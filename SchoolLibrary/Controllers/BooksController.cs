using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Controllers;

public class BooksController : Controller
{
    private readonly LibraryDbContext _db;
    public BooksController(LibraryDbContext db) => _db = db;

    private static bool Has(string? s, string q) =>
        s != null && s.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> Index(string? q, string? genre, bool onlyAvailable = false)
    {
        var all = await _db.Books.Include(b => b.Copies).Where(b => !b.IsArchived).OrderBy(b => b.Title).ToListAsync();
        ViewBag.Genres = all.Where(b => !string.IsNullOrWhiteSpace(b.Genre)).Select(b => b.Genre!).Distinct().OrderBy(g => g).ToList();

        IEnumerable<Book> books = all;
        if (!string.IsNullOrWhiteSpace(q)) books = books.Where(b => Has(b.Title, q) || Has(b.Author, q));
        if (!string.IsNullOrWhiteSpace(genre)) books = books.Where(b => b.Genre == genre);
        if (onlyAvailable) books = books.Where(b => b.AvailableCount > 0 && !b.ReadingRoomOnly);

        ViewBag.Q = q; ViewBag.Genre = genre; ViewBag.OnlyAvailable = onlyAvailable;
        return View(books.ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.Include(b => b.Copies).FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return NotFound();

        if (User.IsInRole(Roles.Librarian))
        {
            var ids = book.Copies.Select(c => c.Id).ToList();
            ViewBag.ActiveLoans = await _db.Loans.Include(l => l.Reader)
                .Where(l => ids.Contains(l.CopyId) && l.ReturnDate == null)
                .ToDictionaryAsync(l => l.CopyId);
        }
        return View(book);
    }

    [Authorize(Roles = Roles.Librarian)]
    public IActionResult Create() => View("Form", new BookForm());

    [Authorize(Roles = Roles.Librarian), HttpPost]
    public async Task<IActionResult> Create(BookForm f)
    {
        var numbers = (f.InventoryNumbers ?? "")
            .Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct().ToList();

        if (numbers.Count == 0) ModelState.AddModelError(nameof(f.InventoryNumbers), "Въведете поне един инвентарен номер.");
        else
        {
            var taken = await _db.Copies.Where(c => numbers.Contains(c.InventoryNumber)).Select(c => c.InventoryNumber).ToListAsync();
            if (taken.Count > 0) ModelState.AddModelError(nameof(f.InventoryNumbers), "Вече съществуват: " + string.Join(", ", taken));
        }
        if (!ModelState.IsValid) return View("Form", f);

        var book = new Book
        {
            Title = f.Title.Trim(), Author = f.Author.Trim(), Genre = f.Genre?.Trim(), Isbn = f.Isbn?.Trim(),
            Year = f.Year, ReadingRoomOnly = f.ReadingRoomOnly,
            Copies = numbers.Select(n => new BookCopy { InventoryNumber = n }).ToList()
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Книгата е добавена.";
        return RedirectToAction(nameof(Details), new { id = book.Id });
    }

    [Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Books.FindAsync(id);
        if (b is null) return NotFound();
        return View("Form", new BookForm
        {
            Id = b.Id, Title = b.Title, Author = b.Author, Genre = b.Genre, Isbn = b.Isbn,
            Year = b.Year, ReadingRoomOnly = b.ReadingRoomOnly
        });
    }

    [Authorize(Roles = Roles.Librarian), HttpPost]
    public async Task<IActionResult> Edit(BookForm f)
    {
        if (!ModelState.IsValid) return View("Form", f);
        var b = await _db.Books.FindAsync(f.Id);
        if (b is null) return NotFound();

        b.Title = f.Title.Trim(); b.Author = f.Author.Trim(); b.Genre = f.Genre?.Trim();
        b.Isbn = f.Isbn?.Trim(); b.Year = f.Year; b.ReadingRoomOnly = f.ReadingRoomOnly;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Промените са записани.";
        return RedirectToAction(nameof(Details), new { id = b.Id });
    }

    [Authorize(Roles = Roles.Librarian), HttpPost]
    public async Task<IActionResult> AddCopy(int bookId, string inventoryNumber)
    {
        var inv = (inventoryNumber ?? "").Trim();
        if (inv.Length == 0) TempData["Error"] = "Въведете инвентарен номер.";
        else if (await _db.Copies.AnyAsync(c => c.InventoryNumber == inv)) TempData["Error"] = "Този инвентарен номер вече съществува.";
        else
        {
            _db.Copies.Add(new BookCopy { BookId = bookId, InventoryNumber = inv });
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Копието е добавено.";
        }
        return RedirectToAction(nameof(Details), new { id = bookId });
    }

    [Authorize(Roles = Roles.Librarian), HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var b = await _db.Books.Include(x => x.Copies).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        if (b.Copies.Any(c => c.Status == CopyStatus.Loaned))
        {
            TempData["Error"] = "Книгата има заети копия и не може да се архивира.";
            return RedirectToAction(nameof(Details), new { id });
        }
        b.IsArchived = true;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Книгата е архивирана.";
        return RedirectToAction(nameof(Index));
    }
}

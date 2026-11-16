using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Controllers;

[Authorize(Roles = Roles.Librarian)]
public class ReadersController : Controller
{
    private readonly LibraryDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    public ReadersController(LibraryDbContext db, IPasswordHasher<User> hasher) { _db = db; _hasher = hasher; }

    public async Task<IActionResult> Index(string? q)
    {
        var readers = await _db.Users.Where(u => u.Role == Roles.Student).OrderBy(u => u.ClassName).ThenBy(u => u.FullName).ToListAsync();
        if (!string.IsNullOrWhiteSpace(q))
            readers = readers.Where(r => r.FullName.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)
                                      || (r.ClassName ?? "").Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        ViewBag.Q = q;
        ViewBag.ActiveCounts = await _db.Loans.Where(l => l.ReturnDate == null)
            .GroupBy(l => l.ReaderId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        return View(readers);
    }

    public IActionResult Create() => View("Form", new ReaderForm());

    [HttpPost]
    public async Task<IActionResult> Create(ReaderForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Password) || f.Password.Length < 6)
            ModelState.AddModelError(nameof(f.Password), "Паролата трябва да е поне 6 символа.");
        if (await _db.Users.AnyAsync(u => u.Username == f.Username.Trim()))
            ModelState.AddModelError(nameof(f.Username), "Това потребителско име вече е заето.");
        if (!ModelState.IsValid) return View("Form", f);

        var u = new User { Username = f.Username.Trim(), FullName = f.FullName.Trim(), ClassName = f.ClassName?.Trim(), Role = Roles.Student };
        u.PasswordHash = _hasher.HashPassword(u, f.Password!);
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Читателят е добавен.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role == Roles.Student);
        if (u is null) return NotFound();
        return View("Form", new ReaderForm { Id = u.Id, Username = u.Username, FullName = u.FullName, ClassName = u.ClassName });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ReaderForm f)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == f.Id && x.Role == Roles.Student);
        if (u is null) return NotFound();
        if (await _db.Users.AnyAsync(x => x.Username == f.Username.Trim() && x.Id != f.Id))
            ModelState.AddModelError(nameof(f.Username), "Това потребителско име вече е заето.");
        if (!string.IsNullOrWhiteSpace(f.Password) && f.Password.Length < 6)
            ModelState.AddModelError(nameof(f.Password), "Паролата трябва да е поне 6 символа.");
        if (!ModelState.IsValid) return View("Form", f);

        u.Username = f.Username.Trim(); u.FullName = f.FullName.Trim(); u.ClassName = f.ClassName?.Trim();
        if (!string.IsNullOrWhiteSpace(f.Password)) u.PasswordHash = _hasher.HashPassword(u, f.Password);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Промените са записани.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(int id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role == Roles.Student);
        if (u is null) return NotFound();
        if (await _db.Loans.AnyAsync(l => l.ReaderId == id && l.ReturnDate == null))
        {
            TempData["Error"] = "Читателят има невърнати книги и не може да бъде деактивиран.";
            return RedirectToAction(nameof(Index));
        }
        u.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Читателят е деактивиран.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Activate(int id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role == Roles.Student);
        if (u is null) return NotFound();
        u.IsActive = true;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

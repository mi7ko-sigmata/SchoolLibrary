using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Controllers;

[Authorize(Roles = Roles.Librarian)]
public class LoansController : Controller
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loans;
    public LoansController(LibraryDbContext db, LoanService loans) { _db = db; _loans = loans; }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Loan> Full() => _db.Loans.Include(l => l.Reader).Include(l => l.Copy).ThenInclude(c => c.Book);

    private IActionResult Done((bool Ok, string Message) r, string action = nameof(Index))
    {
        TempData[r.Ok ? "Ok" : "Error"] = r.Message;
        return RedirectToAction(action);
    }

    public async Task<IActionResult> Index() =>
        View(await Full().Where(l => l.ReturnDate == null).OrderBy(l => l.DueDate).ToListAsync());

    public async Task<IActionResult> Overdue()
    {
        var today = DateTime.Today;
        return View(await Full().Where(l => l.ReturnDate == null && l.DueDate < today).OrderBy(l => l.DueDate).ToListAsync());
    }

    public async Task<IActionResult> Borrow(int? readerId)
    {
        ViewBag.Readers = await _db.Users.Where(u => u.Role == Roles.Student && u.IsActive)
            .OrderBy(u => u.ClassName).ThenBy(u => u.FullName).ToListAsync();
        ViewBag.ReaderId = readerId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Borrow(int readerId, string inventoryNumber)
    {
        var r = await _loans.BorrowAsync(readerId, inventoryNumber ?? "", CurrentUserId);
        if (r.Ok) { TempData["Ok"] = r.Message; return RedirectToAction(nameof(Index)); }
        TempData["Error"] = r.Message;
        return RedirectToAction(nameof(Borrow), new { readerId });
    }

    [HttpPost]
    public async Task<IActionResult> Return(int? id, string? inventoryNumber) =>
        Done(await _loans.ReturnAsync(id, inventoryNumber));

    [HttpPost]
    public async Task<IActionResult> Renew(int id) => Done(await _loans.RenewAsync(id));

    [HttpPost]
    public async Task<IActionResult> MarkLost(int id) => Done(await _loans.MarkLostAsync(id), nameof(Overdue));

    public async Task<IActionResult> History(int? readerId, int? bookId)
    {
        var q = Full();
        if (readerId.HasValue)
        {
            q = q.Where(l => l.ReaderId == readerId);
            ViewBag.Subject = "Читател: " + (await _db.Users.FindAsync(readerId))?.FullName;
        }
        if (bookId.HasValue)
        {
            q = q.Where(l => l.Copy.BookId == bookId);
            ViewBag.Subject = "Книга: " + (await _db.Books.FindAsync(bookId))?.Title;
        }
        return View(await q.OrderByDescending(l => l.LoanDate).ThenByDescending(l => l.Id).ToListAsync());
    }
}

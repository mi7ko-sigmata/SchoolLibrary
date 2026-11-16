using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;

namespace SchoolLibrary.Controllers;

public class MyController : Controller
{
    private readonly LibraryDbContext _db;
    public MyController(LibraryDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var loans = await _db.Loans.Include(l => l.Copy).ThenInclude(c => c.Book)
            .Where(l => l.ReaderId == id).OrderByDescending(l => l.LoanDate).ToListAsync();
        return View(loans);
    }
}

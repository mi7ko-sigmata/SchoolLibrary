using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

public class LoanService
{
    public const int LoanDays = 14;
    public const int MaxBooks = 3;
    public const int RenewDays = 7;
    public const int MaxRenewals = 1;
    public const int PossibleLossAfterDays = 60;

    private readonly LibraryDbContext _db;
    public LoanService(LibraryDbContext db) => _db = db;

    public async Task<(bool Ok, string Message)> BorrowAsync(int readerId, string inventoryNumber, int librarianId)
    {
        var reader = await _db.Users.FindAsync(readerId);
        if (reader is null || reader.Role != Roles.Student || !reader.IsActive)
            return (false, "Читателят не е активен.");

        var inv = inventoryNumber.Trim();
        var copy = await _db.Copies.Include(c => c.Book).FirstOrDefaultAsync(c => c.InventoryNumber == inv);
        if (copy is null) return (false, $"Няма копие с инвентарен номер „{inv}“.");

        if (copy.Book.IsArchived || copy.Status == CopyStatus.Lost) return (false, "Книгата е архивирана или изгубена.");
        if (copy.Book.ReadingRoomOnly) return (false, "Тази книга е само за четене в библиотеката.");
        if (copy.Status != CopyStatus.Available) return (false, "Копието вече е заето.");

        var active = await _db.Loans.Where(l => l.ReaderId == readerId && l.ReturnDate == null).ToListAsync();
        if (active.Any(l => l.IsOverdue))
            return (false, "Читателят има просрочени книги и не може да заема нови, докато не ги върне.");
        if (active.Count >= MaxBooks)
            return (false, $"Достигнат е лимитът от {MaxBooks} едновременно заети книги.");

        var today = DateTime.Today;
        _db.Loans.Add(new Loan
        {
            CopyId = copy.Id, ReaderId = readerId, LibrarianId = librarianId,
            LoanDate = today, DueDate = today.AddDays(LoanDays)
        });
        copy.Status = CopyStatus.Loaned;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Копието току-що е заето от друг запис.");
        }
        return (true, $"„{copy.Book.Title}“ е заета от {reader.FullName} до {today.AddDays(LoanDays):dd.MM.yyyy}.");
    }

    public async Task<(bool Ok, string Message)> ReturnAsync(int? loanId, string? inventoryNumber)
    {
        var q = _db.Loans.Include(l => l.Copy).ThenInclude(c => c.Book).Include(l => l.Reader)
            .Where(l => l.ReturnDate == null);

        Loan? loan = loanId.HasValue
            ? await q.FirstOrDefaultAsync(l => l.Id == loanId.Value)
            : await q.FirstOrDefaultAsync(l => l.Copy.InventoryNumber == (inventoryNumber ?? "").Trim());

        if (loan is null) return (false, "Няма активно заемане за това копие.");

        loan.ReturnDate = DateTime.Today;
        loan.Copy.Status = CopyStatus.Available;
        await _db.SaveChangesAsync();

        var late = loan.DaysLate > 0 ? $" (закъсняло с {loan.DaysLate} дни)" : "";
        return (true, $"„{loan.Copy.Book.Title}“ е върната от {loan.Reader.FullName}{late}.");
    }

    public async Task<(bool Ok, string Message)> RenewAsync(int loanId)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId && l.ReturnDate == null);
        if (loan is null) return (false, "Заемането не е активно.");
        if (loan.IsOverdue) return (false, "Просрочено заемане не може да се удължава.");
        if (loan.Renewals >= MaxRenewals) return (false, "Срокът вече е удължаван максималния брой пъти.");

        loan.DueDate = loan.DueDate.AddDays(RenewDays);
        loan.Renewals++;
        await _db.SaveChangesAsync();
        return (true, $"Новият срок е {loan.DueDate:dd.MM.yyyy}.");
    }

    public async Task<(bool Ok, string Message)> MarkLostAsync(int loanId)
    {
        var loan = await _db.Loans.Include(l => l.Copy).FirstOrDefaultAsync(l => l.Id == loanId && l.ReturnDate == null);
        if (loan is null) return (false, "Заемането не е активно.");
        if (loan.DaysLate < PossibleLossAfterDays)
            return (false, $"Книга може да се отбележи като изгубена след {PossibleLossAfterDays} дни просрочие.");

        loan.ReturnDate = DateTime.Today;
        loan.Note = "Изгубена книга";
        loan.Copy.Status = CopyStatus.Lost;
        await _db.SaveChangesAsync();
        return (true, "Копието е отбелязано като изгубено.");
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolLibrary.Models;

public class Loan
{
    public int Id { get; set; }

    public int CopyId { get; set; }
    public BookCopy Copy { get; set; } = null!;

    public int ReaderId { get; set; }
    public User Reader { get; set; } = null!;

    public int? LibrarianId { get; set; }
    public User? Librarian { get; set; }

    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int Renewals { get; set; }
    public string? Note { get; set; }

    [NotMapped] public bool IsActive => ReturnDate == null;
    [NotMapped] public bool IsOverdue => ReturnDate == null && DueDate.Date < DateTime.Today;

    [NotMapped]
    public int DaysLate
    {
        get
        {
            var end = (ReturnDate ?? DateTime.Today).Date;
            var d = (end - DueDate.Date).Days;
            return d > 0 ? d : 0;
        }
    }
}

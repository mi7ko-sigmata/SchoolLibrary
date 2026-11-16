using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolLibrary.Models;

public enum CopyStatus { Available = 0, Loaned = 1, Lost = 2 }

public class Book
{
    public int Id { get; set; }

    [Required, StringLength(200), Display(Name = "Заглавие")]
    public string Title { get; set; } = "";

    [Required, StringLength(150), Display(Name = "Автор")]
    public string Author { get; set; } = "";

    [StringLength(60), Display(Name = "Жанр")]
    public string? Genre { get; set; }

    [StringLength(20), Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [Display(Name = "Година")]
    public int? Year { get; set; }

    [Display(Name = "Само за четене в библиотеката")]
    public bool ReadingRoomOnly { get; set; }

    public bool IsArchived { get; set; }

    public List<BookCopy> Copies { get; set; } = new();

    [NotMapped] public int AvailableCount => Copies.Count(c => c.Status == CopyStatus.Available);
    [NotMapped] public int TotalCount => Copies.Count(c => c.Status != CopyStatus.Lost);
}

public class BookCopy
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    [Required, StringLength(40)]
    public string InventoryNumber { get; set; } = "";

    public CopyStatus Status { get; set; } = CopyStatus.Available;
}

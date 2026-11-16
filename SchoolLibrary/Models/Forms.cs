using System.ComponentModel.DataAnnotations;

namespace SchoolLibrary.Models;

public class BookForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Заглавието е задължително."), StringLength(200), Display(Name = "Заглавие")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Авторът е задължително."), StringLength(150), Display(Name = "Автор")]
    public string Author { get; set; } = "";

    [StringLength(60), Display(Name = "Жанр")]
    public string? Genre { get; set; }

    [StringLength(20), Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [Range(1000, 2100, ErrorMessage = "Невалидна година."), Display(Name = "Година")]
    public int? Year { get; set; }

    [Display(Name = "Само за четене в библиотеката")]
    public bool ReadingRoomOnly { get; set; }

    [Display(Name = "Инвентарни номера (по един на ред; един ред = едно копие)")]
    public string? InventoryNumbers { get; set; }
}

public class ReaderForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Потребителското име е задължително."), StringLength(50), Display(Name = "Потребителско име")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Името е задължително."), StringLength(100), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = "";

    [StringLength(20), Display(Name = "Клас")]
    public string? ClassName { get; set; }

    [Display(Name = "Парола (при редакция – оставете празно, за да не се променя)")]
    public string? Password { get; set; }
}

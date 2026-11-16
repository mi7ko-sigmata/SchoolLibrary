using System.ComponentModel.DataAnnotations;

namespace SchoolLibrary.Models;

public static class Roles
{
    public const string Librarian = "Librarian";
    public const string Student = "Student";
}

public class User
{
    public int Id { get; set; }

    [Required, StringLength(50), Display(Name = "Потребителско име")]
    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    [Required, StringLength(100), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = "";

    [StringLength(20), Display(Name = "Клас")]
    public string? ClassName { get; set; }

    public string Role { get; set; } = Roles.Student;

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
}

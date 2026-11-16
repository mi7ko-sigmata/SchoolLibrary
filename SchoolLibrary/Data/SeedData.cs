using Microsoft.AspNetCore.Identity;
using SchoolLibrary.Models;

namespace SchoolLibrary.Data;

public static class SeedData
{
    public static void Init(LibraryDbContext db, IPasswordHasher<User> hasher)
    {
        if (db.Users.Any()) return;

        User Make(string username, string name, string? cls, string role, string pass)
        {
            var u = new User { Username = username, FullName = name, ClassName = cls, Role = role };
            u.PasswordHash = hasher.HashPassword(u, pass);
            return u;
        }

        db.Users.AddRange(
            Make("bibliotekar", "Мария Петрова (библиотекар)", null, Roles.Librarian, "Parola123!"),
            Make("ivan", "Иван Иванов", "8А", Roles.Student, "Parola123!"),
            Make("elena", "Елена Георгиева", "9Б", Roles.Student, "Parola123!"));

        Book B(string title, string author, string genre, int year, bool roomOnly, params string[] inv) => new()
        {
            Title = title, Author = author, Genre = genre, Year = year, ReadingRoomOnly = roomOnly,
            Copies = inv.Select(n => new BookCopy { InventoryNumber = n }).ToList()
        };

        db.Books.AddRange(
            B("Под игото", "Иван Вазов", "Роман", 1894, false, "INV-001", "INV-002"),
            B("Железният светилник", "Димитър Талев", "Роман", 1952, false, "INV-003"),
            B("Бай Ганьо", "Алеко Константинов", "Сатира", 1895, false, "INV-004", "INV-005"),
            B("Тютюн", "Димитър Димов", "Роман", 1951, false, "INV-006"),
            B("Български тълковен речник", "Колектив", "Справочник", 2010, true, "INV-007"));

        db.SaveChanges();
    }
}

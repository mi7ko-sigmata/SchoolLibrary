using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Models;

namespace SchoolLibrary.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> Copies => Set<BookCopy>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<BookCopy>().HasIndex(c => c.InventoryNumber).IsUnique();
        b.Entity<Loan>().HasIndex(l => l.CopyId).IsUnique().HasFilter("\"ReturnDate\" IS NULL");

        b.Entity<Loan>().HasOne(l => l.Copy).WithMany().HasForeignKey(l => l.CopyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasOne(l => l.Reader).WithMany().HasForeignKey(l => l.ReaderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasOne(l => l.Librarian).WithMany().HasForeignKey(l => l.LibrarianId).OnDelete(DeleteBehavior.Restrict);
    }
}

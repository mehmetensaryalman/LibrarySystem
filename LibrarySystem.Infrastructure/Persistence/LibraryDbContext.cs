using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Persistence;

public class LibraryDbContext :
    IdentityDbContext<ApplicationUser>
{
    public LibraryDbContext(
        DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books =>
        Set<Book>();

    public DbSet<BorrowRecord> BorrowRecords =>
        Set<BorrowRecord>();

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");

            entity.HasKey(x =>
                x.Id);

            entity.Property(x =>
                    x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x =>
                    x.Author)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x =>
                    x.Stock)
                .IsRequired();

            entity.Property(x =>
                    x.IsArchived)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(x =>
                    x.ArchivedAt)
                .IsRequired(false);

            entity.HasIndex(x => new
            {
                x.Name,
                x.Author
            })
                .IsUnique()
                .HasDatabaseName(
                    "UX_Books_Name_Author");
        });

        builder.Entity<BorrowRecord>(
            entity =>
            {
                entity.ToTable(
                    "BorrowRecords");

                entity.HasKey(x =>
                    x.Id);

                entity.Property(x =>
                        x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x =>
                        x.BorrowDate)
                    .IsRequired();

                entity.Property(x =>
                        x.DueDate)
                    .IsRequired();

                entity.Property(x =>
                        x.ReturnDate)
                    .IsRequired(false);

                entity.Property(x =>
                        x.IsReturned)
                    .IsRequired();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.BookId
                })
                    .IsUnique()
                    .HasFilter(
                        "[IsReturned] = 0")
                    .HasDatabaseName(
                        "UX_BorrowRecords_UserId_BookId_Active");

                entity.HasOne(x =>
                        x.Book)
                    .WithMany(x =>
                        x.BorrowRecords)
                    .HasForeignKey(x =>
                        x.BookId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x =>
                        x.UserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });
    }
}
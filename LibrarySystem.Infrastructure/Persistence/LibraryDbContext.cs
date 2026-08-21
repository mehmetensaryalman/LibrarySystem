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

    public DbSet<BorrowRequest> BorrowRequests =>
        Set<BorrowRequest>();

    public DbSet<BorrowPenalty> BorrowPenalties =>
        Set<BorrowPenalty>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

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
                        x.ReturnRequestedAt)
                    .IsRequired(false);

                entity.Property(x =>
                        x.ReturnDate)
                    .IsRequired(false);

                entity.Property(x =>
                        x.ReturnedToAdminUserId)
                    .IsRequired(false)
                    .HasMaxLength(450);

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

                entity.HasIndex(x => new
                {
                    x.IsReturned,
                    x.ReturnRequestedAt
                })
                    .HasDatabaseName(
                        "IX_BorrowRecords_IsReturned_ReturnRequestedAt");

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

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x =>
                        x.ReturnedToAdminUserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });

        builder.Entity<BorrowRequest>(
            entity =>
            {
                entity.ToTable(
                    "BorrowRequests");

                entity.HasKey(x =>
                    x.Id);

                entity.Property(x =>
                        x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x =>
                        x.BookId)
                    .IsRequired();

                entity.Property(x =>
                        x.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x =>
                        x.RequestedAt)
                    .IsRequired();

                entity.Property(x =>
                        x.ProcessedAt)
                    .IsRequired(false);

                entity.Property(x =>
                        x.ProcessedByAdminUserId)
                    .IsRequired(false)
                    .HasMaxLength(450);

                entity.Property(x =>
                        x.BorrowRecordId)
                    .IsRequired(false);

                entity.Property(x =>
                        x.RejectionReason)
                    .IsRequired(false)
                    .HasMaxLength(500);

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.BookId
                })
                    .IsUnique()
                    .HasFilter(
                        "[Status] = 1")
                    .HasDatabaseName(
                        "UX_BorrowRequests_UserId_BookId_Pending");

                entity.HasIndex(x => new
                {
                    x.Status,
                    x.RequestedAt
                })
                    .HasDatabaseName(
                        "IX_BorrowRequests_Status_RequestedAt");

                entity.HasIndex(x =>
                        x.BorrowRecordId)
                    .IsUnique()
                    .HasFilter(
                        "[BorrowRecordId] IS NOT NULL")
                    .HasDatabaseName(
                        "UX_BorrowRequests_BorrowRecordId");

                entity.HasOne(x =>
                        x.Book)
                    .WithMany()
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

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x =>
                        x.ProcessedByAdminUserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne(x =>
                        x.BorrowRecord)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.BorrowRecordId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });

        builder.Entity<BorrowPenalty>(
            entity =>
            {
                entity.ToTable(
                    "BorrowPenalties");

                entity.HasKey(x =>
                    x.Id);

                entity.Property(x =>
                        x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x =>
                        x.PenaltyDays)
                    .IsRequired();

                entity.Property(x =>
                        x.StartDate)
                    .IsRequired();

                entity.Property(x =>
                        x.EndDate)
                    .IsRequired();

                entity.Property(x =>
                        x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(x =>
                        x.BorrowRecordId)
                    .IsUnique()
                    .HasDatabaseName(
                        "UX_BorrowPenalties_BorrowRecordId");

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.EndDate
                })
                    .HasDatabaseName(
                        "IX_BorrowPenalties_UserId_EndDate");

                entity.HasOne(x =>
                        x.BorrowRecord)
                    .WithOne(x =>
                        x.Penalty)
                    .HasForeignKey<
                        BorrowPenalty>(
                        x =>
                            x.BorrowRecordId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x =>
                        x.UserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });

        builder.Entity<Notification>(
            entity =>
            {
                entity.ToTable(
                    "Notifications");

                entity.HasKey(x =>
                    x.Id);

                entity.Property(x =>
                        x.RecipientUserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x =>
                        x.Type)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x =>
                        x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x =>
                        x.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(x =>
                        x.BorrowRecordId)
                    .IsRequired(false);

                entity.Property(x =>
                        x.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x =>
                        x.CreatedAt)
                    .IsRequired();

                entity.Property(x =>
                        x.ReadAt)
                    .IsRequired(false);

                entity.HasIndex(x => new
                {
                    x.RecipientUserId,
                    x.IsRead,
                    x.CreatedAt
                })
                    .HasDatabaseName(
                        "IX_Notifications_RecipientUserId_IsRead_CreatedAt");

                entity.HasOne(x =>
                        x.BorrowRecord)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.BorrowRecordId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(x =>
                        x.RecipientUserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });
    }
}
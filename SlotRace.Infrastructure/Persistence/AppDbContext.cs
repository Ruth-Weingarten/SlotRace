using Microsoft.EntityFrameworkCore;
using SlotRace.Domain.Entities;

namespace SlotRace.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Slot> Slots => Set<Slot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.Property(s => s.FullName).IsRequired().HasMaxLength(100);
            e.Property(s => s.JoinCode).IsRequired().HasMaxLength(20);
            e.HasIndex(s => s.JoinCode).IsUnique();

            e.HasOne(s => s.HomeroomTeacher)
             .WithMany(u => u.HomeroomStudents)
             .HasForeignKey(s => s.HomeroomTeacherId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(s => s.Parents)
             .WithMany(u => u.Children)
             .UsingEntity(j => j.ToTable("ParentStudent"));
        });

        modelBuilder.Entity<Slot>(e =>
        {
            e.Property(s => s.Version).IsConcurrencyToken();

            // אינדקס ייחודי חלקי: ילדה יכולה להופיע רק בסלוט אחד (תור אחד לילדה).
            // המסד אוכף את זה ברגע השמירה, גם כשאבא ואמא מזמינים סלוטים שונים באותה שנייה.
            // הפילטר: החוק חל רק על סלוטים תפוסים (StudentId לא null)
            e.HasIndex(s => s.StudentId)
             .IsUnique()
             .HasFilter("\"StudentId\" IS NOT NULL");

            // אינדקס רגיל (מאיץ): הרשימה ובדיקת החפיפה מחפשות לפי מחנכת ושעה
            e.HasIndex(s => new { s.TeacherId, s.StartTimeUtc });

            e.HasOne(s => s.Teacher)
             .WithMany(u => u.Slots)
             .HasForeignKey(s => s.TeacherId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.BookedByParent)
             .WithMany(u => u.BookedSlots)
             .HasForeignKey(s => s.BookedByParentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Student)
             .WithMany()
             .HasForeignKey(s => s.StudentId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Slot>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.Version = Guid.NewGuid();
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Password)
                .IsRequired();
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);
            
            entity.Property(e => e.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.EmailVerificationToken)
                .IsRequired(false);
            
            entity.Property(e => e.EmailVerificationTokenExpiry)
                .IsRequired(false);
            
            entity.Property(e => e.PasswordResetToken)
                .IsRequired(false);
            
            entity.Property(e => e.PasswordResetTokenExpiry)
                .IsRequired(false);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.DueDate)
                .IsRequired(false);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);
            
            // Relationships
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.AssignedToUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedToUserId);
            entity.HasIndex(e => e.CreatedByUserId);
        });
    }
}

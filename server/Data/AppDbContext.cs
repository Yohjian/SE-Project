using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Entities;

namespace QuattroLingo.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<VocabularySet> Sets { get; set; }
        public DbSet<VocabularyCard> Cards { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Participant> Participants { get; set; }

        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .HasDefaultValue(UserRole.User);

                e.Property(u => u.IsActive)
                    .HasDefaultValue(true);
            });

            builder.Entity<VocabularySet>()
                .HasMany(s => s.Cards)
                .WithOne(c => c.Set)
                .HasForeignKey(c => c.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Room>()
                .HasMany(r => r.Participants)
                .WithOne(p => p.Room)
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Room>()
                .HasIndex(r => r.JoinCode)
                .IsUnique();

            builder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(q => q.Quiz)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Question>()
                .HasMany(q => q.AnswerOptions)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Quiz>()
                .HasOne(q => q.Owner)
                .WithMany()
                .HasForeignKey(q => q.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

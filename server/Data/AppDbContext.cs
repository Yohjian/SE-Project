using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Entities;

namespace QuattroLingo.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<VocabularySet> Sets { get; set; }
        public DbSet<VocabularyCard> Cards { get; set; }

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

            builder.Entity<Language>()
                .HasIndex(l => l.Code)
                .IsUnique();

            builder.Entity<Translation>()
                .HasIndex(t => new { t.WordId, t.LanguageId })
                .IsUnique();

            builder.Entity<VocabularySet>()
                .HasMany(s => s.Cards)
                .WithOne(c => c.Set)
                .HasForeignKey(c => c.SetId)
                .OnDelete(DeleteBehavior.Cascade);


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

using Microsoft.EntityFrameworkCore;
using QuattroLingo.Entities;

namespace QuattroLingo.Data
{
    public static class PracticeDataSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            string[] languages =
            [
                "German",
                "French",
                "Spanish",
                "Italian",
                "Lithuanian"
            ];

            string[] categories =
            [
                "Vocabulary",
                "Grammar",
                "Phrases",
                "Translation"
            ];

            foreach (var name in languages)
            {
                if (!await db.Languages.AnyAsync(l => l.Name == name))
                {
                    db.Languages.Add(new Language
                    {
                        Name = name
                    });
                }
            }

            foreach (var name in categories)
            {
                if (!await db.Categories.AnyAsync(c => c.Name == name))
                {
                    db.Categories.Add(new Category
                    {
                        Name = name
                    });
                }
            }

            await db.SaveChangesAsync();


            var seedOwner = await db.Users
                .FirstOrDefaultAsync(u => u.UserName == "practice-seed");

            if (seedOwner == null)
            {
                seedOwner = new ApplicationUser
                {
                    UserName = "practice-seed",
                    DisplayName = "QuattroLingo",
                    EmailConfirmed = true
                };

                db.Users.Add(seedOwner);
                await db.SaveChangesAsync();
            }


            await SeedQuizAsync(
                db,
                seedOwner.Id,
                "Lithuanian Phrases",
                "Practice common Lithuanian phrases.",
                "Lithuanian",
                "Phrases",
                [
                    ("What does 'Labas rytas' mean?",
            "Good morning", "Good evening", "Goodbye", "Thank you"),

        ("What does 'Ačiū' mean?",
            "Thank you", "Please", "Hello", "Sorry"),

        ("What does 'Iki pasimatymo' mean?",
            "See you later", "Good night", "You're welcome", "Good morning")
                ]);

            await SeedQuizAsync(
                db,
                seedOwner.Id,
                "German Vocabulary",
                "Learn basic German words.",
                "German",
                "Vocabulary",
                [
                    ("What does 'Haus' mean?",
            "House", "Car", "Book", "School"),

        ("What does 'Apfel' mean?",
            "Apple", "Orange", "Bread", "Water"),

        ("What does 'Hund' mean?",
            "Dog", "Cat", "Bird", "Fish")
                ]);

            await SeedQuizAsync(
                db,
                seedOwner.Id,
                "Italian Grammar",
                "Practice basic Italian grammar.",
                "Italian",
                "Grammar",
                [
                    ("Which article is correct: ___ libro?",
            "il", "la", "le", "gli"),

        ("Complete the sentence: Io ___ italiano.",
            "sono", "sei", "è", "siamo"),

        ("Which is the plural of 'ragazzo'?",
            "ragazzi", "ragazze", "ragazza", "ragazzo")
                ]);


        }


        private static async Task SeedQuizAsync(
            AppDbContext db,
            string ownerId,
            string title,
            string description,
            string languageName,
            string categoryName,
            (string Question, string Correct, string Wrong1,
             string Wrong2, string Wrong3)[] questions)
        {
            var exists = await db.Quizzes.AnyAsync(q =>
                q.OwnerId == ownerId &&
                q.Kind == QuizKind.Practice &&
                q.Title == title);

            if (exists)
                return;

            var language = await db.Languages
                .SingleAsync(l => l.Name == languageName);

            var category = await db.Categories
                .SingleAsync(c => c.Name == categoryName);

            var quiz = new Quiz
            {
                OwnerId = ownerId,
                Title = title,
                Description = description,
                Kind = QuizKind.Practice,
                LanguageId = language.Id,
                CategoryId = category.Id,
                IsPublic = true,

                Questions = questions.Select((item, index) => new Question
                {
                    Text = item.Question,
                    OrderIndex = index,
                    TimeLimitSeconds = 30,
                    Points = 100,

                    AnswerOptions = new List<AnswerOption>
            {
                new() { Text = item.Correct, IsCorrect = true },
                new() { Text = item.Wrong1, IsCorrect = false },
                new() { Text = item.Wrong2, IsCorrect = false },
                new() { Text = item.Wrong3, IsCorrect = false }
            }
                }).ToList()
            };

            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();
        }


    }
}

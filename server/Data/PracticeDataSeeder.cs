
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
        }
    }
}

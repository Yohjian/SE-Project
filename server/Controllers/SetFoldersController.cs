
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;

namespace QuattroLingo.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/set-folders")]
    public class SetFoldersController(AppDbContext db) : ControllerBase
    {
        private string UserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedException(
                "User id is missing from the token.");

        public record FolderRequest(string Name);
        public record MoveSetRequest(int? FolderId);
        public record FolderResponse(int Id, string Name);

        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var folders = await db.SetFolders
                .AsNoTracking()
                .Where(f => f.UserId == UserId)
                .OrderBy(f => f.Name)
                .Select(f => new FolderResponse(f.Id, f.Name))
                .ToListAsync();

            return Ok(folders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder(
            [FromBody] FolderRequest request)
        {
            var name = ValidateName(request.Name);

            await EnsureUniqueNameAsync(name);

            var folder = new SetFolder
            {
                Name = name,
                UserId = UserId
            };

            db.SetFolders.Add(folder);
            await db.SaveChangesAsync();

            return Ok(new FolderResponse(folder.Id, folder.Name));
        }

        [HttpPut("{folderId:int}")]
        public async Task<IActionResult> RenameFolder(
            int folderId,
            [FromBody] FolderRequest request)
        {
            var folder = await db.SetFolders
                .FirstOrDefaultAsync(f =>
                    f.Id == folderId && f.UserId == UserId)
                ?? throw new NotFoundException("Folder not found.");

            var name = ValidateName(request.Name);

            await EnsureUniqueNameAsync(name, folderId);

            folder.Name = name;
            await db.SaveChangesAsync();

            return Ok(new FolderResponse(folder.Id, folder.Name));
        }

        [HttpDelete("{folderId:int}")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            var folder = await db.SetFolders
                .FirstOrDefaultAsync(f =>
                    f.Id == folderId && f.UserId == UserId)
                ?? throw new NotFoundException("Folder not found.");

            var sets = await db.Sets
                .Where(s =>
                    s.UserId == UserId &&
                    s.FolderId == folderId)
                .ToListAsync();

            foreach (var set in sets)
                set.FolderId = null;

            db.SetFolders.Remove(folder);
            await db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("/api/sets/{setId:int}/folder")]
        public async Task<IActionResult> MoveSet(
            int setId,
            [FromBody] MoveSetRequest request)
        {
            var set = await db.Sets
                .FirstOrDefaultAsync(s =>
                    s.Id == setId && s.UserId == UserId)
                ?? throw new NotFoundException("Set not found.");

            if (request.FolderId.HasValue)
            {
                var exists = await db.SetFolders.AnyAsync(f =>
                    f.Id == request.FolderId.Value &&
                    f.UserId == UserId);

                if (!exists)
                    throw new NotFoundException("Folder not found.");
            }

            set.FolderId = request.FolderId;
            await db.SaveChangesAsync();

            return NoContent();
        }

        private static string ValidateName(string? value)
        {
            var name = value?.Trim() ?? "";

            if (name.Length == 0 || name.Length > 100)
                throw new ValidationException(
                    "Folder name must contain 1 to 100 characters.");

            if (string.Equals(
                name, "Basic", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException(
                    "Basic is a reserved folder name.");

            return name;
        }

        private async Task EnsureUniqueNameAsync(
            string name, int? excludeId = null)
        {
            var exists = await db.SetFolders.AnyAsync(f =>
                f.UserId == UserId &&
                f.Id != excludeId &&
                f.Name.ToLower() == name.ToLower());

            if (exists)
                throw new ValidationException(
                    "A folder with this name already exists.");
        }
    }
}

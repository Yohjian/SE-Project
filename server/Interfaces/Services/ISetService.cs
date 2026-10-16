using QuattroLingo.DTOs.Response;
using QuattroLingo.Enums;

namespace QuattroLingo.Services
{
    public interface ISetService
    {
        Task<List<SetSummary>> GetAllAsync(string userId);

        Task<SetContents> GetAsync(string userId, int setId);

        Task<SetSummary> CreateAsync(string userId, string name);

        Task<CardResponse> AddCardAsync(string userId, int setId, string term, string definition);

        Task<List<CardResponse>> GetCardsAsync(string userId, int setId);

        Task<(MemoryStream, string, string)> DownloadSetAsync(string userId, int setId, FileType ft);
    }
}

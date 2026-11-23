
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace QuattroLingo.DTOs.Request
{
    public record CreateCard([Required] string Term, [Required] string Definition, Dictionary<string, JsonElement>? Metadata = null);
}

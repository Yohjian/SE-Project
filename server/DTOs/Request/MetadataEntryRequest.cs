using System.Text.Json;

namespace QuattroLingo.DTOs.Request;

public record MetadataEntryRequest(string Key, JsonElement Value);

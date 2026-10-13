using System.Text.Json;

namespace NoiseVault.Api.ApiResponse
{
    public static class EnvelopeJson
    {
        public static readonly JsonSerializerOptions Options =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static string Serialize<T>(ApiResponse<T> response) =>
            JsonSerializer.Serialize(response, Options);
    }
}

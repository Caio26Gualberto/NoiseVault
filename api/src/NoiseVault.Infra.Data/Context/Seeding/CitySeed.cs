using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Cities;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public static class CitySeed
    {
        private const string ResourceName = "NoiseVault.Infra.Data.Context.Seeding.ibge-municipios.json";

        public static async Task<int> SeedAsync(AppDbContext context)
        {
            var snapshot = Load();
            var existing = await context.Cities.Select(c => c.Id).ToListAsync();
            var known = existing.ToHashSet();

            var missing = snapshot
                .Where(city => !known.Contains(city.Id))
                .ToList();

            if (missing.Count == 0)
                return 0;

            context.Cities.AddRange(missing);
            await context.SaveChangesAsync();

            return missing.Count;
        }

        private static List<City> Load()
        {
            using var stream = typeof(CitySeed).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Snapshot de cidades não encontrado no assembly: {ResourceName}");

            var rows = JsonSerializer.Deserialize<List<JsonElement[]>>(stream)
                ?? throw new InvalidOperationException("Snapshot de cidades vazio.");

            return rows
                .Select(row => new City
                {
                    Id = row[0].GetInt32(),
                    Name = row[1].GetString()!,
                    Uf = row[2].GetString()!,
                    SearchName = CityRules.Normalize(row[1].GetString()),
                })
                .ToList();
        }
    }
}

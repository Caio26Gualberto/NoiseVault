using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Cities;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public static class CitySeed
    {
        private const string ResourceName = "NoiseVault.Infra.Data.Context.Seeding.ibge-municipios.json";
        private const string CoordinatesResourceName = "NoiseVault.Infra.Data.Context.Seeding.municipios-coordenadas.json";

        public static async Task<int> SeedAsync(AppDbContext context)
        {
            var coordinates = LoadCoordinates();
            var snapshot = Load(coordinates);
            var existing = await context.Cities.Select(c => c.Id).ToListAsync();
            var known = existing.ToHashSet();

            var missing = snapshot
                .Where(city => !known.Contains(city.Id))
                .ToList();

            if (missing.Count > 0)
            {
                context.Cities.AddRange(missing);
                await context.SaveChangesAsync();
            }

            await FillCoordinatesAsync(context, coordinates);

            return missing.Count;
        }

        private static async Task FillCoordinatesAsync(AppDbContext context, Dictionary<int, (double Latitude, double Longitude)> coordinates)
        {
            var withoutCoordinates = await context.Cities.Where(c => c.Latitude == null).ToListAsync();
            var filled = 0;

            foreach (var city in withoutCoordinates)
            {
                if (!coordinates.TryGetValue(city.Id, out var point))
                    continue;

                city.Latitude = point.Latitude;
                city.Longitude = point.Longitude;
                filled++;
            }

            if (filled > 0)
                await context.SaveChangesAsync();
        }

        private static List<City> Load(Dictionary<int, (double Latitude, double Longitude)> coordinates)
        {
            using var stream = typeof(CitySeed).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Snapshot de cidades não encontrado no assembly: {ResourceName}");

            var rows = JsonSerializer.Deserialize<List<JsonElement[]>>(stream)
                ?? throw new InvalidOperationException("Snapshot de cidades vazio.");

            return rows
                .Select(row =>
                {
                    var id = row[0].GetInt32();
                    var found = coordinates.TryGetValue(id, out var point);

                    return new City
                    {
                        Id = id,
                        Name = row[1].GetString()!,
                        Uf = row[2].GetString()!,
                        SearchName = CityRules.Normalize(row[1].GetString()),
                        Latitude = found ? point.Latitude : null,
                        Longitude = found ? point.Longitude : null,
                    };
                })
                .ToList();
        }

        private static Dictionary<int, (double Latitude, double Longitude)> LoadCoordinates()
        {
            using var stream = typeof(CitySeed).Assembly.GetManifestResourceStream(CoordinatesResourceName)
                ?? throw new InvalidOperationException($"Snapshot de coordenadas não encontrado no assembly: {CoordinatesResourceName}");

            var rows = JsonSerializer.Deserialize<List<double[]>>(stream)
                ?? throw new InvalidOperationException("Snapshot de coordenadas vazio.");

            return rows.ToDictionary(row => (int)row[0], row => (row[1], row[2]));
        }
    }
}

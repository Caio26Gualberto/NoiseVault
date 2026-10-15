using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Application.Geo;

namespace NoiseVault.Application.Cities
{
    public class CityAppService : ICityAppService
    {
        private readonly ICityRepository _cityRepository;

        public CityAppService(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;
        }

        public async Task<Result<PagedResultDto<CityDto>>> Search(CitySearchQueryDto query)
        {
            query.Normalize();

            var term = CityRules.Normalize(query.Search);

            if (term.Length < CityRules.SearchMinLength)
                return Result<PagedResultDto<CityDto>>.Ok(
                    PagedResultDto<CityDto>.Create([], query.Page, query.PageSize, 0),
                    "CITIES_FOUND",
                    "Type at least two letters");

            var (cities, totalCount) = await _cityRepository.SearchAsync(term, query.Skip, query.PageSize);

            return Result<PagedResultDto<CityDto>>.Ok(
                PagedResultDto<CityDto>.Create(cities.Select(Map).ToList(), query.Page, query.PageSize, totalCount),
                "CITIES_FOUND",
                "Cities found");
        }

        public async Task<CityResolution> Resolve(int? cityId, string? cityName, bool required)
        {
            var name = string.IsNullOrWhiteSpace(cityName) ? null : cityName.Trim();

            if (cityId != null && name != null)
                return Fail("INVALID_CITY", "Pick a Brazilian city or type a city abroad, not both");

            if (cityId != null)
                return await _cityRepository.ExistsAsync(cityId.Value)
                    ? new CityResolution(cityId, null, null)
                    : Fail("INVALID_CITY", "City not found");

            if (name != null)
                return name.Length is >= CityRules.CityNameMinLength and <= CityRules.CityNameMaxLength
                    ? new CityResolution(null, name, null)
                    : Fail("INVALID_CITY_NAME", $"The city abroad must have between {CityRules.CityNameMinLength} and {CityRules.CityNameMaxLength} characters");

            return required
                ? Fail("CITY_REQUIRED", "Tell the city")
                : new CityResolution(null, null, null);
        }

        public async Task<Result<List<CityDto>>> Featured()
        {
            var cities = await _cityRepository.ListByIdsAsync(CityRules.AbcCityIds);
            var order = CityRules.AbcCityIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);

            return Result<List<CityDto>>.Ok(cities.OrderBy(c => order[c.Id]).Select(Map).ToList(), "CITIES_FOUND", "Cities found");
        }

        public async Task<CityDto?> Get(int? cityId)
        {
            if (cityId == null)
                return null;

            var city = await _cityRepository.FindAsync(cityId.Value);
            return city == null ? null : Map(city);
        }

        public async Task<GeoPoint?> Coordinates(int cityId)
        {
            var city = await _cityRepository.FindAsync(cityId);
            return Point(city);
        }

        public static GeoPoint? Point(City? city) =>
            city?.Latitude != null && city.Longitude != null ? new GeoPoint(city.Latitude.Value, city.Longitude.Value) : null;

        public static CityDto Map(City city) => new()
        {
            Id = city.Id,
            Name = city.Name,
            Uf = city.Uf,
        };

        private static CityResolution Fail(string code, string message) =>
            new(null, null, new Error(code, message, ErrorType.Validation));
    }
}

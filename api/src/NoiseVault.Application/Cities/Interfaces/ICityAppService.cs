using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Application.Geo;

namespace NoiseVault.Application.Cities.Interfaces
{
    public sealed record CityResolution(int? CityId, string? CityName, Error? Error);

    public interface ICityAppService
    {
        Task<Result<PagedResultDto<CityDto>>> Search(CitySearchQueryDto query);

        Task<CityResolution> Resolve(int? cityId, string? cityName, bool required);
        Task<CityDto?> Get(int? cityId);
        Task<GeoPoint?> Coordinates(int cityId);
    }
}

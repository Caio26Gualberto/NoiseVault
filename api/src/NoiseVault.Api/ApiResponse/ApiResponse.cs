namespace NoiseVault.Api.ApiResponse
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Code { get; set; } = string.Empty;
        public string? Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}

namespace NoiseVault.Domain.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
    }
}

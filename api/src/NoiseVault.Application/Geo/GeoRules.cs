namespace NoiseVault.Application.Geo
{
    public readonly record struct GeoPoint(double Latitude, double Longitude);

    public static class GeoRules
    {
        public const double GridDegrees = 0.01;
        public const double KmPerDegreeLatitude = 110.574;
        public const double KmPerDegreeLongitudeAtEquator = 111.320;
        public const int MinReachKm = 1;
        public const int MaxReachKm = 500;
        public const int MaxSearchKm = 500;

        private static readonly int[] Buckets = [2, 5, 10, 15, 20, 25, 30, 40, 50, 75, 100];

        public static bool IsValid(double latitude, double longitude) =>
            double.IsFinite(latitude) && double.IsFinite(longitude)
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180;

        public static GeoPoint Snap(double latitude, double longitude) =>
            new(Math.Round(Math.Round(latitude / GridDegrees) * GridDegrees, 2), Math.Round(Math.Round(longitude / GridDegrees) * GridDegrees, 2));

        public static double KmPerDegreeLongitude(double latitude) =>
            KmPerDegreeLongitudeAtEquator * Math.Cos(latitude * Math.PI / 180);

        public static double DistanceKm(GeoPoint from, GeoPoint to)
        {
            const double earthRadiusKm = 6371;
            var dLat = (to.Latitude - from.Latitude) * Math.PI / 180;
            var dLon = (to.Longitude - from.Longitude) * Math.PI / 180;
            var a = Math.Pow(Math.Sin(dLat / 2), 2)
                + Math.Cos(from.Latitude * Math.PI / 180) * Math.Cos(to.Latitude * Math.PI / 180) * Math.Pow(Math.Sin(dLon / 2), 2);
            return 2 * earthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static int Bucket(double km)
        {
            foreach (var bucket in Buckets)
            {
                if (km <= bucket)
                    return bucket;
            }

            return (int)(Math.Round(km / 50, MidpointRounding.AwayFromZero) * 50);
        }
    }
}

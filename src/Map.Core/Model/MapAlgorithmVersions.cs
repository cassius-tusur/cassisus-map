namespace Map.Core.Model
{
    /// <summary>
    /// Defines supported deterministic map-generation algorithm versions.
    /// </summary>
    public static class MapAlgorithmVersions
    {
        public const string V1 = "v1";

        /// <summary>
        /// Determines whether the algorithm version is supported.
        /// </summary>
        /// <param name="version">The version value.</param>
        /// <returns><see langword="true"/> if the algorithm version is supported; otherwise, <see langword="false"/>.</returns>
        public static bool IsSupported(string version) => version == V1;
    }
}

namespace Map.Core.Generation
{
    /// <summary>
    /// Defines a source of deterministic two-dimensional noise samples.
    /// </summary>
    public interface INoise2D
    {
        /// <summary>
        /// Samples two-dimensional noise at the specified coordinates.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        double Sample(double x, double y);
    }
}

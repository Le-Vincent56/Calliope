namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// Generic validator interface; validates content for errors (must fix) and
    /// warnings (should fix);
    /// </summary>
    /// <typeparam name="T">The type to be validating; contravariance ensures that the type is input-only</typeparam>
    public interface IValidator<in T>
    {
        /// <summary>
        /// Validates the specified item and returns the result of the validation,
        /// including any errors and warnings identified during the process
        /// </summary>
        /// <param name="item">The item to be validated</param>
        /// <returns>
        /// A <see cref="ValidationResult"/> containing details about
        /// validation errors and warnings
        /// </returns>
        ValidationResult Validate(T item);
    }
}
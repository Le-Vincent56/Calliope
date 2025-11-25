using System.Collections.Generic;
using System.Text;

namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// A result of validation containing errors and warnings;
    /// errors must be fixed, warnings should be fixed
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// List of human-readable errors
        /// </summary>
        public List<string> Errors { get; }
        
        /// <summary>
        /// List of human-readable warnings
        /// </summary>
        public List<string> Warnings { get; }
        
        /// <summary>
        /// True if no errors were found (warnings are okay)
        /// </summary>
        public bool IsValid => Errors.Count == 0;
        
        /// <summary>
        /// True if any warnings were found
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        public ValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        /// <summary>
        /// Adds an error message to the collection of errors in the validation result
        /// </summary>
        /// <param name="error">The error message to be added</param>
        public void AddError(string error) => Errors.Add(error);

        /// <summary>
        /// Adds a warning message to the collection of warnings in the validation result
        /// </summary>
        /// <param name="warning">The warning message to be added</param>
        public void AddWarning(string warning) => Warnings.Add(warning);

        /// <summary>
        /// Generates a summary of the validation result, including any errors and warnings
        /// </summary>
        /// <returns>
        /// A string containing a summary of errors and warnings, or a message indicating
        /// the result is valid if no issues are found
        /// </returns>
        public string GetSummary()
        {
            if (IsValid && !HasWarnings) return "[ValidationResult] No Errors or Warnings Found; Valid Result";

            // Build summary
            StringBuilder summaryBuilder = new StringBuilder();
            summaryBuilder.Append("[ValidationResult] ");
            summaryBuilder.AppendLine();
            
            // Append errors
            for (int i = 0; i < Errors.Count; i++)
            {
                summaryBuilder.AppendLine(Errors[i]);
            }
            
            // Append warnings
            for(int i = 0; i < Warnings.Count; i++)
            {
                summaryBuilder.AppendLine(Warnings[i]);
            }
            
            return summaryBuilder.ToString();
        }
    }
}
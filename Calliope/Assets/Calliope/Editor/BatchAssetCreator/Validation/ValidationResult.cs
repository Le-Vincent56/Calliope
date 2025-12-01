using System.Collections.Generic;
using UnityEngine;

namespace Calliope.Editor.BatchAssetCreator.Validation
{
    /// <summary>
    /// Represents the result of a validation process, containing error, warning, and informational messages
    /// </summary>
    public class ValidationResult
    {
        private List<ValidationMessage> _messages = new List<ValidationMessage>();
        
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }
        public IReadOnlyList<ValidationMessage> Messages => _messages;
        public bool IsValid => ErrorCount == 0;

        /// <summary>
        /// Adds a validation message to the current instance with the specified severity and row index
        /// </summary>
        /// <param name="message">The validation message to add</param>
        /// <param name="severity">The severity level of the validation message</param>
        /// <param name="rowIndex">
        /// The index of the row to associate with the message;
        /// use -1 for a global message, or specify a non-negative index for a row-specific message
        /// </param>
        private void AddMessage(string message, ValidationSeverity severity, int rowIndex) =>
            _messages.Add(new ValidationMessage { Severity = severity, Message = message, RowIndex = rowIndex });

        /// <summary>
        /// Adds an error validation message to the current instance
        /// </summary>
        /// <param name="message">The error message to add</param>
        /// <param name="rowIndex">
        /// The index of the row to associate with the message;
        /// use -1 for a global message, or specify a non-negative index for a row-specific message
        /// </param>
        public void AddError(string message, int rowIndex = -1) =>
            AddMessage(message, ValidationSeverity.Error, rowIndex);

        /// <summary>
        /// Adds a warning validation message to the current instance
        /// </summary>
        /// <param name="message">The warning message to add</param>
        /// <param name="rowIndex">
        /// The index of the row to associate with the message;
        /// use -1 for a global message, or specify a non-negative index for a row-specific message
        /// </param>
        public void AddWarning(string message, int rowIndex = -1) =>
            AddMessage(message, ValidationSeverity.Warning, rowIndex);

        /// <summary>
        /// Adds an informational validation message to the current instance
        /// </summary>
        /// <param name="message">The informational message to add.</param>
        /// <param name="rowIndex">
        /// The index of the row to associate with the message;
        /// use -1 for a global message, or specify a non-negative index for a row-specific message
        /// </param>
        public void AddInfo(string message, int rowIndex = -1) =>
            AddMessage(message, ValidationSeverity.Info, rowIndex);

        /// <summary>
        /// Clears all validation messages from the current instance, resetting the validation state
        /// </summary>
        public void Clear() => _messages.Clear();

        /// <summary>
        /// Merges the validation messages from another <see cref="ValidationResult"/> instance
        /// into the current instance
        /// </summary>
        /// <param name="result">
        /// The <see cref="ValidationResult"/> instance whose messages are to be merged
        /// into the current validation result
        /// </param>
        public void Merge(ValidationResult result) => _messages.AddRange(result.Messages);
    }
}

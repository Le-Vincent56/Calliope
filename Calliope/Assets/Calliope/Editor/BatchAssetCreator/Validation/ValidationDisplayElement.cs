using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Validation
{
    /// <summary>
    /// Represents a UI element used for displaying validation results;
    /// this element displays errors, warnings, and informational messages
    /// related to a specific validation process
    /// </summary>
    public class ValidationDisplayElement : VisualElement
    {
        private VisualElement _messageContainer;
        private Label _summaryLabel;

        private static readonly Color ErrorColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color WarningColor = new Color(0.8f, 0.6f, 0.2f);
        private static readonly Color InfoColor = new Color(0.4f, 0.6f, 0.8f);
        private static readonly Color SuccessColor = new Color(0.2f, 0.7f, 0.3f);
        private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.15f);

        public ValidationDisplayElement()
        {
            // Style the container
            style.backgroundColor = BackgroundColor;
            style.borderTopLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderBottomRightRadius = 4;
            style.marginTop = 4;
            style.marginBottom = 4;
            style.paddingTop = 6;
            style.paddingBottom = 6;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.display = DisplayStyle.None;
            
            // Summary label at the top
            _summaryLabel = new Label();
            _summaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _summaryLabel.style.marginBottom = 4;
            Add(_summaryLabel);
            
            // Scrollable message container
            _messageContainer = new VisualElement();
            _messageContainer.style.maxHeight = 120;
            Add(_messageContainer);
        }

        /// <summary>
        /// Displays the validation results, including errors, warnings, and informational messages,
        /// and updates the visual element to reflect the current validation state
        /// </summary>
        /// <param name="result">A <see cref="ValidationResult"/> object containing the validation messages and counts of errors and warnings</param>
        public void DisplayResults(ValidationResult result)
        {
            // Clear the current message
            _messageContainer.Clear();

            // Exit case - no validation messages to display
            if (result == null || result.Messages.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            
            // Build the summary
            StringBuilder summary = new StringBuilder();
            
            // Display errors
            if (result.ErrorCount > 0)
            {
                // Append the errors
                summary.Append(result.ErrorCount);
                summary.Append(" error");
                if (result.ErrorCount > 1) summary.Append("s");
                
                // Set the error color
                _summaryLabel.style.color = ErrorColor;
            }

            // Display warnings
            if (result.WarningCount > 0)
            {
                // Append warnings
                if(summary.Length > 0) summary.Append(", ");
                summary.Append(result.WarningCount);
                summary.Append(" warning");
                if (result.WarningCount > 1) summary.Append("s");
                
                // Use the warning color if there are no errors
                if(result.ErrorCount == 0) _summaryLabel.style.color = WarningColor;
            }

            // Display successful validation
            if (result.IsValid && result.WarningCount == 0)
            {
                summary.Append("Validation passed");
                _summaryLabel.style.color = SuccessColor;
            }
            
            // Display the summary
            _summaryLabel.text = summary.ToString();
            
            StringBuilder messageBuilder = new StringBuilder();
            
            // Add individual messages
            foreach (ValidationMessage message in result.Messages)
            {
                // Build the label text
                messageBuilder.Clear();
                messageBuilder.Append("• ");
                messageBuilder.Append(message.Message);
                
                // Create the label
                Label messageLabel = new Label(messageBuilder.ToString());
                
                // Set the color of the label
                messageLabel.style.color = message.Severity switch
                {
                    ValidationSeverity.Error => ErrorColor,
                    ValidationSeverity.Warning => WarningColor,
                    _ => InfoColor
                };
                messageLabel.style.marginLeft = 8;
                messageLabel.style.whiteSpace = WhiteSpace.Normal;
                
                _messageContainer.Add(messageLabel);
            }
        }

        /// <summary>
        /// Hides the visual element by setting its display style to none
        /// </summary>
        public void Hide() => style.display = DisplayStyle.None;
    }
}
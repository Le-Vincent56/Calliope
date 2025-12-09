using System.Collections.Generic;
using System.Text;
using Calliope.Runtime.Diagnostics.Models;
using UnityEngine.UIElements;

namespace Calliope.Editor.DialogueTimeline.Views
{
    /// <summary>
    /// Visual element displaying the full scoring breakdown for
    /// fragment candidates
    /// </summary>
    public class ScoringBreakdownView : VisualElement
    {
        public ScoringBreakdownView(IReadOnlyList<CandidateScoringResult> candidates)
        {
            AddToClassList("detail-section");

            // Header
            int count = candidates?.Count ?? 0;
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("Fragment Scoring (");
            headerBuilder.Append(count);
            headerBuilder.Append(" candidates)");
            
            Label header = new Label(headerBuilder.ToString());
            header.AddToClassList("detail-section-header");
            Add(header);

            // Exit case - no candidates
            if (candidates == null || candidates.Count == 0)
            {
                Label empty = new Label("No candidates available");
                empty.AddToClassList("detail-placeholder");
                Add(empty);
                return;
            }
            
            List<CandidateScoringResult> sorted = new List<CandidateScoringResult>(candidates);
            sorted.Sort((a, b) =>
            {
                // Prioritize selected candidates
                if (a.WasSelected != b.WasSelected)
                    return a.WasSelected ? -1 : 1;
                
                // Then, sort by validity
                if (a.IsValid != b.IsValid)
                    return a.IsValid ? -1 : 1;
                
                return b.Score.CompareTo(a.Score);
            });

            foreach (CandidateScoringResult candidate in sorted)
            {
                Add(CreateCandidateElement(candidate));
            }
        }

        /// <summary>
        /// Creates a visual representation of a candidate scoring result, with styling and information
        /// based on the candidate's properties such as selection, validity, and scoring details
        /// </summary>
        /// <param name="candidate">
        /// The candidate scoring result containing details like fragment ID, text, score, validity,
        /// and optional scoring explanation
        /// </param>
        /// <returns>
        /// A VisualElement representing the candidate, with appropriate styles and details displayed;
        /// includes headers for ID and score, a truncated text preview, and an expandable section for
        /// scoring explanations if available
        /// </returns>
        private VisualElement CreateCandidateElement(CandidateScoringResult candidate)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("scoring-candidate");

            // Apply styling based on selection and validity
            if (candidate.WasSelected)
                container.AddToClassList("scoring-candidate-selected");
            else if (!candidate.IsValid)
                container.AddToClassList("scoring-candidate-invalid");

            // Header row
            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("scoring-candidate-header");

            StringBuilder labelBuilder = new StringBuilder();
            
            // ID
            labelBuilder.Append(candidate.FragmentID);
            labelBuilder.Append(" (SELECTED)");
            
            string idText = candidate.WasSelected 
                ? labelBuilder.ToString()
                : candidate.FragmentID;
            Label idLabel = new Label(idText);
            idLabel.AddToClassList("scoring-candidate-id");
            headerRow.Add(idLabel);
            labelBuilder.Clear();

            // Score
            labelBuilder.Append("Score: ");
            labelBuilder.Append(candidate.Score.ToString("F2"));
            
            string scoreText = candidate.IsValid
                ? labelBuilder.ToString()
                : "INVALID";
            Label scoreLabel = new Label(scoreText);
            scoreLabel.AddToClassList("scoring-candidate-score");
            if (!candidate.IsValid)
                scoreLabel.AddToClassList("scoring-candidate-score-invalid");
            headerRow.Add(scoreLabel);

            container.Add(headerRow);

            // Fragment text preview
            Label textPreview = new Label(TruncateText(candidate.FragmentText, 100));
            textPreview.AddToClassList("scoring-candidate-text");
            container.Add(textPreview);

            // Exit case - no explanation
            if (string.IsNullOrEmpty(candidate.ScoringExplanation)) return container;
            
            // Scoring explanation
            Foldout foldout = new Foldout();
            foldout.text = "Scoring Details";
            foldout.value = candidate.WasSelected; // Auto-expand selected
            foldout.AddToClassList("detail-foldout");

            Label explanation = new Label(candidate.ScoringExplanation);
            explanation.AddToClassList("scoring-candidate-explanation");
            foldout.Add(explanation);

            container.Add(foldout);

            return container;
        }
        
        /// <summary>
        /// Truncates the given text to the specified maximum length; if truncation occurs,
        /// the resulting string is suffixed with ellipses ("..."). If the input text is null or empty,
        /// a default placeholder message is returned
        /// </summary>
        /// <param name="text">The input text to be truncated</param>
        /// <param name="maxLength">The maximum allowable length for the truncated string, including the ellipses</param>
        /// <returns>
        /// A string that is either the original text (if its length is within the limit) or
        /// a truncated version of it with ellipses
        /// </returns>
        private string TruncateText(string text, int maxLength)
        {
            // Exit case - no text
            if (string.IsNullOrEmpty(text))
                return "(No dialogue yet)";

            // Exit case - the text does not need to be truncated
            if (text.Length <= maxLength) return text;
            
            // Truncate the text
            StringBuilder truncatedBuilder = new StringBuilder();
            truncatedBuilder.Append(text[..maxLength]);
            truncatedBuilder.Append("...");

            return truncatedBuilder.ToString();
        }
    }
}
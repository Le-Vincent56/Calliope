using System.Collections.Generic;
using System.Text;
using Calliope.Runtime.Diagnostics.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.DialogueTimeline.Views
{
    /// <summary>
    /// Visual element displaying the full branch evaluation breakdown
    /// </summary>
    public class BranchBreakdownView : VisualElement
    {
        public BranchBreakdownView(IReadOnlyList<BranchEvaluationResult> branches, bool usedDefaultFallback)
        {
            AddToClassList("detail-section");

            // Header
            int count = branches?.Count ?? 0;
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("Branch Evaluation (");
            headerBuilder.Append(count);
            headerBuilder.Append(" branches)");
            
            Label header = new Label(headerBuilder.ToString());
            header.AddToClassList("detail-section-header");
            Add(header);

            // Default fallback notice
            if (usedDefaultFallback)
            {
                VisualElement notice = new VisualElement();
                notice.AddToClassList("default-fallback-notice");

                Label noticeText = new Label("Used default fallback (no branch conditions matched)");
                noticeText.AddToClassList("default-fallback-text");
                notice.Add(noticeText);

                Add(notice);
            }

            // Exit case - no branches
            if (branches == null || branches.Count == 0)
            {
                Label empty = new Label("No branches to evaluate");
                empty.AddToClassList("detail-placeholder");
                Add(empty);
                return;
            }

            foreach (BranchEvaluationResult branch in branches)
            {
                Add(CreateBranchElement(branch));
            }
        }

        /// <summary>
        /// Creates a visual representation of a branch evaluation result, styled and configured
        /// based on its selection status, conditions, and branch metadata
        /// </summary>
        /// <param name="branch">
        /// The branch evaluation result containing information such as the branch index, next beat ID,
        /// whether the branch was selected, if all conditions passed, and additional condition results
        /// </param>
        /// <returns>
        /// A VisualElement representing the branch evaluation, including labels for the branch index,
        /// next beat ID, status (e.g., CONDITION PASSED, FAILED, or UNCONDITIONAL), and optional condition details
        /// </returns>
        private VisualElement CreateBranchElement(BranchEvaluationResult branch)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("branch-item");

            // Apply styling based on selection and status
            if (branch.WasSelected)
                container.AddToClassList("branch-item-selected");
            else if (!branch.AllConditionsPassed && !branch.IsUnconditional)
                container.AddToClassList("branch-item-failed");

            // Header row
            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("branch-header");
            
            StringBuilder labelBuilder = new StringBuilder();
            labelBuilder.Append("Branch ");
            labelBuilder.Append(branch.BranchIndex);
            labelBuilder.Append(" → ");
            labelBuilder.Append(branch.NextBeatID);
            if (branch.WasSelected)
                labelBuilder.Append(" (TAKEN)");
            
            // Target label
            Label targetLabel = new Label(labelBuilder.ToString());
            targetLabel.AddToClassList("branch-target");
            headerRow.Add(targetLabel);

            // Status badge
            Label statusLabel = new Label();
            statusLabel.AddToClassList("branch-status");
            
            if (branch.IsUnconditional)
            {
                statusLabel.text = "UNCONDITIONAL";
                statusLabel.AddToClassList("branch-status-unconditional");
            }
            else if (branch.AllConditionsPassed)
            {
                statusLabel.text = "PASSED";
                statusLabel.AddToClassList("branch-status-passed");
            }
            else
            {
                statusLabel.text = "FAILED";
                statusLabel.AddToClassList("branch-status-failed");
            }
            headerRow.Add(statusLabel);

            container.Add(headerRow);

            // Conditions
            if (branch.ConditionResults is { Count: > 0 })
            {
                foreach (ConditionEvaluationResult condition in branch.ConditionResults)
                {
                    container.Add(CreateConditionElement(condition));
                }
            }
            else if (branch.IsUnconditional)
            {
                // Unconditional branch - no conditions
                Label noConditions = new Label("(No conditions - always passes)");
                noConditions.style.fontSize = 10;
                noConditions.style.color = new Color(0.5f, 0.5f, 0.6f);
                noConditions.style.marginLeft = 8;
                noConditions.style.marginTop = 4;
                container.Add(noConditions);
            }

            return container;
        }

        /// <summary>
        /// Creates a visual representation of a condition evaluation result, styled and configured
        /// based on its type, description, pass/fail status, and optional additional details
        /// </summary>
        /// <param name="condition">
        /// The condition evaluation result containing details such as type, description, pass/fail status,
        /// and optional additional details to display
        /// </param>
        /// <returns>
        /// A VisualElement representing the condition, including labels for its type, description,
        /// and optional details; styled to visually indicate whether the condition was passed or failed
        /// </returns>
        private VisualElement CreateConditionElement(ConditionEvaluationResult condition)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("condition-item");
            container.AddToClassList(condition.Passed 
                ? "condition-passed" 
                : "condition-failed");

            // Condition type
            Label typeLabel = new Label(condition.ConditionType);
            typeLabel.AddToClassList("condition-type");
            container.Add(typeLabel);

            // Description
            Label descriptionLabel = new Label(condition.Description);
            descriptionLabel.AddToClassList("condition-description");
            container.Add(descriptionLabel);

            // Exit case - no details provided
            if (string.IsNullOrEmpty(condition.Details)) return container;
            
            // Details
            Label detailsLabel = new Label(condition.Details);
            detailsLabel.AddToClassList("condition-details");
            container.Add(detailsLabel);

            return container;
        }
    }
}
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Assembles final dialogue text by substituting variables
    ///
    /// Supported patterns:
    /// - {speaker.name} -> Character's display name
    /// - {target.name} -> Target character's display name
    /// - {speaker.pronounce.subject} -> "he", "he", "they", etc.
    /// - {target.pronounce.object} -> "him", "her", "them", etc.
    /// - {target.pronounce.possessive} -> "his", "her", "their", etc.
    /// - {target.pronounce.possessive.short} -> "his", "her", "their", etc.
    /// - {var:customKey} -> Custom variable value from context
    /// </summary>
    public class TextAssembler
    {
        private readonly Dictionary<string, string> _customVariables;
        
        public TextAssembler()
        {
            _customVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets a custom variable with a specified key and value; this variable can be used
        /// for text substitution within the text assembly process
        /// </summary>
        /// <param name="key">The unique key representing the variable to be set</param>
        /// <param name="value">The value to associate with the specified key</param>
        public void SetVariable(string key, string value) => _customVariables[key] = value;

        /// <summary>
        /// Removes a custom variable associated with the specified key from the context,
        /// preventing its use in the text substitution process
        /// </summary>
        /// <param name="key">The unique key representing the variable to be removed</param>
        public void ClearVariable(string key) => _customVariables.Remove(key);

        /// <summary>
        /// Removes all custom variables currently stored in the context,
        /// clearing any existing mappings between keys and values used for text substitution
        /// </summary>
        public void ClearAllVariables() => _customVariables.Clear();

        /// <summary>
        /// Assembles a text string by replacing predefined placeholders with corresponding values
        /// based on the provided template, speaker, target, and custom variables
        /// </summary>
        /// <param name="template">The template string containing placeholders to be substituted with actual values</param>
        /// <param name="speaker">An instance of ICharacter representing the speaker, used for replacing speaker-specific placeholders</param>
        /// <param name="target">An instance of ICharacter representing the target, used for replacing target-specific placeholders</param>
        /// <returns>A string with all applicable placeholders replaced with their corresponding values from the provided data</returns>
        public string Assemble(string template, ICharacter speaker, ICharacter target)
        {
            // Exit case - there is no template to assemble
            if (string.IsNullOrEmpty(template)) return template;

            StringBuilder result = new StringBuilder(template);
            
            // Replace speaker variables
            if (speaker != null)
            {
                result.Replace("{speaker.name}", speaker.DisplayName);
                SubstitutePronounSet(result, "speaker", speaker.Pronouns);
            }
            
            // Replace target variables
            if (target != null)
            {
                result.Replace("{target.name}", target.DisplayName);
                SubstitutePronounSet(result, "target", target.Pronouns);
            }
            
            StringBuilder placeholderBuilder = new StringBuilder();
            
            // Replace custom variables
            foreach (KeyValuePair<string, string> kvp in _customVariables)
            {
                // Create the placeholder string
                placeholderBuilder.Clear();
                placeholderBuilder.Append("{var:");
                placeholderBuilder.Append(kvp.Key);
                placeholderBuilder.Append("}");
                
                result.Replace(placeholderBuilder.ToString(), kvp.Value);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Replaces placeholders in a string with values from a given pronoun set; this method substitutes
        /// specific pronoun-related placeholders (e.g., subject, object, possessive, reflexive) with their corresponding
        /// values from the provided pronoun set, supporting both default and title case formats
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder object containing the text where placeholders will be replaced</param>
        /// <param name="prefix">The prefix used to locate placeholders for pronouns in the text</param>
        /// <param name="pronouns">The PronounSet object containing pronoun values for substitution</param>
        private void SubstitutePronounSet(StringBuilder stringBuilder, string prefix, PronounSet pronouns)
        {
            // Build the subject string
            StringBuilder pronounBuilder = new StringBuilder();
            StringBuilder titleCaseBuilder = new StringBuilder();
            pronounBuilder.Append("{");
            pronounBuilder.Append(prefix);
            pronounBuilder.Append(".pronoun.subject");
            pronounBuilder.Append("}");
            titleCaseBuilder.Append("{");
            titleCaseBuilder.Append(prefix);
            titleCaseBuilder.Append(".pronoun.Subject");
            titleCaseBuilder.Append("}");
            
            // Replace the subject string
            stringBuilder.Replace(pronounBuilder.ToString(), pronouns.Subject);
            stringBuilder.Replace(titleCaseBuilder.ToString(), Capitalize(pronouns.Subject));
            
            // Build the object string
            pronounBuilder.Clear();
            titleCaseBuilder.Clear();
            pronounBuilder.Append("{");
            pronounBuilder.Append(prefix);
            pronounBuilder.Append(".pronoun.object");
            pronounBuilder.Append("}");
            titleCaseBuilder.Append("{");
            titleCaseBuilder.Append(prefix);
            titleCaseBuilder.Append(".pronoun.Object");
            titleCaseBuilder.Append("}");
            
            // Replace the object string
            stringBuilder.Replace(pronounBuilder.ToString(), pronouns.Object);
            stringBuilder.Replace(titleCaseBuilder.ToString(), Capitalize(pronouns.Object));
            
            // Build the possessive string
            pronounBuilder.Clear();
            titleCaseBuilder.Clear();
            pronounBuilder.Append("{");
            pronounBuilder.Append(prefix);
            pronounBuilder.Append(".pronoun.possessive");
            pronounBuilder.Append("}");
            titleCaseBuilder.Append("{");
            titleCaseBuilder.Append(prefix);
            titleCaseBuilder.Append(".pronoun.Possessive");
            titleCaseBuilder.Append("}");
            
            // Replace the possessive string
            stringBuilder.Replace(pronounBuilder.ToString(), pronouns.Possessive);
            stringBuilder.Replace(titleCaseBuilder.ToString(), Capitalize(pronouns.Possessive));
            
            // Build the reflexive string
            pronounBuilder.Clear();
            titleCaseBuilder.Clear();
            pronounBuilder.Append("{");
            pronounBuilder.Append(prefix);
            pronounBuilder.Append(".pronoun.reflexive");
            pronounBuilder.Append("}");
            titleCaseBuilder.Append("{");
            titleCaseBuilder.Append(prefix);
            titleCaseBuilder.Append(".pronoun.Reflexive");
            titleCaseBuilder.Append("}");
            
            // Replace the reflexive string
            stringBuilder.Replace(pronounBuilder.ToString(), pronouns.Reflexive);
            stringBuilder.Replace(titleCaseBuilder.ToString(), Capitalize(pronouns.Reflexive));
        }

        /// <summary>
        /// Capitalizes the first character of the provided text string while keeping the rest of the string unchanged
        /// </summary>
        /// <param name="text">The input string to be capitalized. If null or empty, the method returns the original input</param>
        /// <returns>
        /// The input string with its first character converted to uppercase. If the input contains a single character,
        /// only that character is capitalized; returns the original input if it is null or empty
        /// </returns>
        private string Capitalize(string text)
        {
            // Exit case - there is no text to capitalize
            if(string.IsNullOrEmpty(text)) return text;
            
            // Exit case - there is only one letter
            if(text.Length == 1) return char.ToUpper(text[0]).ToString();
            
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
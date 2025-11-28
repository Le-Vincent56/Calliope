using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// A reusable searchable dropdown component with fuzzy matching,
    /// recently used items, and optional "Create New" functionality
    /// </summary>
    public class SearchableDropdown : VisualElement
    {
        /// <summary>
        /// Represents a dropdown item, with an ID, and an optional display name and category
        /// </summary>
        public class DropdownItem
        {
            public string ID { get; set; }
            public string DisplayName { get; set; }
            public string Category { get; set; }

            public DropdownItem(string id, string displayName = null, string category = null)
            {
                ID = id;
                DisplayName = displayName ?? id;
                Category = category;
            }
        }
        
        private const int MaxRecentItems = 5;
        private readonly string _label;
        private readonly string _prefsKey;
        private readonly bool _allowCreateNew;
        private readonly Func<List<DropdownItem>> _onGetItems;
        private readonly Action<string> _onValueChanged;
        private readonly Action<string> _onCreateNew;
        private readonly Action<string> _onItemSelected;
        private TextField _textField;
        private Button _dropdownButton;
        private string _currentValue;
        private List<String> _recentItems;

        public string Value
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                if (_textField == null) return;
                _textField.SetValueWithoutNotify(value ?? "");
            }
        }
        
        public SearchableDropdown(
            string label,
            string prefsKey,
            Func<List<DropdownItem>> onGetItems,
            Action<string> onValueChanged,
            bool allowCreateNew = false,
            Action<string> onCreateNew = null,
            Action<string> onItemSelected = null
        )
        {
            _label = label;
            _prefsKey = prefsKey;
            _allowCreateNew = allowCreateNew;
            _onGetItems = onGetItems;
            _onValueChanged = onValueChanged;
            _onCreateNew = onCreateNew;
            _onItemSelected = onItemSelected;
            
            // Load recent items
            LoadRecentItems();

            // Build UI
            BuildUI();
        }

        /// <summary>
        /// Constructs the user interface elements for the searchable dropdown component;
        /// this method initializes and arranges the label, text field, and dropdown button
        /// within a container, setting their styles and functionalities
        /// </summary>
        private void BuildUI()
        {
            style.flexDirection = FlexDirection.Row;
            style.marginBottom = 8;

            // Container for label and field
            VisualElement container = new VisualElement();
            container.style.flexDirection =
                FlexDirection.Row;
            container.style.flexGrow = 1;
            container.style.alignItems = Align.Center;

            // Label
            Label labelElement = new Label(_label);
            labelElement.style.minWidth = 120;
            labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(labelElement);

            // Text field for typing/display
            _textField = new TextField();
            _textField.style.flexGrow = 1;
            _textField.RegisterValueChangedCallback(evt =>
            {
                _currentValue = evt.newValue;
                _onValueChanged?.Invoke(evt.newValue);
            });
            container.Add(_textField);

            // Dropdown button
            _dropdownButton = new Button(ShowDropdownMenu);
            _dropdownButton.text = "▼";
            _dropdownButton.style.width = 24;
            _dropdownButton.style.marginLeft = 4;
            container.Add(_dropdownButton);

            Add(container);
        }

        /// <summary>
        /// Displays the dropdown menu for the searchable dropdown component, organizing and
        /// filtering items dynamically based on user input and category structure; this method
        /// populates the menu with recent items, categorized items, and optionally allows
        /// item creation
        /// </summary>
        private void ShowDropdownMenu()
        {
            GenericMenu menu = new GenericMenu();
            
            // Get the current search text for filtering
            string searchText = _textField.value?.ToLowerInvariant() ?? "";
            
            // Get all items
            List<DropdownItem> allItems = _onGetItems?.Invoke() ?? new List<DropdownItem>();
            
            // Add recent items section
            if (_recentItems.Count > 0)
            {
                menu.AddDisabledItem(new GUIContent("Recent Items"));
                for (int i = 0; i < _recentItems.Count; i++)
                {
                    string recentID = _recentItems[i];
                    
                    // Check if the item is still valid
                    bool isValid = false;
                    string displayName = recentID;

                    for (int j = 0; j < allItems.Count; j++)
                    {
                        // Skip if the item doesn't match
                        if (allItems[j].ID != recentID) continue;
                        
                        isValid = true;
                        displayName = allItems[j].DisplayName;
                        break;
                    }

                    // Skip if the item is no longer valid
                    if (!isValid) continue;
                    
                    // Add the item to the dropdown
                    string itemID = recentID;
                    menu.AddItem(new GUIContent(displayName), false, () => SelectItem(itemID));
                }
                
                menu.AddSeparator("");
            }
            
            // Filter and add items
            Dictionary<string, List<DropdownItem>> categorizedItems = new Dictionary<string, List<DropdownItem>>();
            List<DropdownItem> uncategorizedItems = new List<DropdownItem>();

            for (int i = 0; i < allItems.Count; i++)
            {
                DropdownItem item = allItems[i];
                
                // Skip if the filter doesn't match any items
                if (!string.IsNullOrEmpty(searchText) && !FuzzyMatch(item.DisplayName, searchText) && !FuzzyMatch(item.ID, searchText))
                    continue;

                // Add to the appropriate category
                if (!string.IsNullOrEmpty(item.Category))
                {
                    if (!categorizedItems.ContainsKey(item.Category)) 
                        categorizedItems[item.Category] = new List<DropdownItem>();
                    
                    categorizedItems[item.Category].Add(item);
                }
                else uncategorizedItems.Add(item);
            }
            
            // Add categorized item
            foreach (KeyValuePair<string, List<DropdownItem>> category in categorizedItems)
            {
                menu.AddDisabledItem(new GUIContent(category.Key));

                for (int i = 0; i < category.Value.Count; i++)
                {
                    DropdownItem item = category.Value[i];
                    string itemID = item.ID;
                    menu.AddItem(new GUIContent(item.DisplayName), _currentValue == itemID, () => SelectItem(itemID));
                }
                
                menu.AddSeparator("");
            }
            
            // Add uncategorized items
            for (int i = 0; i < uncategorizedItems.Count; i++)
            {
                DropdownItem item = uncategorizedItems[i];
                string itemID = item.ID;
                menu.AddItem(new GUIContent(item.DisplayName), _currentValue == itemID, () => SelectItem(itemID));
            }
            
            // Add "Create New" option
            if (_allowCreateNew && _onCreateNew != null)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("+ Create New..."), false, () => _onCreateNew?.Invoke(_textField.value));
            }
            
            menu.ShowAsContext();
        }

        private void SelectItem(string id)
        {
            _currentValue = id;
            _textField.SetValueWithoutNotify(id);
            
            // Add to recent items
            AddToRecentItems(id);
            
            // Notify callbacks
            _onValueChanged?.Invoke(id);
            _onItemSelected?.Invoke(id);
        }

        /// <summary>
        /// Performs a fuzzy matching algorithm to determine if a given text contains
        /// the characters from a specified pattern in order, ignoring case sensitivity
        /// </summary>
        /// <param name="text">The source text to be searched</param>
        /// <param name="pattern">The pattern of characters to match within the text</param>
        /// <returns>True if the pattern matches the text using the fuzzy matching logic; otherwise, false</returns>
        private bool FuzzyMatch(string text, string pattern)
        {
            // Exit case - no pattern specified
            if (string.IsNullOrEmpty(pattern)) return true;

            // Exit case - no text specified
            if (string.IsNullOrEmpty(text)) return false;
            
            // Sanitize the text
            text = text.ToLowerInvariant();
            pattern = pattern.ToLowerInvariant();
            
            // Exit case - the text contains the pattern
            if(text.Contains(pattern)) return true;
            
            // Check if all pattern characters appear in order
            int patternIndex = 0;
            for (int i = 0; i < text.Length && patternIndex < pattern.Length; i++)
            {
                // Skip if the characters don't match
                if (text[i] != pattern[patternIndex]) continue;
                
                patternIndex++;
            }
            
            return patternIndex == pattern.Length;
        }

        /// <summary>
        /// Loads the list of recently selected items from the editor's preferences storage;
        /// this method retrieves and parses the saved recent items associated with the
        /// preferences key, ensuring the list is populated with a limited number of entries
        /// </summary>
        private void LoadRecentItems()
        {
            _recentItems = new List<string>();

            // Get the key for saving recent items
            string key = GetPrefsKey();
            
            // Exit case - no saved items
            if (!EditorPrefs.HasKey(key)) return;
            
            // Load the saved items
            string saved = EditorPrefs.GetString(key);
            string[] items = saved.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // Add the items to the list
            for (int i = 0; i < items.Length && i < MaxRecentItems; i++)
            {
                _recentItems.Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the specified item to the list of recently used items;
        /// if the item already exists in the list, it is removed and reinserted at the front;
        /// the list is then trimmed to the maximum allowed size, and the updated list is saved
        /// </summary>
        /// <param name="id">The unique identifier of the item to add to the recent items list</param>
        private void AddToRecentItems(string id)
        {
            // Remove if the item already exists
            _recentItems.Remove(id);
            
            // Add to the front of the list
            _recentItems.Insert(0, id);
            
            // Trim to the max size
            while (_recentItems.Count > MaxRecentItems)
            {
                _recentItems.RemoveAt(_recentItems.Count - 1);
            }
            
            // Save to EditorPrefs
            SaveRecentItems();
        }

        /// <summary>
        /// Persists the list of recently used items for the searchable dropdown
        /// by converting the list into a delimited string and saving it
        /// using Unity Editor preferences
        /// </summary>
        private void SaveRecentItems()
        {
            // Build the string representation of the list
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _recentItems.Count; i++)
            {
                // Separate using a "|" as the delimiter
                if(i > 0) builder.Append("|");
                builder.Append(_recentItems[i]);
            }
            
            // Save to EditorPrefs
            EditorPrefs.SetString(GetPrefsKey(), builder.ToString());
        }

        /// <summary>
        /// Generates a unique string key used for storing and retrieving recent items
        /// in the dropdown menu from the Unity Editor preferences
        /// </summary>
        /// <returns>A string representing the unique preferences key for this searchable dropdown</returns>
        private string GetPrefsKey()
        {
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append("Calliope_SearchableDropdown_");
            keyBuilder.Append(_prefsKey);
            return keyBuilder.ToString();
        }
    }
}
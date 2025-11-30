using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.Tabs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator
{
    /// <summary>
    /// A window for creating Calliope assets in bulk
    /// </summary>
    public class BatchAssetCreatorWindow : EditorWindow
    {
        private int _currentTabIndex = 0;
        private string _saveFolderPath = "Assets/Calliope/Content";
        private Label _statusLabel;
        private VisualElement _tabContentContainer;
        private VisualElement _tabBar;

        private List<IBatchTab> _tabs;

        /// <summary>
        /// Displays the Batch Asset Creator window for creating Calliope assets in bulk;
        /// configures the window with a title and minimum size
        /// </summary>
        [MenuItem("Window/Calliope/Batch Asset Creator")]
        public static void ShowWindow()
        {
            BatchAssetCreatorWindow window = GetWindow<BatchAssetCreatorWindow>();
            window.titleContent = new GUIContent("Batch Asset Creator");
            window.minSize = new Vector2(700, 400);
        }
        
        public static void ShowFromSceneTemplateEditor() => ShowWindow();

        private void CreateGUI()
        {
            // Gather tabs
            _tabs = new List<IBatchTab>
            {
                new FragmentBatchTab(),
                new TraitBatchTab()
            };
            
            // Create the root element
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            // Tab bar
            _tabBar = CreateTabBar();
            root.Add(_tabBar);

            // Toolbar
            VisualElement toolbar = CreateToolbar();
            root.Add(toolbar);

            // Tab content container
            _tabContentContainer = new VisualElement();
            _tabContentContainer.style.flexGrow = 1;
            _tabContentContainer.style.marginTop = 8;
            root.Add(_tabContentContainer);

            // Status label
            _statusLabel = new Label("");
            _statusLabel.style.marginTop = 8;
            _statusLabel.style.color = new Color(0.6f, 0.8f, 0.6f);
            root.Add(_statusLabel);

            // Show the first tab
            ShowTab(0);
        }

        /// <summary>
        /// Creates a tab bar element containing buttons for each tab in the Batch Asset Creator;
        /// each button switches between different tab contents and highlights the currently active tab
        /// </summary>
        /// <returns>A VisualElement representing the tab bar with interactive buttons for tab navigation</returns>
        private VisualElement CreateTabBar()
        {
            VisualElement tabBar = new VisualElement();
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.marginBottom = 8;

            // Create each tab
            for (int i = 0; i < _tabs.Count; i++)
            {
                int tabIndex = i;
                Button tabButton = new Button(() => ShowTab(tabIndex));
                tabButton.text = _tabs[i].TabName;
                tabBar.Add(tabButton);

                // Highlight selected tab
                if (i == _currentTabIndex)
                    tabButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.7f);
                
                tabBar.Add(tabButton);
            }

            return tabBar;
        }

        /// <summary>
        /// Creates and configures a toolbar for the Batch Asset Creator window,
        /// including input fields and action buttons such as Browse, Clear, and Create All
        /// </summary>
        /// <returns>
        /// A VisualElement representing the toolbar with configured layout, style, and functionality
        /// </returns>
        private VisualElement CreateToolbar()
        {
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;

            // Create the folder label
            Label folderLabel = new Label("Save Folder:");
            folderLabel.style.marginRight = 8;
            toolbar.Add(folderLabel);

            // Create the folder field
            TextField folderField = new TextField();
            folderField.value = _saveFolderPath;
            folderField.style.flexGrow = 1;
            folderField.RegisterValueChangedCallback(evt => _saveFolderPath = evt.newValue);
            toolbar.Add(folderField);

            // Create the Browse button
            Button browseButton = new Button(BrowseForFolder);
            browseButton.text = "Browse";
            browseButton.style.marginLeft = 4;
            toolbar.Add(browseButton);

            // Create the Clear button
            Button clearButton = new Button(ClearCurrentTab);
            clearButton.text = "Clear";
            clearButton.style.marginLeft = 16;
            toolbar.Add(clearButton);

            // Create the Create All button
            Button createAllButton = new Button(CreateAssets);
            createAllButton.text = "Create All";
            createAllButton.style.marginLeft = 8;
            createAllButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
            toolbar.Add(createAllButton);

            return toolbar;
        }

        private void ShowTab(int index)
        {
            // Set the current index
            _currentTabIndex = index;
            
            // Rebuild tab bar to update selection styling
            VisualElement root = rootVisualElement;
            int tabBarIndex = root.IndexOf(_tabBar);
            root.Remove(_tabBar);
            _tabBar = CreateTabBar();
            root.Insert(tabBarIndex, _tabBar);
            
            // Rebuild content
            _tabContentContainer.Clear();
            _tabContentContainer.Add(_tabs[index].BuildContent(UpdateStatus));

            UpdateStatus();
        }

        /// <summary>
        /// Updates the status label in the Batch Asset Creator window to reflect the number
        /// of valid rows available for creation in the currently active tab;
        /// constructs a status message indicating the count of valid items and the tab name
        /// </summary>
        private void UpdateStatus()
        {
            // Get the valid row count from the current tab
            int validCount = _tabs[_currentTabIndex].ValidRowCount;
            
            // Create the status label
            StringBuilder statusBuilder = new StringBuilder();
            statusBuilder.Append(validCount);
            statusBuilder.Append(" valid ");
            statusBuilder.Append(_tabs[_currentTabIndex].TabName.ToLower());
            statusBuilder.Append(" ready to create");
            _statusLabel.text = statusBuilder.ToString();
        }

        /// <summary>
        /// Opens a folder selection dialog for the user to choose a save location;
        /// if a valid folder within the project's "Assets" directory is selected, updates
        /// the current save folder path; otherwise, maintains the existing path
        /// </summary>
        private void BrowseForFolder()
        {
            // Open a folder selection dialog
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Select Save Folder",
                    _saveFolderPath, 
                ""
            );

            // Exit case - no selected path
            if (string.IsNullOrEmpty(selectedPath)) return;

            // Exit case - the selected path is not inside the Assets folder
            if (!selectedPath.StartsWith(Application.dataPath)) return;

            // Construct the path
            StringBuilder saveFolderBuilder = new StringBuilder();
            saveFolderBuilder.Append("Assets");
            saveFolderBuilder.Append(selectedPath.Substring(Application.dataPath.Length));
            
            _saveFolderPath = saveFolderBuilder.ToString();
        }

        /// <summary>
        /// Clears all data and rows associated with the currently active tab,
        /// resetting it to its initial state and updating the UI accordingly
        /// </summary>
        private void ClearCurrentTab()
        {
            _tabs[_currentTabIndex].ClearRows();
            UpdateStatus();
        }

        /// <summary>
        /// Generates and saves a batch of assets for the currently selected tab;
        /// updates the status label with the number of assets created and their type;
        /// ensures the project's asset database remains up-to-date
        /// </summary>
        private void CreateAssets()
        {
            // Create the assets and get the amount created
            int createdCount = _tabs[_currentTabIndex].CreateAssets(_saveFolderPath);

            // Save and refresh the database
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Build the status message
            StringBuilder statusBuilder = new StringBuilder();
            statusBuilder.Append("Created ");
            statusBuilder.Append(createdCount);
            statusBuilder.Append(" ");
            statusBuilder.Append(_tabs[_currentTabIndex].TabName.ToLower());
            statusBuilder.Append(" asset(s)");
            
            // Set the status
            _statusLabel.text = statusBuilder.ToString();
            _statusLabel.style.color = new Color(0.6f, 0.8f, 0.6f);
        }
    }
}

using System;
using System.Text;
using System.Threading.Tasks;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Infrastructure.Logging;
using Calliope.Runtime.Saliency;
using Calliope.Runtime.Services;
using Calliope.Unity.Repositories;
using UnityEngine;
using ILogger = Calliope.Infrastructure.Logging.ILogger;

namespace Calliope.Unity.Components
{
    public class Calliope : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Selection strategy to use (weighted-random, highest-score, least-recent, custom, etc.")]
        [SerializeField] private string selectionStrategy = "weighted-random";
        
        [Tooltip("Random seed for deterministic selection (0 = random)")]
        [SerializeField] private int randomSeed = 0;
        
        [Tooltip("Number of recently used fragments to track")]
        [SerializeField] private int recencyWindow = 5;
        
        [Tooltip("Score multiplier for recently used fragments (0.5 = haf-score")]
        [SerializeField] [Range(0f, 1f)] private float recencyPenalty = 0.5f;
        
        private bool _initialized = false;
        public static Calliope Instance { get; private set; }
        
        public IEventBus EventBus { get; private set; }
        public ILogger Logger { get; private set; }
        
        public ITraitRepository TraitRepository { get; private set; }
        public ICharacterRepository CharacterRepository { get; private set; }
        public IVariationSetRepository VariationSetRepository { get; private set; }
        public ISceneTemplateRepository SceneTemplateRepository { get; private set; }
        
        public IRelationshipProvider RelationshipProvider { get; private set; }
        public IFragmentScorer FragmentScorer { get; private set; }
        public ISaliencyStrategy SaliencyStrategy { get; private set; }
        public ISelectionContext SelectionContext { get; private set; }
        public TextAssembler TextAssembler { get; private set; }
        public RelationshipModifierApplier RelationshipApplier { get; private set; }
        public DialogueLineBuilder DialogueLineBuilder { get; private set; }
        public CharacterCaster CharacterCaster { get; private set; }
        public SceneOrchestrator SceneOrchestrator { get; private set; }

        private async void Awake()
        {
            // Singleton pattern - only one Calliope instance allowed per scene
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize services
            await InitializeServices();
        }

        private void OnDestroy()
        {
            // Exit case - not initialized or not the current instance
            if (!_initialized || Instance != this) return;
            
            // Clean up Addressable assets
            if(TraitRepository is AddressableTraitRepository addressableTraitRepository)
                addressableTraitRepository.ReleaseAssets();
            
            if(CharacterRepository is AddressableCharacterRepository addressableCharacterRepository)
                addressableCharacterRepository.ReleaseAssets();
            
            if(VariationSetRepository is AddressableVariationSetRepository addressablesVariationSetRepository)
                addressablesVariationSetRepository.ReleaseAssets();
            
            if(SceneTemplateRepository is AddressableSceneTemplateRepository addressableSceneTemplateRepository)
                addressableSceneTemplateRepository.ReleaseAssets();
                
            Debug.Log("[Calliope] Released Addressable assets");
        }

        /// <summary>
        /// Initializes the core services and systems required for the Calliope framework to function;
        /// this includes logging, event handling, repositories, relationship management, scoring,
        /// selection strategies, text assembly, and scene orchestrating components
        /// </summary>
        private async Task InitializeServices()
        {
            // Exit case - already initialized
            if (_initialized) return;

            Debug.Log("[Calliope] Initializing Calliope services");
            
            // Core infrastructure
            Logger = new UnityLogger();
            EventBus = new EventBus();
            
            // Repositories (Addressable-based)
            TraitRepository = new AddressableTraitRepository();
            CharacterRepository = new AddressableCharacterRepository();
            VariationSetRepository = new AddressableVariationSetRepository();
            SceneTemplateRepository = new AddressableSceneTemplateRepository();
            
            // Relationship system
            RelationshipProvider = new RelationshipProvider(EventBus, Logger);
            
            // Scoring System
            FragmentScorer = new FragmentScorer();
            
            // Selection context
            int? seed = randomSeed > 0 ? randomSeed : null;
            SelectionContext = new SelectionContext(seed, recencyWindow, recencyPenalty);
            
            // Selection strategy
            SaliencyStrategy = CreateSelectionStrategy(selectionStrategy);
            
            // Text assembly
            TextAssembler = new TextAssembler();
            RelationshipApplier = new RelationshipModifierApplier(RelationshipProvider, Logger);
            
            // Scene creation
            DialogueLineBuilder = new DialogueLineBuilder(
                FragmentScorer,
                SaliencyStrategy,
                TextAssembler,
                RelationshipApplier,
                RelationshipProvider,
                SelectionContext,
                EventBus,
                Logger
            );
            CharacterCaster = new CharacterCaster(Logger, SelectionContext.Random);
            SceneOrchestrator = new SceneOrchestrator(RelationshipProvider, EventBus, Logger);
            
            // Set to initialized
            _initialized = true;
            
            Debug.Log("[Calliope] Calliope services initialized successfully");

            // Preload all content
            await PreloadAllContent();
        }

        /// <summary>
        /// Creates and returns the selection strategy based on the provided strategy identifier
        /// </summary>
        /// <param name="strategyID">
        /// The identifier of the desired selection strategy. Possible values include:
        /// "weighted-random" for a weighted random selection,
        /// "highest-score" for selecting items with the highest score,
        /// "least-recent" for selecting the least recently used items
        /// </param>
        /// <returns>
        /// An implementation of <see cref="ISaliencyStrategy"/> corresponding to the provided identifier;
        /// if an unknown identifier is provided, defaults to the "weighted-random" strategy
        /// </returns>
        private ISaliencyStrategy CreateSelectionStrategy(string strategyID)
        {
            switch (strategyID.ToLower())
            {
                case "weighted-random": return new WeightedRandomStrategy();
                case "highest-score": return new HighestScoreStrategy();
                case "least-recent": return new LeastRecentStrategy();
                default:
                    StringBuilder debugBuilder = new StringBuilder();
                    debugBuilder.Append("[Calliope] Unknown selection strategy '");
                    debugBuilder.Append(strategyID);
                    debugBuilder.Append("', defaulting to strategy 'weighted-random'");
                    Debug.LogWarning(debugBuilder.ToString());
                    
                    return new WeightedRandomStrategy();
            }
        }

        /// <summary>
        /// Preloads all essential content into memory by asynchronously fetching data from
        /// the trait, character, variation set, and scene template repositories; this ensures
        /// that all necessary resources are available for the Calliope framework to function
        /// without runtime delays; logs success or failure details during the process
        /// </summary>
        public async Task PreloadAllContent()
        {
            Debug.Log("[Calliope] Preloading all content...");

            try
            {
                await TraitRepository.GetAllAsync();
                await CharacterRepository.GetAllAsync();
                await VariationSetRepository.GetAllAsync();
                await SceneTemplateRepository.GetAllAsync();

                Debug.Log("[Calliope] All content preloaded successfully");
            }
            catch (Exception ex)
            {
                StringBuilder errorBuilder = new StringBuilder();
                errorBuilder.Append("[Calliope] Failed to preload all content: ");
                errorBuilder.Append(ex.Message);
                
                Debug.LogError(errorBuilder.ToString());
            }
        }

        /// <summary>
        /// Resets the current selection context to its default state by invoking the reset operation;
        /// clears all selection-related data and provides a clean slate for future operations
        /// </summary>
        public void ResetSelectionContext()
        {
            SelectionContext.Reset();
            Debug.Log("[Calliope] Selection context reset");
        }

        /// <summary>
        /// Resets all character relationships managed by the framework by clearing all entries
        /// from the relationship provider; this ensures a clean slate for relationship data
        /// and logs the operation for debugging purposes
        /// </summary>
        public void ResetAllRelationships()
        {
            RelationshipProvider.ClearAllRelationships();
            Debug.Log("[Calliope] All relationships cleared");
        }
    }
}

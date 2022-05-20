using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace MoreLevelContent.Shared.Generation
{
    public class LevelContentProducer
    {
        /// <summary>
        /// If the the generator has outposts to spawn or not
        /// </summary>
        public bool Enabled { get; private set; }

        private readonly List<Character> characters;
        private readonly Dictionary<Character, List<Item>> characterItems;
        private Submarine enemyBase;

        private readonly float baseChanceForHusk = 0.1f;
        private readonly float spawnChanceRandomOffset = 10.0f;
        private readonly AfflictionPrefab huskAffliction;

        private float spawnChance = 0f;
        private float pirateDiff = 0f;
        private float huskChance = 0f;

        private float addedMissionDifficultyPerPlayer = 0;

        private bool willSpawn = false;
        private bool isHusked = false;
        private PirateNPCSetDef selectedPirateSet;


        public LevelContentProducer()
        {
            characters = new List<Character>();
            characterItems = new Dictionary<Character, List<Item>>();
            SpawnSubOnPath = typeof(Level).GetMethod("SpawnSubOnPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pirateOutposts = new List<PirateOutpostDef>();
            pirateSets = new List<PirateNPCSetDef>();

            huskAffliction = AfflictionPrefab.List.FirstOrDefault(a => a.Identifier == "huskinfection");
            if (huskAffliction == null)
            {
                Log.Error("Couldn't find the husk prefab!!");
            }

            // Create a list of all avaliable pirate outposts
            Enabled = FindAndScoreOutpostFiles() && FindAndScoreNPCs();
        }

        private bool FindAndScoreOutpostFiles()
        {
            Log.Debug("Collecting pirate outposts...");
            var outpostModuleFiles = ContentPackageManager.EnabledPackages.All
            .SelectMany(p => p.GetFiles<OutpostModuleFile>())
            .OrderBy(f => f.UintIdentifier).ToList();

            foreach (var outpostModuleFile in outpostModuleFiles)
            {
                SubmarineInfo subInfo = new SubmarineInfo(outpostModuleFile.Path.Value);
                if (subInfo.OutpostModuleInfo != null)
                {
                    if (subInfo.OutpostModuleInfo.AllowedLocationTypes.Contains("ilo_PirateOutpost"))
                        pirateOutposts.Add(new PirateOutpostDef(outpostModuleFile, subInfo));
                }
            }

            Log.Debug("Sorting modules by their diff ranges...");
            pirateOutposts.Sort();

            foreach (var item in pirateOutposts)
            {
                Log.Debug(item.DifficultyRange.ToString());
            }

            if (pirateOutposts.Count > 0)
            {
                Log.Debug($"Collected {pirateOutposts.Count} pirate outposts");
                return true;
            }
            else
            {
                Log.Error("Failed to find any pirate outposts!!!");
                return false;
            }
        }

        private bool FindAndScoreNPCs()
        {
            List<MissionPrefab> pirateMissions = MissionPrefab.Prefabs.Where(m => m.Identifier.StartsWith("ilo_pirate")).ToList();

            foreach (MissionPrefab prefab in pirateMissions)
            {
                pirateSets.Add(new PirateNPCSetDef(prefab, prefab.Name.Value));
            }

            Log.Debug("Sorting sets by their diff ranges...");
            pirateSets.Sort();

            if (pirateSets.Count == 0)
            {
              Log.Error("Failed to find pirates to spawn :(");
              return false;
            }
            Log.Debug($"Collected {pirateSets.Count} pirate NPC sets to choose from.");
            return true;
        }

        private void RollForSpawn(float levelDiff)
        {
            Log.Debug("Rolling for a priate spawn...");
            Random rand = new MTRandom(ToolBox.StringToInt(Level.Loaded.Seed));
            UpdatePirateSpawnData(levelDiff, rand);
            int spawnInt = rand.Next(100);
            int huskInt = rand.Next(100);
            willSpawn = spawnChance > spawnInt;
            isHusked = huskChance > huskInt;
            Log.Debug($"Will Spawn: {willSpawn} ({spawnInt}), Is Husked: {isHusked} ({huskInt})");
        }

        private void UpdatePirateSpawnData(float levelDiff, Random rand)
        {
            float baseChance = levelDiff < 100 ? MathF.Min(levelDiff / 2, (levelDiff / 5) + 15) : 100f;
            float spawnOffset = MathHelper.Lerp(-spawnChanceRandomOffset, spawnChanceRandomOffset, (float)rand.NextDouble());

            spawnChance = baseChance + spawnOffset;
            Log.Debug($"Modified pirate spawn chance for diff {levelDiff} is {spawnChance}, base chance {baseChance}, offset {spawnOffset}");

            float diffOffset = MathHelper.Lerp(-spawnChanceRandomOffset, spawnChanceRandomOffset, (float)rand.NextDouble());
            pirateDiff = levelDiff + diffOffset;
            Log.Debug($"Modified pirate diff is {pirateDiff}, level diff {levelDiff}, offset {diffOffset}");

            huskChance = MathF.Max(baseChanceForHusk, levelDiff / 10);
            Log.Debug($"Modified chance for pirates to be husked is {huskChance}");

            selectedPirateSet = GetElementsForDiff(pirateDiff, pirateSets).GetRandom(Rand.RandSync.ServerAndClient);
            Log.Debug($"Selected set {selectedPirateSet.Prefab.Name}");
        }

        public void CreatePirateOutpost()
        {
            characters.Clear();
            characterItems.Clear();
            Level level = Level.Loaded;

            RollForSpawn(level.Difficulty);
            if (!willSpawn) return;

            List<PirateOutpostDef> suitableDefs = GetElementsForDiff(pirateDiff, pirateOutposts);

            PirateOutpostDef pickedOutpost = suitableDefs.GetRandom(Rand.RandSync.ServerAndClient);
            enemyBase = SpawnSubOnPath.Invoke(level, new object[] { "Pirate Base", pickedOutpost.ContentFile, SubmarineType.BeaconStation }) as Submarine;
            enemyBase.Info.DisplayName = "Pirate Base";
            enemyBase.ShowSonarMarker = true;

            if (enemyBase.GetItems(alsoFromConnectedSubs: false).Find(i => i.HasTag("reactor") && !i.NonInteractable)?.GetComponent<Reactor>() is Reactor reactor)
            {
                reactor.PowerUpImmediately();
            }

            enemyBase.TeamID = CharacterTeamType.None;
            Log.InternalDebug($"Spawned a pirate base with name {enemyBase.Info.Name}");
        }

        public void CreatePirates()
        {
            if (!willSpawn || enemyBase == null) return;
            bool commanderAssigned = false;
            Log.Debug("Spawning Pirates");

            // pick a set
            // NPCSet selectedSet = pirateSets.GetRandom(Rand.RandSync.ServerAndClient);
            // TODO: Allow modders to add to the NPC set for pirate spawns
            XElement characterConfig = selectedPirateSet.Prefab.ConfigElement.GetChildElement("Characters");
            XElement characterTypeConfig = selectedPirateSet.Prefab.ConfigElement.GetChildElement("CharacterTypes");
            addedMissionDifficultyPerPlayer = selectedPirateSet.Prefab.ConfigElement.GetAttributeFloat("addedmissiondifficultyperplayer", 0);

            foreach (XElement element in characterConfig.Elements())
            {
                // it is possible to get more than the "max" amount of characters if the modified difficulty is high enough; this is intentional
                // if necessary, another "hard max" value could be used to clamp the value for performance/gameplay concerns
                int amountCreated = 1;
                for (int i = 0; i < amountCreated; i++)
                {
                    XElement characterType = 
                        characterTypeConfig.Elements()
                        .Where(e => e.GetAttributeString("typeidentifier", string.Empty) == element.GetAttributeString("typeidentifier", string.Empty))
                        .FirstOrDefault();

                    if (characterType == null)
                    {
                        DebugConsole.NewMessage($"No characters defined in the loaded XML!!");
                        return;
                    }

                    XElement variantElement = GetRandomDifficultyModifiedElement(characterType, pirateDiff, 25f);

                    Character spawnedCharacter = CreateHuman(GetHumanPrefabFromElement(variantElement), characters, characterItems, enemyBase, CharacterTeamType.None, null);
                    if (!commanderAssigned)
                    {
                        bool isCommander = variantElement.GetAttributeBool("iscommander", false);
                        if (isCommander && spawnedCharacter.AIController is HumanAIController humanAIController)
                        {
                            humanAIController.InitShipCommandManager();
                            commanderAssigned = true;
                            Log.Debug("Spawning Commader");
                        }
                    }

                    foreach (Item item in spawnedCharacter.Inventory.AllItems)
                    {
                        if (item?.Prefab.Identifier == "idcard")
                        {
                            item.AddTag("id_pirate");
                        }
                    }
                }
            }

            if (isHusked)
            {
                Log.Debug("Husking pirates...");
                foreach (Character character in characters)
                {
                    character.CharacterHealth.ApplyAffliction(character.AnimController.MainLimb, new Affliction(huskAffliction, 200));
                    character.CharacterHealth.ApplyAffliction(character.AnimController.MainLimb, new Affliction(AfflictionPrefab.InternalDamage, 100));
                }
            }
        }

        private Character CreateHuman(HumanPrefab humanPrefab, List<Character> characters, Dictionary<Character, List<Item>> characterItems, Submarine submarine, CharacterTeamType teamType, ISpatialEntity positionToStayIn = null, Rand.RandSync humanPrefabRandSync = Rand.RandSync.ServerAndClient, bool giveTags = true)
        {
            CharacterInfo characterInfo = humanPrefab.GetCharacterInfo(Rand.RandSync.ServerAndClient) ?? new CharacterInfo(CharacterPrefab.HumanSpeciesName, npcIdentifier: humanPrefab.Identifier, jobOrJobPrefab: humanPrefab.GetJobPrefab(humanPrefabRandSync), randSync: humanPrefabRandSync);
            characterInfo.TeamID = teamType;

            if (positionToStayIn == null)
            {
                positionToStayIn =
                    WayPoint.GetRandom(SpawnType.Human, characterInfo.Job?.Prefab, submarine) ??
                    WayPoint.GetRandom(SpawnType.Human, null, submarine);
            }

            Character spawnedCharacter = Character.Create(characterInfo.SpeciesName, positionToStayIn.WorldPosition, ToolBox.RandomSeed(8), characterInfo);
            spawnedCharacter.HumanPrefab = humanPrefab;
            humanPrefab.InitializeCharacter(spawnedCharacter, positionToStayIn);
            humanPrefab.GiveItems(spawnedCharacter, submarine, Rand.RandSync.ServerAndClient);

            characters.Add(spawnedCharacter);
            characterItems.Add(spawnedCharacter, spawnedCharacter.Inventory.FindAllItems(recursive: true));

            return spawnedCharacter;
        }

        private HumanPrefab GetHumanPrefabFromElement(XElement element)
        {
            if (element.Attribute("name") != null)
            {
                DebugConsole.ThrowError("Error");

                return null;
            }

            Identifier characterIdentifier = element.GetAttributeIdentifier("identifier", Identifier.Empty);
            Identifier characterFrom = element.GetAttributeIdentifier("from", Identifier.Empty);
            HumanPrefab humanPrefab = NPCSet.Get(characterFrom, characterIdentifier);
            if (humanPrefab == null)
            {
                DebugConsole.ThrowError("Couldn't spawn character for mission: character prefab \"" + characterIdentifier + "\" not found");
                return null;
            }

            return humanPrefab;
        }

        private XElement GetRandomDifficultyModifiedElement(XElement parentElement, float levelDifficulty, float randomnessModifier)
        {
            Random rand = new MTRandom(ToolBox.StringToInt(Level.Loaded.Seed));
            // look for the element that is closest to our difficulty, with some randomness
            XElement bestElement = null;
            float bestValue = float.MaxValue;
            foreach (XElement element in parentElement.Elements())
            {
                float applicabilityValue = GetDifficultyModifiedValue(element.GetAttributeFloat(0f, "preferreddifficulty"), levelDifficulty, randomnessModifier, rand);
                if (applicabilityValue < bestValue)
                {
                    bestElement = element;
                    bestValue = applicabilityValue;
                }
            }
            return bestElement;
        }

        private float GetDifficultyModifiedValue(float preferredDifficulty, float levelDifficulty, float randomnessModifier, Random rand) => Math.Abs(levelDifficulty - preferredDifficulty + MathHelper.Lerp(-randomnessModifier, randomnessModifier, (float)rand.NextDouble()));

        private List<T> GetElementsForDiff<T>(float diff, List<T> elements) where T : DefWithDifficultyRange
        {
            Log.InternalDebug($"Looking for {typeof(T).Name} with difficulty of {diff}...");

            List<T> suitableDefs = elements.Where(d => d.DifficultyRange.IsInRangeOf(diff)).ToList();
            if (suitableDefs.Count == 0)
            {
                Log.InternalDebug($"No defs found for difficulty {diff}! Falling back to the highest level difficulty...");
                suitableDefs.Add(elements.First());
            }

            if (suitableDefs.Count == 0)
            {
                Log.Error("DEFS WAS ZERO, SOMETHING WENT __HORRIBLY__ WRONG!");
            }

            Log.InternalDebug($"Found {suitableDefs.Count} suitable {typeof(T).Name} to choose from.");
            suitableDefs.Sort();
            return suitableDefs;
        }
    }
}

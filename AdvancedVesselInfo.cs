using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System;
using KSP.Localization;
using System.Globalization;
using ClickThroughFix; // ClickThroughBlocker support
using ToolbarControl_NS; // ToolbarController support

namespace AdvancedVesselInfo
{
    // The Manager class handles background data logic, persistence, and automated tracking.
    [KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
    public class AdvancedVesselInfoManager : MonoBehaviour
    {
        public static AdvancedVesselInfoManager Instance;
        public string currentMissionPurpose = "Enter Mission";

        private string storagePath;
        private string settingsPath;

        public Rect savedWindowRect = new Rect(300, 300, 450, 650);
        public Rect savedSettingsRect = new Rect(400, 400, 300, 300);
        public float savedLibraryWidth = 250f;
        public bool savedShowLibrary = false;
        public bool savedShowHelp = false;
        public List<string> customFamilies = new List<string>();

        public int descFontSize = 12;
        public bool descFontBold = false;
        public int logFontSize = 11;
        public bool logFontBold = false;
        public int payFontSize = 11;
        public bool payFontBold = false;

        public float savedDescHeight = 80f;
        public float savedLogHeight = 120f;
        public float savedPayHeight = 100f;

        // Initializes the plugin, sets up file paths, and ensures only one instance exists.
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                storagePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/AdvancedVesselInfo/PluginData/CraftData.cfg");
                settingsPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/AdvancedVesselInfo/PluginData/Settings.cfg");

                LoadSettings();
                GameEvents.onLevelWasLoaded.Add(OnLevelLoaded);

                // Register the mod globally ONLY ONCE when the manager is created to prevent duplicates on scene changes.
                ToolbarControl.RegisterMod(AdvancedVesselInfoUI.MODID, AdvancedVesselInfoUI.MODNAME);
            }
            else { Destroy(gameObject); }
        }

        // Reads UI positions, font settings, and custom family tags from the settings configuration file.
        private void LoadSettings()
        {
            if (File.Exists(settingsPath))
            {
                ConfigNode node = ConfigNode.Load(settingsPath);
                if (node != null)
                {
                    float f; bool b; int i;
                    if (node.HasValue("windowX") && float.TryParse(node.GetValue("windowX"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedWindowRect.x = f;
                    if (node.HasValue("windowY") && float.TryParse(node.GetValue("windowY"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedWindowRect.y = f;
                    if (node.HasValue("windowWidth") && float.TryParse(node.GetValue("windowWidth"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedWindowRect.width = f;
                    if (node.HasValue("windowHeight") && float.TryParse(node.GetValue("windowHeight"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedWindowRect.height = f;

                    if (node.HasValue("settingsX") && float.TryParse(node.GetValue("settingsX"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedSettingsRect.x = f;
                    if (node.HasValue("settingsY") && float.TryParse(node.GetValue("settingsY"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedSettingsRect.y = f;

                    if (node.HasValue("libraryWidth") && float.TryParse(node.GetValue("libraryWidth"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedLibraryWidth = f;
                    if (node.HasValue("showLibrary") && bool.TryParse(node.GetValue("showLibrary"), out b)) savedShowLibrary = b;
                    if (node.HasValue("showHelp") && bool.TryParse(node.GetValue("showHelp"), out b)) savedShowHelp = b;

                    if (node.HasValue("descFontSize") && int.TryParse(node.GetValue("descFontSize"), out i)) descFontSize = i;
                    if (node.HasValue("descFontBold") && bool.TryParse(node.GetValue("descFontBold"), out b)) descFontBold = b;
                    if (node.HasValue("logFontSize") && int.TryParse(node.GetValue("logFontSize"), out i)) logFontSize = i;
                    if (node.HasValue("logFontBold") && bool.TryParse(node.GetValue("logFontBold"), out b)) logFontBold = b;
                    if (node.HasValue("payFontSize") && int.TryParse(node.GetValue("payFontSize"), out i)) payFontSize = i;
                    if (node.HasValue("payFontBold") && bool.TryParse(node.GetValue("payFontBold"), out b)) payFontBold = b;

                    if (node.HasValue("descHeight") && float.TryParse(node.GetValue("descHeight"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedDescHeight = f;
                    if (node.HasValue("logHeight") && float.TryParse(node.GetValue("logHeight"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedLogHeight = f;
                    if (node.HasValue("payHeight") && float.TryParse(node.GetValue("payHeight"), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) savedPayHeight = f;

                    ConfigNode famNode = node.GetNode("FAMILIES");
                    if (famNode != null)
                    {
                        foreach (ConfigNode.Value v in famNode.values)
                        {
                            if (v.name == "name" && !customFamilies.Contains(v.value))
                                customFamilies.Add(v.value);
                        }
                    }
                }
            }
        }

        // Saves current UI state, window positions, and font preferences to the configuration file.
        public void SaveSettings(Rect winRect, Rect setRect, float libWidth, bool showLib, bool showHelp)
        {
            savedWindowRect = winRect;
            savedSettingsRect = setRect;
            savedLibraryWidth = libWidth;
            savedShowLibrary = showLib;
            savedShowHelp = showHelp;

            ConfigNode node = new ConfigNode("SETTINGS");
            node.AddValue("windowX", winRect.x.ToString(CultureInfo.InvariantCulture));
            node.AddValue("windowY", winRect.y.ToString(CultureInfo.InvariantCulture));
            node.AddValue("windowWidth", winRect.width.ToString(CultureInfo.InvariantCulture));
            node.AddValue("windowHeight", winRect.height.ToString(CultureInfo.InvariantCulture));

            node.AddValue("settingsX", setRect.x.ToString(CultureInfo.InvariantCulture));
            node.AddValue("settingsY", setRect.y.ToString(CultureInfo.InvariantCulture));

            node.AddValue("libraryWidth", libWidth.ToString(CultureInfo.InvariantCulture));
            node.AddValue("showLibrary", showLib.ToString());
            node.AddValue("showHelp", showHelp.ToString());

            node.AddValue("descFontSize", descFontSize.ToString());
            node.AddValue("descFontBold", descFontBold.ToString());
            node.AddValue("logFontSize", logFontSize.ToString());
            node.AddValue("logFontBold", logFontBold.ToString());
            node.AddValue("payFontSize", payFontSize.ToString());
            node.AddValue("payFontBold", payFontBold.ToString());

            node.AddValue("descHeight", savedDescHeight.ToString(CultureInfo.InvariantCulture));
            node.AddValue("logHeight", savedLogHeight.ToString(CultureInfo.InvariantCulture));
            node.AddValue("payHeight", savedPayHeight.ToString(CultureInfo.InvariantCulture));

            ConfigNode famNode = node.AddNode("FAMILIES");
            foreach (string f in customFamilies)
                famNode.AddValue("name", f);

            node.Save(settingsPath);
        }

        // Deletes a custom family and resets all assigned crafts to Uncategorized.
        public void DeleteFamily(string familyName)
        {
            if (customFamilies.Contains(familyName))
            {
                customFamilies.Remove(familyName);
                SaveSettings(savedWindowRect, savedSettingsRect, savedLibraryWidth, savedShowLibrary, savedShowHelp);
            }

            if (File.Exists(storagePath))
            {
                ConfigNode root = ConfigNode.Load(storagePath);
                if (root != null)
                {
                    bool changed = false;
                    foreach (ConfigNode node in root.GetNodes())
                    {
                        if (node.HasValue("familyTag") && node.GetValue("familyTag") == familyName)
                        {
                            node.SetValue("familyTag", "Uncategorized", true);
                            changed = true;
                        }
                    }
                    if (changed) root.Save(storagePath);
                }
            }
        }

        // Resets the family tag of a specific craft back to Uncategorized.
        public void RemoveCraftFromFamily(string shipName)
        {
            if (File.Exists(storagePath))
            {
                ConfigNode root = ConfigNode.Load(storagePath);
                if (root != null)
                {
                    string nodeName = shipName.Replace(" ", "_");
                    ConfigNode vesselNode = root.GetNode(nodeName);
                    if (vesselNode != null)
                    {
                        vesselNode.SetValue("familyTag", "Uncategorized", true);
                        root.Save(storagePath);
                    }
                }
            }
        }

        // Event handler that detects when a flight begins to automatically record the launch.
        private void OnLevelLoaded(GameScenes scene)
        {
            if (scene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            {
                if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
                    LogLaunch(FlightGlobals.ActiveVessel.vesselName);
            }
            else if (scene == GameScenes.EDITOR)
            {
                currentMissionPurpose = "Enter Mission";
            }
        }

        // Creates a new timestamped flight log entry for the current vessel or its linked alias.
        private void LogLaunch(string shipName)
        {
            string date = KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true);
            ConfigNode root = ConfigNode.Load(storagePath) ?? new ConfigNode();
            string nodeName = shipName.Replace(" ", "_");

            // Check for linked craft
            ConfigNode sourceNode = root.GetNode(nodeName);
            string targetNodeName = nodeName;

            if (sourceNode != null && sourceNode.HasValue("linkedLogCraft"))
            {
                string link = sourceNode.GetValue("linkedLogCraft");
                if (!string.IsNullOrEmpty(link) && link != "None")
                {
                    targetNodeName = link.Replace(" ", "_");
                }
            }

            ConfigNode vesselNode = root.GetNode(targetNodeName) ?? root.AddNode(targetNodeName);
            ConfigNode historyNode = vesselNode.GetNode("HISTORY") ?? vesselNode.AddNode("HISTORY");
            ConfigNode launchEntry = historyNode.AddNode("LAUNCH");
            launchEntry.AddValue("date", date);
            launchEntry.AddValue("purpose", currentMissionPurpose);
            launchEntry.AddValue("status", "0");
            root.Save(storagePath);
        }

        // Saves all custom vessel data including escaped descriptions, payload info, and flight logs.
        public void SaveCraftData(string shipName, string description, string purpose, string family, string linkedCraft, int parts, float mass, float cost, List<AdvancedVesselInfoUI.PayloadEntry> payloads, List<AdvancedVesselInfoUI.LaunchEntry> history = null)
        {
            ConfigNode root = ConfigNode.Load(storagePath) ?? new ConfigNode();
            string nodeName = shipName.Replace(" ", "_");
            ConfigNode vesselNode = root.GetNode(nodeName) ?? root.AddNode(nodeName);

            // Escaping newlines to \n ensures data integrity within the KSP ConfigNode format.
            string safeDesc = string.IsNullOrEmpty(description) ? "" : description.Replace("\n", "\\n");
            vesselNode.SetValue("description", safeDesc, true);

            vesselNode.SetValue("missionPurpose", purpose, true);
            vesselNode.SetValue("familyTag", family, true);
            vesselNode.SetValue("linkedLogCraft", linkedCraft, true);
            vesselNode.SetValue("partCount", parts.ToString(), true);
            vesselNode.SetValue("mass", mass.ToString("F2", CultureInfo.InvariantCulture), true);
            vesselNode.SetValue("cost", cost.ToString("F2", CultureInfo.InvariantCulture), true);

            currentMissionPurpose = purpose;

            if (vesselNode.HasNode("PAYLOADS")) vesselNode.RemoveNode("PAYLOADS");
            ConfigNode payloadNode = vesselNode.AddNode("PAYLOADS");
            foreach (var entry in payloads)
            {
                ConfigNode item = payloadNode.AddNode("ENTRY");
                item.AddValue("dest", entry.destination);
                item.AddValue("tons", entry.amount);
            }

            if (history != null)
            {
                if (vesselNode.HasNode("HISTORY")) vesselNode.RemoveNode("HISTORY");
                ConfigNode historyNode = vesselNode.AddNode("HISTORY");
                foreach (var log in history)
                {
                    ConfigNode entry = historyNode.AddNode("LAUNCH");
                    entry.AddValue("date", log.date);
                    entry.AddValue("purpose", log.purpose);
                    entry.AddValue("status", log.status.ToString());
                }
            }
            root.Save(storagePath);
        }

        // Fetches the specific data node for a vessel from the saved database.
        public ConfigNode GetVesselNode(string shipName)
        {
            if (!File.Exists(storagePath)) return null;
            ConfigNode root = ConfigNode.Load(storagePath);
            return root?.GetNode(shipName.Replace(" ", "_"));
        }

        void OnDestroy()
        {
            if (GameEvents.onLevelWasLoaded != null)
                GameEvents.onLevelWasLoaded.Remove(OnLevelLoaded);
        }
    }

    // The UI class manages the visual interface, window drawing, and user interactions.
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class AdvancedVesselInfoUI : MonoBehaviour
    {
        public class PayloadEntry { public string destination = "Target"; public string amount = "0.0"; }
        public class LaunchEntry { public string date = ""; public string purpose = ""; public int status = 0; }

        // Constants used by ToolbarController to identify the mod globally.
        internal const string MODID = "AdvancedVesselInfo";
        internal const string MODNAME = "Advanced Vessel Info";

        // ToolbarController replaces the standard ApplicationLauncherButton.
        private static ToolbarControl toolbarControl = null;

        private bool showGui = false;
        private bool showCraftList = false;
        private bool showHelp = false;
        private bool showSettings = false;

        private bool editMode = false;
        private bool payloadEditMode = false;
        private bool descriptionEditMode = false;
        private bool familyEditMode = false;
        private bool missionEditMode = false;
        private bool showFamilyDropdown = false;
        private bool linkEditMode = false;
        private bool showLinkDropdown = false;
        private bool sortByFamily = false;

        private Rect windowRect;
        private Rect listRect = new Rect(0, 0, 250, 650);
        private Rect helpRect = new Rect(0, 0, 320, 650);
        private Rect settingsRect;

        private bool isResizing = false;
        private Vector2 resizeStart = Vector2.zero;
        private Rect minSize = new Rect(400, 500, 400, 500);

        private float libraryWidth;
        private bool isResizingLibrary = false;
        private float libraryResizeStartX = 0f;
        private float libraryResizeStartWidth = 0f;

        private float descHeight;
        private float logHeight;
        private float payHeight;
        private bool isResizingDesc = false;
        private bool isResizingLog = false;
        private bool isResizingPay = false;
        private float dragStartY;
        private float dragStartHeight;

        private string searchTerm = "";
        private string newFamilyInput = "";
        private Dictionary<string, List<string>> craftByFolder = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> craftByFamily = new Dictionary<string, List<string>>();
        private Dictionary<string, bool> folderExpanded = new Dictionary<string, bool>();
        private Dictionary<string, string> craftFilePaths = new Dictionary<string, string>();

        private string currentCraftName = "";
        private string customDescription = "";
        private string currentFamilyTag = "";
        private string currentLinkedCraft = "None";
        private int currentPartCount = 0;
        private float currentMass = 0f;
        private float currentCost = 0f;

        private List<PayloadEntry> payloadEntries = new List<PayloadEntry>();
        private List<LaunchEntry> launchHistory = new List<LaunchEntry>();

        private Vector2 libraryScrollPos;
        private Vector2 historyScrollPos;
        private Vector2 payloadScrollPos;
        private Vector2 helpScrollPos;
        private Vector2 descriptionScrollPos;
        private Vector2 familyScrollPos;
        private Vector2 linkScrollPos;

        private GUIStyle folderHeaderStyle;
        private GUIStyle craftButtonStyle;
        private bool stylesInitialized = false;

        // Sets up initial UI states and subscribes to editor events for live updates.
        void Awake()
        {
            windowRect = AdvancedVesselInfoManager.Instance.savedWindowRect;
            settingsRect = AdvancedVesselInfoManager.Instance.savedSettingsRect;
            libraryWidth = AdvancedVesselInfoManager.Instance.savedLibraryWidth;
            showCraftList = AdvancedVesselInfoManager.Instance.savedShowLibrary;
            showHelp = AdvancedVesselInfoManager.Instance.savedShowHelp;

            descHeight = AdvancedVesselInfoManager.Instance.savedDescHeight;
            logHeight = AdvancedVesselInfoManager.Instance.savedLogHeight;
            payHeight = AdvancedVesselInfoManager.Instance.savedPayHeight;

            // Initialize the ToolbarController directly instead of waiting for ApplicationLauncher.
            if (toolbarControl == null) AddToolbarButton();

            if (HighLogic.LoadedSceneIsEditor) GameEvents.onEditorShipModified.Add((ship) => SyncWithCurrentEditor());
        }

        // Defines custom UI styles like backgrounds and text colors used throughout the plugin.
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            folderHeaderStyle = new GUIStyle(GUI.skin.box);
            folderHeaderStyle.normal.textColor = Color.white;
            folderHeaderStyle.fontStyle = FontStyle.Bold;
            folderHeaderStyle.alignment = TextAnchor.MiddleLeft;
            folderHeaderStyle.padding = new RectOffset(5, 5, 2, 2);
            folderHeaderStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.6f));

            craftButtonStyle = new GUIStyle(GUI.skin.label);
            craftButtonStyle.padding = new RectOffset(15, 5, 2, 2);
            craftButtonStyle.fontStyle = FontStyle.Bold;
            craftButtonStyle.hover.textColor = new Color(0.73f, 0.85f, 0.33f);
            craftButtonStyle.normal.textColor = Color.white;
            craftButtonStyle.hover.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.1f));

            stylesInitialized = true;
        }

        // Generates a simple texture from a single color for UI element backgrounds.
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Helper method to draw a draggable handle for resizing UI sections
        private float DrawResizeHandle(float currentHeight, ref bool isResizingFlag)
        {
            GUILayout.Box("≡", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fixedHeight = 10, padding = new RectOffset(0, 0, 0, 0) }, GUILayout.ExpandWidth(true));
            Rect handleRect = GUILayoutUtility.GetLastRect();

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && handleRect.Contains(e.mousePosition))
            {
                isResizingFlag = true;
                dragStartY = e.mousePosition.y;
                dragStartHeight = currentHeight;
                e.Use();
            }

            if (isResizingFlag)
            {
                if (e.type == EventType.MouseDrag)
                {
                    float newHeight = dragStartHeight + (e.mousePosition.y - dragStartY);
                    currentHeight = Mathf.Clamp(newHeight, 30f, 600f);
                }
                else if (e.type == EventType.MouseUp)
                {
                    isResizingFlag = false;
                }
            }
            return currentHeight;
        }

        // Synchronizes the plugin with the current ship being edited in the VAB or SPH.
        private void SyncWithCurrentEditor()
        {
            if (EditorLogic.fetch != null && EditorLogic.fetch.shipNameField != null)
            {
                if (currentCraftName != EditorLogic.fetch.shipNameField.text)
                {
                    currentCraftName = EditorLogic.fetch.shipNameField.text;
                    LoadCraftData(currentCraftName);
                }
                UpdateAutoSpecs();
            }
        }

        // Automatically fetches part count, mass, and cost directly from the editor logic.
        private void UpdateAutoSpecs()
        {
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts != null)
            {
                currentPartCount = EditorLogic.fetch.ship.parts.Count;
                float dryMass, fuelMass, dryCost, fuelCost;
                EditorLogic.fetch.ship.GetShipMass(out dryMass, out fuelMass);
                EditorLogic.fetch.ship.GetShipCosts(out dryCost, out fuelCost);
                currentMass = dryMass + fuelMass;
                currentCost = dryCost + fuelCost;
            }
        }

        // Adds the mod's icon to both Blizzy's toolbar and the stock toolbar via ToolbarController.
        void AddToolbarButton()
        {
            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(
                    () => {
                        showGui = true;
                        LoadAvailableCrafts();
                        if (HighLogic.LoadedSceneIsEditor) SyncWithCurrentEditor();
                        else if (FlightGlobals.ActiveVessel != null) LoadCraftData(FlightGlobals.ActiveVessel.vesselName);
                    },
                    () => {
                        showGui = false; editMode = false; payloadEditMode = false; descriptionEditMode = false;
                        familyEditMode = false; showFamilyDropdown = false; linkEditMode = false; showLinkDropdown = false;
                        missionEditMode = false; showSettings = false;
                    },
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
                    MODID,
                    "advancedVesselInfoButton",
                    "AdvancedVesselInfo/Icons/Toolbar",
                    "AdvancedVesselInfo/Icons/Toolbar",
                    MODNAME
                );
            }
        }

        // Scans the KSP directory structure to build a list of all available .craft files.
        private void LoadAvailableCrafts()
        {
            craftByFolder.Clear();
            craftByFamily.Clear();
            craftFilePaths.Clear();

            foreach (string f in AdvancedVesselInfoManager.Instance.customFamilies)
            {
                craftByFamily[f] = new List<string>();
                if (!folderExpanded.ContainsKey(f)) folderExpanded[f] = true;
            }

            string savePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships");
            string stockPath = Path.Combine(KSPUtil.ApplicationRootPath, "Ships");
            string gameDataPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");

            ScanDirectoryExact(Path.Combine(savePath, "VAB"), "VAB (Custom)");
            ScanDirectoryExact(Path.Combine(savePath, "SPH"), "SPH (Custom)");
            ScanDirectoryExact(Path.Combine(stockPath, "VAB"), "VAB (Stock)");
            ScanDirectoryExact(Path.Combine(stockPath, "SPH"), "SPH (Stock)");

            ScanGameDataForShips(gameDataPath);

            foreach (var key in new List<string>(craftByFolder.Keys)) { craftByFolder[key].Sort(); }
            foreach (var key in new List<string>(craftByFamily.Keys)) { craftByFamily[key].Sort(); }
        }

        // Direct scan for .craft files in official save and ship folders.
        private void ScanDirectoryExact(string folderPath, string categoryName)
        {
            if (!Directory.Exists(folderPath)) return;
            string[] files = Directory.GetFiles(folderPath, "*.craft", SearchOption.AllDirectories);

            foreach (string file in files) { AddCraftToCategory(file, categoryName); }
        }

        // Deep-scans GameData for modded ships.
        private void ScanGameDataForShips(string gameDataPath)
        {
            if (!Directory.Exists(gameDataPath)) return;
            string[] files = Directory.GetFiles(gameDataPath, "*.craft", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normPath = file.Replace('\\', '/');
                if (normPath.IndexOf("/Missions/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normPath.IndexOf("/Tutorials/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normPath.IndexOf("/Scenarios/", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                if (normPath.IndexOf("/Ships/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool isExpansion = normPath.IndexOf("/SquadExpansion/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       normPath.IndexOf("/Squad/", StringComparison.OrdinalIgnoreCase) >= 0;
                    string vesselType = GetVesselType(file);

                    string category = "";
                    if (vesselType == "SPH") category = isExpansion ? "SPH (Stock)" : "SPH (Modded)";
                    else category = isExpansion ? "VAB (Stock)" : "VAB (Modded)";

                    AddCraftToCategory(file, category);
                }
            }
        }

        // Reads the first few lines of a .craft file to check if it's a VAB or SPH vessel.
        private string GetVesselType(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("type =")) return line.Split('=')[1].Trim();
                    }
                }
            }
            catch { }
            return "VAB";
        }

        // Organizes crafts into both physical folder categories and custom metadata family groups.
        private void AddCraftToCategory(string file, string categoryName)
        {
            string craftName = Path.GetFileNameWithoutExtension(file);

            if (!craftByFolder.ContainsKey(categoryName))
            {
                craftByFolder[categoryName] = new List<string>();
                if (!folderExpanded.ContainsKey(categoryName)) folderExpanded[categoryName] = true;
            }
            if (!craftByFolder[categoryName].Contains(craftName)) craftByFolder[categoryName].Add(craftName);

            string family = "Uncategorized";
            ConfigNode savedData = AdvancedVesselInfoManager.Instance.GetVesselNode(craftName);
            if (savedData != null && savedData.HasValue("familyTag"))
            {
                string tag = savedData.GetValue("familyTag");
                if (!string.IsNullOrEmpty(tag)) family = tag;
            }

            if (!craftByFamily.ContainsKey(family))
            {
                craftByFamily[family] = new List<string>();
                if (!folderExpanded.ContainsKey(family)) folderExpanded[family] = true;
            }
            if (!craftByFamily[family].Contains(craftName)) craftByFamily[family].Add(craftName);

            if (!craftFilePaths.ContainsKey(craftName)) craftFilePaths[craftName] = file;
        }

        // Main rendering loop for all active plugin windows.
        private void OnGUI()
        {
            if (!showGui) return;

            Vector2 oldMainPos = new Vector2(windowRect.x, windowRect.y);

            // Replaced GUILayout.Window with ClickThruBlocker.GUILayoutWindow
            windowRect = ClickThruBlocker.GUILayoutWindow(8842, windowRect, DrawWindowContent, "Advanced Vessel Info");

            if (windowRect.x != oldMainPos.x || windowRect.y != oldMainPos.y)
            {
                settingsRect.x += (windowRect.x - oldMainPos.x);
                settingsRect.y += (windowRect.y - oldMainPos.y);
            }

            if (showCraftList)
            {
                listRect.x = windowRect.x - libraryWidth;
                listRect.y = windowRect.y;
                listRect.width = libraryWidth;
                listRect.height = windowRect.height;
                // Replaced GUILayout.Window with ClickThruBlocker.GUILayoutWindow
                listRect = ClickThruBlocker.GUILayoutWindow(8845, listRect, DrawCraftList, "Craft Library");
            }
            if (showHelp)
            {
                helpRect.x = windowRect.x + windowRect.width; helpRect.y = windowRect.y; helpRect.height = windowRect.height;
                // Replaced GUILayout.Window with ClickThruBlocker.GUILayoutWindow
                helpRect = ClickThruBlocker.GUILayoutWindow(8843, helpRect, DrawHelpContent, "System Help");
            }

            // Replaced GUILayout.Window with ClickThruBlocker.GUILayoutWindow
            if (showSettings) settingsRect = ClickThruBlocker.GUILayoutWindow(8846, settingsRect, DrawSettingsWindow, "UI Settings");

            HandleResize();
        }

        // Renders the main data display containing vessel specs, descriptions, logs, and payloads.
        private void DrawWindowContent(int windowID)
        {
            var mgr = AdvancedVesselInfoManager.Instance;
            if (GUI.Button(new Rect(5, 2, 85, 18), showCraftList ? "<< Close" : "Library >>")) showCraftList = !showCraftList;
            if (GUI.Button(new Rect(windowRect.width - 90, 2, 85, 18), showHelp ? "<< Close" : "Info >>")) showHelp = !showHelp;

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("<b>" + currentCraftName + "</b>", BoldStyle(14));
            GUILayout.Label("<color=silver>Parts: " + currentPartCount + " | Mass: " + currentMass.ToString("F2", CultureInfo.InvariantCulture) + " t | Cost: " + currentCost.ToString("N0", CultureInfo.InvariantCulture) + " √</color>", new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true });

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Family Tag:", BoldStyle(12), GUILayout.Width(80));
            if (familyEditMode)
            {
                string dispTagBtn = string.IsNullOrEmpty(currentFamilyTag) ? "Uncategorized" : currentFamilyTag;
                if (GUILayout.Button(dispTagBtn + " ▼", GUI.skin.button, GUILayout.ExpandWidth(true))) showFamilyDropdown = !showFamilyDropdown;
            }
            else
            {
                string dispTag = string.IsNullOrEmpty(currentFamilyTag) ? "Uncategorized" : currentFamilyTag;
                GUILayout.Label("<color=silver>" + dispTag + "</color>", new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true });
            }
            if (GUILayout.Button(familyEditMode ? "Done" : "Edit", GUILayout.Width(50)))
            {
                familyEditMode = !familyEditMode;
                if (!familyEditMode) showFamilyDropdown = false;
            }
            GUILayout.EndHorizontal();

            if (familyEditMode && showFamilyDropdown)
            {
                GUILayout.BeginVertical("box");
                familyScrollPos = GUILayout.BeginScrollView(familyScrollPos, GUILayout.Height(100));
                if (GUILayout.Button("Uncategorized", craftButtonStyle)) { currentFamilyTag = "Uncategorized"; showFamilyDropdown = false; }
                foreach (string fam in mgr.customFamilies)
                    if (GUILayout.Button(fam, craftButtonStyle)) { currentFamilyTag = fam; showFamilyDropdown = false; }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("General Description:", BoldStyle(12));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(descriptionEditMode ? "Done" : "Edit", GUILayout.Width(50))) descriptionEditMode = !descriptionEditMode;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            descriptionScrollPos = GUILayout.BeginScrollView(descriptionScrollPos, GUILayout.Height(descHeight));
            GUIStyle descStyle = new GUIStyle(GUI.skin.label) { fontSize = mgr.descFontSize, fontStyle = mgr.descFontBold ? FontStyle.Bold : FontStyle.Normal, wordWrap = true };
            if (descriptionEditMode) customDescription = GUILayout.TextArea(customDescription, GUILayout.ExpandHeight(true));
            else GUILayout.Label(customDescription, descStyle);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            descHeight = DrawResizeHandle(descHeight, ref isResizingDesc);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mission Purpose:", BoldStyle(12));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(missionEditMode ? "Done" : "Edit", GUILayout.Width(50))) missionEditMode = !missionEditMode;
            GUILayout.EndHorizontal();

            if (missionEditMode)
            {
                mgr.currentMissionPurpose = GUILayout.TextField(mgr.currentMissionPurpose);
            }
            else
            {
                GUILayout.Label("<color=silver>" + mgr.currentMissionPurpose + "</color>", new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true });
            }

            // --- LINK LOG UI ---
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Link Log To:", BoldStyle(12), GUILayout.Width(80));
            if (linkEditMode)
            {
                string dispLinkBtn = string.IsNullOrEmpty(currentLinkedCraft) ? "None" : currentLinkedCraft;
                if (GUILayout.Button(dispLinkBtn + " ▼", GUI.skin.button, GUILayout.ExpandWidth(true))) showLinkDropdown = !showLinkDropdown;
            }
            else
            {
                string dispLink = string.IsNullOrEmpty(currentLinkedCraft) ? "None" : currentLinkedCraft;
                GUILayout.Label("<color=silver>" + dispLink + "</color>", new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true });
            }
            if (GUILayout.Button(linkEditMode ? "Done" : "Edit", GUILayout.Width(50)))
            {
                linkEditMode = !linkEditMode;
                if (!linkEditMode) showLinkDropdown = false;
            }
            GUILayout.EndHorizontal();

            if (linkEditMode && showLinkDropdown)
            {
                GUILayout.BeginVertical("box");
                linkScrollPos = GUILayout.BeginScrollView(linkScrollPos, GUILayout.Height(100));
                if (GUILayout.Button("None", craftButtonStyle)) { currentLinkedCraft = "None"; showLinkDropdown = false; }

                List<string> allCrafts = new List<string>(craftFilePaths.Keys);
                allCrafts.Sort();
                foreach (string c in allCrafts)
                {
                    if (c != currentCraftName) // Ensure a craft cannot link to itself
                    {
                        if (GUILayout.Button(c, craftButtonStyle)) { currentLinkedCraft = c; showLinkDropdown = false; }
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Flight Log", BoldStyle(12));
            GUILayout.FlexibleSpace();

            // --- FLIGHT COUNTER & STATS ---
            string lastLaunch = launchHistory.Count > 0 ? launchHistory[launchHistory.Count - 1].date : "Never";
            int successCount = launchHistory.FindAll(l => l.status == 0).Count;
            float reliability = launchHistory.Count > 0 ? ((float)successCount / launchHistory.Count) * 100f : 0f;
            GUILayout.Label("<color=silver>Flights: " + launchHistory.Count + "  |  Last Flight: " + lastLaunch + "  |  Success Rate: " + reliability.ToString("F0", CultureInfo.InvariantCulture) + "%</color>", BoldStyle(12));

            GUILayout.Space(10);
            if (GUILayout.Button(editMode ? "Done" : "Edit", GUILayout.Width(50))) editMode = !editMode;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            historyScrollPos = GUILayout.BeginScrollView(historyScrollPos, GUILayout.Height(logHeight));
            GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = mgr.logFontSize, fontStyle = mgr.logFontBold ? FontStyle.Bold : FontStyle.Normal, richText = true };
            for (int i = launchHistory.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                if (editMode)
                {
                    if (GUILayout.Button(launchHistory[i].status == 0 ? "S" : "S", GUILayout.Width(25))) launchHistory[i].status = 0;
                    if (GUILayout.Button(launchHistory[i].status == 1 ? "P" : "P", GUILayout.Width(25))) launchHistory[i].status = 1;
                    if (GUILayout.Button(launchHistory[i].status == 2 ? "F" : "F", GUILayout.Width(25))) launchHistory[i].status = 2;
                    launchHistory[i].purpose = GUILayout.TextField(launchHistory[i].purpose);
                    if (GUILayout.Button("X", GUILayout.Width(22))) { launchHistory.RemoveAt(i); break; }
                }
                else
                {
                    string statusColor = launchHistory[i].status == 0 ? "#BADA55" : (launchHistory[i].status == 1 ? "yellow" : "red");
                    string statusText = launchHistory[i].status == 0 ? "[Success]" : (launchHistory[i].status == 1 ? "[Partial]" : "[Failure]");
                    GUILayout.Label("<color=" + statusColor + ">" + statusText + "</color> <color=silver>" + launchHistory[i].date + ":</color> " + launchHistory[i].purpose, logStyle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            logHeight = DrawResizeHandle(logHeight, ref isResizingLog);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Payload Capabilities:", BoldStyle(12));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(payloadEditMode ? "Done" : "Edit", GUILayout.Width(50))) payloadEditMode = !payloadEditMode;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            payloadScrollPos = GUILayout.BeginScrollView(payloadScrollPos, GUILayout.Height(payHeight));
            GUIStyle payStyle = new GUIStyle(GUI.skin.label) { fontSize = mgr.payFontSize, fontStyle = mgr.payFontBold ? FontStyle.Bold : FontStyle.Normal, richText = true };
            for (int i = 0; i < payloadEntries.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (payloadEditMode)
                {
                    payloadEntries[i].destination = GUILayout.TextField(payloadEntries[i].destination, GUILayout.Width(windowRect.width * 0.3f));
                    GUILayout.Label(" t:", GUILayout.Width(15));
                    payloadEntries[i].amount = GUILayout.TextField(payloadEntries[i].amount, GUILayout.Width(40));
                    if (GUILayout.Button("X", GUILayout.Width(22))) { payloadEntries.RemoveAt(i); break; }
                }
                else
                {
                    float amt = 0; string cstr = "-";
                    if (float.TryParse(payloadEntries[i].amount, NumberStyles.Any, CultureInfo.InvariantCulture, out amt) && amt > 0 && currentCost > 0)
                        cstr = (currentCost / amt).ToString("N0", CultureInfo.InvariantCulture) + " √/t";
                    GUILayout.Label("<color=silver>" + payloadEntries[i].destination + ":</color> " + payloadEntries[i].amount + " t (" + cstr + ")", payStyle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (payloadEditMode && GUILayout.Button("+ Add Capability")) payloadEntries.Add(new PayloadEntry());
            GUILayout.EndVertical();
            payHeight = DrawResizeHandle(payHeight, ref isResizingPay);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Data", GUILayout.Height(30)))
            {
                if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && EditorLogic.fetch.shipNameField != null && currentCraftName == EditorLogic.fetch.shipNameField.text) UpdateAutoSpecs();
                mgr.SaveCraftData(currentCraftName, customDescription, mgr.currentMissionPurpose, currentFamilyTag, currentLinkedCraft, currentPartCount, currentMass, currentCost, payloadEntries, launchHistory);
                LoadAvailableCrafts();
            }

            if (GUILayout.Button("UI Settings", GUILayout.Height(25))) showSettings = !showSettings;

            // Replaced toolbarButton.SetFalse() with toolbarControl.SetFalse()
            if (GUILayout.Button("Close"))
            {
                showGui = false;
                if (toolbarControl != null) toolbarControl.SetFalse();
                showSettings = false;
            }
            GUI.Label(new Rect(windowRect.width - 15, windowRect.height - 15, 15, 15), "◢");
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
        }

        // Renders the separate settings menu for customizing font sizes and weights.
        private void DrawSettingsWindow(int windowID)
        {
            var mgr = AdvancedVesselInfoManager.Instance;
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Description Text Appearance</b>", BoldStyle(12));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(35))) mgr.descFontSize = Mathf.Max(8, mgr.descFontSize - 1);
            GUILayout.Label("Size: " + mgr.descFontSize, GUILayout.Width(65));
            if (GUILayout.Button("+", GUILayout.Width(35))) mgr.descFontSize = Mathf.Min(24, mgr.descFontSize + 1);
            GUILayout.FlexibleSpace();
            mgr.descFontBold = GUILayout.Toggle(mgr.descFontBold, "Bold Text");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("<b>Flight Log Text Appearance</b>", BoldStyle(12));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(35))) mgr.logFontSize = Mathf.Max(8, mgr.logFontSize - 1);
            GUILayout.Label("Size: " + mgr.logFontSize, GUILayout.Width(65));
            if (GUILayout.Button("+", GUILayout.Width(35))) mgr.logFontSize = Mathf.Min(24, mgr.logFontSize + 1);
            GUILayout.FlexibleSpace();
            mgr.logFontBold = GUILayout.Toggle(mgr.logFontBold, "Bold Text");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("<b>Payload List Text Appearance</b>", BoldStyle(12));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(35))) mgr.payFontSize = Mathf.Max(8, mgr.payFontSize - 1);
            GUILayout.Label("Size: " + mgr.payFontSize, GUILayout.Width(65));
            if (GUILayout.Button("+", GUILayout.Width(35))) mgr.payFontSize = Mathf.Min(24, mgr.payFontSize + 1);
            GUILayout.FlexibleSpace();
            mgr.payFontBold = GUILayout.Toggle(mgr.payFontBold, "Bold Text");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close Settings", GUILayout.Height(30))) showSettings = false;
            GUI.DragWindow(new Rect(0, 0, settingsRect.width, 20));
        }

        // Renders the side library window for searching and selecting vessels.
        private void DrawCraftList(int windowID)
        {
            InitializeStyles();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!sortByFamily, "Folders", GUI.skin.button)) sortByFamily = false;
            if (GUILayout.Toggle(sortByFamily, "Families", GUI.skin.button)) sortByFamily = true;
            GUILayout.EndHorizontal();

            if (sortByFamily)
            {
                GUILayout.BeginHorizontal();
                newFamilyInput = GUILayout.TextField(newFamilyInput);
                if (GUILayout.Button("+ Add", GUILayout.Width(50)))
                {
                    string cleanFam = newFamilyInput.Trim();
                    if (!string.IsNullOrEmpty(cleanFam) && cleanFam != "Uncategorized" && !AdvancedVesselInfoManager.Instance.customFamilies.Contains(cleanFam))
                    {
                        AdvancedVesselInfoManager.Instance.customFamilies.Add(cleanFam);
                        AdvancedVesselInfoManager.Instance.SaveSettings(windowRect, settingsRect, libraryWidth, showCraftList, showHelp);
                        LoadAvailableCrafts();
                    }
                    newFamilyInput = "";
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            searchTerm = GUILayout.TextField(searchTerm);
            GUILayout.Label("Search", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            libraryScrollPos = GUILayout.BeginScrollView(libraryScrollPos);
            var activeDictionary = sortByFamily ? craftByFamily : craftByFolder;
            foreach (var group in activeDictionary)
            {
                GUILayout.BeginHorizontal(folderHeaderStyle);
                folderExpanded[group.Key] = GUILayout.Toggle(folderExpanded[group.Key], folderExpanded[group.Key] ? "▼" : "▶", GUI.skin.label, GUILayout.Width(20));
                if (GUILayout.Button(group.Key, GUI.skin.label)) folderExpanded[group.Key] = !folderExpanded[group.Key];

                if (sortByFamily && group.Key != "Uncategorized")
                {
                    if (GUILayout.Button("Delete", GUILayout.Width(50)))
                    {
                        AdvancedVesselInfoManager.Instance.DeleteFamily(group.Key);
                        LoadAvailableCrafts();
                        break;
                    }
                }
                GUILayout.EndHorizontal();

                if (folderExpanded[group.Key])
                {
                    bool breakOuter = false;
                    foreach (string craft in group.Value)
                    {
                        if (!string.IsNullOrEmpty(searchTerm) && !craft.ToLower().Contains(searchTerm.ToLower())) continue;

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(craft, craftButtonStyle, GUILayout.ExpandWidth(true))) { currentCraftName = craft; LoadCraftData(craft); }

                        // Adds the "X" button to quickly remove a craft from a custom family
                        if (sortByFamily && group.Key != "Uncategorized")
                        {
                            if (GUILayout.Button("X", GUILayout.Width(22)))
                            {
                                AdvancedVesselInfoManager.Instance.RemoveCraftFromFamily(craft);
                                LoadAvailableCrafts();
                                breakOuter = true;
                            }
                        }
                        GUILayout.EndHorizontal();
                        if (breakOuter) break;
                    }
                    // Safely exit the loop to reload the interface smoothly
                    if (breakOuter) break;
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Refresh List")) LoadAvailableCrafts();
            GUILayout.EndVertical();
        }

        // Renders the interactive documentation panel with usage guides.
        private void DrawHelpContent(int windowID)
        {
            GUILayout.BeginVertical();
            helpScrollPos = GUILayout.BeginScrollView(helpScrollPos);
            GUILayout.Label("<b>User Guide & Documentation</b>", BoldStyle(16));
            GUILayout.Space(10);

            GUILayout.Label("<b>1. General Description</b>", BoldStyle(14));
            GUILayout.Label("Store notes about your vessel. It automatically loads vanilla descriptions or your custom metadata.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>2. Family Tag</b>", BoldStyle(14));
            GUILayout.Label("Organize your fleet into custom groups. Assign vessels via the dropdown menu.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>3. Mission Purpose</b>", BoldStyle(14));
            GUILayout.Label("Define the goal for your mission. This is recorded in the flight log upon launch.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>4. Flight Log & Link Log</b>", BoldStyle(14));
            GUILayout.Label("Tracks launches automatically. You can 'Link' a log to another craft to keep your rocket launches bundled even when using payloads.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>5. Payload Capabilities</b>", BoldStyle(14));
            GUILayout.Label("Record payload targets. Cost per ton is calculated automatically.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>6. UI Settings Menu</b>", BoldStyle(14));
            GUILayout.Label("Customize text sizes and styles for better readability.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>7. Link Log To</b>", BoldStyle(14));
            GUILayout.Label("Redirect the flight log of this craft to another base craft. Useful for payload variations.", HelpTextStyle());

            GUILayout.Space(15);
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b><color=orange>IMPORTANT:</color></b>", BoldStyle(12));
            GUILayout.Label("Changes are only permanent after clicking <color=white><b>'Save Data'</b></color>!", HelpTextStyle());
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.Space(5);
            GUILayout.Label("<color=silver><size=10>Advanced Vessel Info v1.7.0\nStatus: Systems Operational</size></color>", new GUIStyle(LogStyle()) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private GUIStyle HelpTextStyle() => new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13, richText = true, padding = new RectOffset(5, 5, 2, 2) };

        // Loads all custom saved data for a selected vessel, with automatic fallbacks for missing data.
        private void LoadCraftData(string shipName)
        {
            ConfigNode vesselNode = AdvancedVesselInfoManager.Instance.GetVesselNode(shipName);
            payloadEntries.Clear(); launchHistory.Clear();
            editMode = false; payloadEditMode = false; descriptionEditMode = false;
            familyEditMode = false; showFamilyDropdown = false;
            linkEditMode = false; showLinkDropdown = false; currentLinkedCraft = "None";
            missionEditMode = false;
            currentPartCount = 0; currentMass = 0f; currentCost = 0f; currentFamilyTag = "";

            if (vesselNode != null)
            {
                string savedDesc = vesselNode.GetValue("description");
                if (!string.IsNullOrEmpty(savedDesc)) customDescription = savedDesc.Replace("\\n", "\n");
                else customDescription = GetVanillaDescription(shipName);

                currentFamilyTag = vesselNode.GetValue("familyTag") ?? "";
                currentLinkedCraft = vesselNode.GetValue("linkedLogCraft") ?? "None";
                if (string.IsNullOrEmpty(currentLinkedCraft)) currentLinkedCraft = "None";

                AdvancedVesselInfoManager.Instance.currentMissionPurpose = vesselNode.GetValue("missionPurpose") ?? "Enter Mission";

                int.TryParse(vesselNode.GetValue("partCount"), out currentPartCount);
                float.TryParse(vesselNode.GetValue("mass"), NumberStyles.Any, CultureInfo.InvariantCulture, out currentMass);
                if (vesselNode.HasValue("cost")) float.TryParse(vesselNode.GetValue("cost"), NumberStyles.Any, CultureInfo.InvariantCulture, out currentCost);

                ConfigNode historyNode = vesselNode.GetNode("HISTORY");
                if (historyNode != null)
                {
                    foreach (ConfigNode n in historyNode.GetNodes("LAUNCH"))
                    {
                        int stat = 0; int.TryParse(n.GetValue("status"), out stat);
                        launchHistory.Add(new LaunchEntry { date = n.GetValue("date"), purpose = n.GetValue("purpose"), status = stat });
                    }
                }

                ConfigNode payloadNode = vesselNode.GetNode("PAYLOADS");
                if (payloadNode != null)
                    foreach (ConfigNode n in payloadNode.GetNodes("ENTRY"))
                        payloadEntries.Add(new PayloadEntry { destination = n.GetValue("dest"), amount = n.GetValue("tons") });
            }
            else { customDescription = GetVanillaDescription(shipName); AdvancedVesselInfoManager.Instance.currentMissionPurpose = "Enter Mission"; }

            if (currentPartCount == 0 && currentMass == 0f && currentCost == 0f)
            {
                if (craftFilePaths.ContainsKey(shipName))
                {
                    string metaPath = Path.ChangeExtension(craftFilePaths[shipName], ".loadmeta");
                    if (File.Exists(metaPath))
                    {
                        ConfigNode metaNode = ConfigNode.Load(metaPath);
                        if (metaNode != null)
                        {
                            if (metaNode.HasValue("partCount")) int.TryParse(metaNode.GetValue("partCount"), out currentPartCount);
                            if (metaNode.HasValue("totalMass")) float.TryParse(metaNode.GetValue("totalMass"), NumberStyles.Any, CultureInfo.InvariantCulture, out currentMass);
                            if (metaNode.HasValue("totalCost")) float.TryParse(metaNode.GetValue("totalCost"), NumberStyles.Any, CultureInfo.InvariantCulture, out currentCost);
                        }
                    }
                }
            }
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && EditorLogic.fetch.shipNameField != null && currentCraftName == EditorLogic.fetch.shipNameField.text) UpdateAutoSpecs();
        }

        // Reads the original description directly from the .craft file if no custom metadata exists.
        private string GetVanillaDescription(string shipName)
        {
            if (craftFilePaths.ContainsKey(shipName))
            {
                ConfigNode craftNode = ConfigNode.Load(craftFilePaths[shipName]);
                if (craftNode != null)
                {
                    string rawDesc = craftNode.GetValue("description") ?? "";
                    return rawDesc.StartsWith("#") ? Localizer.Format(rawDesc).Replace("\\n", "\n") : rawDesc.Replace("\\n", "\n");
                }
            }
            return "";
        }

        // Logic for handling the drag-to-resize behavior of the main UI and library panels.
        private void HandleResize()
        {
            Vector2 mousePos = Event.current.mousePosition;
            if (Event.current.type == EventType.MouseDown && new Rect(windowRect.x + windowRect.width - 20, windowRect.y + windowRect.height - 20, 20, 20).Contains(mousePos))
            { isResizing = true; resizeStart = new Vector2(mousePos.x - windowRect.width, mousePos.y - windowRect.height); Event.current.Use(); }
            if (isResizing)
            {
                windowRect.width = Mathf.Max(minSize.width, mousePos.x - resizeStart.x);
                windowRect.height = Mathf.Max(minSize.height, mousePos.y - resizeStart.y);
                if (Event.current.type == EventType.MouseUp) isResizing = false;
            }
            if (showCraftList)
            {
                if (Event.current.type == EventType.MouseDown && new Rect(listRect.x, listRect.y, 15, listRect.height).Contains(mousePos))
                { isResizingLibrary = true; libraryResizeStartX = mousePos.x; libraryResizeStartWidth = libraryWidth; Event.current.Use(); }
                if (isResizingLibrary)
                { libraryWidth = Mathf.Clamp(libraryResizeStartWidth + (libraryResizeStartX - mousePos.x), 150f, 800f); if (Event.current.type == EventType.MouseUp) isResizingLibrary = false; }
            }
        }

        // Properly cleans up the ToolbarController button upon destruction.
        void OnDestroy()
        {
            if (AdvancedVesselInfoManager.Instance != null)
            {
                AdvancedVesselInfoManager.Instance.savedDescHeight = descHeight;
                AdvancedVesselInfoManager.Instance.savedLogHeight = logHeight;
                AdvancedVesselInfoManager.Instance.savedPayHeight = payHeight;
                AdvancedVesselInfoManager.Instance.SaveSettings(windowRect, settingsRect, libraryWidth, showCraftList, showHelp);
            }

            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarControl = null;
            }
        }

        private GUIStyle BoldStyle(int size) => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = size, richText = true };
        private GUIStyle LogStyle() => new GUIStyle(GUI.skin.label) { fontSize = 11, richText = true };
    }
}
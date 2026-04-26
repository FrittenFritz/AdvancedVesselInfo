using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System;
using KSP.Localization;
using System.Globalization;
using ClickThroughFix;
using ToolbarControl_NS;

namespace AdvancedVesselInfo
{
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

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                storagePath = Path.Combine(KSPUtil.ApplicationRootPath, "PluginData/AdvancedVesselInfo/CraftData.cfg");
                settingsPath = Path.Combine(KSPUtil.ApplicationRootPath, "PluginData/AdvancedVesselInfo/Settings.cfg");

                string directoryPath = Path.GetDirectoryName(storagePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                LoadSettings();
                LoadFamilies();
                GameEvents.onLevelWasLoaded.Add(OnLevelLoaded);

                ToolbarControl.RegisterMod(AdvancedVesselInfoUI.MODID, AdvancedVesselInfoUI.MODNAME);
            }
            else { Destroy(gameObject); }
        }

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
                }
            }
        }

        private void LoadFamilies()
        {
            customFamilies.Clear();
            bool needsMigration = false;

            if (File.Exists(storagePath))
            {
                ConfigNode root = ConfigNode.Load(storagePath);
                if (root != null && root.HasNode("FAMILIES"))
                {
                    ConfigNode famNode = root.GetNode("FAMILIES");
                    foreach (ConfigNode.Value v in famNode.values)
                    {
                        if (v.name == "name" && !customFamilies.Contains(v.value))
                            customFamilies.Add(v.value);
                    }
                    return;
                }
            }

            if (File.Exists(settingsPath))
            {
                ConfigNode settingsNode = ConfigNode.Load(settingsPath);
                if (settingsNode != null && settingsNode.HasNode("FAMILIES"))
                {
                    ConfigNode famNode = settingsNode.GetNode("FAMILIES");
                    foreach (ConfigNode.Value v in famNode.values)
                    {
                        if (v.name == "name" && !customFamilies.Contains(v.value))
                            customFamilies.Add(v.value);
                    }

                    settingsNode.RemoveNode("FAMILIES");
                    settingsNode.Save(settingsPath);
                    needsMigration = true;
                }
            }

            if (needsMigration)
            {
                SaveFamilies();
            }
        }

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

            node.Save(settingsPath);
        }

        public void SaveFamilies()
        {
            ConfigNode root = File.Exists(storagePath) ? ConfigNode.Load(storagePath) : new ConfigNode();

            if (root.HasNode("FAMILIES")) root.RemoveNode("FAMILIES");

            if (customFamilies.Count > 0)
            {
                ConfigNode famNode = root.AddNode("FAMILIES");
                foreach (string f in customFamilies)
                    famNode.AddValue("name", f);
            }

            root.Save(storagePath);
        }

        public void DeleteFamily(string familyName)
        {
            bool changed = false;

            if (customFamilies.Contains(familyName))
            {
                customFamilies.Remove(familyName);
                changed = true;
            }

            if (File.Exists(storagePath))
            {
                ConfigNode root = ConfigNode.Load(storagePath);
                if (root != null)
                {
                    foreach (ConfigNode node in root.GetNodes())
                    {
                        if (node.name != "FAMILIES" && node.HasValue("familyTag") && node.GetValue("familyTag") == familyName)
                        {
                            node.SetValue("familyTag", "Uncategorized", true);
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        if (root.HasNode("FAMILIES")) root.RemoveNode("FAMILIES");
                        if (customFamilies.Count > 0)
                        {
                            ConfigNode famNode = root.AddNode("FAMILIES");
                            foreach (string f in customFamilies) famNode.AddValue("name", f);
                        }

                        root.Save(storagePath);
                    }
                }
            }
        }

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

        private void LogLaunch(string shipName)
        {
            string date = KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true);
            ConfigNode root = ConfigNode.Load(storagePath) ?? new ConfigNode();
            string nodeName = shipName.Replace(" ", "_");

            ConfigNode sourceNode = root.GetNode(nodeName);
            List<string> targetNodes = new List<string>();

            if (sourceNode != null && sourceNode.HasValue("linkedLogCraft"))
            {
                string[] links = sourceNode.GetValues("linkedLogCraft");
                foreach (string link in links)
                {
                    if (!string.IsNullOrEmpty(link) && link != "None")
                    {
                        targetNodes.Add(link.Replace(" ", "_"));
                    }
                }
            }

            if (targetNodes.Count == 0)
            {
                targetNodes.Add(nodeName);
            }

            foreach (string target in targetNodes)
            {
                ConfigNode vesselNode = root.GetNode(target) ?? root.AddNode(target);
                ConfigNode historyNode = vesselNode.GetNode("HISTORY") ?? vesselNode.AddNode("HISTORY");
                ConfigNode launchEntry = historyNode.AddNode("LAUNCH");
                launchEntry.AddValue("date", date);
                launchEntry.AddValue("purpose", currentMissionPurpose);
                launchEntry.AddValue("status", "0");
            }
            root.Save(storagePath);
        }

        public void SaveCraftData(string shipName, string description, string purpose, string family, List<string> linkedCrafts, int parts, float mass, float cost, List<AdvancedVesselInfoUI.PayloadEntry> payloads, List<AdvancedVesselInfoUI.LaunchEntry> history = null, List<AdvancedVesselInfoUI.MilestoneEntry> milestones = null)
        {
            ConfigNode root = ConfigNode.Load(storagePath) ?? new ConfigNode();
            string nodeName = shipName.Replace(" ", "_");
            ConfigNode vesselNode = root.GetNode(nodeName) ?? root.AddNode(nodeName);

            string safeDesc = string.IsNullOrEmpty(description) ? "" : description.Replace("\n", "\\n");
            vesselNode.SetValue("description", safeDesc, true);

            vesselNode.SetValue("missionPurpose", purpose, true);
            vesselNode.SetValue("familyTag", family, true);

            vesselNode.RemoveValues("linkedLogCraft");
            if (linkedCrafts != null && linkedCrafts.Count > 0)
            {
                foreach (string link in linkedCrafts) vesselNode.AddValue("linkedLogCraft", link);
            }
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

            if (milestones != null)
            {
                if (vesselNode.HasNode("MILESTONES")) vesselNode.RemoveNode("MILESTONES");
                ConfigNode milNode = vesselNode.AddNode("MILESTONES");
                foreach (var mil in milestones)
                {
                    ConfigNode entry = milNode.AddNode("ENTRY");
                    entry.AddValue("date", mil.date);
                    entry.AddValue("text", mil.text);
                }
            }

            root.Save(storagePath);
        }

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

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class AdvancedVesselInfoUI : MonoBehaviour
    {
        public class PayloadEntry { public string destination = "Target"; public string amount = "0.0"; }
        public class LaunchEntry { public string date = ""; public string purpose = ""; public int status = 0; }
        public class MilestoneEntry { public string date = ""; public string text = ""; }

        internal const string MODID = "AdvancedVesselInfo";
        internal const string MODNAME = "Advanced Vessel Info";

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

        private int logTabIndex = 0;
        private string newMilestoneInput = "";

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
        private List<string> currentLinkedCrafts = new List<string>();
        private int currentPartCount = 0;
        private float currentMass = 0f;
        private float currentCost = 0f;

        private List<PayloadEntry> payloadEntries = new List<PayloadEntry>();
        private List<LaunchEntry> launchHistory = new List<LaunchEntry>();
        private List<MilestoneEntry> milestones = new List<MilestoneEntry>();

        private Vector2 libraryScrollPos;
        private Vector2 historyScrollPos;
        private Vector2 milestoneScrollPos;
        private Vector2 payloadScrollPos;
        private Vector2 helpScrollPos;
        private Vector2 descriptionScrollPos;
        private Vector2 familyScrollPos;
        private Vector2 linkScrollPos;

        private GUIStyle folderHeaderStyle;
        private GUIStyle craftButtonStyle;
        private bool stylesInitialized = false;

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

            if (toolbarControl == null) AddToolbarButton();

            if (HighLogic.LoadedSceneIsEditor) GameEvents.onEditorShipModified.Add((ship) => SyncWithCurrentEditor());
        }

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

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

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

        private void ScanDirectoryExact(string folderPath, string categoryName)
        {
            if (!Directory.Exists(folderPath)) return;
            string[] files = Directory.GetFiles(folderPath, "*.craft", SearchOption.AllDirectories);

            foreach (string file in files) { AddCraftToCategory(file, categoryName); }
        }

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

        private void OnGUI()
        {
            if (!showGui) return;

            Vector2 oldMainPos = new Vector2(windowRect.x, windowRect.y);

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
                listRect = ClickThruBlocker.GUILayoutWindow(8845, listRect, DrawCraftList, "Craft Library");
            }
            if (showHelp)
            {
                helpRect.x = windowRect.x + windowRect.width; helpRect.y = windowRect.y; helpRect.height = windowRect.height;
                helpRect = ClickThruBlocker.GUILayoutWindow(8843, helpRect, DrawHelpContent, "System Help");
            }

            if (showSettings) settingsRect = ClickThruBlocker.GUILayoutWindow(8846, settingsRect, DrawSettingsWindow, "UI Settings");

            HandleResize();
        }

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

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Link Log To:", BoldStyle(12), GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(linkEditMode ? "Done" : "Edit", GUILayout.Width(50)))
            {
                linkEditMode = !linkEditMode;
                if (!linkEditMode) showLinkDropdown = false;
            }
            GUILayout.EndHorizontal();

            if (!linkEditMode)
            {
                string dispLink = currentLinkedCrafts.Count > 0 ? string.Join(", ", currentLinkedCrafts) : "None";
                GUILayout.Label("<color=silver>" + dispLink + "</color>", new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true });
            }
            else
            {
                GUILayout.BeginVertical("box");
                for (int i = 0; i < currentLinkedCrafts.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("• " + currentLinkedCrafts[i], new GUIStyle(GUI.skin.label) { fontSize = 12 });
                    if (GUILayout.Button("X", GUILayout.Width(22))) { currentLinkedCrafts.RemoveAt(i); break; }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("+ Add Craft", GUI.skin.button, GUILayout.ExpandWidth(true))) showLinkDropdown = !showLinkDropdown;

                if (showLinkDropdown)
                {
                    GUILayout.BeginVertical("box");
                    linkScrollPos = GUILayout.BeginScrollView(linkScrollPos, GUILayout.Height(100));
                    List<string> allCrafts = new List<string>(craftFilePaths.Keys);
                    allCrafts.Sort();
                    foreach (string c in allCrafts)
                    {
                        if (c != currentCraftName && !currentLinkedCrafts.Contains(c))
                        {
                            if (GUILayout.Button(c, craftButtonStyle)) { currentLinkedCrafts.Add(c); showLinkDropdown = false; }
                        }
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(logTabIndex == 0, "Flight Log", GUI.skin.button, GUILayout.Width(100))) logTabIndex = 0;
            if (GUILayout.Toggle(logTabIndex == 1, "Milestones", GUI.skin.button, GUILayout.Width(100))) logTabIndex = 1;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(editMode ? "Done" : "Edit", GUILayout.Width(50))) editMode = !editMode;
            GUILayout.EndHorizontal();

            if (logTabIndex == 0)
            {
                GUILayout.BeginHorizontal();
                string lastLaunch = launchHistory.Count > 0 ? launchHistory[launchHistory.Count - 1].date : "Never";
                int successCount = launchHistory.FindAll(l => l.status == 0).Count;
                float reliability = launchHistory.Count > 0 ? ((float)successCount / launchHistory.Count) * 100f : 0f;
                GUILayout.Label("<color=silver>Flights: " + launchHistory.Count + "  |  Last Flight: " + lastLaunch + "  |  Success Rate: " + reliability.ToString("F0", CultureInfo.InvariantCulture) + "%</color>", BoldStyle(12));
                GUILayout.EndHorizontal();
            }
            else if (logTabIndex == 1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=silver>Total Milestones: " + milestones.Count + "</color>", BoldStyle(12));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical("box");
            GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = mgr.logFontSize, fontStyle = mgr.logFontBold ? FontStyle.Bold : FontStyle.Normal, richText = true };

            if (logTabIndex == 0)
            {
                historyScrollPos = GUILayout.BeginScrollView(historyScrollPos, GUILayout.Height(logHeight));
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
            }
            else if (logTabIndex == 1)
            {
                milestoneScrollPos = GUILayout.BeginScrollView(milestoneScrollPos, GUILayout.Height(logHeight));
                for (int i = milestones.Count - 1; i >= 0; i--)
                {
                    GUILayout.BeginHorizontal();
                    if (editMode)
                    {
                        milestones[i].text = GUILayout.TextField(milestones[i].text);
                        if (GUILayout.Button("X", GUILayout.Width(22))) { milestones.RemoveAt(i); break; }
                    }
                    else
                    {
                        GUILayout.Label("<color=yellow>[Milestone]</color> <color=silver>" + milestones[i].date + ":</color> " + milestones[i].text, logStyle);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                if (editMode)
                {
                    GUILayout.BeginHorizontal();
                    newMilestoneInput = GUILayout.TextField(newMilestoneInput);
                    if (GUILayout.Button("+ Add", GUILayout.Width(60)))
                    {
                        if (!string.IsNullOrEmpty(newMilestoneInput))
                        {
                            string dateStr = KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true);
                            milestones.Add(new MilestoneEntry { date = dateStr, text = newMilestoneInput });
                            newMilestoneInput = "";
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

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
                mgr.SaveCraftData(currentCraftName, customDescription, mgr.currentMissionPurpose, currentFamilyTag, currentLinkedCrafts, currentPartCount, currentMass, currentCost, payloadEntries, launchHistory, milestones);
                LoadAvailableCrafts();
            }

            if (GUILayout.Button("UI Settings", GUILayout.Height(25))) showSettings = !showSettings;

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
                        AdvancedVesselInfoManager.Instance.SaveFamilies();
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
                    if (breakOuter) break;
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Refresh List")) LoadAvailableCrafts();
            GUILayout.EndVertical();
        }

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

            GUILayout.Label("<b>4. Flight Log</b>", BoldStyle(14));
            GUILayout.Label("Tracks launches automatically. Define the purpose before launch. Use the Edit button to update success status or add notes.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>5. Milestones</b>", BoldStyle(14));
            GUILayout.Label("Manually record important milestones for your vessels (e.g., 'Mun Landing'). Note: Milestones are NOT tracked automatically. You must add them manually using the Edit button as needed.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>6. Link Log To</b>", BoldStyle(14));
            GUILayout.Label("You can 'Link' a log to one or multiple crafts to keep your rocket launches bundled even when using varying payloads. All added crafts will receive the launch log.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>7. Payload Capabilities</b>", BoldStyle(14));
            GUILayout.Label("Record payload targets. Cost per ton is calculated automatically.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Label("<b>8. UI Settings Menu</b>", BoldStyle(14));
            GUILayout.Label("Customize text sizes and styles for better readability.", HelpTextStyle());
            GUILayout.Space(8);

            GUILayout.Space(15);
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b><color=orange>IMPORTANT:</color></b>", BoldStyle(12));
            GUILayout.Label("Changes are only permanent after clicking <color=white><b>'Save Data'</b></color>!", HelpTextStyle());
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.Space(5);
            GUILayout.Label("<color=silver><size=10>Advanced Vessel Info v1.7.3\nStatus: Systems Operational</size></color>", new GUIStyle(LogStyle()) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private GUIStyle HelpTextStyle() => new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13, richText = true, padding = new RectOffset(5, 5, 2, 2) };

        private void LoadCraftData(string shipName)
        {
            currentCraftName = shipName;

            ConfigNode vesselNode = AdvancedVesselInfoManager.Instance.GetVesselNode(shipName);
            payloadEntries.Clear(); launchHistory.Clear(); milestones.Clear();
            editMode = false; payloadEditMode = false; descriptionEditMode = false;
            familyEditMode = false; showFamilyDropdown = false;
            linkEditMode = false; showLinkDropdown = false; currentLinkedCrafts.Clear();
            missionEditMode = false;
            currentPartCount = 0; currentMass = 0f; currentCost = 0f; currentFamilyTag = "";
            newMilestoneInput = "";

            if (vesselNode != null)
            {
                string savedDesc = vesselNode.GetValue("description");
                if (!string.IsNullOrEmpty(savedDesc)) customDescription = savedDesc.Replace("\\n", "\n");
                else customDescription = GetVanillaDescription(shipName);

                currentFamilyTag = vesselNode.GetValue("familyTag") ?? "";
                currentLinkedCrafts.Clear();
                if (vesselNode.HasValue("linkedLogCraft"))
                {
                    string[] links = vesselNode.GetValues("linkedLogCraft");
                    foreach (string link in links)
                    {
                        if (!string.IsNullOrEmpty(link) && link != "None")
                            currentLinkedCrafts.Add(link);
                    }
                }
                if (currentLinkedCrafts.Count == 0)
                    currentLinkedCrafts.Add(shipName);

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

                ConfigNode milNode = vesselNode.GetNode("MILESTONES");
                if (milNode != null)
                {
                    foreach (ConfigNode n in milNode.GetNodes("ENTRY"))
                    {
                        milestones.Add(new MilestoneEntry { date = n.GetValue("date"), text = n.GetValue("text") });
                    }
                }
            }
            else
            {
                customDescription = GetVanillaDescription(shipName);
                AdvancedVesselInfoManager.Instance.currentMissionPurpose = "Enter Mission";
                currentLinkedCrafts.Add(shipName);
            }

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
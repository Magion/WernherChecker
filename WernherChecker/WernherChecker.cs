/*
 * License: The MIT License (MIT)
 * Version: v0.4
 * 
 * Minimizing button powered by awesome Toolbar Plugin - http://forum.kerbalspaceprogram.com/threads/60863 by blizzy78!
 */
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.Events;

namespace WernherChecker
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WernherChecker : MonoBehaviour
    {
        //Window variables
        public static float panelWidth = EditorPanels.Instance.partsEditor.panelTransform.rect.xMax;
        public Rect mainWindow = new Rect(panelWidth + 3, 120, 0, 0);
        Rect settingsWindow = new Rect();
        bool showAdvanced = false;
        bool showSettings = false;
        public bool minimized = false;
        bool minimizedManually = false;

        //Checklist managment
        public bool checklistSelected = false;
        public bool selectionInProgress = false;
        public bool checkSelected = false;
        bool selectedShowed = false;
        public PartSelection partSelection;

        //Tooltips
        public string globalTooltip = "";
        public float hoverTime = 0f;
        public string lastTooltip;

        //Other
        UnityAction launchDelegate = new UnityAction(CrewCheck.OnButtonInput);
        UnityAction defaultLaunchDelegate = new UnityAction(EditorLogic.fetch.launchVessel);
        bool KCTInstalled = false;
        public toolbarType activeToolbar;
        bool settings_BlizzyToolbar = false;
        bool settings_CheckCrew = true;
        bool settings_LockWindow = true;
        public static string DataPath = KSPUtil.ApplicationRootPath + "GameData/WernherChecker/Data/";
        public static Texture2D settingsTexture = GameDatabase.Instance.GetTexture("WernherChecker/Data/settings", false);
        public static Texture2D tooltipBGTexture = GameDatabase.Instance.GetTexture("WernherChecker/Data/tooltip_BG", false);
        IButton wcbutton;
        ApplicationLauncherButton appButton;
        public Vector2 mousePos = Input.mousePosition;
        public static List<Part> VesselParts
        {
            get { return EditorLogic.fetch.ship.Parts; }
        }

        //Instances
        public WCSettings Settings = new WCSettings();
        public ChecklistSystem checklistSystem = new ChecklistSystem();
        public static WernherChecker Instance;

        public enum toolbarType
        {
            STOCK,
            BLIZZY
        }

        // GUI Styles
        public static GUIStyle windowStyle = new GUIStyle(HighLogic.Skin.window);
        public static GUIStyle boxStyle = new GUIStyle(HighLogic.Skin.box);
        public static GUIStyle buttonStyle = new GUIStyle(HighLogic.Skin.button);
        public static GUIStyle toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
        public static GUIStyle labelStyle = new GUIStyle(HighLogic.Skin.label);
        public static GUIStyle tooltipStyle = new GUIStyle(HighLogic.Skin.textArea)
        {
            padding = new RectOffset(4, 4, 4, 4),
            border = new RectOffset(2, 2, 2, 2),
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            normal = { background = tooltipBGTexture },
            richText = true,
        };

        public void Start()
        {
            Debug.LogWarning("WernherChecker v0.4.1 is loading...");
            Instance = this;
            if (Settings.Load())
            {
                minimized = Settings.minimized;
                mainWindow.x = Settings.windowX;
                mainWindow.y = Settings.windowY;
            }
            checklistSystem.LoadChecklists();
            GameEvents.onEditorScreenChange.Add(onEditorPanelChange);
            GameEvents.onEditorShipModified.Add(checklistSystem.CheckVessel);
            GameEvents.onEditorRestart.Add(checklistSystem.CheckVessel);
            GameEvents.onEditorShowPartList.Add(checklistSystem.CheckVessel);

            if (AssemblyLoader.loadedAssemblies.Any(a => a.dllName == "KerbalConstructionTime"))
                KCTInstalled = true;
            else
                KCTInstalled = false;

            if (Settings.checkCrewAssignment && !KCTInstalled)
            {
                EditorLogic.fetch.launchBtn.onClick.RemoveListener(defaultLaunchDelegate);
                EditorLogic.fetch.launchBtn.onClick.AddListener(launchDelegate);
            }

            if (Settings.wantedToolbar == toolbarType.BLIZZY && ToolbarManager.ToolbarAvailable)
            {
                AddToolbarButton(toolbarType.BLIZZY, true);
            }
            else
            {
                AddToolbarButton(toolbarType.STOCK, true);
            }  
        }

        void OnDestroy()
        {
            if (activeToolbar == toolbarType.BLIZZY)
                wcbutton.Destroy();
            if (InputLockManager.lockStack.ContainsKey("WernherChecker_partSelection"))
            {
                InputLockManager.RemoveControlLock("WernherChecker_partSelection");
                selectionInProgress = false;
            }

            GameEvents.onEditorScreenChange.Remove(onEditorPanelChange);
            GameEvents.onEditorShipModified.Remove(checklistSystem.CheckVessel);
            GameEvents.onEditorRestart.Remove(checklistSystem.CheckVessel);
            GameEvents.onEditorShowPartList.Remove(checklistSystem.CheckVessel);

            Settings.Save();
        }

        #region Toolbar stuff
        void AddToolbarButton(toolbarType type, bool useStockIfProblem)
        {
            if (type == toolbarType.BLIZZY)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    {
                        activeToolbar = toolbarType.BLIZZY;
                        wcbutton = ToolbarManager.Instance.add("WernherChecker", "wcbutton"); //creating toolbar button
                        wcbutton.TexturePath = "WernherChecker/Data/icon_24";
                        wcbutton.ToolTip = "WernherChecker";
                        wcbutton.OnClick += (e) =>
                        {
                            if (minimizedManually)
                                MiniOff();
                            else
                                MiniOn();
                        };
                    }
                }
                else if (useStockIfProblem)
                    AddToolbarButton(toolbarType.STOCK, true);
            }

            if(type == toolbarType.STOCK)
            {
                activeToolbar = toolbarType.STOCK;
                if (ApplicationLauncher.Ready)
                    CreateAppButton();
                GameEvents.onGUIApplicationLauncherReady.Add(CreateAppButton);
                GameEvents.onGUIApplicationLauncherUnreadifying.Add(DestroyAppButton);
            }
        }
        
        void CreateAppButton()
        {
            appButton = ApplicationLauncher.Instance.AddModApplication(MiniOff, MiniOn, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture)GameDatabase.Instance.GetTexture("WernherChecker/Data/icon",false));
            if (!minimized)
                appButton.SetTrue(true);
        }

        void DestroyAppButton(GameScenes gameScenes)
        {
            ApplicationLauncher.Instance.RemoveModApplication(appButton);
            GameEvents.onGUIApplicationLauncherReady.Remove(CreateAppButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(DestroyAppButton);
        }
        #endregion

        void onEditorPanelChange(EditorScreen screen)
        {
            if (screen == EditorScreen.Actions || screen == EditorScreen.Crew)
            {
                minimized = true;
            }

            if (screen == EditorScreen.Parts)
            {
                minimized = minimizedManually;
            }
        }

        void MiniOn()
        {
            minimizedManually = true;
            minimized = true;
        }

        void MiniOff()
        {
            minimizedManually = false;
            minimized = false;
        }

        void OnGUI()
        {
            mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            if (!minimized)
                mainWindow = GUILayout.Window(1, mainWindow, OnWindow, "WernherChecker v0.4.1"/*." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision + "dev"*/, windowStyle);
            if (showSettings && !minimized)
                settingsWindow = GUILayout.Window(2, settingsWindow, OnSettingsWindow, "WernherChecker - Settings", windowStyle);
            if (checklistSelected && checklistSystem.ActiveChecklist.items.Exists(i => i.paramsDisplayed) && !minimized)
                checklistSystem.paramsWindow = GUILayout.Window(3, checklistSystem.paramsWindow, checklistSystem.DrawParamsWindow, "Edit Parameters", HighLogic.Skin.window);
            
            mainWindow.x = Mathf.Clamp(mainWindow.x, 0, Screen.width - mainWindow.width);
            mainWindow.y = Mathf.Clamp(mainWindow.y, 0, Screen.height - mainWindow.height);

            if (partSelection != null && selectionInProgress)
                partSelection.Update(mousePos);           

            if (Settings.lockOnHover)
            {
                if (!minimized && (mainWindow.Contains(mousePos) || (showSettings && settingsWindow.Contains(mousePos)) || (checklistSystem.ActiveChecklist.items.Exists(i => i.paramsDisplayed) && checklistSystem.paramsWindow.Contains(mousePos))) && !InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Lock(true, true, true, "WernherChecker_windowLock");
                else if (((!mainWindow.Contains(mousePos) && !settingsWindow.Contains(mousePos) && !checklistSystem.paramsWindow.Contains(mousePos)) || minimized)  && InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Unlock("WernherChecker_windowLock");
            }

            DrawToolTip(globalTooltip);
        }

        public void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
                globalTooltip = GUI.tooltip;
        }

        void DrawToolTip(string tooltipText)
        {
            if (tooltipText != lastTooltip)
                hoverTime = 0;
            if (lastTooltip == tooltipText && Event.current.type == EventType.Repaint && tooltipText != "")
                hoverTime += Time.deltaTime;

            lastTooltip = tooltipText;

            if (tooltipText == "" || hoverTime < 0.5f)
                return;           
            
            //Debug.Log(tooltipText);           
            GUIContent tooltip = new GUIContent(tooltipText);
            Rect tooltipPosition = new Rect(mousePos.x + 15, mousePos.y + 15, 0, 0);
            float maxw, minw;
            tooltipStyle.CalcMinMaxWidth(tooltip, out minw, out maxw);
            tooltipPosition.width = Math.Min(Math.Max(200, minw), maxw);
            tooltipPosition.height = tooltipStyle.CalcHeight(tooltip, tooltipPosition.width);
            GUI.Label(/*new Rect(EditorPanels.Instance.partsPanelWidth + 5, Screen.height - 60, 100, 60)*/tooltipPosition, tooltip, tooltipStyle);
            GUI.depth = 0;


        }

        public void SelectChecklist()
        {
            GUILayout.BeginVertical(boxStyle);
            foreach (Checklist checklist in checklistSystem.availableChecklists)
            {
                if (GUILayout.Button(new GUIContent(checklist.name, "Items:\n" + string.Join("\n", checklist.items.Select(x => "<color=cyan><b>–</b></color> <i>" + x.name + "</i>").ToArray())), buttonStyle))
                {
                    checklistSystem.ActiveChecklist = checklist;
                    checklistSelected = true;
                    checklistSystem.CheckVessel();
                }
            }
            if (checklistSystem.availableChecklists.Count == 0)
            {
                GUILayout.Label("No valid checklists found!");
                if (GUILayout.Button("Try Again", buttonStyle))
                {
                    Settings.Load();
                    checklistSystem.LoadChecklists();
                    mainWindow.height = 0;
                }
            }

            GUILayout.EndVertical();
            GUILayout.Label("You can create your own checklist in the config file.", labelStyle);
        }

        void OnSettingsWindow(int windowID)
        {
            settingsWindow.x = mainWindow.x + mainWindow.width;
            settingsWindow.y = mainWindow.y;
            GUILayout.BeginVertical(GUILayout.Width(220f), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(boxStyle);
            settings_LockWindow = GUILayout.Toggle(settings_LockWindow, new GUIContent("Prevent clicking-throught", "Lock the editor while hovering over a window"), toggleStyle);

            if (!KCTInstalled)
                settings_CheckCrew = GUILayout.Toggle(settings_CheckCrew, new GUIContent("Check crew assignment", "Allow pre-launch crew assignment reminder"), toggleStyle);

            if (ToolbarManager.ToolbarAvailable)
            {
                settings_BlizzyToolbar = GUILayout.Toggle(settings_BlizzyToolbar, settings_BlizzyToolbar ? "Use blizzy78's toolbar" : "Use stock toolbar", toggleStyle);
            }
            GUILayout.EndVertical();
            if (GUILayout.Button(new GUIContent("Reload data", "Reload the config file"), buttonStyle))
            {
                Settings.Load();
                if(checklistSystem.LoadChecklists())
                    checklistSelected = false;
                mainWindow.height = 0;
            }

            if (GUILayout.Button("Apply & Close", buttonStyle))
            {
                Settings.lockOnHover = settings_LockWindow;

                if (!settings_CheckCrew && Settings.checkCrewAssignment)
                {
                    Settings.checkCrewAssignment = false;
                    EditorLogic.fetch.launchBtn.onClick.RemoveListener(launchDelegate);
                    EditorLogic.fetch.launchBtn.onClick.AddListener(defaultLaunchDelegate);
                }

                else if(settings_CheckCrew && !Settings.checkCrewAssignment)
                {
                    Settings.checkCrewAssignment = true;
                    EditorLogic.fetch.launchBtn.onClick.RemoveListener(defaultLaunchDelegate);
                    EditorLogic.fetch.launchBtn.onClick.AddListener(launchDelegate);
                }
                //--------------------------------------------------------------------------
                if (activeToolbar == toolbarType.BLIZZY && !settings_BlizzyToolbar)
                {
                    wcbutton.Destroy();
                    AddToolbarButton(toolbarType.STOCK, true);
                }

                if (activeToolbar == toolbarType.STOCK && settings_BlizzyToolbar)
                {
                    DestroyAppButton(GameScenes.EDITOR);
                    AddToolbarButton(toolbarType.BLIZZY, true);
                }

                showSettings = false;
            }

            GUILayout.EndVertical();

            SetTooltipText();
        }

        void OnWindow(int windowID)
        {
            GUILayout.BeginVertical( GUILayout.Width(225));

            if (Settings.cfgLoaded) //If the cfg file exists
            {
                if (checklistSelected) //If the checklist is selected
                {
                    if (!selectionInProgress) //If the mode, where the checked parts are set, is active
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Current checklist:");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(checklistSystem.ActiveChecklist.name, labelStyle);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginVertical(boxStyle);
                        for (int i = 0; i < checklistSystem.ActiveChecklist.items.Count; i++)
                        {
                            ChecklistItem tempItem = checklistSystem.ActiveChecklist.items[i];
                            tempItem.DrawItem();
                            checklistSystem.ActiveChecklist.items[i] = tempItem;
                        }
                        if (Settings.jebEnabled == true)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("MOAR BOOSTERS!!!", labelStyle); //small joke :P
                            GUILayout.FlexibleSpace();
                            GUILayout.Toggle(false, "", ChecklistItem.checkboxStyle);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        if (GUILayout.Button("Change checklist", buttonStyle, GUILayout.Height(24f)))
                        {
                            mainWindow.height = 0f;
                            checklistSelected = false;
                        }

                        //-------------------------------------------------------------------------------------------
                        //⇓︾▼↓︽
                        if (showAdvanced) //Advanced options showed
                        {
                            if (!showSettings)
                            {
                                if (GUILayout.Button("Show settings", buttonStyle, GUILayout.Height(24f)))
                                {
                                    mainWindow.height = 0f;
                                    showSettings = true;
                                    if (activeToolbar == toolbarType.BLIZZY)
                                        settings_BlizzyToolbar = true;
                                    else
                                        settings_BlizzyToolbar = false;
                                    settings_CheckCrew = Settings.checkCrewAssignment;
                                    settings_LockWindow = Settings.lockOnHover;
                                }
                            }

                            if (GUILayout.Button(new GUIContent("Recheck vessel", "Use this if the automatic checking doesn't work for some reason"), buttonStyle,GUILayout.Height(24f)))
                                checklistSystem.CheckVessel();

                            GUILayout.Label("Checked area:", labelStyle);
                            if (GUILayout.Toggle(!checkSelected, new GUIContent("Entire ship", "Check the entire ship"), toggleStyle) != !checkSelected)
                            {
                                checkSelected = false;
                                checklistSystem.CheckVessel();
                                mainWindow.height = 0f;
                            }
                            if (GUILayout.Toggle(checkSelected, new GUIContent(partSelection == null || EditorLogic.RootPart == null ? "Selected parts (0)" : "Selected parts (" + partSelection.selectedParts.Intersect(EditorLogic.fetch.ship.parts).ToList().Count + ")", "Check only a selected section of the ship (e.g. lander/booster stage)"), toggleStyle) == !checkSelected)
                            {
                                checkSelected = true;
                                checklistSystem.CheckVessel();
                            }

                            if (checkSelected && EditorLogic.RootPart != null)
                            {
                                if (GUILayout.Button(new GUIContent("Select parts", "Select the checked parts"), buttonStyle, GUILayout.Height(24f)))
                                {
                                    mainWindow.height = 0f;
                                    print("[WernherChecker]: Engaging selection mode");
                                    foreach (Part part in VesselParts)
                                    {
                                        part.SetHighlightDefault();
                                    }
                                    partSelection = new PartSelection();
                                    selectionInProgress = true;
                                    selectedShowed = false;
                                    InputLockManager.SetControlLock(ControlTypes.EDITOR_PAD_PICK_PLACE | ControlTypes.EDITOR_UI, "WernherChecker_partSelection");
                                }

                                if (!selectedShowed)
                                {
                                    if (GUILayout.Button(new GUIContent("Highlight selected parts", "Highlight the parts selected for checking"), buttonStyle, GUILayout.Height(24f)))
                                    {
                                        if (partSelection != null)
                                        {
                                            foreach (Part part in partSelection.selectedParts)
                                            {
                                                if (WernherChecker.VesselParts.Contains(part))
                                                {
                                                    part.SetHighlightType(Part.HighlightType.AlwaysOn);
                                                    part.SetHighlightColor(new Color(10f, 0.9f, 0f));
                                                }
                                            }
                                        }
                                        selectedShowed = true;
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Hide selected parts", buttonStyle, GUILayout.Height(24f)))
                                    {
                                        /*float max, min;
                                        GUI.skin.label.CalcMinMaxWidth(new GUIContent("Thisisthecontentasdsdfsdfsd"), out min, out max);
                                        Debug.Log("Min: " + min + ", Max: " + max);*/
                                        foreach (Part part in WernherChecker.VesselParts)
                                        {
                                            part.SetHighlightDefault();
                                        }
                                        selectedShowed = false;
                                    }
                                }
                            }

                        }

                        if (GUILayout.Button(new GUIContent(showAdvanced ? "︽ Fewer Options ︽" : "︾ More Options ︾", "Show/Hide advanced options"), buttonStyle, GUILayout.Height(24f)))
                        {
                            mainWindow.height = 0f;
                            showAdvanced = !showAdvanced;
                        }
                    }
                    else
                    {
                        GUILayout.Label("Select parts to check by holding LMB and moving mouse", labelStyle);
                        GUILayout.Label("Current selection: " + partSelection.selectedParts.Count + " part(s)");
                        if (GUILayout.Button(new GUIContent("Done", "Finish part selection"), buttonStyle))
                        {
                            mainWindow.height = 0f;
                            print("[WernherChecker]: " + partSelection.selectedParts.Count + " parts selected");
                            foreach (Part part in WernherChecker.VesselParts)
                            {
                                part.SetHighlightDefault();
                            }
                            selectionInProgress = false;
                            InputLockManager.RemoveControlLock("WernherChecker_partSelection");
                            checklistSystem.CheckVessel();
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Please select checklist", labelStyle);
                    SelectChecklist();
                }
            }

            else
            {
                GUILayout.Label("Cannot find config file!", labelStyle);
            }

            GUILayout.EndVertical();
            GUI.DragWindow(); //making it dragable
            SetTooltipText();
        }
    }
}
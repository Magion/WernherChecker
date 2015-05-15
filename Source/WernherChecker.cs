/*
 * License: The MIT License (MIT)
 * Version: v0.4
 * 
 * Minimizing button powered by awesome Toolbar Plugin - http://forum.kerbalspaceprogram.com/threads/60863 by blizzy78
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WernherChecker
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WernherChecker : MonoBehaviour
    {   
        //Window variables
        public Rect mainWindow = new Rect(EditorPanels.Instance.partsPanelWidth + 3, 120, 0, 0);
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
        Dictionary<string, List<string>> itemModules = new Dictionary<string, List<string>>();


        //Other
        string defaultLaunchMethod;
        MonoBehaviour defaultLaunchBehaviour;
        EZInputDelegate launchDelegate = new EZInputDelegate(CrewCheck.OnButtonInput);
        bool KCTInstalled = false;
        public toolbarType activeToolbar;
        bool settings_BlizzyToolbar = false;
        bool settings_CheckCrew = true;
        bool settings_LockWindow = true;
        public static string DataPath = KSPUtil.ApplicationRootPath + "GameData/WernherChecker/Data/";
        public static Texture2D settingsTexture = GameDatabase.Instance.GetTexture("WernherChecker/Data/settings", false);
        IButton wcbutton;
        ApplicationLauncherButton appButton;
        public Vector2 mousePos = Input.mousePosition;
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

        public void Start()
        {
            Debug.LogWarning("WernherChecker v0.4 has been loaded");
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
            KCTInstalled = false;
            foreach (AssemblyLoader.LoadedAssembly assebmly in AssemblyLoader.loadedAssemblies)
                if (assebmly.dllName == "KerbalConstructionTime")
                {
                    KCTInstalled = true;
                    break;
                }

            defaultLaunchMethod = EditorLogic.fetch.launchBtn.methodToInvoke;
            defaultLaunchBehaviour = EditorLogic.fetch.launchBtn.scriptWithMethodToInvoke;

            if (Settings.checkCrewAssignment && !KCTInstalled)
            {
                EditorLogic.fetch.launchBtn.methodToInvoke = null;
                EditorLogic.fetch.launchBtn.scriptWithMethodToInvoke = null;
                EditorLogic.fetch.launchBtn.SetInputDelegate(launchDelegate);
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
            Settings.Save();
        }

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
                appButton.toggleButton.SetTrue(false, true);
        }

        void DestroyAppButton(GameScenes gameScenes)
        {
            ApplicationLauncher.Instance.RemoveModApplication(appButton);
            GameEvents.onGUIApplicationLauncherReady.Remove(CreateAppButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(DestroyAppButton);
        }

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
                mainWindow = GUILayout.Window(52, mainWindow, OnWindow, "WernherChecker v0.4", windowStyle);
            if (showSettings && !minimized)
                settingsWindow = GUILayout.Window(53, settingsWindow, OnSettingsWindow, "WernherChecker - Settings", windowStyle);
            if (checklistSelected)
                if (checklistSystem.activeChecklist.items.Exists(i => i.paramsDisplayed))
                    checklistSystem.paramsWindow = GUILayout.Window(3, checklistSystem.paramsWindow, checklistSystem.DrawParamsWindow, "Edit Parameters", HighLogic.Skin.window);
            mainWindow.x = Mathf.Clamp(mainWindow.x, 0, Screen.width - mainWindow.width);
            mainWindow.y = Mathf.Clamp(mainWindow.y, 0, Screen.height - mainWindow.height);
            if (partSelection != null && selectionInProgress)
                partSelection.Update(mousePos);

            if (Settings.lockOnHover)
            {
                if (((!minimized && mainWindow.Contains(mousePos)) || (!minimized && showSettings && settingsWindow.Contains(mousePos))) && !InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Lock(true, true, true, "WernherChecker_windowLock");
                else if (((!mainWindow.Contains(mousePos) && !settingsWindow.Contains(mousePos)) || minimized)  && InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Unlock("WernherChecker_windowLock");
            }
        }

        public void SelectChecklist()
        {
            GUILayout.BeginVertical(boxStyle);
            foreach (Checklist checklist in checklistSystem.availableChecklists)
            {
                if (GUILayout.Button(checklist.name, buttonStyle))
                {
                    checklistSystem.activeChecklist = checklist;
                    checklistSelected = true;
                    checklistSystem.CheckVessel(EditorLogic.fetch.ship);
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
            settings_LockWindow = GUILayout.Toggle(settings_LockWindow, "Prevent clicking-throught", toggleStyle);

            if (!KCTInstalled)
                settings_CheckCrew = GUILayout.Toggle(settings_CheckCrew, "Check crew assignment", toggleStyle);

            if (ToolbarManager.ToolbarAvailable)
            {
                settings_BlizzyToolbar = GUILayout.Toggle(settings_BlizzyToolbar, settings_BlizzyToolbar ? "Use blizzy78's toolbar" : "Use stock toolbar", toggleStyle);
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Reload data", buttonStyle))
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
                    EditorLogic.fetch.launchBtn.methodToInvoke = defaultLaunchMethod;
                    EditorLogic.fetch.launchBtn.scriptWithMethodToInvoke = defaultLaunchBehaviour;
                    EditorLogic.fetch.launchBtn.RemoveInputDelegate(launchDelegate);
                }

                else if(settings_CheckCrew && !Settings.checkCrewAssignment)
                {
                    Settings.checkCrewAssignment = true;
                    EditorLogic.fetch.launchBtn.methodToInvoke = null;
                    EditorLogic.fetch.launchBtn.scriptWithMethodToInvoke = null;
                    EditorLogic.fetch.launchBtn.SetInputDelegate(launchDelegate);
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
                        GUILayout.Label(checklistSystem.activeChecklist.name, labelStyle);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginVertical(boxStyle);
                        for (int i = 0; i < checklistSystem.activeChecklist.items.Count; i++)
                        {
                            ChecklistItem tempItem = checklistSystem.activeChecklist.items[i];
                            tempItem.DrawItem();
                            checklistSystem.activeChecklist.items[i] = tempItem;
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

                            GUILayout.Label("Checked area:", labelStyle);
                            if (GUILayout.Toggle(!checkSelected, "Entire ship", toggleStyle) != !checkSelected)
                            {
                                checkSelected = false;
                                checklistSystem.CheckVessel(EditorLogic.fetch.ship);
                                mainWindow.height = 0f;
                            }
                            if (GUILayout.Toggle(checkSelected, "Selected parts", toggleStyle))
                            {
                                checkSelected = true;
                                checklistSystem.CheckVessel(EditorLogic.fetch.ship);
                            }

                            if (checkSelected && EditorLogic.RootPart != null)
                            {
                                if (GUILayout.Button("Select parts", buttonStyle, GUILayout.Height(24f)))
                                {
                                    mainWindow.height = 0f;
                                    print("[WernherChecker]: Engaging selection mode");
                                    foreach (Part part in EditorLogic.SortedShipList)
                                    {
                                        part.SetHighlightDefault();
                                    }
                                    partSelection = new PartSelection();
                                    selectionInProgress = true;
                                    selectedShowed = false;
                                    InputLockManager.SetControlLock(ControlTypes.EDITOR_PAD_PICK_PLACE , "WernherChecker_partSelection");
                                }

                                if (!selectedShowed)
                                {
                                    if (GUILayout.Button("Show selected parts", buttonStyle, GUILayout.Height(24f)))
                                    {
                                        if (partSelection != null)
                                        {
                                            foreach (Part part in partSelection.selectedParts)
                                            {
                                                if (EditorLogic.SortedShipList.Contains(part))
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
                                        foreach (Part part in EditorLogic.SortedShipList)
                                        {
                                            part.SetHighlightDefault();
                                        }
                                        selectedShowed = false;
                                    }
                                }
                            }

                        }

                        if (GUILayout.Button(showAdvanced ? "︽ Fewer Options ︽" : "︾ More Options ︾", buttonStyle, GUILayout.Height(24f)))
                        {
                            mainWindow.height = 0f;
                            showAdvanced = !showAdvanced;
                        }
                    }
                    else
                    {
                        GUILayout.Label("Select parts, which should be checked by holding LMB and moving mouse", labelStyle);
                        if (GUILayout.Button("Done", buttonStyle))
                        {
                            mainWindow.height = 0f;
                            print("[WernherChecker]: " + partSelection.selectedParts.Count + " parts selected");
                            foreach (Part part in EditorLogic.SortedShipList)
                            {
                                part.SetHighlightDefault();
                            }
                            selectionInProgress = false;
                            InputLockManager.RemoveControlLock("WernherChecker_partSelection");
                            checklistSystem.CheckVessel(EditorLogic.fetch.ship);
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
        }
    }
}

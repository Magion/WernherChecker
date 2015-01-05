/*
 * License: The MIT License (MIT)
 * Version: v0.3.4
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
        public static Rect mainWindow = new Rect(EditorPanels.Instance.partsPanelWidth + 3, 120, 0, 0);
        static Rect selectWindow = new Rect(Screen.width / 2 - 30, Screen.height / 2 - selectWindow.height / 2, 0, 0);
        static Rect settingsWindow = new Rect();
        const float windowBaseHeight = 35f;//34(36).9f
        const float windowButtonHeight = 27f;
        const float windowSmallButtonHeight = 24f;
        const float windowToggleHeight = 30f;
        const float windowLabelHeight = 30f;
        const float windowMargin = 1.9f; //4f
        float windowHeight;
        bool showAdvanced = false;
        bool showSettings = false;
        public static bool minimized = false;
        bool minimizedManually = false;

        //Checklist managment
        string currentChecklistName;
        bool checklistSelected = false;
        public static bool selectionInProgress = false;
        public static bool checkSelected = false;
        bool selectedShowed = false;
        PartSelection partSelection;
        Dictionary<string, List<string>> itemModules = new Dictionary<string, List<string>>();

        //Other
        string defaultLaunchMethod;
        MonoBehaviour defaultLaunchBehaviour;
        EZInputDelegate launchDelegate = new EZInputDelegate(CrewCheck.OnButtonInput);
        bool KCTInstalled = false;
        public static toolbarType activeToolbar;
        bool settings_BlizzyToolbar = false;
        bool settings_CheckCrew = true;
        bool settings_LockWindow = true;
        public static string DataPath = KSPUtil.ApplicationRootPath + "GameData/WernherChecker/Data/";
        IButton wcbutton;
        ApplicationLauncherButton appButton;
        public static Vector2 mousePos = Input.mousePosition;
        WCSettings Settings = new WCSettings();
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
            Debug.LogWarning("WernherChecker v0.3.4 has been loaded");
            if (Settings.Load())
            {
                mainWindow.x = Settings.windowX;
                mainWindow.y = Settings.windowY;
            }

            GameEvents.onEditorScreenChange.Add(onEditorPanelChange);

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
            appButton.SetTrue();
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
                mainWindow = GUILayout.Window(52, mainWindow, OnWindow, "WernherChecker v0.3.4", windowStyle);
            if (showSettings && !minimized)
                settingsWindow = GUILayout.Window(53, settingsWindow, OnSettingsWindow, "WernherChecker - Settings", windowStyle);
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
            foreach (ConfigNode node in Settings.cfg.GetNodes("CHECKLIST"))
            {
                if (GUILayout.Button(node.GetValue("name"), buttonStyle))
                {
                    LoadChecklist(node);
                    checklistSelected = true;
                }
                windowHeight += windowButtonHeight + windowMargin;
            }
            GUILayout.EndVertical();
            GUILayout.Label("You can create your own checklist in the config file.", labelStyle);
            windowHeight += windowLabelHeight + 22;
        }

        public void LoadChecklist(ConfigNode checklistNode)
        {
            print("[WernherChecker]: Loading " + '"' + checklistNode.GetValue("name") + '"' + " checklist");
            currentChecklistName = checklistNode.GetValue("name");
            itemModules.Clear();
            foreach (ConfigNode itemNode in checklistNode.GetNodes("CHECKLIST_ITEM"))
            {
                List<string> modules = new List<string>();
                foreach (string module in itemNode.GetValue("modules").Trim().Split(','))
                {
                    modules.Add(module);
                }
                itemModules.Add(itemNode.GetValue("name"), modules);
            }
            print("[WernherChecker]: Checklist " + '"' + checklistNode.GetValue("name") + '"' + " loaded");
        }

        public void DrawListItems()
        {
            foreach (string item in itemModules.Keys)
            {
                bool hasIt = false;
                if (EditorLogic.RootPart != null)
                {
                    foreach (string module in itemModules[item])
                    {
                        if (checkSelected && partSelection != null)
                        {
                            foreach (Part part in partSelection.selectedParts)
                            {
                                if (EditorLogic.SortedShipList.Contains(part))
                                {
                                    if (part.Modules.Contains(module))
                                        hasIt = true;
                                }
                            }
                        }
                        else
                        {
                            foreach (Part part in EditorLogic.SortedShipList)
                            {
                                if (part.Modules.Contains(module))
                                    hasIt = true;
                            }
                        }
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(item, labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Toggle(hasIt, "", toggleStyle);
                GUILayout.EndHorizontal();
                windowHeight += windowLabelHeight +windowMargin;

            }
        }

        void OnSettingsWindow(int windowID)
        {
            settingsWindow.x = mainWindow.x + mainWindow.width;
            settingsWindow.y = mainWindow.y;
            GUILayout.BeginVertical(GUILayout.Width(220f));
            GUILayout.BeginVertical(boxStyle);
            settings_LockWindow = GUILayout.Toggle(settings_LockWindow, "Prevent clicking-throught", toggleStyle);

            if (!KCTInstalled)
                settings_CheckCrew = GUILayout.Toggle(settings_CheckCrew, "Check crew assignment", toggleStyle);

            if (ToolbarManager.ToolbarAvailable)
            {
                settings_BlizzyToolbar = GUILayout.Toggle(settings_BlizzyToolbar, "Use blizzy78's toolbar", toggleStyle);
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Reload data", buttonStyle))
            {
                Settings.Load();
                checklistSelected = false;
            }

            if (GUILayout.Button("OK", buttonStyle))
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
            windowHeight = windowBaseHeight;
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
                        GUILayout.Label(currentChecklistName, labelStyle);
                        GUILayout.EndHorizontal();
                        windowHeight += windowLabelHeight + windowMargin;

                        GUILayout.BeginVertical(boxStyle);
                        DrawListItems();
                        if (Settings.jebEnabled == true)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("MOAR BOOSTERS!!!", labelStyle); //small joke :P
                            GUILayout.FlexibleSpace();
                            GUILayout.Toggle(false, "", toggleStyle);
                            GUILayout.EndHorizontal();
                            windowHeight += windowLabelHeight + windowMargin;
                        }
                        GUILayout.EndVertical();

                        if (GUILayout.Button("Change checklist", buttonStyle, GUILayout.Height(24f)))
                        {
                            checklistSelected = false;
                        }
                        windowHeight += 24 + windowMargin;

                        //-------------------------------------------------------------------------------------------
                        //⇓︾▼↓︽
                        if (showAdvanced) //Advanced options showed
                        {
                            if (GUILayout.Button("Show settings", buttonStyle, GUILayout.Height(24f)))
                            {
                                showSettings = true;
                                if (activeToolbar == toolbarType.BLIZZY)
                                    settings_BlizzyToolbar = true;
                                else
                                    settings_BlizzyToolbar = false;
                                settings_CheckCrew = Settings.checkCrewAssignment;
                                settings_LockWindow = Settings.lockOnHover;
                            }
                            windowHeight += windowSmallButtonHeight + windowMargin;

                            GUILayout.Label("Checked area:", labelStyle);
                            windowHeight += windowLabelHeight + windowMargin;
                            if (GUILayout.Toggle(!checkSelected, "Entire ship", toggleStyle))
                                checkSelected = false;
                            if (GUILayout.Toggle(checkSelected, "Selected parts", toggleStyle))
                                checkSelected = true;

                            windowHeight += 2 * windowToggleHeight + windowMargin;

                            if (checkSelected && EditorLogic.RootPart != null)
                            {
                                if (GUILayout.Button("Select parts", buttonStyle, GUILayout.Height(24f)))
                                {
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
                                windowHeight += windowSmallButtonHeight + windowMargin;

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
                                    windowHeight += windowSmallButtonHeight + windowMargin;
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
                                    windowHeight += windowSmallButtonHeight + windowMargin;
                                }
                            }

                            if (GUILayout.Button("︽ Fewer Options ︽", buttonStyle, GUILayout.Height(24f)))
                            {
                                showAdvanced = false;
                            }
                            windowHeight += windowSmallButtonHeight + windowMargin;

                        }

                        else
                        {
                            if (GUILayout.Button("︾ More Options ︾", buttonStyle, GUILayout.Height(24f)))
                            {
                                showAdvanced = true;
                            }
                            windowHeight += windowSmallButtonHeight + windowMargin;
                        }
                    }
                    else
                    {
                        GUILayout.Label("Select parts, which should be checked by holding LMB and moving mouse", labelStyle);
                        windowHeight += windowLabelHeight + 25 + windowMargin;
                        if (GUILayout.Button("Done", buttonStyle))
                        {
                            print("[WernherChecker]: " + partSelection.selectedParts.Count + " parts selected");
                            foreach (Part part in EditorLogic.SortedShipList)
                            {
                                part.SetHighlightDefault();
                            }
                            selectionInProgress = false;
                            InputLockManager.RemoveControlLock("WernherChecker_partSelection");
                        }
                        windowHeight += windowSmallButtonHeight + windowMargin;
                    }
                }
                else
                {
                    GUILayout.Label("Please select checklist", labelStyle);
                    windowHeight += windowLabelHeight + windowMargin;
                    SelectChecklist();
                }
            }

            else
            {
                GUILayout.Label("Cannot find config file!", labelStyle);
                windowHeight += windowLabelHeight + windowMargin;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(); //making it dragable
            mainWindow.height = windowHeight;
        }
    }
}

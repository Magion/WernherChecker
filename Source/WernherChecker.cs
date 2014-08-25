/*
 * License: The MIT License (MIT)
 * Version: v0.3
 * 
 * Minimizing button powered by awesome Toolbar Plugin - http://forum.kerbalspaceprogram.com/threads/60863 by blizzy78
 */
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace WernherChecker
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WernherChecker : MonoBehaviour
    {   
        public static Rect mainWindow = new Rect(EditorPanels.Instance.partsPanelWidth + 5, 44, 0, 0);
        static Rect selectWindow = new Rect(Screen.width / 2 - 30, Screen.height / 2 - selectWindow.height / 2, 0, 0);
        const float windowBaseHeight = 35f;//34(36).9f
        const float windowButtonHeight = 27f;
        const float windowSmallButtonHeight = 24f;
        const float windowToggleHeight = 30f;
        const float windowLabelHeight = 30f;
        const float windowMargin = 1.9f; //4f
        float windowHeight;
        bool showAdvanced = false;

        string currentChecklistName;
        bool checklistSelected = false;
        public static bool selectionInProgress = false;
        public static bool checkSelected = false;
        bool selectedShowed = false;
        PartSelection partSelection;
        public static bool minimized = false;
        toolbarType activeToolbar;
        public static string DataPath = KSPUtil.ApplicationRootPath + "GameData/WernherChecker/Data/";
        IButton wcbutton;
        ApplicationLauncherButton appButton;
        public static Vector2 mousePos = Input.mousePosition;
        Dictionary<string, List<string>> itemModules = new Dictionary<string, List<string>>();
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
            Debug.LogWarning("WernherChecker v0.3 has been loaded");
            Settings.Load();
            

            if (Settings.wantedToolbar == toolbarType.BLIZZY && ToolbarManager.ToolbarAvailable)
            {
                activeToolbar = toolbarType.BLIZZY;
                wcbutton = ToolbarManager.Instance.add("WernherChecker", "wcbutton"); //creating toolbar button
                wcbutton.TexturePath = "WernherChecker/Data/icon_24";
                wcbutton.ToolTip = "WernherChecker";
                wcbutton.OnClick += (e) => minimized = !minimized;
            }

            else
            {
                activeToolbar = toolbarType.STOCK;
                GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
            }  
        }

        void onGUIApplicationLauncherReady()
        {
            appButton = ApplicationLauncher.Instance.AddModApplication(miniOff, miniOn, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture)GameDatabase.Instance.GetTexture("WernherChecker/Data/icon",false));
            appButton.SetTrue();
        }

        void miniOn()
        {
            minimized = true;
        }

        void miniOff()
        {
            minimized = false;
        }

        void OnDestroy()
        {
            if (activeToolbar == toolbarType.STOCK)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(onGUIApplicationLauncherReady);
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
            }
            else if (activeToolbar == toolbarType.BLIZZY)
                wcbutton.Destroy();
            if (InputLockManager.lockStack.ContainsKey("WernherChecker_partSelection"))
            {
                InputLockManager.RemoveControlLock("WernherChecker_partSelection");
                selectionInProgress = false;
            }
        }


        void OnGUI() //drawing GUI Window
        {   
            mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            if (!minimized)
                mainWindow = GUILayout.Window(52, mainWindow, OnWindow, "WernherChecker v0.3", windowStyle);
            mainWindow.x = Mathf.Clamp(mainWindow.x, 0, Screen.width - mainWindow.width);
            mainWindow.y = Mathf.Clamp(mainWindow.y, 0, Screen.height - mainWindow.height);
            if (Settings.checkCrewAssignment)
                CrewCheck.CheckLaunchButton(mousePos);
            if (partSelection != null && selectionInProgress)
                partSelection.Update(mousePos);
        }

        public void SelectChecklist()
        {
            foreach (ConfigNode node in Settings.cfg.GetNodes("CHECKLIST"))
            {
                if (GUILayout.Button(node.GetValue("name"), buttonStyle))
                {
                    LoadChecklist(node);
                    checklistSelected = true;
                }
                windowHeight += windowButtonHeight + windowMargin;
            }
            GUILayout.Label("You can create your own checklist in the config file.", labelStyle);
            windowHeight += windowLabelHeight + 15;
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
                if (EditorLogic.startPod != null)
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

        void OnWindow(int WindowID)
        {
            windowHeight = windowBaseHeight;
            GUILayout.BeginVertical( GUILayout.Width(225));

            if (System.IO.File.Exists(DataPath + "WernherChecker.cfg"))
            {
                if (checklistSelected)
                {
                    if (!selectionInProgress)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Current checklist:");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(currentChecklistName, labelStyle);
                        GUILayout.EndHorizontal();
                        windowHeight += windowLabelHeight + windowMargin;

                        GUILayout.BeginVertical(boxStyle);
                        DrawListItems();

                        if (Settings.j_sugg == true)
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
                        if (showAdvanced)
                        {
                            if (!selectionInProgress)
                            {
                                GUILayout.Label("Check area:", labelStyle);
                                windowHeight += windowLabelHeight + windowMargin;
                                if (GUILayout.Toggle(!checkSelected, "Entire ship", toggleStyle))
                                    checkSelected = false;
                                if (GUILayout.Toggle(checkSelected, "Selected parts", toggleStyle))
                                    checkSelected = true;

                                windowHeight += 2 * windowToggleHeight + windowMargin;

                                if (checkSelected && EditorLogic.startPod != null)
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

            if (Settings.lockOnHover)
            {
                if (mainWindow.Contains(mousePos) && !InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Lock(true, true, true, "WernherChecker_windowLock");
                else if (!mainWindow.Contains(mousePos) && InputLockManager.lockStack.ContainsKey("WernherChecker_windowLock"))
                    EditorLogic.fetch.Unlock("WernherChecker_windowLock");
            }
        }
    }
}

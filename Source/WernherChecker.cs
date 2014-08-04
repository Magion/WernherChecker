/*
 * License: The MIT License (MIT)
 * Version: v0.2
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
        float windowBaseHeight = 34.9f;
        float windowElementHeight = 30.7f;
        float windowHeight;
        
        bool checklistSelected = false;
        public static bool minimized = false;       
        static bool editorLocked = false;
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

        static string[] items;
        static string[] modules;

        // GUI Styles
        public static GUIStyle windowStyle = new GUIStyle(HighLogic.Skin.window);
        public static GUIStyle buttonStyle = new GUIStyle(HighLogic.Skin.button);
        public static GUIStyle toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
        public static GUIStyle labelStyle = new GUIStyle(HighLogic.Skin.label);             

        
        public void Start()
        {
            Debug.LogWarning("WernherChecker v0.2 has been loaded");

            Settings.Load();
            if (Settings.cfgFound)
                RenderingManager.AddToPostDrawQueue(8, Draw_SelectChecklist);

            if (Settings.activeToolbar == toolbarType.BLIZZY && ToolbarManager.ToolbarAvailable)
            {
                wcbutton = ToolbarManager.Instance.add("WernherChecker", "wcbutton"); //creating toolbar button
                wcbutton.TexturePath = "WernherChecker/Data/icon_24";
                wcbutton.ToolTip = "WernherChecker";
                wcbutton.OnClick += (e) => minimized = !minimized;
            }

            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
            }  
        }

        void onGUIApplicationLauncherReady()
        {
            appButton = ApplicationLauncher.Instance.AddModApplication(miniOff, miniOn, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture)GameDatabase.Instance.GetTexture("WernherChecker/Data/icon",false));
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
            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIApplicationLauncherReady);
            if (Settings.activeToolbar == toolbarType.STOCK)
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
            else if(Settings.activeToolbar == toolbarType.BLIZZY)
                wcbutton.Destroy();
            if (InputLockManager.lockStack.ContainsKey("WernherChecker_selectChecklist"))
                EditorLogic.fetch.Unlock("WernherChecker_selectChecklist");
        }

        public void Draw_SelectChecklist()
        {
            selectWindow = GUILayout.Window(51, selectWindow, Window_SelectChecklist, "Please select checklist", windowStyle);
        }

        void OnGUI() //drawing GUI Window
        {   
            mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            if (!minimized)
                mainWindow = GUILayout.Window(52, mainWindow, OnWindow, "WernherChecker v0.2", windowStyle);
            mainWindow.x = Mathf.Clamp(mainWindow.x, 0, Screen.width - mainWindow.width);
            mainWindow.y = Mathf.Clamp(mainWindow.y, 0, Screen.height - mainWindow.height);
            if (Settings.checkCrewAssignment)
                CrewCheck.CheckLaunchButton(mousePos);
            
        }

        public void Window_SelectChecklist(int WindowID)
        {
            EditorLogic.fetch.Lock(false, false, false, "WernherChecker_selectChecklist");
            itemModules.Clear();
            GUILayout.BeginVertical(GUILayout.Width(150));
            foreach (ConfigNode node in Settings.cfg.GetNodes("CHECKLIST"))
            {
                if (GUILayout.Button(node.GetValue("name"), buttonStyle))
                {
                    print("[WernherChecker]: Selected " + '"' + node.GetValue("name") + '"' + " checklist");
                    items = node.GetValue("items").Split(',');
                    modules = node.GetValue("modules").Trim().Split(',');                    
                    RenderingManager.RemoveFromPostDrawQueue(6, Draw_SelectChecklist);
                    EditorLogic.fetch.Unlock("WernherChecker_selectChecklist");
                    //----------------------------------------------------------------------------
                    foreach (string item in items)
                    {
                        if (item != "" && item != null)
                        {
                            int index = Array.IndexOf(items, item);
                            List<string> mods = new List<string>();
                            do
                            {
                                mods.Add(modules[index]);
                                index++;
                                if (index >= items.Length) break;
                            }
                            while (items[index] == "");
                            itemModules.Add(item, mods);
                        }
                    }
                    checklistSelected = true;
                    print("[WernherChecker]: Checklist loading completed");
                    //--------------------------------------------------------------------------
                }
            }
            GUILayout.EndVertical();            
        }

        public void DrawListItems()//int item)
        {
            foreach (string item in itemModules.Keys)
            {
                bool hasIt = false;
                if (EditorLogic.startPod != null)
                {
                    foreach (string module in itemModules[item])
                    {
                        foreach (Part part in EditorLogic.SortedShipList)
                        {
                            if (part.Modules.Contains(module)) hasIt = true;
                        }
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(item, labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Toggle(hasIt, "", toggleStyle);
                GUILayout.EndHorizontal();
                windowHeight += windowElementHeight;

            }
        }

        void OnWindow(int WindowID)
        {
            windowHeight = windowBaseHeight;
            GUILayout.BeginVertical(GUILayout.Width(200), GUILayout.ExpandHeight(true));

            if (System.IO.File.Exists(DataPath + "WernherChecker.cfg"))
            {
                if (checklistSelected)
                {
                    DrawListItems();

                    if (Settings.j_sugg == true)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MOAR BOOSTERS!!!", labelStyle); //small joke :P
                        GUILayout.FlexibleSpace();
                        GUILayout.Toggle(false, "", toggleStyle);
                        GUILayout.EndHorizontal();
                        windowHeight += windowElementHeight;
                    }

                    if (GUILayout.Button("Change checklist", buttonStyle, GUILayout.Height(20)))
                    {
                        RenderingManager.AddToPostDrawQueue(5, Draw_SelectChecklist);
                        checklistSelected = false;
                    }
                    windowHeight += 28f;
                }
                else
                {
                    GUILayout.Label("Please select checklist");
                    windowHeight += windowElementHeight;
                }
            }

            else
            {
                GUILayout.Label("Cannot find config file!");
                windowHeight += windowElementHeight;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(); //making it dragable
            mainWindow.height = windowHeight;

            if (Settings.lockOnHover)
            {     
                if (mainWindow.Contains(mousePos) && !editorLocked) { EditorLogic.fetch.Lock(true, true, true, "WernherChecker_windowLock"); editorLocked = true; }
                else if (!mainWindow.Contains(mousePos) && editorLocked) { EditorLogic.fetch.Unlock("WernherChecker_windowLock"); editorLocked = false; }
            }
        }
    }
}

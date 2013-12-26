using System;
using System.Collections.Generic;
using UnityEngine;

namespace WernherChecker
{   
    [KSPAddon(KSPAddon.Startup.EditorVAB, false)]
    public class WernherChecker : MonoBehaviour
    {         
        private static Rect Window = new Rect(EditorPanels.Instance.partsPanelWidth + 5, 40, 200, 250);

        // GUI Styles
        private static GUIStyle windowStyle = new GUIStyle(HighLogic.Skin.window);
        private static GUIStyle buttonStyle = new GUIStyle(HighLogic.Skin.button);
        private static GUIStyle toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
        private static GUIStyle labelStyle = new GUIStyle(HighLogic.Skin.label);
        
        // bool variables
        public static bool hasControlSource = false;
        public static bool hasPowerSource = false;
        public static bool hasEngines = false;
        public static bool hasParachutes = false;
        public static bool hasAntennas = false;
        public static bool hasReactionWheels = false;
        public static bool hasScience = false;

        public void Start()
        {
            Debug.LogWarning("WernherChecker v0.1 has started");
            RenderingManager.AddToPostDrawQueue(0, DrawWindow); //adding window to RenderingManagers queue
        }

        public void Update()
        {
            hasControlSource = false;
            hasPowerSource = false;
            hasEngines = false;
            hasParachutes = false;
            hasAntennas = false;
            hasReactionWheels = false;
            hasScience = false;

            if (EditorLogic.startPod != null) //if root part selected (without this it is throwing Exceptions)
            {
                foreach (Part part in EditorLogic.SortedShipList) //check for modules on every part
                {
                    if (part.Modules.Contains("ModuleCommand")) //check for control source
                    {
                        hasControlSource = true;
                    }

                    if (part.Modules.Contains("ModuleDeployableSolarPanel") || part.Modules.Contains("ModuleGenerator")) //check for power source
                    {
                        hasPowerSource = true;
                    }

                    if (part.Modules.Contains("ModuleEngines")) //check for engines
                    {
                        hasEngines = true;
                    }
                   
                    if (part.Modules.Contains("ModuleParachute") || part.Modules.Contains("RealChuteModule")) //check for parachutes (even from stupid_chris!)
                    {
                        hasParachutes = true;
                    }

                    if (part.Modules.Contains("ModuleDataTransmitter")) //check for antennas
                    {
                        hasAntennas = true;
                    }

                    if (part.Modules.Contains("ModuleReactionWheel")) //check for reaction wheels
                    {
                        hasReactionWheels = true;
                    }

                    if (part.Modules.Contains("ModuleScienceExperiment")) //check for SCIENCE!
                    {
                        hasScience = true;
                    }
                }    
            }
        }

        public void DrawWindow() //drawing GUI Window
        { 
            Window = GUI.Window(1, Window, OnWindow, "WernherChecker v0.1", windowStyle);
        }

        public static void OnWindow(int WindowID) //Displaying content
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Control Source", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasControlSource, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Reaction Wheels", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasReactionWheels, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Source", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasPowerSource, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Engines", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasEngines, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Parachutes", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasParachutes, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Communication", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasAntennas, "", toggleStyle);
            GUILayout.EndHorizontal();           

            GUILayout.BeginHorizontal();
            GUILayout.Label("SCIENCE!", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(hasScience, "", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow(); //make it dragable
        }
    }
}

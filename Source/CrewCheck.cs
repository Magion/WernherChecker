using System;
using System.Collections.Generic;
using UnityEngine;

namespace WernherChecker
{
    class CrewCheck
    {
        static Rect crewWindow = new Rect(0, 0, 100, 50);
        static bool crewWindowDisplayed = false;
        static GUIStyle buttonStyle = new GUIStyle(HighLogic.Skin.button);
        static bool isManned = false;

        public static void OnButtonInput(ref POINTER_INFO ptr)
        {
            if (ptr.evt == POINTER_INFO.INPUT_EVENT.TAP)
            {
                isManned = false;
                foreach (Part part in EditorLogic.SortedShipList)
                {
                    if (part.CrewCapacity > 0)
                        isManned = true;
                }
                if (isManned)
                {
                    if (!crewWindowDisplayed)
                    {
                        Debug.Log("[WernherChecker] Displaying CrewCheck window");
                        RenderingManager.AddToPostDrawQueue(54, DrawWindow);
                        EditorLogic.fetch.Lock(true, true, true, "WernherChecker_crewCheck");
                        crewWindowDisplayed = true;
                    }
                }
                else
                    EditorLogic.fetch.launchVessel();   
            }
        }

        static void DrawWindow()
        {
            crewWindow = GUILayout.Window(54, crewWindow, Window, "Have you checked the crew assignment?", GUILayout.Width(300f));
            crewWindow.center = new Vector2(Screen.width / 2, Screen.height / 2);
        }

        static void Window(int WindowID)
        {
            if (GUILayout.Button("Yes, I have. Go for LAUNCH!", buttonStyle))
            {
                RenderingManager.RemoveFromPostDrawQueue(54, DrawWindow);
                crewWindowDisplayed = false;
                EditorLogic.fetch.Unlock("WernherChecker_crewCheck");
                Debug.Log("[WernherChecker] Launching vessel!");
                EditorLogic.fetch.launchVessel();
            }
            if (GUILayout.Button("No, I haven't! Thanks for the reminder!", buttonStyle))
            {
                RenderingManager.RemoveFromPostDrawQueue(54, DrawWindow);
                crewWindowDisplayed = false;
                EditorLogic.fetch.SelectPanelCrew();
                EditorLogic.fetch.Unlock("WernherChecker_crewCheck");
            }
        }
    }        
}

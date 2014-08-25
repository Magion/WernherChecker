using System;
using System.Collections.Generic;
using UnityEngine;

namespace WernherChecker
{
    class CrewCheck
    {
        static Rect launchBlock = new Rect(Screen.width - 97, 0, 50, 43);
        static Rect crewWindow = new Rect(0, 0, 100, 50);
        static bool crewWindowDisplayed = false;
        static bool launchBlocked;
        static GUIStyle buttonStyle = new GUIStyle(HighLogic.Skin.button);
        static bool isManned = false;
        static bool launchAllowed = true;
        

        public static void CheckLaunchButton(Vector2 mousePos)
        {
            if (EditorLogic.startPod != null)
            {
                isManned = false;
                foreach (Part part in EditorLogic.SortedShipList)
                {
                    if (part.CrewCapacity > 0)
                        isManned = true;
                }
            }

            foreach (string lockKey in InputLockManager.lockStack.Keys)
            {
                if (InputLockManager.GetControlLock(lockKey) == ControlTypes.EDITOR_LAUNCH && lockKey != "WernherChecker_launchBlock")
                    launchAllowed = false;
                else
                    launchAllowed = true;
            }

            if (launchBlock.Contains(mousePos) && EditorLogic.startPod != null && isManned && !InputLockManager.lockStack.ContainsKey("EditorLogic_launchSequence") && launchAllowed)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    if (!crewWindowDisplayed)
                    {
                        Debug.Log("[WernherChecker] Displayng CrewCheck window");
                        RenderingManager.AddToPostDrawQueue(54, DrawWindow);
                        crewWindowDisplayed = true;
                    }
                }

                if (!launchBlocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LAUNCH, "WernherChecker_launchBlock");
                    launchBlocked = true;
                }
            }

            if (!launchBlock.Contains(mousePos) && launchBlocked)
            {
                InputLockManager.RemoveControlLock("WernherChecker_launchBlock");
                launchBlocked = false;
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
                Debug.Log("[WernherChecker] Launching vessel!");
                EditorLogic.fetch.launchVessel();
            }
            if (GUILayout.Button("No, I haven't! Thanks for the reminder!", buttonStyle))
            {
                RenderingManager.RemoveFromPostDrawQueue(54, DrawWindow);
                crewWindowDisplayed = false;
                EditorLogic.fetch.SelectPanelCrew();
                WernherChecker.minimized = true;
            }
        }
    }
}

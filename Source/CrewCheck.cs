using System;
using System.Collections.Generic;
using UnityEngine;
using PreFlightTests;

namespace WernherChecker
{
    class CrewCheck : IPreFlightTest
    {
        static PreFlightCheck checks;

        public static void OnButtonInput(ref POINTER_INFO ptr)
        {
            if (ptr.evt == POINTER_INFO.INPUT_EVENT.TAP)
            {
                checks = new PreFlightCheck(Complete, Abort);
                checks.AddTest(new CrewCheck());
                checks.RunTests();
            }
        }
       
        public bool Test()
        {
            if (EditorLogic.fetch.editorScreen == EditorScreen.Crew)
                return true;

                foreach (Part part in EditorLogic.SortedShipList)
                {
                    if (part.CrewCapacity > 0)
                        return false;
                }
                return true;
        }

        public string GetWarningTitle()
        {
            return "Warning: Crew Assignment";
        }

        public string GetWarningDescription()
        {
            return "Have you checked the crew assignment?";
        }

        public string GetProceedOption()
        {
            return "Yes, I have. Go for LAUNCH!";
        }

        public string GetAbortOption()
        {
            return "No, I haven't! Thanks for the reminder!";
        }

        public static void Complete()
        {
            Debug.Log("[WernherChecker]: Crew is OK, launching vessel.");
            EditorLogic.fetch.launchVessel();
        }

        public static void Abort()
        {
            Debug.Log("[WernherChecker]: Showing crew panel.");
            EditorLogic.fetch.SelectPanelCrew();
        }
    }        
}

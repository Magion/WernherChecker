using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WernherChecker
{
    class WCSettings
    {
        public ConfigNode cfg = ConfigNode.Load(WernherChecker.DataPath + "WernherChecker.cfg");
        public bool j_sugg = true;
        public bool lockOnHover = true;
        public bool checkCrewAssignment = true;
        public bool cfgFound;
        public WernherChecker.toolbarType activeToolbar;

        public void Load()
        {
            if (System.IO.File.Exists(WernherChecker.DataPath + "WernherChecker.cfg"))
            {
                cfgFound = true;
                Debug.Log("[WernherChecker]: Config file found at " + WernherChecker.DataPath + "WernherChecker.cfg");
                //-----------------------------------------------------------------------------
                try
                {
                    this.j_sugg = bool.Parse(cfg.GetValue("j_sugg"));
                    Debug.Log("[WernherChecker]: SETTINGS - Jeb's suggestion enabled: " + this.j_sugg);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - j_sugg field has an invalid value assigned (" + cfg.GetValue("j_sugg") + "). Please assign valid boolean value."); }
                //------------------------------------------------------------------------------
                try
                {
                    this.lockOnHover = bool.Parse(cfg.GetValue("lockOnHover"));
                    Debug.Log("[WernherChecker]: SETTINGS - Lock editor while hovering over window: " + this.lockOnHover);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - lockOnHover field has an invalid value assigned (" + cfg.GetValue("lockOnHover") + "). Please assign valid boolean value."); }
                //----------------------------------------------------------------------------
                try
                {
                    this.checkCrewAssignment = bool.Parse(cfg.GetValue("checkCrewAssignment"));
                    Debug.Log("[WernherChecker]: SETTINGS - Check crew assignment before launch: " + this.checkCrewAssignment);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - checkCrewAssignment field has an invalid value assigned (" + cfg.GetValue("checkCrewAssignment") + "). Please assign valid boolean value."); }
                //--------------------------------------------------------------------------
                try
                {
                    this.activeToolbar = (WernherChecker.toolbarType)Enum.Parse(typeof(WernherChecker.toolbarType), cfg.GetValue("toolbarType"));
                    Debug.Log("[WernherChecker]: SETTINGS - Active toolbar: " + this.activeToolbar.ToString());
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - toolbarType field has an invalid value assigned (" + cfg.GetValue("toolbarType") + "). Please assign valid value (BLIZZY / STOCK)."); }
            }

            else
            {
                Debug.LogWarning("[WernherChecker]: Missing config file!");
                cfgFound = false;
            }
        }
    }
}

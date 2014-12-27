using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WernherChecker
{
    class WCSettings
    {
        public ConfigNode cfg;
        public bool cfgLoaded = false;
        public bool lockOnHover = true;
        public bool checkCrewAssignment = true;
        public bool jebEnabled = true;
        public float windowX = EditorPanels.Instance.partsPanelWidth + 3;
        public float windowY = 120;
        public WernherChecker.toolbarType wantedToolbar;

        public bool Load()
        {
            Debug.Log("[WernherChecker]: ========= Loading Settings =========");
            if (CfgExists())
            {
                cfg = ConfigNode.Load(WernherChecker.DataPath + "WernherChecker.cfg");
                Debug.Log("[WernherChecker]: Config file found at " + WernherChecker.DataPath + "WernherChecker.cfg");
                //------------------------------------------------------------------------------
                try
                {
                    this.lockOnHover = bool.Parse(cfg.GetValue("lockOnHover"));
                    Debug.Log("[WernherChecker]: SETTINGS - Lock editor while hovering over the main window: " + this.lockOnHover);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - lockOnHover field has an invalid value assigned (" + cfg.GetValue("lockOnHover") + "). Please assign valid boolean value."); }
                //----------------------------------------------------------------------------
                try
                {
                    this.checkCrewAssignment = bool.Parse(cfg.GetValue("checkCrewAssignment"));
                    Debug.Log("[WernherChecker]: SETTINGS - Check crew assignment before launch: " + this.checkCrewAssignment);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - checkCrewAssignment field has an invalid value assigned (" + cfg.GetValue("checkCrewAssignment") + "). Please assign valid boolean value."); }
                //-----------------------------------------------------------------------------
                try
                {
                    this.jebEnabled = bool.Parse(cfg.GetValue("jebEnabled"));
                    Debug.Log("[WernherChecker]: SETTINGS - Jeb's advice enabled: " + this.jebEnabled);
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - jebEnabled field has an invalid value assigned (" + cfg.GetValue("jebEnabled") + "). Please assign valid boolean value."); }
                //--------------------------------------------------------------------------
                try
                {
                    this.wantedToolbar = (WernherChecker.toolbarType)Enum.Parse(typeof(WernherChecker.toolbarType), cfg.GetValue("toolbarType"));
                    Debug.Log("[WernherChecker]: SETTINGS - Active toolbar: " + this.wantedToolbar.ToString());
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - toolbarType field has an invalid value assigned (" + cfg.GetValue("toolbarType") + "). Please assign valid value (BLIZZY / STOCK)."); }
                //--------------------------------------------------------------------------
                try
                {
                    this.windowX = float.Parse(cfg.GetValue("windowX"));
                    Debug.Log("[WernherChecker]: SETTINGS - Window X: " + this.windowX.ToString());
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - windowX field value is unsupported or null."); }
                //--------------------------------------------------------------------------
                try
                {
                    this.windowY = float.Parse(cfg.GetValue("windowY"));
                    Debug.Log("[WernherChecker]: SETTINGS - Window Y: " + this.windowY.ToString());
                }
                catch { Debug.LogWarning("[WernherChecker]: SETTINGS - windowY field value is unsupported or null."); }

                cfgLoaded = true;
                return true;
            }

            else
            {
                Debug.LogWarning("[WernherChecker]: Missing config file!");
                return false;
            }
        }

        public void Save()
        {
            Debug.Log("[WernherChecker]: ========= Saving Settings =========");
            if (CfgExists() && cfgLoaded)
            {
                //cfg.SetValue("j_sugg", j_sugg.ToString());
                //cfg.SetValue("lockOnHover", lockOnHover.ToString());
                //cfg.SetValue("checkCrewAssignment", checkCrewAssignment.ToString());
                //cfg.SetValue("toolbarType", wantedToolbar.ToString());
                //--------------------------------------------------------------------------
                if(cfg.HasValue("windowX"))
                    cfg.SetValue("windowX", WernherChecker.mainWindow.x.ToString());
                else
                    cfg.AddValue("windowX", WernherChecker.mainWindow.x.ToString());
                //--------------------------------------------------------------------------
                if (cfg.HasValue("windowY"))
                    cfg.SetValue("windowY", WernherChecker.mainWindow.y.ToString());
                else
                    cfg.AddValue("windowY", WernherChecker.mainWindow.y.ToString());
                //--------------------------------------------------------------------------
                cfg.Save(WernherChecker.DataPath + "WernherChecker.cfg");
            }

            else
                Debug.LogWarning("[WernherChecker]: Missing config file!");
        }

        public bool CfgExists()
        {
            return
                System.IO.File.Exists(WernherChecker.DataPath + "WernherChecker.cfg");
        }
    }
}

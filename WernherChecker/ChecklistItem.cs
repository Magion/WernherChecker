using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WernherChecker
{
    public class ChecklistItem
    {
        public WernherChecker MainInstance
        {
            get { return WernherChecker.Instance; }
        }
        public Checklist activeChecklist
        {
            get { return MainInstance.checklistSystem.ActiveChecklist; }
        }
        public ChecklistSystem checklistsInstance
        {
            get { return MainInstance.checklistSystem; }
        }
        //GUIStyle
        public GUIStyle labelStyle = new GUIStyle(HighLogic.Skin.label);
        public static GUIStyle checkboxStyle = new GUIStyle(HighLogic.Skin.toggle)
        {
            normal = HighLogic.Skin.toggle.hover,
            active = HighLogic.Skin.toggle.hover,
            //hover = HighLogic.Skin.toggle.normal,
            focused = HighLogic.Skin.toggle.hover,
            onActive = HighLogic.Skin.toggle.onNormal,
            onFocused = HighLogic.Skin.toggle.onNormal,
            onHover = HighLogic.Skin.toggle.onNormal
        };
        public static GUIStyle manualCheckboxStyle = new GUIStyle(HighLogic.Skin.toggle)
        {
            onNormal = HighLogic.Skin.toggle.onHover,
            onHover = HighLogic.Skin.toggle.onNormal
        };
        public static GUIStyle settingsButtonStyle = new GUIStyle(HighLogic.Skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(2, 2, 2, 2),
            fixedHeight = WernherChecker.buttonStyle.CalcSize(new GUIContent("abc")).y,
            fixedWidth = WernherChecker.buttonStyle.CalcSize(new GUIContent("abc")).y

        };


        public string name;
        public bool state;
        public bool isManual = false;
        public bool allRequired = true;
        public bool paramsDisplayed;
        public List<Criterion> criteria;

        public void DrawItem()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(isManual ? new GUIContent("<color=#0099ffff>" + name + "</color>", "Manually controlled item") : new GUIContent(name, "Criteria:\n" + string.Join("\n", criteria.Select(x => "<color=cyan><b>–</b></color> <i>" + x.valuesFull + " " + x.measure + "</i>").ToArray())), labelStyle);
            GUILayout.FlexibleSpace();
            if (criteria.Any(c => c.hasParameter))
            {
                if (GUILayout.Toggle(paramsDisplayed, new GUIContent(WernherChecker.settingsTexture, "Modify parameters"), settingsButtonStyle) != paramsDisplayed)
                {
                    if (paramsDisplayed)
                    {
                        activeChecklist.items.ForEach(p => p.paramsDisplayed = false);
                    }
                    else
                    {
                        activeChecklist.items.ForEach(p => p.paramsDisplayed = false);
                        activeChecklist.items.ForEach(p => p.criteria.ForEach(c => c.tempParam = c.parameter));
                        paramsDisplayed = true;
                    }
                    checklistsInstance.paramsWindow.height = 0f;
                }
                if (paramsDisplayed)
                {
                    if (Event.current.type == EventType.Repaint)
                        checklistsInstance.paramsWindow.y = MainInstance.mainWindow.y + GUILayoutUtility.GetLastRect().y - 28;
                    checklistsInstance.paramsWindow.x = MainInstance.mainWindow.x - checklistsInstance.paramsWindow.width;

                    //checklistsInstance.paramsWindow = GUILayout.Window(3, checklistsInstance.paramsWindow, checklistsInstance.DrawParamsWindow, "Edit Parameters          >", HighLogic.Skin.window);
                }
            }
            if (!isManual)
                GUILayout.Toggle(state, "", checkboxStyle);
            else
                state = GUILayout.Toggle(state, "", manualCheckboxStyle);
            GUILayout.EndHorizontal();
        }


    }
}

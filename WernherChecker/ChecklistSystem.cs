using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WernherChecker
{
    public class ChecklistSystem
    {
        public WernherChecker MainInstance
        {
            get { return WernherChecker.Instance; }
        }
        public Checklist ActiveChecklist
        {
            get
            {
                if (activeChecklist == null)
                    return new Checklist();
                else
                    return activeChecklist;
            }
            set { activeChecklist = value; }
        }

        public List<Checklist> availableChecklists = new List<Checklist>();
        Checklist activeChecklist;
        public Rect paramsWindow = new Rect(0, 0, 200, 0);
        public List<Part> partsToCheck;

        //GUIStyles
        public static GUIStyle normalLabel = new GUIStyle(HighLogic.Skin.label);
        public static GUIStyle orangeLabel = new GUIStyle(HighLogic.Skin.label) { normal = { textColor = new Color(1f, 0.5f, 0.2f) } };
        public static GUIStyle centredLabel = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleCenter };
        

        public bool LoadChecklists()
        {
            try
            {
                if (WernherChecker.Instance.Settings.cfgLoaded)
                {
                    availableChecklists.Clear();
                    foreach (ConfigNode checklistNode in WernherChecker.Instance.Settings.cfg.GetNodes("CHECKLIST"))
                    {
                        Debug.Log("[WernherChecker]: Parsing checklist - " + checklistNode.GetValue("name"));
                        Checklist parsedChecklist = new Checklist();
                        parsedChecklist.items = new List<ChecklistItem>();
                        parsedChecklist.name = checklistNode.GetValue("name");

                        ///Begining item cycle
                        foreach (ConfigNode itemNode in checklistNode.GetNodes("CHECKLIST_ITEM"))
                        {
                            //Debug.Log("parsing item " + itemNode.GetValue("name"));
                            ChecklistItem parsedItem = new ChecklistItem();
                            parsedItem.criteria = new List<Criterion>();
                            parsedItem.name = itemNode.GetValue("name");
                            if (!bool.TryParse(itemNode.GetValue("isManual"), out parsedItem.isManual))
                                parsedItem.isManual = false;
                            if(!bool.TryParse(itemNode.GetValue("allRequired"), out parsedItem.allRequired))
                                parsedItem.allRequired = true; ;

                            //Begining criterion cycle
                            foreach (ConfigNode criterionNode in itemNode.GetNodes("CRITERION"))
                            {
                                //Debug.Log("parsing criterion of type " + criterionNode.GetValue("type"));
                                Criterion parsedCriterion = new Criterion(criterionNode);
                                
                                parsedItem.criteria.Add(parsedCriterion);
                            }
                            parsedChecklist.items.Add(parsedItem);
                        }
                        availableChecklists.Add(parsedChecklist);
                    }
                }
                return true;
            }

            catch
            {
                Debug.LogWarning("[WernherChecker]: Error loading checklist. Please, check your cfg file.");
                return false;
            }
        }

        public void CheckVessel()
        {
            CheckVessel(EditorLogic.fetch.ship);
        }

        public void CheckVessel(ShipConstruct ship)
        {
            if (!MainInstance.checklistSelected)
                return;

            if (EditorLogic.RootPart == null || (MainInstance.partSelection == null && MainInstance.checkSelected))
            {
                ActiveChecklist.items.ForEach(i => i.state = false);
                return;
            }

            if (MainInstance.checkSelected && MainInstance.partSelection != null)
                partsToCheck = MainInstance.partSelection.selectedParts.Intersect(ship.Parts).ToList();
            else
                partsToCheck = ship.Parts;

            for (int j = 0; j < activeChecklist.items.Count; j++)
            //foreach (ChecklistItem item in activeChecklist.items)
            {
                ChecklistItem item = activeChecklist.items[j];
                if (item.isManual)
                    continue;
                item.state = true;
                for (int i = 0; i < item.criteria.Count; i++)
                //foreach(Criterion crton in item.criteria)
                {
                    Criterion crton = item.criteria[i];
                    switch (crton.type)
                    {
                        case CriterionType.Module:
                            crton.met = CheckForModules(crton);
                            break;
                        case CriterionType.Part:
                            crton.met = CheckForParts(crton);
                            break;
                        case CriterionType.MinResourceLevel:
                            crton.met = CheckForResourceLevel(crton);
                            break;
                        case CriterionType.MinResourceCapacity:
                            crton.met = CheckForResourceCapacity(crton);
                            break;
                        case CriterionType.CrewMember:
                            crton.met = CheckForCrewMember(crton);
                            break;
                    }
                    item.criteria[i] = crton;
                }
                if (!item.allRequired)
                {
                    if (item.criteria.TrueForAll(c => !c.met))
                        item.state = false;
                }
                else if (item.criteria.Any(c => !c.met))
                    item.state = false;

                activeChecklist.items[j] = item;
                continue;
            }
        }

        bool CheckForModules(Criterion crton)
        {
            int quantity = 0;
            foreach (string module in crton.modules)
            {
                quantity += partsToCheck.Where(p => p.Modules.Contains(module)).Count();
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForParts(Criterion crton)
        {
            int quantity = 0;
            foreach (string part in crton.parts)
            {
                quantity += partsToCheck.Where(p => p.name == part).Count();
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForResourceLevel(Criterion crton)
        {
            double quantity = 0;
            foreach (Part part in partsToCheck.Where(p => p.Resources.Contains(crton.resourceName)))
            {
                quantity += part.Resources.list.Find(r => r.resourceName == crton.resourceName).amount;
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForResourceCapacity(Criterion crton)
        {
            double quantity = 0;
            foreach (Part part in partsToCheck.Where(p => p.Resources.Contains(crton.resourceName)))
            {
                quantity += part.Resources.list.Find(r => r.resourceName == crton.resourceName).maxAmount;
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForCrewMember(Criterion crton)
        {
            try
            {
                foreach (PartCrewManifest part in CMAssignmentDialog.Instance.GetManifest().GetCrewableParts().Where(p => partsToCheck.Exists(pt => pt.partInfo == p.PartInfo)))
                {               
                    if (part.GetPartCrew().Where(c => c != null).Any(c => c.experienceTrait.Title == crton.experienceTrait && c.experienceLevel >= int.Parse(crton.parameter.ToString())))
                    {
                        //Debug.Log("Crew OK");
                        return true;
                    }
                }
                //Debug.Log("Crew KO");
                return false;
            }
            catch(Exception ex)
            {
                Debug.LogWarning("[WernherChecker]: Error checking crew:\n" + ex + "\n\n<b><color=lime>Please note, that this can sometimes happen after entering the editor and attaching the part for the first time.</color> <color=#ff4444ff>If this is not the case, please, report it.</color></b>");
                return false;
            }
        }

        public void DrawParamsWindow(int WindowID)
        {
            ChecklistItem item = activeChecklist.items.Find(p => p.paramsDisplayed);
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            GUILayout.Label("Item: " + item.name + "\n<color=#ffd333ff>" + (item.allRequired ? "All criteria met required" : "One criterion met suffices") + "</color>");
            GUILayout.FlexibleSpace();
            GUILayout.Label("►");
            GUILayout.EndHorizontal();
            item.criteria.ForEach(c => c.tempParam = c.paramsGUIFunction(c));

            if (item.criteria.TrueForAll(c => c.paramValid))
            {
                if (GUILayout.Button("Done", HighLogic.Skin.button))
                {
                    item.paramsDisplayed = false;
                    item.criteria.ForEach(c => c.parameter = c.tempParam);
                    CheckVessel();
                }
            }
            else
            {
                GUILayout.Label("<color=#FF1111FF><b> ! Some paramaters are invalid !</b></color>", centredLabel);
            }
            activeChecklist.items[activeChecklist.items.IndexOf(item)] = item;
            GUILayout.EndVertical();
            MainInstance.SetTooltipText();
        }

        public static object ParamsTextField(Criterion crton)
        {
            int i;
            if (int.TryParse(crton.tempParam.ToString(), out i))
                crton.paramValid = true;
            else
                crton.paramValid = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(crton.valuesShortened + " " + crton.measure, crton.tooltip), crton.paramValid ? normalLabel : orangeLabel);
            GUILayout.FlexibleSpace();
            crton.tempParam = GUILayout.TextField(crton.tempParam.ToString(), 11, HighLogic.Skin.textField, GUILayout.Width(68f));
            GUILayout.EndHorizontal();
            
            return crton.tempParam;
        }

        /*public static object ParamsContractSelect(Criterion crton)
        {
            if (Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().Count() == 0)
                return null;

            GUILayout.Label(crton.type.ToString(), normalLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().First<Contracts.Contract>().Title, HighLogic.Skin.button))
            {
                //GUILayout.BeginArea(paramInspector, "Select Parameter", HighLogic.Skin.window);
                foreach (Contracts.Contract contract in Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().Where(c => c.AllParameters.Any(p => p.GetType() == typeof(Contracts.Parameters.PartTest) || p.GetType() == typeof(FinePrint.Contracts.Parameters.CrewCapacityParameter) || p.GetType() == typeof(FinePrint.Contracts.Parameters.PartRequestParameter))))
                    Debug.Log(contract.Title);
            }
            return crton.tempParam;
        }*/
    }
}

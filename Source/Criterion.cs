using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WernherChecker
{
    public enum CriterionType
    {
        Module,
        MinResourceLevel,
        MinResourceCapacity,
        Part,
        CrewMember,
        ContractRequirements
    }

    public class Criterion
    {

        public WernherChecker MainInstance
        {
            get { return WernherChecker.Instance; }
        }
        public Checklist activeChecklist
        {
            get { return MainInstance.checklistSystem.ActiveChecklist; }
        }

        public CriterionType type;
        public bool met;
        public Func<Criterion, object> paramsGUIFunction;
        public object parameter;
        public object tempParam;
        public Type parameterType;
        public bool hasParameter;
        public string parameterText;
        public bool paramValid;
        public string tooltip;
        public string valuesString;
        public string resourceName;
        public List<string> parts;
        public List<string> modules;
        public string experienceTrait;

        public Criterion(ConfigNode node)
        {
            this.type = (CriterionType)Enum.Parse(typeof(CriterionType), node.GetValue("type"));
            if (type == CriterionType.Part || type == CriterionType.Module || type == CriterionType.MinResourceLevel || type == CriterionType.MinResourceCapacity || type == CriterionType.CrewMember)
            {
                paramsGUIFunction = ChecklistSystem.ParamsTextField;
                parameter = 1;
                tempParam = 1;
                parameterType = typeof(int);
                hasParameter = true;
            }

            if (node.HasValue("defaultParameter"))
            {
                int i;
                if (int.TryParse(node.GetValue("defaultParameter"), out i))
                {
                    this.parameter = i;
                    this.tempParam = this.parameter;
                }
            }

            switch (this.type)
            {
                case CriterionType.Module:
                    this.modules = node.GetValue("modules").Trim().Split(',').ToList<string>();
                    this.valuesString = string.Join(", ", this.modules.ToArray());
                    this.parameterText = this.modules.First() + (this.modules.Count == 1 ? string.Empty : ",...") + " QTY";
                    this.tooltip = "How many of " + this.valuesString + " should your vessel contain";
                    break;

                case CriterionType.Part:
                    this.parts = node.GetValue("parts").Trim().Split(',').ToList<string>();
                    this.valuesString = string.Join(", ", this.parts.ToArray());
                    this.parameterText = this.parts.First() + (this.parts.Count == 1 ? string.Empty : ",...") + " QTY";
                    this.tooltip = "How many of " + this.valuesString + " should your vessel contain";
                    break;

                case CriterionType.MinResourceLevel:
                    this.resourceName = node.GetValue("resourceName");
                    this.valuesString = this.resourceName;
                    this.parameterText = this.resourceName + " AMT";
                    this.tooltip = "How much of " + this.valuesString + " should your vessel contain";
                    break;
                case CriterionType.MinResourceCapacity:
                    this.resourceName = node.GetValue("resourceName");
                    this.valuesString = this.resourceName;
                    this.parameterText = this.resourceName + " CAPY";
                    this.tooltip = "How much of " + this.valuesString + " should your vessel has capacity for";
                    break;
                case CriterionType.CrewMember:
                    this.experienceTrait = node.GetValue("experienceTrait");
                    this.valuesString = this.experienceTrait;
                    this.parameterText = this.experienceTrait + " LVL";
                    this.tooltip = "Minimum experience level of your " + this.valuesString;
                    break;
                case CriterionType.ContractRequirements:
                    break;
            }
        }

        public Criterion(CriterionType type)
        {
            this.type = type;
            if (type == CriterionType.Part || type == CriterionType.Module || type == CriterionType.MinResourceLevel || type == CriterionType.MinResourceCapacity || type == CriterionType.CrewMember)
            {
                paramsGUIFunction = ChecklistSystem.ParamsTextField;
                parameter = 1;
                tempParam = 1;
                parameterType = typeof(int);
                hasParameter = true;
            }
            /*switch (type)
            {
                case CriterionType.CrewMember:
                    parameterText = experienceTrait + " LVL";
                    break;
                case CriterionType.MinResourceCapacity:
                    parameterText = resourceName + " CAPY";
                    break;
                case CriterionType.MinResourceLevel:
                    parameterText = resourceName + " AMT";
                    break;
                case CriterionType.Module:
                    parameterText = modules.First() + ",... QTY";
                    break;
                case CriterionType.Part:
                    parameterText = parts.First() + ",... QTY";
                    break;
            }*/

            /*else if (type == CriterionType.ContractRequirements)
            {
                parameterType = typeof(Contracts.Contract);
                hasParameter = true;
                paramsGUIAction = Checklists.ParamsContractSelect;
                parameter = Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().DefaultIfEmpty(new Contracts.Contract()).First();
                tempParam = parameter;

            }*/
        }
    }
}

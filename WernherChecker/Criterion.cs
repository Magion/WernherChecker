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
        public string valuesShortened;
        public string measure;
        public bool paramValid;
        public string tooltip;
        public string valuesFull;
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
                    this.measure = "QTY";
                    this.valuesFull = string.Join(", ", this.modules.ToArray());
                    this.valuesShortened = this.modules.First() + (this.modules.Count == 1 ? string.Empty : ",...");
                    this.tooltip = "How many of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
                    break;

                case CriterionType.Part:
                    this.parts = node.GetValue("parts").Trim().Split(',').ToList<string>();
                    this.measure = "QTY";
                    this.valuesFull = string.Join(", ", this.parts.ToArray());
                    this.valuesShortened = this.parts.First() + (this.parts.Count == 1 ? string.Empty : ",...");
                    this.tooltip = "How many of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
                    break;

                case CriterionType.MinResourceLevel:
                    this.resourceName = node.GetValue("resourceName");
                    this.measure = "AMT";
                    this.valuesFull = this.resourceName;
                    this.valuesShortened = this.resourceName;
                    this.tooltip = "How much of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
                    break;
                case CriterionType.MinResourceCapacity:
                    this.resourceName = node.GetValue("resourceName");
                    this.measure = "CAPY";
                    this.valuesFull = this.resourceName;
                    this.valuesShortened = this.resourceName;
                    this.tooltip = "How much of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel has capacity for";
                    break;
                case CriterionType.CrewMember:
                    this.experienceTrait = node.GetValue("experienceTrait");
                    this.measure = "LVL";
                    this.valuesFull = this.experienceTrait;
                    this.valuesShortened = this.experienceTrait;
                    this.tooltip = "Minimum experience level of your <b><color=#90FF3E>" + this.valuesFull + "</color></b>";
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

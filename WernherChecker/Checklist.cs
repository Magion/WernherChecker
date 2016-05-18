using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WernherChecker
{
    public class Checklist
    {
        public string name = "";
        public List<ChecklistItem> items = new List<ChecklistItem>();

        public WernherChecker MainInstance
        {
            get { return WernherChecker.Instance; }
        }

    }
}

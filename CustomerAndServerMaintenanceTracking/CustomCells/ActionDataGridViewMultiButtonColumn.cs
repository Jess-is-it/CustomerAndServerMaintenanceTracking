using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerAndServerMaintenanceTracking.CustomCells
{
    public class ActionDataGridViewMultiButtonColumn : DataGridViewColumn
    {
        public ActionDataGridViewMultiButtonColumn() : base(new ActionDataGridViewMultiButtonCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                if (value != null && !value.GetType().IsAssignableFrom(typeof(ActionDataGridViewMultiButtonCell)))
                {
                    throw new System.InvalidCastException("Must be a ActionDataGridViewMultiButtonCell");
                }
                base.CellTemplate = value;
            }
        }
    }
}

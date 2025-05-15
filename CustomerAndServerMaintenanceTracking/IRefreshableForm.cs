using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking
{
    public interface IRefreshableForm
    {
        void RefreshDataViews(); // This method will be called to refresh the form's data
    }

}

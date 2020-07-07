using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Report.PlugIn;
using Kingdee.BOS.Util;

namespace Para.App.Report.POExecuteRpt
{
    [HotUpdate]
    [Description("采购订单执行情况表客户端插件")]
    public class POExecuteRptEdit : AbstractSysReportPlugIn
    {
        public override void CellDbClick(Kingdee.BOS.Core.Report.PlugIn.Args.CellEventArgs Args)
        {
            base.CellDbClick(Args);
            //采购订单号
            if (Args.Header.FieldName.EqualsIgnoreCase("FPurNo"))
            {
                BillShowParameter parameter = new BillShowParameter();
                parameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
                parameter.FormId = "PUR_PurchaseOrder";
                parameter.PKey = this.SysReportView.SelectedDataRows[0]["FPurId"].ToString();                
                parameter.Status = OperationStatus.VIEW;
                this.View.ShowForm(parameter);
            }
            //销售订单号
            if (Args.Header.FieldName.EqualsIgnoreCase("FSaleNo"))
            {
                BillShowParameter parameter = new BillShowParameter();
                parameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
                parameter.FormId = "SAL_SaleOrder";
                parameter.PKey = this.SysReportView.SelectedDataRows[0]["FSaleOrderId"].ToString();
                parameter.Status = OperationStatus.VIEW;
                this.View.ShowForm(parameter);
            }
        }
    }
}

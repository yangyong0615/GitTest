using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.CommonFilter.PlugIn;

namespace Para.App.Report.AP_MaturedDebt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("到期债务表—过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            //开始日期（审核）
            //this.View.Model.SetValue("FBeginAuditDate_Filter", DateTime.Today.AddYears(-1));
        }
    }
}

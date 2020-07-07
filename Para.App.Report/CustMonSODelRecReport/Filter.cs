using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Para.App.Report.CustMonSODelRecReport
{
    [HotUpdate]
    [Description("客户业务往来月度表 - 过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //开始日期
            DateTime beginDate = new DateTime(DateTime.Today.AddMonths(-1).Year, 1, 1);
            this.View.Model.SetValue("FBeginDate_F", beginDate);
            //结束日期
            DateTime endDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 1);
            this.View.Model.SetValue("FEndDate_F", endDate);
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "客户业务往来月度表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
    }
}

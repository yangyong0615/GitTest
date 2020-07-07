using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Para.App.Report.CustContactAnalyzeReport
{
    [HotUpdate]
    [Description("客户业务往来分析表 - 过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //年度
            this.View.Model.SetValue("FYear_F", DateTime.Today.AddMonths(-1).Year);
            //月度
            this.View.Model.SetValue("FMonth_F", DateTime.Today.AddMonths(-1).Month);
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "客户业务往来分析表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
    }
}

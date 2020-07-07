using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Util;
using System.IO;

namespace Para.App.Report.ProfitTrackRpt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("出货利润跟踪表—过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //按照查询方式设置默认值
            this.SetDefaultValueByQueryStyle();
        }
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("FQueryStyle_Filter"))
            {
                //按照查询方式设置默认值
                this.SetDefaultValueByQueryStyle();
            }
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "出货利润跟踪表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
        //按照查询方式设置默认值
        private void SetDefaultValueByQueryStyle()
        {
            //查询方式
            string queryStyle = Convert.ToString(this.View.Model.GetValue("FQueryStyle_Filter"));
            //查询方式 = 按期间查询
            if (queryStyle == "1")
            {
                //设置默认值为上个月
                int year = DateTime.Today.AddMonths(-1).Year;
                int month = DateTime.Today.AddMonths(-1).Month;
                this.View.Model.SetValue("FYear_Filter", year);
                this.View.Model.SetValue("FMonth_Filter", month);
            }
            //查询方式 = 按日期查询
            else if (queryStyle == "2")
            {
                //上个月1号
                DateTime beginDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 1);
                this.View.Model.SetValue("FBeginDate_Filter", beginDate);
            }
        }
    }
}

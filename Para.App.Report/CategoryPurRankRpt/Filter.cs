using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;


namespace Para.App.Report.CategoryPurRankRpt
{
    [HotUpdate]
    [Description("品类采购排名表—过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            //多选组织
            ComboFieldEditor headComboEidtor = this.View.GetControl<ComboFieldEditor>("FMulSelOrgList_Filter");
            List<EnumItem> comboOptions = new List<EnumItem>();
            //获取有查看权限的组织
            List<long> orgIdList = this.GetOrg();
            foreach (long orgId in orgIdList)
            {
                comboOptions.Add(new EnumItem() { EnumId = orgId.ToString(), Value = orgId.ToString(), Caption = new LocaleValue(this.GetOrgName(orgId)) });
            }
            headComboEidtor.SetComboItems(comboOptions);
            //设置默认组织：高山，阳普生
            this.View.Model.SetValue("FMulSelOrgList_Filter", "1,100246");
            //设置起始日期：默认上个月1号
            DateTime beginDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 1);
            this.View.Model.SetValue("FBeginDate_Filter", beginDate);
            //获取上个月有多少天
            int monthDay = DateTime.DaysInMonth(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month);
            //设置结束日期：默认上个月最后一天
            DateTime endDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, monthDay);
            this.View.Model.SetValue("FEndDate_Filter", endDate);
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "品类采购排名表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
        //获取组织列表
        private List<long> GetOrg()
        {
            List<long> orgIds = new List<long>();
            string sql = "SELECT FORGID FROM T_ORG_ORGANIZATIONS WHERE FDOCUMENTSTATUS = 'C'";
            DynamicObjectCollection col = DBUtils.ExecuteDynamicObject(this.Context, sql);
            foreach (DynamicObject obj in col)
            {
                orgIds.Add(Convert.ToInt64(obj["FORGID"]));
            }
            return orgIds;
        }
        //获取组织名称
        private string GetOrgName(long orgId)
        {
            string sql = string.Format("SELECT FNAME FROM T_ORG_ORGANIZATIONS_L WHERE FLOCALEID = '2052' AND FORGID = '{0}'", orgId);
            return DBUtils.ExecuteScalar<string>(this.Context, sql, string.Empty, new Kingdee.BOS.SqlParam[0]);
        }
    }
}

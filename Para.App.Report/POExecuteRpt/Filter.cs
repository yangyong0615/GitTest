using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Util;

namespace Para.App.Report.POExecuteRpt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("采购订单执行情况表—过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            //多选组织
            ComboFieldEditor headComboEidtor = this.View.GetControl<ComboFieldEditor>("FMulSelOrgList_Filter");
            List<EnumItem> comboOptions = new List<EnumItem>();
            //获取有查看权限的组织
            List<long> orgIdList = this.GetPremissionOrg("PAWK_PurPayDetailReport");
            foreach (long orgId in orgIdList)
            {
                comboOptions.Add(new EnumItem() { EnumId = orgId.ToString(), Value = orgId.ToString(), Caption = new LocaleValue(this.GetOrgName(orgId)) });
            }
            headComboEidtor.SetComboItems(comboOptions);
            //设置当前多选组织为默认查询组织
            this.View.Model.SetValue("FMulSelOrgList_Filter", this.Context.CurrentOrganizationInfo.ID);
            //设置起始日期：默认本月1号
            DateTime beginDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            this.View.Model.SetValue("FBeginDate_Filter", beginDate);
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "采购订单执行情况表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
        //获取有查看权的组织列表
        private List<long> GetPremissionOrg(string formId)
        {
            BusinessObject bizObject = new BusinessObject()
            {
                //报表标识
                Id = formId,
                //报表权限控制选项：是否受权限控制
                PermissionControl = this.View.ParentFormView.BillBusinessInfo.GetForm().SupportPermissionControl,
                //报表所在子系统
                SubSystemId = this.View.ParentFormView.Model.SubSytemId
            };
            //获取授予了查看权的组织
            List<long> orgIds = Kingdee.BOS.ServiceHelper.PermissionServiceHelper.GetPermissionOrg(
                this.Context,
                bizObject,
                PermissionConst.View);
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

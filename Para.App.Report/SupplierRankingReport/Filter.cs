using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System.IO;

namespace Para.App.Report.SupplierRankingReport
{
    [HotUpdate]
    [Description("供应商排名表 - 过滤插件")]
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
            //设置默认组织：高山，阳普生
            this.View.Model.SetValue("FMulSelOrgList_Filter", "1,100246");
            //设置多选下拉列表_公司和事业部
            this.SetMulComboCompany();
            //设置多选下拉列表_部门
            this.SetMulComboDept();
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "供应商排名表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("FMulSelOrgList_Filter"))
            {
                //设置多选下拉列表_部门
                this.SetMulComboDept();
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
        //设置多选下拉列表_公司和事业部
        private void SetMulComboCompany()
        {
            //定义List<EnumItem>用于存储下拉列表枚举值；
            List<EnumItem> list = new List<EnumItem>();
            //外贸公司
            EnumItem item1 = new EnumItem();
            item1.Caption = new LocaleValue("外贸公司", base.Context.UserLocale.LCID);
            item1.EnumId = "外贸公司";
            item1.Value = "外贸公司";
            list.Add(item1);
            //知客
            EnumItem item2 = new EnumItem();
            item2.Caption = new LocaleValue("知客", base.Context.UserLocale.LCID);
            item2.EnumId = "知客";
            item2.Value = "知客";
            list.Add(item2);
            //千源
            EnumItem item3 = new EnumItem();
            item3.Caption = new LocaleValue("千源", base.Context.UserLocale.LCID);
            item3.EnumId = "千源";
            item3.Value = "千源";
            list.Add(item3);
            string sql = string.Format(@"   SELECT DISTINCT
	                                                DEP_L.FNAME
                                                FROM T_BD_DEPARTMENT DEP
                                                LEFT JOIN T_BD_DEPARTMENT_L DEP_L
                                                ON DEP.FDEPTID = DEP_L.FDEPTID AND DEP_L.FLOCALEID = '2052'
                                                WHERE DEP.FDOCUMENTSTATUS = 'C' AND DEP.FFORBIDSTATUS = 'A' AND DEP_L.FNAME LIKE '%事业部%'
                                                ORDER BY DEP_L.FNAME
                                                ");
            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (result != null && result.Count > 0)
            {
                foreach (DynamicObject obj in result)
                {
                    string name = Convert.ToString(obj["FNAME"]);
                    EnumItem item = new EnumItem();
                    item.Caption = new LocaleValue(name, base.Context.UserLocale.LCID);
                    item.EnumId = string.Format("{0}", name);
                    item.Value = string.Format("{0}", name);
                    list.Add(item);
                }
            }
            //SetComboItems绑定值
            this.View.GetControl<ComboFieldEditor>("FCompany_Filter").SetComboItems(list);
            this.View.Model.SetValue("FCompany_Filter", "外贸公司");
        }
        //设置多选下拉列表_部门
        private void SetMulComboDept()
        {
            //组织
            string orgId = Convert.ToString(this.View.Model.GetValue("FMulSelOrgList_Filter"));
            //定义List<EnumItem>用于存储下拉列表枚举值；
            List<EnumItem> list = new List<EnumItem>();
            if (!orgId.IsNullOrEmptyOrWhiteSpace())
            {
                string sql = string.Format(@"   SELECT DISTINCT
	                                                DEP_L.FNAME
                                                FROM T_BD_DEPARTMENT DEP
                                                LEFT JOIN T_BD_DEPARTMENT_L DEP_L
                                                ON DEP.FDEPTID = DEP_L.FDEPTID AND DEP_L.FLOCALEID = '2052'
                                                WHERE DEP.FDOCUMENTSTATUS = 'C' AND DEP.FFORBIDSTATUS = 'A'
                                                AND DEP.FUSEORGID IN ({0})
                                                ORDER BY DEP_L.FNAME
                                                ", orgId);
                DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (result != null && result.Count > 0)
                {
                    foreach (DynamicObject obj in result)
                    {
                        string name = Convert.ToString(obj["FNAME"]);
                        EnumItem item = new EnumItem();
                        item.Caption = new LocaleValue(name, base.Context.UserLocale.LCID);
                        item.EnumId = string.Format("\'{0}\'", name);
                        item.Value = string.Format("\'{0}\'", name);
                        list.Add(item);
                    }
                }
            }
            //SetComboItems绑定值
            this.View.GetControl<ComboFieldEditor>("FDeptItems_Filter").SetComboItems(list);
        }
    }
}

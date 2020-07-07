using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;

namespace Para.App.Report.PurPayDetailRpt
{
    [HotUpdate]
    [Description("采购付款明细表 - 过滤插件")]
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


            //设置组织机构
            if (orgIdList.Contains(this.Context.CurrentOrganizationInfo.ID))
            {
                this.View.Model.SetValue("FOrgId_Filter", this.Context.CurrentOrganizationInfo.ID);
            }
        }
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            #region 组织机构
            if (e.FieldKey.EqualsIgnoreCase("FOrgId_Filter"))
            {
                e.ListFilterParameter.Filter = string.Format("FORGID IN ({0})", this.GetOrgFilter());
            }
            #endregion
            #region 销售订单号
            if (e.FieldKey.EqualsIgnoreCase("FSaleOrderNo_Filter"))
            {
                //组织机构
                DynamicObject orgObj = this.View.Model.GetValue("FOrgId_Filter") as DynamicObject;
                long orgId = orgObj == null ? 0L : Convert.ToInt64(orgObj["Id"]);
                if (orgId == 0L)
                {
                    this.View.ShowErrMessage("请先选择组织机构！");
                    return;
                }
                ListShowParameter listpara = new ListShowParameter();
                //采购订单
                listpara.FormId = "SAL_SaleOrder";
                listpara.IsShowApproved = true;
                listpara.ParentPageId = this.View.PageId;
                listpara.IsIsolationOrg = false;
                listpara.MultiSelect = false;
                listpara.ListFilterParameter.Filter = string.Format("FDOCUMENTSTATUS = 'C' AND FSALEORGID = {0}", orgId.ToString());
                listpara.OpenStyle.CacheId = listpara.PageId;
                listpara.IsLookUp = true;
                this.View.ShowForm(listpara, new Action<FormResult>((result) =>
                {
                    object data = result.ReturnData as ListSelectedRowCollection;
                    if (data is ListSelectedRowCollection)
                    {
                        ListSelectedRowCollection listData = data as ListSelectedRowCollection;
                        if (listData != null && listData.Count > 0)
                        {
                            List<SelectorItemInfo> selector = new List<SelectorItemInfo>();
                            selector.Add(new SelectorItemInfo("FBillNo"));
                            string fid = listData[0].PrimaryKeyValue;
                            string filter = string.Empty;
                            if (!String.IsNullOrEmpty(fid))
                            {
                                filter = string.Format("fid={0}", fid);
                            }
                            QueryBuilderParemeter para = new QueryBuilderParemeter()
                            {
                                FormId = "SAL_SaleOrder",
                                FilterClauseWihtKey = filter,
                                SelectItems = selector
                            };
                            DynamicObjectCollection linkmanData = QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                            //销售订单号
                            this.View.Model.SetValue(e.FieldKey, Convert.ToString(linkmanData.FirstOrDefault()["FBillNo"]));
                        }
                    }
                }));
            }
            #endregion
            #region 采购订单号
            if (e.FieldKey.EqualsIgnoreCase("FPurOrderNo_Filter"))
            {
                //组织机构
                DynamicObject orgObj = this.View.Model.GetValue("FOrgId_Filter") as DynamicObject;
                long orgId = orgObj == null ? 0L : Convert.ToInt64(orgObj["Id"]);
                if (orgId == 0L)
                {
                    this.View.ShowErrMessage("请先选择组织机构！");
                    return;
                }
                ListShowParameter listpara = new ListShowParameter();
                //采购订单
                listpara.FormId = "PUR_PurchaseOrder";
                listpara.IsShowApproved = true;
                listpara.ParentPageId = this.View.PageId;
                listpara.MultiSelect = false;
                listpara.IsIsolationOrg = false;
                listpara.ListFilterParameter.Filter = string.Format("FDOCUMENTSTATUS = 'C' AND FPURCHASEORGID = {0}", orgId.ToString());
                listpara.OpenStyle.CacheId = listpara.PageId;
                listpara.IsLookUp = true;
                this.View.ShowForm(listpara, new Action<FormResult>((result) =>
                {
                    object data = result.ReturnData as ListSelectedRowCollection;
                    if (data is ListSelectedRowCollection)
                    {
                        ListSelectedRowCollection listData = data as ListSelectedRowCollection;
                        if (listData != null && listData.Count > 0)
                        {
                            List<SelectorItemInfo> selector = new List<SelectorItemInfo>();
                            selector.Add(new SelectorItemInfo("FBillNo"));
                            string fid = listData[0].PrimaryKeyValue;
                            string filter = string.Empty;
                            if (!String.IsNullOrEmpty(fid))
                            {
                                filter = string.Format("fid={0}", fid);
                            }
                            QueryBuilderParemeter para = new QueryBuilderParemeter()
                            {
                                FormId = "PUR_PurchaseOrder",
                                FilterClauseWihtKey = filter,
                                SelectItems = selector
                            };
                            DynamicObjectCollection linkmanData = QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                            //采购订单号
                            this.View.Model.SetValue(e.FieldKey, Convert.ToString(linkmanData.FirstOrDefault()["FBillNo"]));
                        }
                    }
                }));
            }
            #endregion
            #region 付款单号
            if (e.FieldKey.EqualsIgnoreCase("FPayNo_Filter"))
            {
                //组织机构
                DynamicObject orgObj = this.View.Model.GetValue("FOrgId_Filter") as DynamicObject;
                long orgId = orgObj == null ? 0L : Convert.ToInt64(orgObj["Id"]);
                if (orgId == 0L)
                {
                    this.View.ShowErrMessage("请先选择组织机构！");
                    return;
                }
                ListShowParameter listpara = new ListShowParameter();
                //付款单
                listpara.FormId = "AP_PAYBILL";
                listpara.IsShowApproved = true;
                listpara.ParentPageId = this.View.PageId;
                listpara.MultiSelect = false;
                listpara.IsIsolationOrg = false;
                listpara.ListFilterParameter.Filter = string.Format("FDOCUMENTSTATUS = 'C' AND FPAYORGID = {0}", orgId.ToString());
                listpara.OpenStyle.CacheId = listpara.PageId;
                listpara.IsLookUp = true;
                this.View.ShowForm(listpara, new Action<FormResult>((result) =>
                {
                    object data = result.ReturnData as ListSelectedRowCollection;
                    if (data is ListSelectedRowCollection)
                    {
                        ListSelectedRowCollection listData = data as ListSelectedRowCollection;
                        if (listData != null && listData.Count > 0)
                        {
                            List<SelectorItemInfo> selector = new List<SelectorItemInfo>();
                            selector.Add(new SelectorItemInfo("FBillNo"));
                            string fid = listData[0].PrimaryKeyValue;
                            string filter = string.Empty;
                            if (!String.IsNullOrEmpty(fid))
                            {
                                filter = string.Format("fid={0}", fid);
                            }
                            QueryBuilderParemeter para = new QueryBuilderParemeter()
                            {
                                FormId = "AP_PAYBILL",
                                FilterClauseWihtKey = filter,
                                SelectItems = selector
                            };
                            DynamicObjectCollection linkmanData = QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                            //付款单号
                            this.View.Model.SetValue(e.FieldKey, Convert.ToString(linkmanData.FirstOrDefault()["FBillNo"]));
                        }
                    }
                }));
            }
            #endregion
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "采购付款明细表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}",user,businessObj,time));
                sw.Close();
            }
        }
        //获取组织机构过滤
        private string GetOrgFilter()
        {
            //获取有查看权限的组织
            List<long> orgIdList = this.GetPremissionOrg("PAWK_PurPayDetailReport");
            if (orgIdList.Count > 0)
            {
                StringBuilder orgBuilder = new StringBuilder();
                foreach (long orgId in orgIdList)
                {
                    orgBuilder.AppendFormat("{0},", orgId.ToString());
                }
                return orgBuilder.ToString().TrimEnd(new char[] { ',' });
            }
            else
            {
                return string.Empty;
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

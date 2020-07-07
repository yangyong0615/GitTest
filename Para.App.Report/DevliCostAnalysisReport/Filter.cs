using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Para.App.Report.DevliCostAnalysisReport
{
    [HotUpdate]
    [Description("出货成本分析表 - 过滤插件")]
    public class Filter : AbstractCommonFilterPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //设置起始日期：默认上个月1号
            DateTime beginDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 1);
            this.View.Model.SetValue("FBeginDate_Filter", beginDate);
            //获取上个月有多少天
            int monthDay = DateTime.DaysInMonth(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month);
            //设置结束日期：默认上个月最后一天
            DateTime endDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, monthDay);
            this.View.Model.SetValue("FEndDate_Filter", endDate);
            //设置下拉列表_事业部
            this.SetComboBusDep();
            //设置多选下拉列表_业务部
            this.SetMulComBoDep();
            //设置权限项
            this.SetPermission();
        }
        public override void AfterButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            //【确定】
            if (e.Key.EqualsIgnoreCase("FBtnOK"))
            {
                string user = this.Context.UserName;
                string time = DateTime.Now.ToString();
                string businessObj = "出货成本分析表";
                //写入
                StreamWriter sw = new StreamWriter(@"E:\报表访问日志.txt", true);
                sw.WriteLine(string.Format("{0} {1} {2}", user, businessObj, time));
                sw.Close();
            }
        }
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("FBusDep_Filter"))
            {
                //设置多选下拉列表_业务部
                this.SetMulComBoDep();
            }
        }
        //设置下拉列表_事业部
        private void SetComboBusDep()
        {
            //定义List<EnumItem>用于存储下拉列表枚举值；
            List<EnumItem> list = new List<EnumItem>();
            //外贸公司
            EnumItem item1 = new EnumItem();
            item1.Caption = new LocaleValue("全部", base.Context.UserLocale.LCID);
            item1.EnumId = "全部";
            item1.Value = "全部";
            list.Add(item1);
            string sql = @" SELECT DISTINCT
                                DEP_L.FNAME
                            FROM T_BD_DEPARTMENT DEP
                            LEFT JOIN T_BD_DEPARTMENT_L DEP_L
                            ON DEP.FDEPTID = DEP_L.FDEPTID AND DEP_L.FLOCALEID = '2052'
                            LEFT JOIN T_ORG_ORGANIZATIONS ORG
                            ON ORG.FORGID = DEP.FUSEORGID
                            WHERE DEP.FDOCUMENTSTATUS = 'C' AND DEP.FFORBIDSTATUS = 'A' AND DEP_L.FNAME LIKE '%事业部%'
                            AND ORG.FNUMBER IN ('PM','EG')
                            ORDER BY DEP_L.FNAME";
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
            this.View.GetControl<ComboFieldEditor>("FBusDep_Filter").SetComboItems(list);
        }
        //设置多选下拉列表_业务部
        private void SetMulComBoDep()
        {
            //定义List<EnumItem>用于存储下拉列表枚举值；
            List<EnumItem> list = new List<EnumItem>();
            //事业部
            string busDep = Convert.ToString(this.View.Model.GetValue("FBusDep_Filter"));
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT DISTINCT	");
            sqlBuilder.AppendLine("	    DEP_L.FNAME	");
            sqlBuilder.AppendLine("	FROM T_BD_DEPARTMENT DEP	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP.FDEPTID = DEP_L.FDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = DEP.FUSEORGID	");
            sqlBuilder.AppendLine("	WHERE DEP.FDOCUMENTSTATUS = 'C' AND DEP.FFORBIDSTATUS = 'A'	");
            sqlBuilder.AppendLine("	AND ORG.FNUMBER IN ('PM','EG')	");
            sqlBuilder.AppendLine("	AND (DEP_L.FFULLNAME LIKE '%事业部%' OR DEP_L.FFULLNAME LIKE '%义乌办%')	");
            if (busDep != "全部")
            {
                sqlBuilder.AppendLine("	AND DEP_L.FFULLNAME LIKE '%" + busDep + "%'	");
            }
            sqlBuilder.AppendLine("	ORDER BY DEP_L.FNAME	");
            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sqlBuilder.ToString());
            if (result != null && result.Count > 0)
            {
                foreach (DynamicObject obj in result)
                {
                    string name = Convert.ToString(obj["FNAME"]);
                    EnumItem item = new EnumItem();
                    item.Caption = new LocaleValue(name, base.Context.UserLocale.LCID);
                    item.EnumId = string.Format("{0}", name);
                    item.Value = string.Format("'{0}'", name);
                    list.Add(item);
                }
            }
            //SetComboItems绑定值
            this.View.GetControl<ComboFieldEditor>("FDep_Filter").SetComboItems(list);
        }
        //权限设置
        private void SetPermission()
        {
            string userName = this.Context.UserName;
            if (userName.Contains("周文博")
                || userName.Contains("项前")
                || userName.Contains("沈鹰")
                || userName.Contains("周贤昌")
                || userName.Contains("谭静")
                || userName.Contains("杨永平")
                || userName.Contains("严吉德")
                || userName.Contains("张晓红")
                || userName.Contains("杨勇")
                )
            {
                //设置事业部默认值：全部
                this.View.Model.SetValue("FBusDep_Filter", "全部");
            }
            else if (userName.Contains("樊波"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "高山事业部");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
            }
            else if (userName.Contains("吴培立"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "伟景事业部");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
            }
            else if (userName.Contains("徐兵"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "长江事业部");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
            }
            else if (userName.Contains("秦建英"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "昆仑事业部");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
            }
            else if (userName.Contains("赵攀"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "长隆事业部");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
            }
            else if (userName.Contains("吴云龙"))
            {
                //设置事业部默认值
                this.View.Model.SetValue("FBusDep_Filter", "全部");
                //设置业务部
                this.View.Model.SetValue("FDep_Filter", "'义乌办'");
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
                this.View.GetControl("FDep_Filter").Enabled = false;
            }
            else
            {
                //锁定字段
                this.View.GetControl("FBusDep_Filter").Enabled = false;
                this.View.GetControl("FDep_Filter").Enabled = false;
                //锁定【确认】按钮
                this.View.GetControl("FBtnOK").Enabled = false;
            }
        }
    }
}

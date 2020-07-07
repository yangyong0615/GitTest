using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;

namespace Para.App.Report.CustomerDeductionReport
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("客户扣款表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //部门
        string depName = string.Empty;
        //组织机构
        string orgName = string.Empty;
        string orgId = string.Empty;
        //客户
        string custNum = string.Empty;
        string custName = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("客户扣款表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsUIDesignerColumns = true;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.SimpleAllCols = false;
            this.SetDecimalControl();
        }
        //设置精度
        private void SetDecimalControl()
        {
            List<DecimalControlField> list = new List<DecimalControlField>();
            //扣款金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            //扣款金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FAmtLC",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        //小计，合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            //扣款金额
            list.Add(new SummaryField("FAmt", BOSEnums.Enu_SummaryType.SUM));
            //扣款金额本位币
            list.Add(new SummaryField("FAmtLC", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT distinct	");
            sqlBuilder.AppendLine("		ORG_L.FNAME							FOrgName	--组织机构	");
            sqlBuilder.AppendLine("		,BILLTYPE_L.FNAME					FBillType	--单据类型	");
            sqlBuilder.AppendLine("		,OTHERPAYABLE.FBILLNO				FBillNo		--单据编号	");
            sqlBuilder.AppendLine("		,DEP_L.FNAME						FDepName	--部门	");
            sqlBuilder.AppendLine("		,STAFF_L.FNAME						FStaff		--员工	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME						FCustName	--客户	");
            sqlBuilder.AppendLine("		,CURR_L.FNAME						FCurrency	--币别	");
            sqlBuilder.AppendLine("		,OTHERPAYABLE.FTOTALAMOUNTFOR		FAmt		--扣款金额	");
            sqlBuilder.AppendLine("		,OTHERPAYABLE.FTOTALAMOUNT			FAmtLC		--扣款金额本位币	");
            sqlBuilder.AppendLine("		,ASSISTANTDATAENTRY_L.FDATAVALUE	FDeductType	--扣款类型	");
            sqlBuilder.AppendLine("		,OTHERPAYABLE.FREMARK				FRemark		--备注	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--其他应付单	");
            sqlBuilder.AppendLine("	T_AP_OTHERPAYABLE OTHERPAYABLE	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE_L BILLTYPE_L	");
            sqlBuilder.AppendLine("	ON BILLTYPE_L.FBILLTYPEID = OTHERPAYABLE.FBILLTYPEID AND BILLTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = OTHERPAYABLE.FDEPARTMENTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--员工任岗信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("	ON STAFF_L.FSTAFFID = OTHERPAYABLE.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = OTHERPAYABLE.FCONTACTUNIT	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--币别	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CURRENCY_L CURR_L	");
            sqlBuilder.AppendLine("	ON CURR_L.FCURRENCYID = OTHERPAYABLE.FCURRENCYID AND CURR_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--辅助资料	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSISTANTDATAENTRY_L	");
            sqlBuilder.AppendLine("	ON ASSISTANTDATAENTRY_L.FENTRYID = OTHERPAYABLE.FDEDUCTTYPEID AND ASSISTANTDATAENTRY_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--组织机构	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("	ON ORG_L.FORGID = OTHERPAYABLE.FSETTLEORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	WHERE OTHERPAYABLE.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	AND OTHERPAYABLE.FBILLTYPEID = '5a48a6bfd82ae6'		--单击类型 = 销售扣款其他应付	");
            sqlBuilder.AppendLine("	AND OTHERPAYABLE.FCONTACTUNITTYPE = 'BD_Customer'	--往来单位类型 = 客户	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', OTHERPAYABLE.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, OTHERPAYABLE.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	AND FORGID IN (" + orgId + ")	");
            //客户
            if (!custNum.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND CUST.FNUMBER = '" + custNum + "'	");
            }
            //部门
            if (!depName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DEP_L.FNAME LIKE '%" + depName + "%'	");
            }
            return sqlBuilder.ToString();
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FBillNo");
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("/*dialect*/	");
            sql.AppendLine("	SELECT	");
            sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
            sql.AppendLine("		,FOrgName		    --组织机构	");
            sql.AppendLine("		,FBillType			--单据类型	");
            sql.AppendLine("		,FBillNo			--单据编号	");
            sql.AppendLine("		,FDepName			--部门	");
            sql.AppendLine("		,FStaff			    --员工	");
            sql.AppendLine("		,FCustName			--客户 ");
            sql.AppendLine("		,FCurrency			--币别 ");
            sql.AppendLine("		,FAmt			    --扣款金额 ");
            sql.AppendLine("		,FAmtLC			    --扣款金额本位币	");
            sql.AppendLine("		,FDeductType		--扣款类型	");
            sql.AppendLine("		,FRemark			--备注	");
            sql.AppendLine("		,2 FPRECISION	    --精度	");
            sql.AppendFormat("	INTO {0}	\r\n", tableName);
            sql.AppendLine("	FROM	");
            sql.AppendLine("	(	");
            sql.Append(this.GetSql());
            sql.AppendLine("	) TT	");
            sql.AppendLine("	WHERE 1 = 1	");
            if (!filter.FilterParameter.FilterString.IsNullOrEmptyOrWhiteSpace())
            {
                sql.AppendLine("	AND " + filter.FilterParameter.FilterString + "   ");
            }
            DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //起始日期
                beginDate = Convert.ToDateTime(dyFilter["FBeginDate_Filter"]);
                //截止日期
                endDate = Convert.ToDateTime(dyFilter["FEndDate_Filter"]);
                //部门
                depName = Convert.ToString(dyFilter["FDepName_Filter"]);
                //客户
                DynamicObject cust = dyFilter["FCustId_Filter"] as DynamicObject;
                custName = cust == null ? string.Empty : Convert.ToString(cust["Name"]);
                custNum = cust == null ? string.Empty : Convert.ToString(cust["Number"]);
                //组织机构
                orgId = Convert.ToString(dyFilter["FMulSelOrgList_Filter"]);
                orgName = this.GetOrgName(orgId);
            }
        }
        private string GetOrgName(string orgId)
        {
            string sql = string.Format("/*dialect*/\r\nSELECT FNAME+ '，' FROM T_ORG_ORGANIZATIONS_L WHERE FLOCALEID = '2052' AND FORGID IN ({0}) FOR XML PATH('')", orgId);
            return DBUtils.ExecuteScalar<string>(this.Context, sql, string.Empty, new Kingdee.BOS.SqlParam[0]);
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            //客户
            title.AddTitle("FCustName_H", custName);
            //部门
            title.AddTitle("FDepName_H", depName);
            //组织机构
            title.AddTitle("FOrgName_H", orgName);
            return title;
        }
    }
}

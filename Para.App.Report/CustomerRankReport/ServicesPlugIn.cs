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

namespace Para.App.Report.CustomerRankReport
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("客户排名表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("客户排名表", base.Context.UserLocale.LCID);
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
            //报关金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		ROW_NUMBER() OVER(ORDER BY FAMT DESC) FRANK	--排名	");
            sqlBuilder.AppendLine("		,FCUSTNUM									--客户编码	");
            sqlBuilder.AppendLine("		,FCUSTNAME									--客户名称	");
            sqlBuilder.AppendLine("		,FAMT										--报关金额	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER						FCUSTNUM	--客户编码	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME						FCUSTNAME	--客户名称	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(DECBILL.FBILLAMT,0))	FAMT		--报关金额	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--出口报关单	");
            sqlBuilder.AppendLine("	TPT_FZH_DECALREDOC DECBILL	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = DECBILL.FCUSID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	WHERE DECBILL.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', DECBILL.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, DECBILL.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER,CUST_L.FNAME	");
            sqlBuilder.AppendLine("	) T	");
            return sqlBuilder.ToString();
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FRANK");
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("/*dialect*/	");
            sql.AppendLine("	SELECT	");
            sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
            sql.AppendLine("		,FRANK		        --排名	");
            sql.AppendLine("		,FCUSTNUM			--客户编码	");
            sql.AppendLine("		,FCUSTNAME			--客户名称	");
            sql.AppendLine("		,FAMT			    --报关金额	");
            sql.AppendLine("		,2 FPRECISION	    --精度	");
            sql.AppendFormat("	INTO {0}	\r\n", tableName);
            sql.AppendLine("	FROM	");
            sql.AppendLine("	(	");
            sql.Append(this.GetSql());
            sql.AppendLine("	) TT	");
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
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            return title;
        }
    }
}

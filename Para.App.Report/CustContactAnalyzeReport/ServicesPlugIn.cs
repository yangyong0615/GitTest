using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Para.App.Report.CustContactAnalyzeReport
{
    [HotUpdate]
    [Description("客户业务往来分析表 - 服务器插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //年月
        DateTime currYearAndMonth = DateTime.Today;
        //开始日期（当年累计用）
        DateTime beginDate = DateTime.Today;
        //结束日期（当年累计用）
        DateTime endDate = DateTime.Today;
        //主临时表
        string mainTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("客户业务往来分析表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            //false表示用代码构建表头，true表示用BOS构建表头
            this.ReportProperty.IsUIDesignerColumns = true;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.SimpleAllCols = false;
            this.SetDecimalControl();
        }
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            //接单&当期
            list.Add(new SummaryField("FCurrPrdSOAmt", BOSEnums.Enu_SummaryType.SUM));
            //接单&上期
            list.Add(new SummaryField("FPriorPrdSOAmt", BOSEnums.Enu_SummaryType.SUM));
            //接单&上年同期
            list.Add(new SummaryField("FLastYearPrdSOAmt", BOSEnums.Enu_SummaryType.SUM));
            //接单&当年累计
            list.Add(new SummaryField("FTotalSOAmt", BOSEnums.Enu_SummaryType.SUM));
            //出货&当期
            list.Add(new SummaryField("FCurrPrdDelAmt", BOSEnums.Enu_SummaryType.SUM));
            //出货&上期
            list.Add(new SummaryField("FPriorPrdDelAmt", BOSEnums.Enu_SummaryType.SUM));
            //出货&上年同期
            list.Add(new SummaryField("FLastYearPrdDelAmt", BOSEnums.Enu_SummaryType.SUM));
            //出货&当年累计
            list.Add(new SummaryField("FTotalDelAmt", BOSEnums.Enu_SummaryType.SUM));
            //收汇&当期
            list.Add(new SummaryField("FCurrPrdRecAmt", BOSEnums.Enu_SummaryType.SUM));
            //收汇&上期
            list.Add(new SummaryField("FPriorPrdRecAmt", BOSEnums.Enu_SummaryType.SUM));
            //收汇&上年同期
            list.Add(new SummaryField("FLastYearPrdRecAmt", BOSEnums.Enu_SummaryType.SUM));
            //收汇&当年累计
            list.Add(new SummaryField("FTotalRecAmt", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        //设置精度
        private void SetDecimalControl()
        {
            List<DecimalControlField> list = new List<DecimalControlField>();
            //接单&当期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FCurrPrdSOAmt", DecimalControlFieldName = "FPRECISION" });
            //接单&上期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FPriorPrdSOAmt", DecimalControlFieldName = "FPRECISION" });
            //接单&上年同期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FLastYearPrdSOAmt", DecimalControlFieldName = "FPRECISION" });
            //接单&当年累计
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FTotalSOAmt", DecimalControlFieldName = "FPRECISION" });
            //出货&当期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FCurrPrdDelAmt", DecimalControlFieldName = "FPRECISION" });
            //出货&上期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FPriorPrdDelAmt", DecimalControlFieldName = "FPRECISION" });
            //出货&上年同期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FLastYearPrdDelAmt", DecimalControlFieldName = "FPRECISION" });
            //出货&当年累计
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FTotalDelAmt", DecimalControlFieldName = "FPRECISION" });
            //收汇&当期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FCurrPrdRecAmt", DecimalControlFieldName = "FPRECISION" });
            //收汇&上期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FPriorPrdRecAmt", DecimalControlFieldName = "FPRECISION" });
            //收汇&上年同期
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FLastYearPrdRecAmt", DecimalControlFieldName = "FPRECISION" });
            //收汇&当年累计
            list.Add(new DecimalControlField { ByDecimalControlFieldName = "FTotalRecAmt", DecimalControlFieldName = "FPRECISION" });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建主临时表
                mainTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#mainTemp", this.CreatMainTemp());
                //插入客户数据
                this.InsertCustData();
                //插入当期接单数据
                this.InsertCurrSOAmtData();
                //插入上期接单数据
                this.InsertPriorPrdSOAmtData();
                //插入上年同期接单数据
                this.InsertLastYearPrdSOAmtData();
                //插入当年累计接单数据
                this.InsertTotalSOAmtData();
                //插入接单毛利数据
                this.InsertAvgSOProfitRate();
                //插入接单目标毛利
                this.InsertTargetProfitRate();
                //插入当期出货数据
                this.InsertCurrPrdDelData();
                //插入上期出货数据
                this.InsertPriorPrdDelData();
                //插入上年同期出货数据
                this.InsertLastYearPrdDelData();
                //插入当年累计出货数据
                this.InsertTotalDelData();
                //插入当年出货目标
                this.InsertDelTargetData();
                //插入当期收汇数据
                this.InsertCurrRecData();
                //插入上期收汇数据
                this.InsertPriorPrdRecData();
                //插入上年同期收汇数据
                this.InsertLastYearPrdRecData();
                //插入当年累计收汇数据
                this.InsertTotalRecData();
                //计算同比环比等其他数据
                this.CalOtherDate();
                //删除无数据行
                this.DeleteBlankDate();
                //排序按照：出货 倒序，接单 倒序，收汇 倒序
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FTotalDelAmt desc, FTotalSOAmt desc, FTotalRecAmt desc");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("	SELECT	");
                sql.AppendLine("	    " + base.KSQL_SEQ + "		--序号");
                sql.AppendLine("		,FCUSTNUM		            --客户编码	");
                sql.AppendLine("		,FCUSTNAME		            --客户名称	");
                sql.AppendLine("		,FCUSTTYPE		            --客户类别	");
                sql.AppendLine("		,FRECCONDITION		        --账期	");

                sql.AppendLine("		,FCurrPrdSOAmt		        --接单&当期	");
                sql.AppendLine("		,FPriorPrdSOAmt		        --接单&上期	");
                sql.AppendLine("		,FLastYearPrdSOAmt		    --接单&上年同期	");
                sql.AppendLine("		,FSOYoY                     --接单&同比	");
                sql.AppendLine("		,FSOMoM         		    --接单&环比	");
                sql.AppendLine("		,FTotalSOAmt    		    --接单&当年累计	");

                sql.AppendLine("		,FAvgSOProfitRate    		--年度接单平均毛利（%）&接单毛利	");
                sql.AppendLine("		,FTargetSOProfitRate    	--年度接单平均毛利（%）&接单目标毛利	");
                sql.AppendLine("		,FDiffSOProfitRate    		--年度接单平均毛利（%）&比目标值增减	");

                sql.AppendLine("		,FCurrPrdDelAmt		        --出货&当期	");
                sql.AppendLine("		,FPriorPrdDelAmt		    --出货&上期	");
                sql.AppendLine("		,FLastYearPrdDelAmt		    --出货&上年同期	");
                sql.AppendLine("		,FDelYoY                    --出货&同比	");
                sql.AppendLine("		,FDelMoM         		    --出货&环比	");
                sql.AppendLine("		,FTotalDelAmt    		    --出货&当年累计	");
                sql.AppendLine("		,FDelTargetAmt    		    --出货&当年出货目标	");
                sql.AppendLine("		,FFinishDelRatio    		--出货&完成目标	");

                sql.AppendLine("		,FCurrPrdRecAmt		        --收汇&当期	");
                sql.AppendLine("		,FPriorPrdRecAmt		    --收汇&上期	");
                sql.AppendLine("		,FLastYearPrdRecAmt		    --收汇&上年同期	");
                sql.AppendLine("		,FRecYoY                    --收汇&同比	");
                sql.AppendLine("		,FRecMoM         		    --收汇&环比	");
                sql.AppendLine("		,FTotalRecAmt    		    --收汇&当年累计	");

                sql.AppendLine("		,2 FPRECISION	            --精度	");
                sql.AppendFormat("  INTO {0}\r\n", tableName);
                sql.AppendFormat("  FROM {0}\r\n", mainTemp);
                DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, mainTemp);
            }
        }
        //创建主零时表
        private string CreatMainTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            StringBuilder.AppendLine("FCUSTNUM NVARCHAR(300)");                         //客户编码
            StringBuilder.AppendLine(",FCUSTNAME NVARCHAR(300)");                       //客户名称
            StringBuilder.AppendLine(",FCUSTTYPE NVARCHAR(300)");                       //客户类别
            StringBuilder.AppendLine(",FRECCONDITION NVARCHAR(300)");                   //账期

            StringBuilder.AppendLine(",FCurrPrdSOAmt DECIMAL(23, 10) DEFAULT(0)");      //接单&当期
            StringBuilder.AppendLine(",FPriorPrdSOAmt DECIMAL(23, 10) DEFAULT(0)");     //接单&上期
            StringBuilder.AppendLine(",FLastYearPrdSOAmt DECIMAL(23, 10) DEFAULT(0)");  //接单&上年同期
            StringBuilder.AppendLine(",FSOYoY DECIMAL(23, 10) DEFAULT(0)");             //接单&同比
            StringBuilder.AppendLine(",FSOMoM DECIMAL(23, 10) DEFAULT(0)");             //接单&环比
            StringBuilder.AppendLine(",FTotalSOAmt DECIMAL(23, 10) DEFAULT(0)");        //接单&当年累计

            StringBuilder.AppendLine(",FAvgSOProfitRate DECIMAL(23, 10) DEFAULT(0)");   //年度接单平均毛利（%）&接单毛利
            StringBuilder.AppendLine(",FTargetSOProfitRate DECIMAL(23, 10) DEFAULT(0)");//年度接单平均毛利（%）&接单目标毛利
            StringBuilder.AppendLine(",FDiffSOProfitRate DECIMAL(23, 10) DEFAULT(0)");  //年度接单平均毛利（%）&比目标值增减

            StringBuilder.AppendLine(",FCurrPrdDelAmt DECIMAL(23, 10) DEFAULT(0)");     //出货&当期
            StringBuilder.AppendLine(",FPriorPrdDelAmt DECIMAL(23, 10) DEFAULT(0)");    //出货&上期
            StringBuilder.AppendLine(",FLastYearPrdDelAmt DECIMAL(23, 10) DEFAULT(0)"); //出货&上年同期
            StringBuilder.AppendLine(",FDelYoY DECIMAL(23, 10) DEFAULT(0)");            //出货&同比
            StringBuilder.AppendLine(",FDelMoM DECIMAL(23, 10) DEFAULT(0)");            //出货&环比
            StringBuilder.AppendLine(",FTotalDelAmt DECIMAL(23, 10) DEFAULT(0)");       //出货&当年累计
            StringBuilder.AppendLine(",FDelTargetAmt DECIMAL(23, 10) DEFAULT(0)");      //出货&当年出货目标
            StringBuilder.AppendLine(",FFinishDelRatio DECIMAL(23, 10) DEFAULT(0)");    //出货&完成目标

            StringBuilder.AppendLine(",FCurrPrdRecAmt DECIMAL(23, 10) DEFAULT(0)");     //收汇&当期
            StringBuilder.AppendLine(",FPriorPrdRecAmt DECIMAL(23, 10) DEFAULT(0)");    //收汇&上期
            StringBuilder.AppendLine(",FLastYearPrdRecAmt DECIMAL(23, 10) DEFAULT(0)"); //收汇&上年同期
            StringBuilder.AppendLine(",FRecYoY DECIMAL(23, 10) DEFAULT(0)");            //收汇&同比
            StringBuilder.AppendLine(",FRecMoM DECIMAL(23, 10) DEFAULT(0)");            //收汇&环比
            StringBuilder.AppendLine(",FTotalRecAmt DECIMAL(23, 10) DEFAULT(0)");       //收汇&当年累计
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //插入客户数据
        private void InsertCustData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + mainTemp + " (FCUSTNUM,FCUSTNAME,FRECCONDITION,FCUSTTYPE)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER		FCUSTNUM		--客户编码	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME		FCUSTNAME		--客户名称	");
            sqlBuilder.AppendLine("		,RECCON_L.FNAME		FRECCONDITION	--账期	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN CUST.FCUSTTYPE = '0' THEN 'TO R'	");
            sqlBuilder.AppendLine("			WHEN CUST.FCUSTTYPE = '1' THEN 'TO B'	");
            sqlBuilder.AppendLine("			WHEN CUST.FCUSTTYPE = '2' THEN '品牌'	");
            sqlBuilder.AppendLine("			ELSE ''	");
            sqlBuilder.AppendLine("		END					FCUSTTYPE		--客户类别	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--收款条件	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RecCondition RECCON	");
            sqlBuilder.AppendLine("	ON RECCON.FID = CUST.FRECCONDITIONID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RECCONDITION_L RECCON_L	");
            sqlBuilder.AppendLine("	ON RECCON.FID = RECCON_L.FID AND RECCON_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON CUST.FUSEORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("	WHERE ORG.FNUMBER = 'PMGC'	");
            sqlBuilder.AppendLine("	AND CUST.FCORRESPONDORGID = 0    --排除内部客户	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当期接单数据
        private void InsertCurrSOAmtData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FCurrPrdSOAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(SOFIN.FBILLALLAMOUNT_USD,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	--销售订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERFIN SOFIN	");
            sqlBuilder.AppendLine("	ON SO.FID = SOFIN.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON SO.FCUSTID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = SO.FSALEORGID	");
            sqlBuilder.AppendLine("	WHERE SO.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG','PM','EG')	");
            sqlBuilder.AppendLine("	AND YEAR(SO.FAPPROVEDATE) = " + currYearAndMonth.Year + " AND MONTH(SO.FAPPROVEDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上期接单数据
        private void InsertPriorPrdSOAmtData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FPriorPrdSOAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(SOFIN.FBILLALLAMOUNT_USD,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	--销售订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERFIN SOFIN	");
            sqlBuilder.AppendLine("	ON SO.FID = SOFIN.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON SO.FCUSTID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = SO.FSALEORGID	");
            sqlBuilder.AppendLine("	WHERE SO.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG','PM','EG')	");
            sqlBuilder.AppendLine("	AND YEAR(SO.FAPPROVEDATE) = " + currYearAndMonth.AddMonths(-1).Year + " AND MONTH(SO.FAPPROVEDATE) = " + currYearAndMonth.AddMonths(-1).Month + "	");
            sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上年同期接单数据
        private void InsertLastYearPrdSOAmtData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FLastYearPrdSOAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(SOFIN.FBILLALLAMOUNT_USD,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	--销售订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERFIN SOFIN	");
            sqlBuilder.AppendLine("	ON SO.FID = SOFIN.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON SO.FCUSTID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = SO.FSALEORGID	");
            sqlBuilder.AppendLine("	WHERE SO.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG','PM','EG')	");
            sqlBuilder.AppendLine("	AND YEAR(SO.FAPPROVEDATE) = " + currYearAndMonth.AddYears(-1).Year + " AND MONTH(SO.FAPPROVEDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当年累计接单数据
        private void InsertTotalSOAmtData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FTotalSOAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(SOFIN.FBILLALLAMOUNT_USD,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	--销售订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERFIN SOFIN	");
            sqlBuilder.AppendLine("	ON SO.FID = SOFIN.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON SO.FCUSTID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = SO.FSALEORGID	");
            sqlBuilder.AppendLine("	WHERE SO.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG','PM','EG')	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', SO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, SO.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入接单毛利数据
        private void InsertAvgSOProfitRate()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FAvgSOProfitRate = T.FAvgSOProfitRate	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FCUSTNUM				--客户编码	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN FAMT <> 0 THEN ROUND(FPROFIT / FAMT * 100,2)	");
            sqlBuilder.AppendLine("			ELSE 0	");
            sqlBuilder.AppendLine("		END	FAvgSOProfitRate	--年度接单平均毛利率 = 总利润额 / 总销售收入 * 100	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER						FCUSTNUM	--客户编码	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PALENTRY1.FPALAMT,0))	FPROFIT		--总利润额	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PALENTRY2.FPALAMT,0))	FAMT		--总销售收入	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售订单	");
            sqlBuilder.AppendLine("		T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("		--销售订单.盈亏预测	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_PALENTRY PALENTRY1	");
            sqlBuilder.AppendLine("		ON PALENTRY1.FID = SO.FID	");
            sqlBuilder.AppendLine("		--销售订单.盈亏预测	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_PALENTRY PALENTRY2	");
            sqlBuilder.AppendLine("		ON PALENTRY2.FID = SO.FID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = SO.FSALEORGID	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON CUST.FCUSTID = SO.FCUSTID	");
            sqlBuilder.AppendLine("		WHERE SO.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG','PM','EG')	");
            sqlBuilder.AppendLine("		AND PALENTRY1.FFEEITEM = '利润额' AND PALENTRY2.FFEEITEM = '销售收入'	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', SO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, SO.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("	    ) TT	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入接单目标毛利
        private void InsertTargetProfitRate()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FTargetSOProfitRate = T.FTargetSOProfitRate	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T1.FNUMBER									FCUSTNUM			--客户编码	");
            sqlBuilder.AppendLine("		,MAX(ISNULL(T2.FGROSSPROFITRATETARGET,0))	FTargetSOProfitRate	--接单目标毛利率	");
            sqlBuilder.AppendLine("	FROM T_BD_CUSTOMER T1	");
            sqlBuilder.AppendLine("	LEFT JOIN PAWK_T_CusProRateTarEntry T2	");
            sqlBuilder.AppendLine("	ON T1.FCUSTID = T2.FCUSTID	");
            sqlBuilder.AppendLine("	WHERE T2.FGORSSPROFITTARGETYEAR = 2020	");
            sqlBuilder.AppendLine("	GROUP BY T1.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当期出货数据
        private void InsertCurrPrdDelData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FCurrPrdDelAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FCUSTNUM				FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FAMT,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		--离岸公司销售出库单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(OUTSTOCKFIN.FBILLALLAMOUNT,0))	FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售出库单	");
            sqlBuilder.AppendLine("		T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("		--销售出库单.财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("		ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON OUTSTOCK.FCUSTOMERID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = OUTSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("		WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG')	");
            sqlBuilder.AppendLine("		AND YEAR(OUTSTOCK.FDATE) = " + currYearAndMonth.Year + " AND MONTH(OUTSTOCK.FDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--高山、阳普生报关单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(DECBILL.FBILLUSDAMT,0))			FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--报关单	");
            sqlBuilder.AppendLine("		TPT_FZH_DECALREDOC DECBILL	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON DECBILL.FCUSID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		WHERE DECBILL.FDOCUMENTSTATUS = 'C' AND DECBILL.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("		AND YEAR(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.Year + " AND MONTH(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		AND CUST.FNUMBER NOT IN ('PG','IG')	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		) DECTEMP	");
            sqlBuilder.AppendLine("		GROUP BY DECTEMP.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上期出货数据
        private void InsertPriorPrdDelData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FPriorPrdDelAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FCUSTNUM				FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FAMT,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		--离岸公司销售出库单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(OUTSTOCKFIN.FBILLALLAMOUNT,0))	FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售出库单	");
            sqlBuilder.AppendLine("		T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("		--销售出库单.财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("		ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON OUTSTOCK.FCUSTOMERID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = OUTSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("		WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG')	");
            sqlBuilder.AppendLine("		AND YEAR(OUTSTOCK.FDATE) = " + currYearAndMonth.AddMonths(-1).Year + " AND MONTH(OUTSTOCK.FDATE) = " + currYearAndMonth.AddMonths(-1).Month + "	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--高山、阳普生报关单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(DECBILL.FBILLUSDAMT,0))			FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--报关单	");
            sqlBuilder.AppendLine("		TPT_FZH_DECALREDOC DECBILL	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON DECBILL.FCUSID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		WHERE DECBILL.FDOCUMENTSTATUS = 'C' AND DECBILL.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("		AND YEAR(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.AddMonths(-1).Year + " AND MONTH(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.AddMonths(-1).Month + "	");
            sqlBuilder.AppendLine("		AND CUST.FNUMBER NOT IN ('PG','IG')	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		) DECTEMP	");
            sqlBuilder.AppendLine("		GROUP BY DECTEMP.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上年同期出货数据
        private void InsertLastYearPrdDelData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FLastYearPrdDelAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FCUSTNUM				FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FAMT,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		--离岸公司销售出库单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(OUTSTOCKFIN.FBILLALLAMOUNT,0))	FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售出库单	");
            sqlBuilder.AppendLine("		T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("		--销售出库单.财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("		ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON OUTSTOCK.FCUSTOMERID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = OUTSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("		WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG')	");
            sqlBuilder.AppendLine("		AND YEAR(OUTSTOCK.FDATE) = " + currYearAndMonth.AddYears(-1).Year + " AND MONTH(OUTSTOCK.FDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--高山、阳普生报关单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(DECBILL.FBILLUSDAMT,0))			FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--报关单	");
            sqlBuilder.AppendLine("		TPT_FZH_DECALREDOC DECBILL	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON DECBILL.FCUSID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		WHERE DECBILL.FDOCUMENTSTATUS = 'C' AND DECBILL.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("		AND YEAR(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.AddYears(-1).Year + " AND MONTH(DECBILL.FOFFSHOREDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		AND CUST.FNUMBER NOT IN ('PG','IG')	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		) DECTEMP	");
            sqlBuilder.AppendLine("		GROUP BY DECTEMP.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当年累计出货数据
        private void InsertTotalDelData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FTotalDelAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FCUSTNUM				FCUSTNUM	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FAMT,0))	FAMT	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		--离岸公司销售出库单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(OUTSTOCKFIN.FBILLALLAMOUNT,0))	FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售出库单	");
            sqlBuilder.AppendLine("		T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("		--销售出库单.财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("		ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON OUTSTOCK.FCUSTOMERID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = OUTSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("		WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('PG','IG')	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', OUTSTOCK.FDATE) >= 0 AND DATEDIFF(DAY, OUTSTOCK.FDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--高山、阳普生报关单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			CUST.FNUMBER								FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(DECBILL.FBILLUSDAMT,0))			FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--报关单	");
            sqlBuilder.AppendLine("		TPT_FZH_DECALREDOC DECBILL	");
            sqlBuilder.AppendLine("		--客户	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("		ON DECBILL.FCUSID = CUST.FCUSTID	");
            sqlBuilder.AppendLine("		WHERE DECBILL.FDOCUMENTSTATUS = 'C' AND DECBILL.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', DECBILL.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, DECBILL.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		AND CUST.FNUMBER NOT IN ('PG','IG')	");
            sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
            sqlBuilder.AppendLine("		) DECTEMP	");
            sqlBuilder.AppendLine("		GROUP BY DECTEMP.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当年出货目标
        private void InsertDelTargetData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE TEMP	");
            sqlBuilder.AppendLine("		SET TEMP.FDelTargetAmt = T.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " TEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T1.FNUMBER									FCUSTNUM			--客户编码	");
            sqlBuilder.AppendLine("		,MAX(ISNULL(T2.FSALETARGETAMT,0)*10000)		FAMT        		--年度出货目标	");
            sqlBuilder.AppendLine("	FROM T_BD_CUSTOMER T1	");
            sqlBuilder.AppendLine("	LEFT JOIN PAWK_t_CustSaleTargetEntry T2	");
            sqlBuilder.AppendLine("	ON T1.FCUSTID = T2.FCUSTID	");
            sqlBuilder.AppendLine("	WHERE T2.FSALETARGETYEAR = 2020	");
            sqlBuilder.AppendLine("	GROUP BY T1.FNUMBER	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当期收汇数据
        private void InsertCurrRecData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("		SET T1.FCurrPrdRecAmt = T2.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(FAMT)	FAMT	");
            sqlBuilder.AppendLine("		FROM (	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				CUST.FNUMBER							FCUSTNUM	");
            sqlBuilder.AppendLine("				,CASE	");
            sqlBuilder.AppendLine("					--结算币别 = 美元：SUM(本次核销金额)	");
            sqlBuilder.AppendLine("					WHEN FCURRENCYID = 7 THEN ISNULL(RECMATCHLOGENTRY.FCURWRITTENOFFAMOUNTFOR,0)	");
            sqlBuilder.AppendLine("					--结算币别 ≠ 美元：SUM(本次核销金额本位币 * 美元间接汇率)	");
            sqlBuilder.AppendLine("					ELSE ROUND(ISNULL(FCURWRITTENOFFAMOUNT,0) * ISNULL(BD_RATE.FREVERSEEXRATE,0),2)	");
            sqlBuilder.AppendLine("				END										FAMT	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--应收收款核销记录.表头	");
            sqlBuilder.AppendLine("			T_AR_RECMacthLog RECMacthLog	");
            sqlBuilder.AppendLine("			--核销记录.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AR_RECMacthLogENTRY RECMATCHLOGENTRY	");
            sqlBuilder.AppendLine("			ON RECMacthLog.FID = RECMATCHLOGENTRY.FID	");
            sqlBuilder.AppendLine("			--组织	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("			ON ORG.FORGID = RECMATCHLOGENTRY.FSETTLEORGID	");
            sqlBuilder.AppendLine("			--客户	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("			ON CUST.FCUSTID = RECMATCHLOGENTRY.FCONTACTUNIT	");
            sqlBuilder.AppendLine("			--汇率	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("			ON BD_RATE.FCYFORID = 7					--汇率.原币 = 美元	");
            sqlBuilder.AppendLine("			AND BD_RATE.FCYTOID = 1					--汇率.目标比 = 本位币	");
            sqlBuilder.AppendLine("			AND BD_RATE.FRATETYPEID = 1				--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, BD_RATE.FBEGDATE, RECMacthLog.FVERIFYDATE) >= 0	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, RECMacthLog.FVERIFYDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("			WHERE FCONTACTUNITTYPE = 'BD_Customer'	--往来单位类型 = 客户	");
            sqlBuilder.AppendLine("			AND FSOURCEFROMID = 'AR_RECEIVEBILL'	--源单 = 收款单	");
            sqlBuilder.AppendLine("			AND ORG.FNUMBER IN ('PM','IG','PG','EG')	");
            sqlBuilder.AppendLine("			AND CUST.FCORRESPONDORGID = 0			--排除内部客户	");
            sqlBuilder.AppendLine("			AND YEAR(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.Year + " AND MONTH(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		) T	");
            sqlBuilder.AppendLine("		GROUP BY T.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FCUSTNUM = T2.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上期收汇数据
        private void InsertPriorPrdRecData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("		SET T1.FPriorPrdRecAmt = T2.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(FAMT)	FAMT	");
            sqlBuilder.AppendLine("		FROM (	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				CUST.FNUMBER							FCUSTNUM	");
            sqlBuilder.AppendLine("				,CASE	");
            sqlBuilder.AppendLine("					--结算币别 = 美元：SUM(本次核销金额)	");
            sqlBuilder.AppendLine("					WHEN FCURRENCYID = 7 THEN ISNULL(RECMATCHLOGENTRY.FCURWRITTENOFFAMOUNTFOR,0)	");
            sqlBuilder.AppendLine("					--结算币别 ≠ 美元：SUM(本次核销金额本位币 * 美元间接汇率)	");
            sqlBuilder.AppendLine("					ELSE ROUND(ISNULL(FCURWRITTENOFFAMOUNT,0) * ISNULL(BD_RATE.FREVERSEEXRATE,0),2)	");
            sqlBuilder.AppendLine("				END										FAMT	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--应收收款核销记录.表头	");
            sqlBuilder.AppendLine("			T_AR_RECMacthLog RECMacthLog	");
            sqlBuilder.AppendLine("			--核销记录.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AR_RECMacthLogENTRY RECMATCHLOGENTRY	");
            sqlBuilder.AppendLine("			ON RECMacthLog.FID = RECMATCHLOGENTRY.FID	");
            sqlBuilder.AppendLine("			--组织	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("			ON ORG.FORGID = RECMATCHLOGENTRY.FSETTLEORGID	");
            sqlBuilder.AppendLine("			--客户	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("			ON CUST.FCUSTID = RECMATCHLOGENTRY.FCONTACTUNIT	");
            sqlBuilder.AppendLine("			--汇率	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("			ON BD_RATE.FCYFORID = 7					--汇率.原币 = 美元	");
            sqlBuilder.AppendLine("			AND BD_RATE.FCYTOID = 1					--汇率.目标比 = 本位币	");
            sqlBuilder.AppendLine("			AND BD_RATE.FRATETYPEID = 1				--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, BD_RATE.FBEGDATE, RECMacthLog.FVERIFYDATE) >= 0	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, RECMacthLog.FVERIFYDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("			WHERE FCONTACTUNITTYPE = 'BD_Customer'	--往来单位类型 = 客户	");
            sqlBuilder.AppendLine("			AND FSOURCEFROMID = 'AR_RECEIVEBILL'	--源单 = 收款单	");
            sqlBuilder.AppendLine("			AND ORG.FNUMBER IN ('PM','IG','PG','EG')	");
            sqlBuilder.AppendLine("			AND CUST.FCORRESPONDORGID = 0			--排除内部客户	");
            sqlBuilder.AppendLine("			AND YEAR(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.AddMonths(-1).Year + " AND MONTH(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.AddMonths(-1).Month + "	");
            sqlBuilder.AppendLine("		) T	");
            sqlBuilder.AppendLine("		GROUP BY T.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FCUSTNUM = T2.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入上年同期收汇数据
        private void InsertLastYearPrdRecData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("		SET T1.FLastYearPrdRecAmt = T2.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(FAMT)	FAMT	");
            sqlBuilder.AppendLine("		FROM (	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				CUST.FNUMBER							FCUSTNUM	");
            sqlBuilder.AppendLine("				,CASE	");
            sqlBuilder.AppendLine("					--结算币别 = 美元：SUM(本次核销金额)	");
            sqlBuilder.AppendLine("					WHEN FCURRENCYID = 7 THEN ISNULL(RECMATCHLOGENTRY.FCURWRITTENOFFAMOUNTFOR,0)	");
            sqlBuilder.AppendLine("					--结算币别 ≠ 美元：SUM(本次核销金额本位币 * 美元间接汇率)	");
            sqlBuilder.AppendLine("					ELSE ROUND(ISNULL(FCURWRITTENOFFAMOUNT,0) * ISNULL(BD_RATE.FREVERSEEXRATE,0),2)	");
            sqlBuilder.AppendLine("				END										FAMT	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--应收收款核销记录.表头	");
            sqlBuilder.AppendLine("			T_AR_RECMacthLog RECMacthLog	");
            sqlBuilder.AppendLine("			--核销记录.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AR_RECMacthLogENTRY RECMATCHLOGENTRY	");
            sqlBuilder.AppendLine("			ON RECMacthLog.FID = RECMATCHLOGENTRY.FID	");
            sqlBuilder.AppendLine("			--组织	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("			ON ORG.FORGID = RECMATCHLOGENTRY.FSETTLEORGID	");
            sqlBuilder.AppendLine("			--客户	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("			ON CUST.FCUSTID = RECMATCHLOGENTRY.FCONTACTUNIT	");
            sqlBuilder.AppendLine("			--汇率	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("			ON BD_RATE.FCYFORID = 7					--汇率.原币 = 美元	");
            sqlBuilder.AppendLine("			AND BD_RATE.FCYTOID = 1					--汇率.目标比 = 本位币	");
            sqlBuilder.AppendLine("			AND BD_RATE.FRATETYPEID = 1				--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, BD_RATE.FBEGDATE, RECMacthLog.FVERIFYDATE) >= 0	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, RECMacthLog.FVERIFYDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("			WHERE FCONTACTUNITTYPE = 'BD_Customer'	--往来单位类型 = 客户	");
            sqlBuilder.AppendLine("			AND FSOURCEFROMID = 'AR_RECEIVEBILL'	--源单 = 收款单	");
            sqlBuilder.AppendLine("			AND ORG.FNUMBER IN ('PM','IG','PG','EG')	");
            sqlBuilder.AppendLine("			AND CUST.FCORRESPONDORGID = 0			--排除内部客户	");
            sqlBuilder.AppendLine("			AND YEAR(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.AddYears(-1).Year + " AND MONTH(RECMacthLog.FVERIFYDATE) = " + currYearAndMonth.Month + "	");
            sqlBuilder.AppendLine("		) T	");
            sqlBuilder.AppendLine("		GROUP BY T.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FCUSTNUM = T2.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入当年累计收汇数据
        private void InsertTotalRecData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("		SET T1.FTotalRecAmt = T2.FAMT	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FCUSTNUM	");
            sqlBuilder.AppendLine("			,SUM(FAMT)	FAMT	");
            sqlBuilder.AppendLine("		FROM (	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				CUST.FNUMBER							FCUSTNUM	");
            sqlBuilder.AppendLine("				,CASE	");
            sqlBuilder.AppendLine("					--结算币别 = 美元：SUM(本次核销金额)	");
            sqlBuilder.AppendLine("					WHEN FCURRENCYID = 7 THEN ISNULL(RECMATCHLOGENTRY.FCURWRITTENOFFAMOUNTFOR,0)	");
            sqlBuilder.AppendLine("					--结算币别 ≠ 美元：SUM(本次核销金额本位币 * 美元间接汇率)	");
            sqlBuilder.AppendLine("					ELSE ROUND(ISNULL(FCURWRITTENOFFAMOUNT,0) * ISNULL(BD_RATE.FREVERSEEXRATE,0),2)	");
            sqlBuilder.AppendLine("				END										FAMT	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--应收收款核销记录.表头	");
            sqlBuilder.AppendLine("			T_AR_RECMacthLog RECMacthLog	");
            sqlBuilder.AppendLine("			--核销记录.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AR_RECMacthLogENTRY RECMATCHLOGENTRY	");
            sqlBuilder.AppendLine("			ON RECMacthLog.FID = RECMATCHLOGENTRY.FID	");
            sqlBuilder.AppendLine("			--组织	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("			ON ORG.FORGID = RECMATCHLOGENTRY.FSETTLEORGID	");
            sqlBuilder.AppendLine("			--客户	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("			ON CUST.FCUSTID = RECMATCHLOGENTRY.FCONTACTUNIT	");
            sqlBuilder.AppendLine("			--汇率	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("			ON BD_RATE.FCYFORID = 7					--汇率.原币 = 美元	");
            sqlBuilder.AppendLine("			AND BD_RATE.FCYTOID = 1					--汇率.目标比 = 本位币	");
            sqlBuilder.AppendLine("			AND BD_RATE.FRATETYPEID = 1				--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, BD_RATE.FBEGDATE, RECMacthLog.FVERIFYDATE) >= 0	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, RECMacthLog.FVERIFYDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("			WHERE FCONTACTUNITTYPE = 'BD_Customer'	--往来单位类型 = 客户	");
            sqlBuilder.AppendLine("			AND FSOURCEFROMID = 'AR_RECEIVEBILL'	--源单 = 收款单	");
            sqlBuilder.AppendLine("			AND ORG.FNUMBER IN ('PM','IG','PG','EG')	");
            sqlBuilder.AppendLine("			AND CUST.FCORRESPONDORGID = 0			--排除内部客户	");
            sqlBuilder.AppendLine("			AND DATEDIFF(DAY, '" + beginDate + "', RECMacthLog.FVERIFYDATE) >= 0 AND DATEDIFF(DAY, RECMacthLog.FVERIFYDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		) T	");
            sqlBuilder.AppendLine("		GROUP BY T.FCUSTNUM	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FCUSTNUM = T2.FCUSTNUM	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //计算同比环比等其他数据
        private void CalOtherDate()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE " + mainTemp + "	");
            sqlBuilder.AppendLine("	SET	");
            sqlBuilder.AppendLine("		--接单&同比 = (接单&当期 - 接单&上年同期) / 接单&上年同期	");
            sqlBuilder.AppendLine("		FSOYoY = CASE	");
            sqlBuilder.AppendLine("					WHEN FLastYearPrdSOAmt = 0 OR FCurrPrdSOAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdSOAmt - FLastYearPrdSOAmt) / FLastYearPrdSOAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--接单&环比 = (接单&当期 - 接单&上期) / 接单&上期	");
            sqlBuilder.AppendLine("		,FSOMoM = CASE	");
            sqlBuilder.AppendLine("					WHEN FCurrPrdSOAmt = 0 OR FPriorPrdSOAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdSOAmt - FPriorPrdSOAmt) / FPriorPrdSOAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--比目标值增减 = 接单毛利 - 接单目标毛利	");
            sqlBuilder.AppendLine("		,FDiffSOProfitRate = CASE	");
            sqlBuilder.AppendLine("					WHEN FAvgSOProfitRate = 0 OR FTargetSOProfitRate = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE FAvgSOProfitRate - FTargetSOProfitRate	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--出货&同比 = (出货&当期 - 出货&上年同期) / 出货&上年同期	");
            sqlBuilder.AppendLine("		,FDelYoY = CASE	");
            sqlBuilder.AppendLine("					WHEN FCurrPrdDelAmt = 0 OR FLastYearPrdDelAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdDelAmt - FLastYearPrdDelAmt) / FLastYearPrdDelAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--出货&环比 = (出货&当期 - 出货&上期) / 出货&上期	");
            sqlBuilder.AppendLine("		,FDelMoM = CASE	");
            sqlBuilder.AppendLine("					WHEN FCurrPrdDelAmt = 0 OR FPriorPrdDelAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdDelAmt - FPriorPrdDelAmt) / FPriorPrdDelAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--完成目标 = 出货当年累计 / 当年出货目标	");
            sqlBuilder.AppendLine("		,FFinishDelRatio = CASE	");
            sqlBuilder.AppendLine("					WHEN FTotalDelAmt = 0 OR FDelTargetAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND(FTotalDelAmt / FDelTargetAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--收汇&同比 = (收汇&当期 - 收汇&上年同期) / 收汇&上年同期	");
            sqlBuilder.AppendLine("		,FRecYoY = CASE	");
            sqlBuilder.AppendLine("					WHEN FCurrPrdRecAmt = 0 OR FLastYearPrdRecAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdRecAmt - FLastYearPrdRecAmt) / FLastYearPrdRecAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            sqlBuilder.AppendLine("		--收汇&环比 = (收汇&当期 - 收汇&上期) / 收汇&上期	");
            sqlBuilder.AppendLine("		,FRecMoM = CASE	");
            sqlBuilder.AppendLine("					WHEN FCurrPrdRecAmt = 0 OR FPriorPrdRecAmt = 0 THEN 0	");
            sqlBuilder.AppendLine("					ELSE ROUND((FCurrPrdRecAmt - FPriorPrdRecAmt) / FPriorPrdRecAmt * 100,2)	");
            sqlBuilder.AppendLine("				END	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //删除无数据行
        private void DeleteBlankDate()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	DELETE FROM " + mainTemp + "	");
            sqlBuilder.AppendLine("	WHERE FCurrPrdSOAmt = 0 AND FPriorPrdSOAmt = 0 AND FLastYearPrdSOAmt = 0 AND FTotalSOAmt = 0	");
            sqlBuilder.AppendLine("	AND FCurrPrdDelAmt = 0 AND FPriorPrdDelAmt = 0 AND FLastYearPrdDelAmt = 0 AND FTotalDelAmt = 0	");
            sqlBuilder.AppendLine("	AND FCurrPrdRecAmt = 0 AND FPriorPrdRecAmt = 0 AND FLastYearPrdRecAmt = 0 AND FTotalRecAmt = 0	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //年
                int year = Convert.ToInt32(dyFilter["FYear_F"]);
                //月
                int month = Convert.ToInt32(dyFilter["FMonth_F"]);
                //年月
                currYearAndMonth = new DateTime(year, month, 1);
                //开始日期（当年累计用）
                beginDate = new DateTime(year, 1, 1);
                //结束日期（当年累计用）
                endDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            title.AddTitle("FYear_H", string.Format("{0}", currYearAndMonth.Year));      //年度
            title.AddTitle("FMonth_H", string.Format("{0}", currYearAndMonth.Month));    //月度
            return title;
        }
    }
}

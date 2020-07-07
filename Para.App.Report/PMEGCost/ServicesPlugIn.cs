using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Para.App.Report.PMEGCost
{
    [HotUpdate]
    [Description("高山阳普生成本表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //开始日期
        DateTime beginDate = DateTime.Today;
        //结束日期
        DateTime endDate = DateTime.Today;
        //外销发票号
        string invoNo = string.Empty;
        //报关数据临时表
        string decTemp = string.Empty;
        //走阳普生采购平台的成本临时表（该表中排除了阳普生报关的数据，即来自外贸 = 否）
        string EGCostTemp = string.Empty;
        //成本匹配单上非阳普生采购部分的成本
        string costMatchTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("高山阳普生成本表", base.Context.UserLocale.LCID);
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
                ByDecimalControlFieldName = "FDECAMT",
                DecimalControlFieldName = "FPRECISION"
            });
            //退税额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FRETURNTAX",
                DecimalControlFieldName = "FPRECISION"
            });
            //退税前成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FCOST1",
                DecimalControlFieldName = "FPRECISION"
            });
            //退税后成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FCOST2",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生含税采购金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGPURALLTAM",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGCOST",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生进项税
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGPURTAX",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生销项税
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGSALETAX",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //报关数据临时表
                decTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#decTemp", this.CreateDecTemp());
                //阳普生成本临时表
                EGCostTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#EGCostTemp", this.CreateEGCostTemp());
                //成本临时表
                costMatchTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#costTemp", this.CreateCostTemp());
                //插入报关数据
                this.InsertIntoDecTemp();
                //插入走阳普生采购平台的成本数据
                this.InsertIntoEGCostTemp();
                //插入成本匹配单上非阳普生采购部分的成本和退税额
                this.InsertIntoCostTemp();
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FOFFSHOREDATE DESC");
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("/*dialect*/	");
                sqlBuilder.AppendLine("	SELECT	");
                sqlBuilder.AppendLine("		" + base.KSQL_SEQ + "   --序号	");
                sqlBuilder.AppendLine("		,FINVOICENO			    --外销发票号	");
                sqlBuilder.AppendLine("		,FOFFSHOREDATE		    --离岸日期	");
                sqlBuilder.AppendLine("		,FDECAMT			    --报关金额	");
                sqlBuilder.AppendLine("		,FRETURNTAX			    --退税额	");
                sqlBuilder.AppendLine("		,FCOST1				    --退税前成本	");
                sqlBuilder.AppendLine("		,FCOST2				    --退税后成本	");
                sqlBuilder.AppendLine("		,FEGPURALLTAM		    --阳普生含税采购金额	");
                sqlBuilder.AppendLine("		,FEGCOST			    --阳普生成本	");
                sqlBuilder.AppendLine("		,FEGPURTAX			    --阳普生进项税	");
                sqlBuilder.AppendLine("		,FEGSALETAX			    --阳普生销项税	");
                sqlBuilder.AppendLine("		,2 FPRECISION	        --精度	");
                sqlBuilder.AppendFormat("	INTO {0}	\r\n", tableName);
                sqlBuilder.AppendLine("	FROM	");
                sqlBuilder.AppendLine("	(	");
                sqlBuilder.Append(this.GetSql());
                sqlBuilder.AppendLine("	) TT	");
                DBUtils.ExecuteDynamicObject(this.Context, sqlBuilder.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, decTemp);
                DBUtils.DropSessionTemplateTable(base.Context, EGCostTemp);
                DBUtils.DropSessionTemplateTable(base.Context, costMatchTemp);
            }
        }
        //创建报关数据临时表
        private string CreateDecTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FINVOICENO nvarchar(100)    ");         //外销发票号			
            sqlBuilder.AppendLine("	,FOFFSHOREDATE datetime ");             //离岸日期
            sqlBuilder.AppendLine("	,FDECAMT decimal(23, 10)    ");         //报关金额   
            sqlBuilder.AppendLine("	,FRETURNTAX decimal(23, 10)    ");      //退税额
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //走阳普生采购平台的成本临时表（该表中排除了阳普生报关的数据，即来自外贸 = 否）
        private string CreateEGCostTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FINVOICENO nvarchar(100)    ");         //外销发票号
            sqlBuilder.AppendLine("	,FEGSALETAX decimal(23, 10) ");         //阳普生销项税
            sqlBuilder.AppendLine("	,FEGPURALLTAM decimal(23, 10)   ");     //阳普生含税采购金额            
            sqlBuilder.AppendLine("	,FEGPURTAX decimal(23, 10) ");          //阳普生进项税            
            sqlBuilder.AppendLine("	,FEGCOST decimal(23, 10) ");            //阳普生成本 = 采购金额 + (销项税 - 进项税)
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //成本匹配单上非阳普生采购部分的成本和退税额
        private string CreateCostTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FINVOICENO nvarchar(100)    ");         //外销发票号
            sqlBuilder.AppendLine("	,FCOST decimal(23, 10)    ");           //成本匹配单上非阳普生采购的成本
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //插入报关数据
        private void InsertIntoDecTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + decTemp + " (FINVOICENO,FOFFSHOREDATE,FDECAMT,FRETURNTAX)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECALREDOC.FBILLNO										FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("		,CONVERT(VARCHAR(100), DECALREDOC.FOFFSHOREDATE, 23)	FOFFSHOREDATE	--离岸日期	");
            sqlBuilder.AppendLine("		,DECALREDOC.FBILLAMT									FDECAMT			--报关金额	");
            sqlBuilder.AppendLine("		,COSTMATCH.FRETURNTAX									FRETURNTAX		--退税额	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--出口报关单	");
            sqlBuilder.AppendLine("	TPT_FZH_DECALREDOC DECALREDOC	");
            sqlBuilder.AppendLine("	--成本匹配单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FINVOICENO											FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(FRETURNTAX,0))							FRETURNTAX		--退税额	");
            sqlBuilder.AppendLine("		FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			NEWCOSTMATCH.FBILLNO								FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("			,ISNULL(NEWCOSTMATCH.FTOTALRETURNTAXAMT,0)			FRETURNTAX		--退税额	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--新成本匹配单	");
            sqlBuilder.AppendLine("		T_CM_NewCostMatchingBill NEWCOSTMATCH	");
            sqlBuilder.AppendLine("		WHERE NEWCOSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			OUDCOSTMATCH.FINVOICENO								FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("			,ISNULL(OUDCOSTMATCH.FTOTALRETURNTAXAMT,0)			FRETURNTAX		--退税额	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--成本匹配单	");
            sqlBuilder.AppendLine("		T_CM_CostMatchingBill OUDCOSTMATCH	");
            sqlBuilder.AppendLine("		WHERE OUDCOSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		) C	");
            sqlBuilder.AppendLine("		GROUP BY C.FINVOICENO	");
            sqlBuilder.AppendLine("	) COSTMATCH	");
            sqlBuilder.AppendLine("	ON COSTMATCH.FINVOICENO = DECALREDOC.FBILLNO	");
            sqlBuilder.AppendLine("	WHERE DECALREDOC.FDOCUMENTSTATUS = 'C' AND DECALREDOC.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("	--成本匹配单	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', DECALREDOC.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY ,DECALREDOC.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            //外销发票号
            if (!invoNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DECALREDOC.FBILLNO LIKE '%" + invoNo + "%'	");
            }
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入走阳普生采购平台的成本数据
        private void InsertIntoEGCostTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	--走阳普生采购平台的成本临时表（该表中排除了阳普生报关的数据，即来自外贸 = 否）	");
            sqlBuilder.AppendLine("	INSERT INTO " + EGCostTemp + " (FINVOICENO,FEGSALETAX,FEGPURALLTAM,FEGPURTAX,FEGCOST)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		TEMP.FINVOICENO																								FINVOICENO	--外销发票号	");
            sqlBuilder.AppendLine("		,EGSALETAX_TEMP.FEGSALETAX																					FEGSALETAX	--阳普生销项税	");
            sqlBuilder.AppendLine("		,EGPURDATA.FEGPURALLAMT																						FEGPURALLTAM--阳普生采购价税合计	");
            sqlBuilder.AppendLine("		,EGPURDATA.FEGPURTAX																						FEGPURTAX	--阳普生进项税	");
            sqlBuilder.AppendLine("		,ISNULL(EGPURDATA.FEGPURALLAMT,0) + (ISNULL(EGSALETAX_TEMP.FEGSALETAX,0) - ISNULL(EGPURDATA.FEGPURTAX,0))	FEGCOST		--阳普生成本 = 阳普生含税采购金额 + (销项税 - 进项税)	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " TEMP	");
            sqlBuilder.AppendLine("	--阳普生销项税	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			OUTSTOCK.FOUTINVOICENO																					FINVOICENO	--外销发票号	");
            sqlBuilder.AppendLine("			,SUM(ROUND((OUTSTOCKENTRY_F.FPRICEUNITQTY-OUTSTOCKENTRY_R.FRETURNQTY)*OUTSTOCKENTRY_F.FPRICE*0.13,2))	FEGSALETAX	--销项税额 = (计价数量 - 关联退货数量) * 单价 * 13%	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售出库单	");
            sqlBuilder.AppendLine("		T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("		--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("		ON OUTSTOCKENTRY_F.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("		--销售出库单.明细_关联分录信息	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_OUTSTOCKENTRY_R OUTSTOCKENTRY_R	");
            sqlBuilder.AppendLine("		ON OUTSTOCKENTRY_F.FENTRYID = OUTSTOCKENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON OUTSTOCK.FSTOCKORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("		WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("		--来自外贸 = 否	");
            sqlBuilder.AppendLine("		AND FISFOREIGNTRADE = '0'	");
            sqlBuilder.AppendLine("		GROUP BY OUTSTOCK.FOUTINVOICENO	");
            sqlBuilder.AppendLine("	)	");
            sqlBuilder.AppendLine("	EGSALETAX_TEMP	");
            sqlBuilder.AppendLine("	ON EGSALETAX_TEMP.FINVOICENO = TEMP.FINVOICENO	");
            sqlBuilder.AppendLine("	--阳普生含税采购金额,税额	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				INSTOCK.FOUTINVOICENO_H												FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("				,SUM(INSTOCKENTRY_F.FALLAMOUNT - ISNULL(MRB.FALLAMT,0))				FEGPURALLAMT	--采购价税合计	");
            sqlBuilder.AppendLine("				,SUM(INSTOCKENTRY_F.FTAXAMOUNT - ISNULL(MRB.FTAX,0))				FEGPURTAX		--阳普生进项税	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--采购入库单	");
            sqlBuilder.AppendLine("			T_STK_INSTOCK INSTOCK	");
            sqlBuilder.AppendLine("			--采购入库单.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_STK_INSTOCKENTRY INSTOCKENTRY	");
            sqlBuilder.AppendLine("			ON INSTOCKENTRY.FID = INSTOCK.FID	");
            sqlBuilder.AppendLine("			--采购入库单.明细_财务	");
            sqlBuilder.AppendLine("			LEFT JOIN T_STK_INSTOCKENTRY_F INSTOCKENTRY_F	");
            sqlBuilder.AppendLine("			ON INSTOCKENTRY_F.FENTRYID = INSTOCKENTRY.FENTRYID	");
            sqlBuilder.AppendLine("			--组织	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("			ON ORG.FORGID = INSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("			--采购订单	");
            sqlBuilder.AppendLine("			LEFT JOIN T_PUR_POORDER POORDER	");
            sqlBuilder.AppendLine("			ON POORDER.FID = INSTOCKENTRY.FPURCHASEORDERID	");
            sqlBuilder.AppendLine("			--采购退料单	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					MRBENTRY_LK.FSID				FSID		--源单EntryId	");
            sqlBuilder.AppendLine("					,SUM(MRBENTRY_F.FTAXAMOUNT)		FTAX		--税额	");
            sqlBuilder.AppendLine("					,SUM(MRBENTRY_F.FALLAMOUNT)		FALLAMT		--价税合计	");
            sqlBuilder.AppendLine("				FROM	");
            sqlBuilder.AppendLine("				--采购退料单	");
            sqlBuilder.AppendLine("				T_PUR_MRB MRB	");
            sqlBuilder.AppendLine("				--采购退料单.明细_财务	");
            sqlBuilder.AppendLine("				LEFT JOIN T_PUR_MRBENTRY_F MRBENTRY_F	");
            sqlBuilder.AppendLine("				ON MRBENTRY_F.FID = MRB.FID	");
            sqlBuilder.AppendLine("				--采购退料单.关联表	");
            sqlBuilder.AppendLine("				LEFT JOIN T_PUR_MRBENTRY_LK MRBENTRY_LK	");
            sqlBuilder.AppendLine("				ON MRBENTRY_F.FENTRYID = MRBENTRY_LK.FENTRYID	");
            sqlBuilder.AppendLine("				WHERE MRB.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				AND MRBENTRY_LK.FSTABLENAME = 'T_STK_INSTOCKENTRY'	");
            sqlBuilder.AppendLine("				GROUP BY MRBENTRY_LK.FSID	");
            sqlBuilder.AppendLine("			) MRB	");
            sqlBuilder.AppendLine("			ON MRB.FSID = INSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("			WHERE INSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("			--来自外贸 = 否	");
            sqlBuilder.AppendLine("			AND INSTOCK.FISFOREIGNTRADE = '0'	");
            sqlBuilder.AppendLine("			--未作废的采购订单	");
            sqlBuilder.AppendLine("			AND POOrder.FISCANCEL = '0'	");
            sqlBuilder.AppendLine("			GROUP BY INSTOCK.FOUTINVOICENO_H	");
            sqlBuilder.AppendLine("	)	");
            sqlBuilder.AppendLine("	EGPURDATA	");
            sqlBuilder.AppendLine("	ON EGPURDATA.FINVOICENO = TEMP.FINVOICENO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入成本匹配单上非阳普生采购部分的成本
        private void InsertIntoCostTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	--走阳普生采购平台的成本临时表（该表中排除了阳普生报关的数据，即来自外贸 = 否）	");
            sqlBuilder.AppendLine("	INSERT INTO " + costMatchTemp + " (FINVOICENO,FCOST)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FINVOICENO								FINVOICENO		--外销发货号	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FCOST,0))					FCOST			--含税采购金额	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FINVOICENO						FINVOICENO		--外销发货号	");
            sqlBuilder.AppendLine("		,ISNULL(COSTMATCH.FTOTALINVOAMT,0)		FCOST			--含税采购金额	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--成本匹配单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_CM_CostMatchingBill COSTMATCH	");
            sqlBuilder.AppendLine("	ON DECTEMP.FINVOICENO = COSTMATCH.FINVOICENO	");
            sqlBuilder.AppendLine("	WHERE COSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	UNION ALL	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FINVOICENO						FINVOICENO		--外销发货号	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(T3.FSUBPURTAXAMT,0))		FCOST			--成本（价税合计）	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--新成本匹配单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_CM_NewCostMatchingBill T1	");
            sqlBuilder.AppendLine("	ON T1.FINVOICENO = DECTEMP.FINVOICENO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_CM_NewDecMsgEntry T2	");
            sqlBuilder.AppendLine("	ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_CM_NewSubInvoEntry T3	");
            sqlBuilder.AppendLine("	ON T2.FEntryID = T3.FEntryID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON T3.FSUBSUPID = SUP.FSUPPLIERID	");
            sqlBuilder.AppendLine("	WHERE T1.FDOCUMENTSTATUS = 'C' AND SUP.FNUMBER <> 'EG'	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FINVOICENO	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	GROUP BY T.FINVOICENO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FINVOICENO																					FINVOICENO		--外销发票号	");
            sqlBuilder.AppendLine("		,DECTEMP.FOFFSHOREDATE																				FOFFSHOREDATE	--离岸日期	");
            sqlBuilder.AppendLine("		,DECTEMP.FDECAMT																					FDECAMT			--报关金额	");
            sqlBuilder.AppendLine("		,DECTEMP.FRETURNTAX																			        FRETURNTAX		--退税额	");
            sqlBuilder.AppendLine("		,ISNULL(EGCOSTTEMP.FEGCOST,0) + ISNULL(COSTMATCHTEMP.FCOST,0)										FCOST1			--退税前成本	");
            sqlBuilder.AppendLine("		,ISNULL(EGCOSTTEMP.FEGCOST,0) + ISNULL(COSTMATCHTEMP.FCOST,0) - ISNULL(DECTEMP.FRETURNTAX,0)	    FCOST2			--退税后成本	");
            sqlBuilder.AppendLine("		,EGCOSTTEMP.FEGPURALLTAM																			FEGPURALLTAM	--阳普生含税采购金额	");
            sqlBuilder.AppendLine("		,EGCOSTTEMP.FEGCOST																					FEGCOST			--阳普生成本	");
            sqlBuilder.AppendLine("		,EGCOSTTEMP.FEGPURTAX																				FEGPURTAX		--阳普生进项税	");
            sqlBuilder.AppendLine("		,EGCOSTTEMP.FEGSALETAX																				FEGSALETAX		--阳普生销项税	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	LEFT JOIN " + EGCostTemp + " EGCOSTTEMP	");
            sqlBuilder.AppendLine("	ON EGCOSTTEMP.FINVOICENO = DECTEMP.FINVOICENO	");
            sqlBuilder.AppendLine("	LEFT JOIN " + costMatchTemp + " COSTMATCHTEMP	");
            sqlBuilder.AppendLine("	ON COSTMATCHTEMP.FINVOICENO = DECTEMP.FINVOICENO	");
            return sqlBuilder.ToString();
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //开始日期
                beginDate = Convert.ToDateTime(dyFilter["FBeginDate_Filter"]).Date;
                //结束日期
                endDate = Convert.ToDateTime(dyFilter["FEndDate_Filter"]).Date;
                //外销发票号
                invoNo = Convert.ToString(dyFilter["FInvoNo_Filter"]);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //开始日期
            title.AddTitle("FBeginDate_H", beginDate.ToString());
            //结束日期
            title.AddTitle("FEndDate_H", endDate.ToString());
            //外销发票号
            title.AddTitle("FInvoNo_H", invoNo);
            return title;
        }
    }
}

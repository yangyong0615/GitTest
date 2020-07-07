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

namespace Para.App.Report.DepPurCutPaymentDetailRpt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("供应商扣款表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //采购部门
        string purDepName = string.Empty;
        //采购员
        string purchaserName = string.Empty;
        //组织机构
        string orgName = string.Empty;
        string orgId = string.Empty;
        //供应商
        string supName = string.Empty;
        //销售订单号
        string saleNo = string.Empty;
        //采购订单号
        string purNo = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("部门采购扣款明细表", base.Context.UserLocale.LCID);
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
            //应付金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            //扣款金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FCUTPAYMENT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        //小计，合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            //应付金额
            list.Add(new SummaryField("FAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            //扣款金额
            list.Add(new SummaryField("FCUTPAYMENT_LOC", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				FPURDEPNAME					--采购部门	");
            sqlBuilder.AppendLine("				,FPurchaser					--采购员	");
            sqlBuilder.AppendLine("				,FBILLTYPE					--单据类型	");
            sqlBuilder.AppendLine("				,FBILLNO					--单据编号	");
            sqlBuilder.AppendLine("				,FAUDITDATE					--审核日期	");
            sqlBuilder.AppendLine("				,FSALENO					--销售订单号	");
            sqlBuilder.AppendLine("				,FPURNO						--采购订单号	");
            sqlBuilder.AppendLine("				,FOUTINVONO					--外销发票号	");
            sqlBuilder.AppendLine("				,FCUSTNAME					--客户	");
            sqlBuilder.AppendLine("				,FSUPNAME					--供应商	");
            sqlBuilder.AppendLine("				,FORGNAME					--组织机构	");
            sqlBuilder.AppendLine("				,FORGID						--组织机构ID	");
            sqlBuilder.AppendLine("				,FAMT_LOC					--应付金额	");
            sqlBuilder.AppendLine("				,FCUTPAYMENT_LOC			--扣款	");
            sqlBuilder.AppendLine("				,FCUTTYPE					--扣款类型	");
            sqlBuilder.AppendLine("				,FREMARKS					--扣款说明	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			(	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					DEP_L.FNAME						    FPURDEPNAME					--采购部门	");
            sqlBuilder.AppendLine("					,STAFF_L.FNAME					    FPurchaser					--采购员	");
            sqlBuilder.AppendLine("					,'应付单'						    FBILLTYPE					--单据类型	");
            sqlBuilder.AppendLine("					,PAYABLE.FBILLNO				    FBILLNO						--单据编号	");
            sqlBuilder.AppendLine("					,CONVERT(VARCHAR(100), PAYABLE.FAPPROVEDATE, 23)	");
            sqlBuilder.AppendLine("													    FAUDITDATE					--审核日期	");
            sqlBuilder.AppendLine("					,TEMP1.FSALEORDERNOS			    FSALENO						--销售订单号	");
            sqlBuilder.AppendLine("					,TEMP1.FPURORDERNOS				    FPURNO						--采购订单号	");
            sqlBuilder.AppendLine("					,TEMP1.FOUTINVONO				    FOUTINVONO					--外销发票号	");
            sqlBuilder.AppendLine("					,TEMP1.FCUSTNAME					FCUSTNAME					--客户	");
            sqlBuilder.AppendLine("					,SUP_L.FNAME					    FSUPNAME					--供应商	");
            sqlBuilder.AppendLine("					,ORG_L.FNAME					    FORGNAME					--组织机构	");
            sqlBuilder.AppendLine("					,ORG_L.FORGID					    FORGID						--组织机构ID	");
            sqlBuilder.AppendLine("					,PAYABLEFIN.FALLAMOUNT			    FAMT_LOC					--应付金额	");
            sqlBuilder.AppendLine("					,PAYABLE.FTOTALADJUSTAMT * PAYABLEFIN.FEXCHANGERATE * (-1)	");
            sqlBuilder.AppendLine("													    FCUTPAYMENT_LOC				--扣款	");
            sqlBuilder.AppendLine("					,ASSISTANTDATAENTRY_L.FDATAVALUE    FCUTTYPE					--扣款类型	");
            sqlBuilder.AppendLine("					,PAYABLE.FADJUSTREMARKS			    FREMARKS					--扣款说明	");
            sqlBuilder.AppendLine("				FROM	");
            sqlBuilder.AppendLine("				--应付单	");
            sqlBuilder.AppendLine("				T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("				--应付单.财务	");
            sqlBuilder.AppendLine("				LEFT JOIN T_AP_PAYABLEFIN PAYABLEFIN	");
            sqlBuilder.AppendLine("				ON PAYABLEFIN.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("				--应付单.采购订单号，销售订单号，外销发票号	");
            sqlBuilder.AppendLine("				LEFT JOIN (	");
            sqlBuilder.AppendLine("					SELECT	");
            sqlBuilder.AppendLine("						FID	");
            sqlBuilder.AppendLine("						,FSALEORDERNOS = STUFF((SELECT DISTINCT ',' + FSALEORDERNO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	--销售订单号	");
            sqlBuilder.AppendLine("						,FPURORDERNOS = STUFF((SELECT DISTINCT ',' + FORDERNUMBER FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')		--采购订单号	");
            sqlBuilder.AppendLine("						,FOUTINVONO = STUFF((SELECT DISTINCT ',' + FOUTINVOICENO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')		--外销发票号	");
            sqlBuilder.AppendLine("						,FCUSTNAME = STUFF((SELECT DISTINCT ',' + FCUSTNAME	");
            sqlBuilder.AppendLine("											 FROM (	");
            sqlBuilder.AppendLine("												SELECT	");
            sqlBuilder.AppendLine("													DISTINCT	");
            sqlBuilder.AppendLine("													PAYAENTRY.FID	");
            sqlBuilder.AppendLine("													--有穿透取终端客户，无穿透取销售订单客户	");
            sqlBuilder.AppendLine("													,CASE	");
            sqlBuilder.AppendLine("														WHEN S.FPURCHASEORDERID = '' OR S.FPURCHASEORDERID IS NULL THEN C1.FNAME	");
            sqlBuilder.AppendLine("														ELSE C2.FNAME	");
            sqlBuilder.AppendLine("													END	FCUSTNAME	");
            sqlBuilder.AppendLine("												FROM T_AP_PAYABLEENTRY PAYAENTRY	");
            sqlBuilder.AppendLine("												LEFT JOIN T_SAL_ORDER S	");
            sqlBuilder.AppendLine("												ON PAYAENTRY.FSALEORDERID = S.FID	");
            sqlBuilder.AppendLine("												--穿透部分采购订单	");
            sqlBuilder.AppendLine("												LEFT JOIN T_PUR_POORDER P	");
            sqlBuilder.AppendLine("												ON S.FPURCHASEORDERID = P.FID	");
            sqlBuilder.AppendLine("												--高山,阳普生的客户	");
            sqlBuilder.AppendLine("												LEFT JOIN T_BD_CUSTOMER_L C1	");
            sqlBuilder.AppendLine("												ON C1.FCUSTID = S.FCUSTID AND C1.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("												--穿透过来的终端客户	");
            sqlBuilder.AppendLine("												LEFT JOIN T_BD_CUSTOMER_L C2	");
            sqlBuilder.AppendLine("												ON C2.FCUSTID = P.FCUSTID AND C2.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("											 ) INNERTEMP	");
            sqlBuilder.AppendLine("											 WHERE INNERTEMP.FID = T.FID FOR XML PATH(''))	");
            sqlBuilder.AppendLine("											, 1, 1, '')		--外销发票号	");
            sqlBuilder.AppendLine("					FROM T_AP_PAYABLEENTRY T	");
            sqlBuilder.AppendLine("					GROUP BY FID	");
            sqlBuilder.AppendLine("				) TEMP1	");
            sqlBuilder.AppendLine("				ON TEMP1.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("				--部门	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("				ON DEP_L.FDEPTID = PAYABLE.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--业务员	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("				ON OPERATORENTRY.FENTRYID = PAYABLE.FPURCHASERID	");
            sqlBuilder.AppendLine("				--员工任岗明细	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("				ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--供应商	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("				ON SUP.FSUPPLIERID = PAYABLE.FSUPPLIERID	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("				ON SUP_L.FSUPPLIERID = PAYABLE.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--组织	");
            sqlBuilder.AppendLine("				LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("				ON ORG.FORGID = PAYABLE.FSETTLEORGID	");
            sqlBuilder.AppendLine("				LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("				ON ORG_L.FORGID = ORG.FORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--扣款类型（辅助资料）	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSISTANTDATAENTRY_L	");
            sqlBuilder.AppendLine("				ON ASSISTANTDATAENTRY_L.FENTRYID = PAYABLE.FSUPPLIERDEDUCTTYPEID AND ASSISTANTDATAENTRY_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				WHERE PAYABLE.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				AND PAYABLE.FTOTALADJUSTAMT <> 0	");
            sqlBuilder.AppendLine("				--剔除内部供应商	");
            sqlBuilder.AppendLine("				AND SUP.FCORRESPONDORGID = 0	");
            sqlBuilder.AppendLine("				UNION ALL	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					DEP_L.FNAME				    		FPURDEPNAME					--采购部门	");
            sqlBuilder.AppendLine("					,STAFF_L.FNAME			    		FPurchaser					--采购员	");
            sqlBuilder.AppendLine("					,'其他应收单'			    		FBILLTYPE					--单据类型	");
            sqlBuilder.AppendLine("					,OTHERRECABLE.FBILLNO	    		FBILLNO						--单据编号	");
            sqlBuilder.AppendLine("					,CONVERT(VARCHAR(100), OTHERRECABLE.FAPPROVEDATE, 23)	");
            sqlBuilder.AppendLine("											    		FAUDITDATE					--审核日期	");
            sqlBuilder.AppendLine("					,PO1.FSALEORDERNO			    	FSALENO						--销售订单号	");
            sqlBuilder.AppendLine("					,PO1.FBILLNO				    	FPURNO						--采购订单号	");
            sqlBuilder.AppendLine("					,''							    	FOUTINVONO					--外销发票号	");
            sqlBuilder.AppendLine("					,CASE	");
            sqlBuilder.AppendLine("						WHEN SO.FPURCHASEORDERID = '' OR SO.FPURCHASEORDERID IS NULL THEN CUST_L1.FNAME	");
            sqlBuilder.AppendLine("						ELSE CUST_L2.FNAME	");
            sqlBuilder.AppendLine("					END									FCUSTNAME					--客户	");
            sqlBuilder.AppendLine("					,SUP_L.FNAME				    	FSUPNAME					--供应商	");
            sqlBuilder.AppendLine("					,ORG_L.FNAME					    FORGNAME					--组织机构	");
            sqlBuilder.AppendLine("					,ORG_L.FORGID					    FORGID						--组织机构ID	");
            sqlBuilder.AppendLine("					,OTHERRECABLE.FAMOUNT			    FAMT_LOC					--应付金额	");
            sqlBuilder.AppendLine("					,OTHERRECABLE.FAMOUNT			    FCUTPAYMENT_LOC				--扣款	");
            sqlBuilder.AppendLine("					,ASSISTANTDATAENTRY_L.FDATAVALUE    FCUTTYPE					--扣款类型	");
            sqlBuilder.AppendLine("					,OTHERRECABLE.FREMARK		    	FREMARKS					--扣款说明	");
            sqlBuilder.AppendLine("				--其他应收单	");
            sqlBuilder.AppendLine("				FROM T_AR_OTHERRECABLE OTHERRECABLE	");
            sqlBuilder.AppendLine("				--申请部门	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("				ON DEP_L.FDEPTID = OTHERRECABLE.FDEPARTMENTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--采购订单	");
            sqlBuilder.AppendLine("				LEFT JOIN T_PUR_POORDER PO1	");
            sqlBuilder.AppendLine("				ON PO1.FID = OTHERRECABLE.FPURORDERID	");
            sqlBuilder.AppendLine("				--销售订单	");
            sqlBuilder.AppendLine("				LEFT JOIN T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("				ON PO1.FSALEORDERID = SO.FID	");
            sqlBuilder.AppendLine("				--穿透的采购订单	");
            sqlBuilder.AppendLine("				LEFT JOIN T_PUR_POORDER PO2	");
            sqlBuilder.AppendLine("				ON PO2.FID = SO.FPURCHASEORDERID	");
            sqlBuilder.AppendLine("				--客户1	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_CUSTOMER_L CUST_L1	");
            sqlBuilder.AppendLine("				ON CUST_L1.FCUSTID = SO.FCUSTID AND CUST_L1.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--客户2	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_CUSTOMER_L CUST_L2	");
            sqlBuilder.AppendLine("				ON CUST_L2.FCUSTID = PO2.FCUSTID AND CUST_L2.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--业务员	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("				ON OPERATORENTRY.FENTRYID = PO1.FPURCHASERID	");
            sqlBuilder.AppendLine("				--员工任岗明细	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("				ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--供应商	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("				ON SUP.FSUPPLIERID = OTHERRECABLE.FCONTACTUNIT	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("				ON SUP_L.FSUPPLIERID = OTHERRECABLE.FCONTACTUNIT AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--组织	");
            sqlBuilder.AppendLine("				LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("				ON ORG.FORGID = OTHERRECABLE.FSETTLEORGID	");
            sqlBuilder.AppendLine("				LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("				ON ORG_L.FORGID = ORG.FORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				--扣款类型（辅助资料）	");
            sqlBuilder.AppendLine("				LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSISTANTDATAENTRY_L	");
            sqlBuilder.AppendLine("				ON ASSISTANTDATAENTRY_L.FENTRYID = OTHERRECABLE.FSUPPLIERDEDUCTTYPEID AND ASSISTANTDATAENTRY_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("				WHERE OTHERRECABLE.FBILLTYPEID = '5a48a68dd82a8c'	--采购扣款其他应收	");
            sqlBuilder.AppendLine("				AND OTHERRECABLE.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				--剔除内部供应商	");
            sqlBuilder.AppendLine("				AND SUP.FCORRESPONDORGID = 0	");
            sqlBuilder.AppendLine("			) T	");
            sqlBuilder.AppendLine("	WHERE  DATEDIFF(DAY, '" + beginDate + "', T.FAUDITDATE) >= 0 AND DATEDIFF(DAY, T.FAUDITDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	AND FORGID IN (" + orgId + ")   ");
            //采购部门
            if (!purDepName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND FPURDEPNAME LIKE ('%" + purDepName + "%')   ");
            }
            //采购员
            if (!purchaserName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND FPurchaser LIKE ('%" + purchaserName + "%')   ");
            }
            //供应商
            if (!supName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND FSUPNAME LIKE ('%" + supName + "%')   ");
            }
            //销售订单号
            if (!saleNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND FSALENO LIKE ('%" + saleNo + "%')   ");
            }
            //采购订单号
            if (!purNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND FPURNO LIKE ('%" + purNo + "%')   ");
            }
            return sqlBuilder.ToString();
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FPURDEPNAME,FBILLTYPE,FSUPNAME,FORGNAME,FCUTPAYMENT_LOC");
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("/*dialect*/	");
            sql.AppendLine("	SELECT	");
            sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
            sql.AppendLine("		,FPURDEPNAME		--采购部门	");
            sql.AppendLine("		,FPurchaser			--采购员	");
            sql.AppendLine("		,FBILLTYPE			--单据类型	");
            sql.AppendLine("		,FBILLNO			--单据编号	");
            sql.AppendLine("		,FAUDITDATE			--审核日期	");
            sql.AppendLine("		,FSALENO			--销售订单号 ");
            sql.AppendLine("		,FPURNO				--采购订单号 ");
            sql.AppendLine("		,FOUTINVONO			--外销发票号 ");
            sql.AppendLine("		,FSUPNAME			--供应商	");
            sql.AppendLine("		,FCUSTNAME			--客户	");
            sql.AppendLine("		,FORGNAME			--组织机构	");
            sql.AppendLine("		,FAMT_LOC			--应付金额	");
            sql.AppendLine("		,FCUTPAYMENT_LOC	--扣款	");
            sql.AppendLine("		,FCUTTYPE       	--扣款类型	");
            sql.AppendLine("		,FREMARKS			--扣款说明	");
            sql.AppendLine("		,2 FPRECISION	    --精度	");
            sql.AppendFormat("	INTO {0}	\r\n", tableName);
            sql.AppendLine("	FROM	");
            sql.AppendLine("	(	");
            sql.Append(this.GetSql());
            sql.AppendLine("	) TT	");
            sql.AppendLine("	WHERE FCUTPAYMENT_LOC > 0	");
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
                //采购部门
                purDepName = Convert.ToString(dyFilter["FDepName_Filter"]);
                //采购员
                purchaserName = Convert.ToString(dyFilter["FPurchaser_Filter"]);
                //供应商
                supName = Convert.ToString(dyFilter["FSupName_Filter"]);
                //组织机构
                orgId = Convert.ToString(dyFilter["FMulSelOrgList_Filter"]);
                orgName = this.GetOrgName(orgId);
                //销售订单号
                saleNo = Convert.ToString(dyFilter["FSaleNo_Filter"]);
                //采购订单号
                purNo = Convert.ToString(dyFilter["FPurNo_Filter"]);
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
            //供应商
            title.AddTitle("FSupName_H", supName);
            //采购部门
            title.AddTitle("FDepName_H", purDepName);
            //采购员
            title.AddTitle("FPurchaser_H", purchaserName);
            //组织机构
            title.AddTitle("FOrgName_H", orgName);
            //销售订单号
            title.AddTitle("FSaleNO_H", saleNo);
            //采购订单号
            title.AddTitle("FPurNO_H", purNo);
            return title;
        }
    }
}

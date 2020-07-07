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

namespace Para.App.Report.PurPayDetailRpt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("采购付款明细表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //组织机构
        string orgName = string.Empty;
        //采购部门
        string purDepName = string.Empty;
        //采购员
        string purchaserName = string.Empty;
        //供应商
        string supName = string.Empty;
        //付款单号
        string payNo = string.Empty;
        //销售订单号
        string saleOrderNo = string.Empty;
        //采购订单号
        string purOrderNo = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("采购付款明细表", base.Context.UserLocale.LCID);
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
            //付款单表头.实际付款金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FBILLAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            //付款单.整单已核销金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FMATCHEDAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            //付款单.整单未核销金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FNOTMATCHEDAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            //应付金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPAYABLEAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            //本次付款金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FREALAMT_LOC",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        //小计，合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            //付款单表头.实际付款金额
            list.Add(new SummaryField("FBILLAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            //付款单.整单已核销金额
            list.Add(new SummaryField("FMATCHEDAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            //付款单.整单未核销金额
            list.Add(new SummaryField("FNOTMATCHEDAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            //应付金额
            //list.Add(new SummaryField("FPAYABLEAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            //本次付款金额
            list.Add(new SummaryField("FREALAMT_LOC", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			FID					--付款单ID	");
            sqlBuilder.AppendLine("			,FORGNAME			--组织机构	");
            sqlBuilder.AppendLine("			,FBILLTYPE			--单据类型	");
            sqlBuilder.AppendLine("			,FPAYBILLNO			--付款单号	");
            sqlBuilder.AppendLine("			,FBUSSINESSDATE		--业务日期	");
            sqlBuilder.AppendLine("			,FAPPROVEDATE		--审核日期	");
            sqlBuilder.AppendLine("			,FBILLAMT_LOC		--付款单表头.实际付款金额	");
            sqlBuilder.AppendLine("			,FMATCHEDAMT_LOC	--付款单.整单已核销金额	");
            sqlBuilder.AppendLine("			,FNOTMATCHEDAMT_LOC	--付款单.整单未核销金额	");
            sqlBuilder.AppendLine("			,FMATCHSTATUS		--核销状态	");
            sqlBuilder.AppendLine("			,FPAYMENTTYPE		--付款类型	");
            sqlBuilder.AppendLine("			,FSRCFORMID			--源单FORMID	");
            sqlBuilder.AppendLine("			,FSRCBILLTYPE		--源单类型	");
            sqlBuilder.AppendLine("			,FSRCBILLNO			--源单编号	");
            sqlBuilder.AppendLine("			,FSRCID				--源单ID	");
            sqlBuilder.AppendLine("			,FSUPNAME			--供应商	");
            sqlBuilder.AppendLine("			,FPURDEPTNAME		--采购部门	");
            sqlBuilder.AppendLine("			,FPURCHASER			--采购员	");
            sqlBuilder.AppendLine("			,FSALEORDERNO		--销售订单号	");
            sqlBuilder.AppendLine("			,FPURORDERNO		--采购订单号	");
            sqlBuilder.AppendLine("			,FOUTINVONO			--外销发票号	");
            sqlBuilder.AppendLine("			,FPAYABLEAMT_LOC	--应付金额	");
            sqlBuilder.AppendLine("			,FREALAMT_LOC		--本次付款金额	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		(	");
            sqlBuilder.AppendLine("			--预付款部分	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				PAYBILL.FID				FID				--付款单ID	");
            sqlBuilder.AppendLine("				,ORG_L.FNAME			FORGNAME		--组织机构	");
            sqlBuilder.AppendLine("				,BILLTYPE_L.FNAME		FBILLTYPE		--单据类型	");
            sqlBuilder.AppendLine("				,PAYBILL.FBILLNO		FPAYBILLNO		--付款单号	");
            sqlBuilder.AppendLine("				,CONVERT(VARCHAR(100), PAYBILL.FDATE, 23)	");
            sqlBuilder.AppendLine("										FBUSSINESSDATE	--业务日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FAPPROVEDATE	FAPPROVEDATE	--审核日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE	");
            sqlBuilder.AppendLine("										FBILLAMT_LOC	--付款单表头.实际付款金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FMATCHEDAMT_LOC	--付款单.整单已核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE - PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FNOTMATCHEDAMT_LOC--付款单.整单未核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FWRITTENOFFSTATUS	");
            sqlBuilder.AppendLine("				                        FMATCHSTATUS	--核销状态	");
            sqlBuilder.AppendLine("				,'预付款'				FPAYMENTTYPE	--付款类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSOURCETYPE	FSRCFORMID		--源单FORMID	");
            sqlBuilder.AppendLine("				,OBJECTTYPE_L.FNAME		FSRCBILLTYPE	--源单类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSRCBILLNO	FSRCBILLNO		--源单编号	");
            sqlBuilder.AppendLine("				,PAYAPPLY.FID			FSRCID			--源单ID	");
            sqlBuilder.AppendLine("				,SUP_L.FNAME            FSUPNAME        --供应商	");
            sqlBuilder.AppendLine("				,DEP_L.FNAME			FPURDEPTNAME	--采购部门	");
            sqlBuilder.AppendLine("				,STAFF_L.FNAME			FPURCHASER		--采购员	");
            sqlBuilder.AppendLine("				,PO.FSALEORDERNO		FSALEORDERNO	--销售订单号	");
            sqlBuilder.AppendLine("				,PO.FBILLNO				FPURORDERNO		--采购订单号	");
            sqlBuilder.AppendLine("				,''						FOUTINVONO		--外销发票号	");
            sqlBuilder.AppendLine("				,PAYAPPLY.FPAYAMOUNTFOR_H * PAYAPPLY.FSETTLERATE	");
            sqlBuilder.AppendLine("				                        FPAYABLEAMT_LOC	--应付金额	");
            sqlBuilder.AppendLine("				,PAYBILL.FSETTLERATE * SRCENTRY.FREALPAYAMOUNT	");
            sqlBuilder.AppendLine("										FREALAMT_LOC	--本次付款金额	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--付款单	");
            sqlBuilder.AppendLine("			T_AP_PAYBILL PAYBILL	");
            sqlBuilder.AppendLine("			--付款单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					T1.FID                                                      --付款单ID	");
            sqlBuilder.AppendLine("					,SUM(FWRITTENOFFAMOUNTFOR * FEXCHANGERATE)	FMATCHEDAMT_LOC --已核销金额(本位币)	");
            sqlBuilder.AppendLine("				FROM T_AP_PAYBILL T1	");
            sqlBuilder.AppendLine("				LEFT JOIN T_AP_PAYBILLENTRY T2	");
            sqlBuilder.AppendLine("				ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("				WHERE T1.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				GROUP BY T1.FID	");
            sqlBuilder.AppendLine("			) PAYENTRY	");
            sqlBuilder.AppendLine("			ON PAYENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--单据类型	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BAS_BILLTYPE_L BILLTYPE_L	");
            sqlBuilder.AppendLine("			ON BILLTYPE_L.FBILLTYPEID = PAYBILL.FBILLTYPEID AND BILLTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--组织机构	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("			ON ORG_L.FORGID = PAYBILL.FPAYORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--源单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY SRCENTRY	");
            sqlBuilder.AppendLine("			ON SRCENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--FORMID?");
            sqlBuilder.AppendLine("			LEFT JOIN T_META_OBJECTTYPE_L OBJECTTYPE_L	");
            sqlBuilder.AppendLine("			ON OBJECTTYPE_L.FID = SRCENTRY.FSOURCETYPE AND OBJECTTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--关联表");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY_LK LK	");
            sqlBuilder.AppendLine("			ON LK.FENTRYID = SRCENTRY.FENTRYID AND LK.FRULEID = 'CN_PAYAPPLYTOPAYBILL'	");
            sqlBuilder.AppendLine("			--付款申请单	");
            sqlBuilder.AppendLine("			LEFT JOIN T_CN_PAYAPPLY PAYAPPLY	");
            sqlBuilder.AppendLine("			ON PAYAPPLY.FID = LK.FSBILLID	");
            sqlBuilder.AppendLine("			--采购订单	");
            sqlBuilder.AppendLine("			LEFT JOIN T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("			ON PO.FID = PAYAPPLY.FPURORDERID	");
            sqlBuilder.AppendLine("		    --供应商	");
            sqlBuilder.AppendLine("		    LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("		    ON SUP_L.FSUPPLIERID = PO.FSUPPLIERID AND SUP_L.FLOCALEID =  '2052'	");
            sqlBuilder.AppendLine("			--采购部门	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("			ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--业务员	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("			ON OPERATORENTRY.FENTRYID = PO.FPURCHASERID	");
            sqlBuilder.AppendLine("			--员工任岗明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("			ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			WHERE	PAYBILL.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("					AND PAYBILL.FBILLTYPEID = 'D9652BC4-1420-4D3B-A214-2509BC9BF925'	--单据类型 = 采购业务付款单	");
            sqlBuilder.AppendLine("					AND PAYAPPLY.FBILLNO IS NOT NULL	");
            sqlBuilder.AppendLine("					AND PAYAPPLY.FPAYPURPOSEID_H = 20018	--付款用途 = 预付款	");
            sqlBuilder.AppendLine("			UNION ALL	");
            sqlBuilder.AppendLine("			--应付->付款申请单部分	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				PAYBILL.FID				FID				--付款单ID	");
            sqlBuilder.AppendLine("				,ORG_L.FNAME			FORGNAME		--组织机构	");
            sqlBuilder.AppendLine("				,BILLTYPE_L.FNAME		FBILLTYPE		--单据类型	");
            sqlBuilder.AppendLine("				,PAYBILL.FBILLNO		FPAYBILLNO		--付款单号	");
            sqlBuilder.AppendLine("				,CONVERT(VARCHAR(100), PAYBILL.FDATE, 23)	");
            sqlBuilder.AppendLine("										FBUSSINESSDATE	--业务日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FAPPROVEDATE	FAPPROVEDATE	--审核日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE	");
            sqlBuilder.AppendLine("										FBILLAMT_LOC	--付款单表头.实际付款金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FMATCHEDAMT_LOC	--付款单.整单已核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE - PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FNOTMATCHEDAMT_LOC--付款单.整单未核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FWRITTENOFFSTATUS	");
            sqlBuilder.AppendLine("				                        FMATCHSTATUS	--核销状态	");
            sqlBuilder.AppendLine("				,'正常货款'				FPAYMENTTYPE	--付款类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSOURCETYPE	FSRCFORMID		--源单FORMID	");
            sqlBuilder.AppendLine("				,OBJECTTYPE_L.FNAME		FSRCBILLTYPE	--源单类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSRCBILLNO	FSRCBILLNO		--源单编号	");
            sqlBuilder.AppendLine("				,PAYABLE.FID			FSRCID			--源单ID	");
            sqlBuilder.AppendLine("				,SUP_L.FNAME            FSUPNAME        --供应商	");
            sqlBuilder.AppendLine("				,DEP_L.FNAME			FPURDEPTNAME	--采购部门	");
            sqlBuilder.AppendLine("				,STAFF_L.FNAME			FPURCHASER		--采购员	");
            sqlBuilder.AppendLine("				,TEMP.FSALEORDERNOS		FSALEORDERNO	--销售订单号	");
            sqlBuilder.AppendLine("				,TEMP.FPURORDERNOS		FPURORDERNO		--采购订单号	");
            sqlBuilder.AppendLine("				,TEMP.FOUTINVONO		FOUTINVONO		--外销发票号	");
            sqlBuilder.AppendLine("				,PAYABLEFIN.FALLAMOUNT	FPAYABLEAMT_LOC	--应付金额	");
            sqlBuilder.AppendLine("				,PAYBILL.FSETTLERATE * SRCENTRY.FREALPAYAMOUNT	");
            sqlBuilder.AppendLine("										FREALAMT_LOC	--本次付款金额	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--付款单	");
            sqlBuilder.AppendLine("			T_AP_PAYBILL PAYBILL	");
            sqlBuilder.AppendLine("			--付款单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					T1.FID                                                      --付款单ID	");
            sqlBuilder.AppendLine("					,SUM(FWRITTENOFFAMOUNTFOR * FEXCHANGERATE)	FMATCHEDAMT_LOC --已核销金额(本位币)	");
            sqlBuilder.AppendLine("				FROM T_AP_PAYBILL T1	");
            sqlBuilder.AppendLine("				LEFT JOIN T_AP_PAYBILLENTRY T2	");
            sqlBuilder.AppendLine("				ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("				WHERE T1.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				GROUP BY T1.FID	");
            sqlBuilder.AppendLine("			) PAYENTRY	");
            sqlBuilder.AppendLine("			ON PAYENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--单据类型	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BAS_BILLTYPE_L BILLTYPE_L	");
            sqlBuilder.AppendLine("			ON BILLTYPE_L.FBILLTYPEID = PAYBILL.FBILLTYPEID AND BILLTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--组织机构	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("			ON ORG_L.FORGID = PAYBILL.FPAYORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--源单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY SRCENTRY	");
            sqlBuilder.AppendLine("			ON SRCENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--FORMID表");
            sqlBuilder.AppendLine("			LEFT JOIN T_META_OBJECTTYPE_L OBJECTTYPE_L	");
            sqlBuilder.AppendLine("			ON OBJECTTYPE_L.FID = SRCENTRY.FSOURCETYPE AND OBJECTTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--关联表");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY_LK LK	");
            sqlBuilder.AppendLine("			ON LK.FENTRYID = SRCENTRY.FENTRYID AND LK.FRULEID = 'CN_PAYAPPLYTOPAYBILL'	");
            sqlBuilder.AppendLine("			--付款申请单.表头	");
            sqlBuilder.AppendLine("			LEFT JOIN T_CN_PAYAPPLY PAYAPPLY	");
            sqlBuilder.AppendLine("			ON PAYAPPLY.FID = LK.FSBILLID	");
            sqlBuilder.AppendLine("			--付款申请单.明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_CN_PAYAPPLYENTRY APPLYENTRY	");
            sqlBuilder.AppendLine("			ON APPLYENTRY.FENTRYID = LK.FSID	");
            sqlBuilder.AppendLine("			--付款申请单.关联表");
            sqlBuilder.AppendLine("			LEFT JOIN T_CN_PAYAPPLYENTRY_LK APPLY_LK	");
            sqlBuilder.AppendLine("			ON APPLY_LK.FENTRYID = APPLYENTRY.FENTRYID AND APPLY_LK.FRULEID = 'AP_PAYABLETOPAYAPPLY'	");
            sqlBuilder.AppendLine("			--应付单	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("			ON PAYABLE.FID = APPLY_LK.FSBILLID	");
            sqlBuilder.AppendLine("			--应付单.表头财务");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYABLEFIN PAYABLEFIN	");
            sqlBuilder.AppendLine("			ON PAYABLEFIN.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("			--供应商	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("			ON SUP_L.FSUPPLIERID = PAYABLE.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--采购部门	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("			ON DEP_L.FDEPTID = PAYABLE.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--业务员	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("			ON OPERATORENTRY.FENTRYID = PAYABLE.FPURCHASERID	");
            sqlBuilder.AppendLine("			--员工任岗明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("			ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--销售订单号,采购订单号	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					FID	");
            sqlBuilder.AppendLine("					,FSALEORDERNOS = STUFF((SELECT DISTINCT ',' + FSALEORDERNO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("					,FPURORDERNOS = STUFF((SELECT DISTINCT ',' + FORDERNUMBER FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("					,FOUTINVONO = STUFF((SELECT DISTINCT ',' + FOUTINVOICENO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("				FROM T_AP_PAYABLEENTRY T	");
            sqlBuilder.AppendLine("				GROUP BY FID	");
            sqlBuilder.AppendLine("			) TEMP	");
            sqlBuilder.AppendLine("			ON TEMP.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("			WHERE	PAYBILL.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("					AND PAYBILL.FBILLTYPEID = 'D9652BC4-1420-4D3B-A214-2509BC9BF925'	--单据类型 = 采购业务付款单	");
            sqlBuilder.AppendLine("					AND PAYABLE.FBILLNO IS NOT NULL	");
            sqlBuilder.AppendLine("					AND PAYAPPLY.FPAYPURPOSEID_H <> 20018	--付款用途 ≠ 预付款	");
            sqlBuilder.AppendLine("			UNION ALL	");
            sqlBuilder.AppendLine("			--应付->付款单部分	");
            sqlBuilder.AppendLine("			SELECT	");
            sqlBuilder.AppendLine("				PAYBILL.FID				FID				--付款单ID	");
            sqlBuilder.AppendLine("				,ORG_L.FNAME			FORGNAME		--组织机构	");
            sqlBuilder.AppendLine("				,BILLTYPE_L.FNAME		FBILLTYPE		--单据类型	");
            sqlBuilder.AppendLine("				,PAYBILL.FBILLNO		FPAYBILLNO		--付款单号	");
            sqlBuilder.AppendLine("				,CONVERT(VARCHAR(100), PAYBILL.FDATE, 23)	");
            sqlBuilder.AppendLine("										FBUSSINESSDATE	--业务日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FAPPROVEDATE	FAPPROVEDATE	--审核日期	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE	");
            sqlBuilder.AppendLine("										FBILLAMT_LOC	--付款单表头.实际付款金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FMATCHEDAMT_LOC	--付款单.整单已核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FREALPAYAMOUNTFOR * PAYBILL.FEXCHANGERATE - PAYENTRY.FMATCHEDAMT_LOC	");
            sqlBuilder.AppendLine("										FNOTMATCHEDAMT_LOC--付款单.整单未核销金额(本位币)	");
            sqlBuilder.AppendLine("				,PAYBILL.FWRITTENOFFSTATUS	");
            sqlBuilder.AppendLine("				                        FMATCHSTATUS	--核销状态	");
            sqlBuilder.AppendLine("				,'正常货款'				FPAYMENTTYPE	--付款类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSOURCETYPE	FSRCFORMID		--源单FORMID	");
            sqlBuilder.AppendLine("				,OBJECTTYPE_L.FNAME		FSRCBILLTYPE	--源单类型	");
            sqlBuilder.AppendLine("				,SRCENTRY.FSRCBILLNO	FSRCBILLNO		--源单编号	");
            sqlBuilder.AppendLine("				,PAYABLE.FID			FSRCID			--源单ID	");
            sqlBuilder.AppendLine("		        ,SUP_L.FNAME            FSUPNAME        --供应商	");
            sqlBuilder.AppendLine("				,DEP_L.FNAME			FPURDEPTNAME	--采购部门	");
            sqlBuilder.AppendLine("				,STAFF_L.FNAME			FPURCHASER		--采购员	");
            sqlBuilder.AppendLine("				,TEMP.FSALEORDERNOS		FSALEORDERNO	--销售订单号	");
            sqlBuilder.AppendLine("				,TEMP.FPURORDERNOS		FPURORDERNO		--采购订单号	");
            sqlBuilder.AppendLine("				,TEMP.FOUTINVONO		FOUTINVONO		--外销发票号	");
            sqlBuilder.AppendLine("				,PAYABLEFIN.FALLAMOUNT	FPAYABLEAMT_LOC	--应付金额	");
            sqlBuilder.AppendLine("				,PAYBILL.FSETTLERATE * SRCENTRY.FREALPAYAMOUNT	");
            sqlBuilder.AppendLine("										FREALAMT_LOC	--本次付款金额	");
            sqlBuilder.AppendLine("			FROM	");
            sqlBuilder.AppendLine("			--付款单	");
            sqlBuilder.AppendLine("			T_AP_PAYBILL PAYBILL	");
            sqlBuilder.AppendLine("			--付款单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					T1.FID                                                      --付款单ID	");
            sqlBuilder.AppendLine("					,SUM(FWRITTENOFFAMOUNTFOR * FEXCHANGERATE)	FMATCHEDAMT_LOC --已核销金额(本位币)	");
            sqlBuilder.AppendLine("				FROM T_AP_PAYBILL T1	");
            sqlBuilder.AppendLine("				LEFT JOIN T_AP_PAYBILLENTRY T2	");
            sqlBuilder.AppendLine("				ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("				WHERE T1.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("				GROUP BY T1.FID	");
            sqlBuilder.AppendLine("			) PAYENTRY	");
            sqlBuilder.AppendLine("			ON PAYENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--单据类型	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BAS_BILLTYPE_L BILLTYPE_L	");
            sqlBuilder.AppendLine("			ON BILLTYPE_L.FBILLTYPEID = PAYBILL.FBILLTYPEID AND BILLTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--组织机构	");
            sqlBuilder.AppendLine("			LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("			ON ORG_L.FORGID = PAYBILL.FPAYORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--源单明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY SRCENTRY	");
            sqlBuilder.AppendLine("			ON SRCENTRY.FID = PAYBILL.FID	");
            sqlBuilder.AppendLine("			--FORMID表");
            sqlBuilder.AppendLine("			LEFT JOIN T_META_OBJECTTYPE_L OBJECTTYPE_L	");
            sqlBuilder.AppendLine("			ON OBJECTTYPE_L.FID = SRCENTRY.FSOURCETYPE AND OBJECTTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--关联表");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYBILLSRCENTRY_LK LK	");
            sqlBuilder.AppendLine("			ON LK.FENTRYID = SRCENTRY.FENTRYID AND LK.FRULEID = 'AP_PAYABLETOPAYBILL'	");
            sqlBuilder.AppendLine("			--应付单	");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("			ON PAYABLE.FID = LK.FSBILLID	");
            sqlBuilder.AppendLine("			--应付单.表头财务");
            sqlBuilder.AppendLine("			LEFT JOIN T_AP_PAYABLEFIN PAYABLEFIN	");
            sqlBuilder.AppendLine("			ON PAYABLEFIN.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("			--供应商	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("			ON SUP_L.FSUPPLIERID = PAYABLE.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--采购部门	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("			ON DEP_L.FDEPTID = PAYABLE.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--业务员	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("			ON OPERATORENTRY.FENTRYID = PAYABLE.FPURCHASERID	");
            sqlBuilder.AppendLine("			--员工任岗明细	");
            sqlBuilder.AppendLine("			LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("			ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			--销售订单号,采购订单号	");
            sqlBuilder.AppendLine("			LEFT JOIN (	");
            sqlBuilder.AppendLine("				SELECT	");
            sqlBuilder.AppendLine("					FID	");
            sqlBuilder.AppendLine("					,FSALEORDERNOS = STUFF((SELECT DISTINCT ',' + FSALEORDERNO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("					,FPURORDERNOS = STUFF((SELECT DISTINCT ',' + FORDERNUMBER FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("					,FOUTINVONO = STUFF((SELECT DISTINCT ',' + FOUTINVOICENO FROM T_AP_PAYABLEENTRY WHERE FID = T.FID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("				FROM T_AP_PAYABLEENTRY T	");
            sqlBuilder.AppendLine("				GROUP BY FID	");
            sqlBuilder.AppendLine("			) TEMP	");
            sqlBuilder.AppendLine("			ON TEMP.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("			WHERE	PAYBILL.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("					AND PAYBILL.FBILLTYPEID = 'D9652BC4-1420-4D3B-A214-2509BC9BF925'	--单据类型 = 采购业务付款单	");
            sqlBuilder.AppendLine("					AND PAYABLE.FBILLNO IS NOT NULL	");
            sqlBuilder.AppendLine("		) T	");
            sqlBuilder.AppendLine("	WHERE DATEDIFF(DAY, '" + beginDate + "', FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	--组织机构	");
            sqlBuilder.AppendLine("	AND FORGNAME = '" + orgName + "'	");
            if (!saleOrderNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--销售订单号	");
                sqlBuilder.AppendLine("	AND FSALEORDERNO LIKE '%" + saleOrderNo + "%'	");
            }
            if (!purOrderNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--采购订单号	");
                sqlBuilder.AppendLine("	AND FPURORDERNO LIKE '%" + purOrderNo + "%'	");
            }
            if (!supName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--供应商	");
                sqlBuilder.AppendLine("	AND FSUPNAME = '" + supName + "'	");
            }
            if (!purDepName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--采购部门	");
                sqlBuilder.AppendLine("	AND FPURDEPTNAME = '" + purDepName + "'	");
            }
            if (!purchaserName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--采购员	");
                sqlBuilder.AppendLine("	AND FPURCHASER = '" + purchaserName + "'	");
            }
            if (!payNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	--付款单号	");
                sqlBuilder.AppendLine("	AND FPAYBILLNO LIKE '%" + payNo + "%'	");
            }
            return sqlBuilder.ToString();
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FSUPNAME,FAPPROVEDATE,FPURDEPTNAME,FPURCHASER");
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("/*dialect*/	");
            sql.AppendLine("	SELECT	");
            sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
            sql.AppendLine("		,FID				--付款单ID ");
            sql.AppendLine("		,FORGNAME			--组织机构 ");
            sql.AppendLine("		,FBILLTYPE			--单据类型 ");
            sql.AppendLine("		,FPAYBILLNO			--付款单号 ");
            sql.AppendLine("		,FBUSSINESSDATE		--业务日期 ");
            sql.AppendLine("		,FAPPROVEDATE		--审核日期 ");
            sql.AppendLine("		,FBILLAMT_LOC		--付款单表头.实际付款金额 ");
            sql.AppendLine("		,FMATCHEDAMT_LOC	--付款单.整单已核销金额 ");
            sql.AppendLine("		,FNOTMATCHEDAMT_LOC	--付款单.整单未核销金额 ");
            sql.AppendLine("		,CASE	");
            sql.AppendLine("			WHEN FMATCHSTATUS = 'A' THEN '空'	");
            sql.AppendLine("			WHEN FMATCHSTATUS = 'B' THEN '部分'	");
            sql.AppendLine("			WHEN FMATCHSTATUS = 'C' THEN '完全'	");
            sql.AppendLine("		END	FMATCHSTATUS	--核销状态	");
            sql.AppendLine("		,FPAYMENTTYPE		--付款类型 ");
            sql.AppendLine("		,FSRCFORMID			--源单FORMID ");
            sql.AppendLine("		,FSRCBILLTYPE		--源单类型 ");
            sql.AppendLine("		,FSRCBILLNO			--源单编号 ");
            sql.AppendLine("		,FSRCID				--源单ID ");
            sql.AppendLine("		,FSUPNAME			--供应商 ");
            sql.AppendLine("		,FPURDEPTNAME		--采购部门 ");
            sql.AppendLine("		,FPURCHASER			--采购员 ");
            sql.AppendLine("		,FSALEORDERNO		--销售订单号 ");
            sql.AppendLine("		,FPURORDERNO		--采购订单号 ");
            sql.AppendLine("		,FOUTINVONO			--外销发票号 ");
            sql.AppendLine("		,FPAYABLEAMT_LOC	--应付金额 ");
            sql.AppendLine("		,FREALAMT_LOC		--本次付款金额 ");
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
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            //付款类型
            string paymentType = Convert.ToString(dyFilter["FPaymentType_Filter"]);
            if (paymentType == "1")
            {
                sql.AppendLine("	AND FPAYMENTTYPE = '预付款'   ");
            }
            else if (paymentType == "2")
            {
                sql.AppendLine("	AND FPAYMENTTYPE = '正常货款'   ");
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
                //组织机构
                DynamicObject orgObj = dyFilter["FOrgId_Filter"] as DynamicObject;
                orgName = orgObj == null ? string.Empty : Convert.ToString(orgObj["Name"]);
                //供应商
                DynamicObject supObj = dyFilter["FSupId_Filter"] as DynamicObject;
                supName = supObj == null ? string.Empty : Convert.ToString(supObj["Name"]);
                //采购部门
                DynamicObject depObj = dyFilter["FPurDepId_Filter"] as DynamicObject;
                purDepName = depObj == null ? string.Empty : Convert.ToString(depObj["Name"]);
                //采购员
                DynamicObject purchaserObj = dyFilter["FPurchaserId_Filter"] as DynamicObject;
                purchaserName = purchaserObj == null ? string.Empty : Convert.ToString(purchaserObj["Name"]);
                //付款单号
                payNo = Convert.ToString(dyFilter["FPayNo_Filter"]);
                //销售订单号
                saleOrderNo = Convert.ToString(dyFilter["FSaleOrderNo_Filter"]);
                //采购订单号
                purOrderNo = Convert.ToString(dyFilter["FPurOrderNo_Filter"]);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            //组织机构
            title.AddTitle("FOrgName_H", orgName);
            //供应商
            title.AddTitle("FSupName_H", supName);
            //采购部门
            title.AddTitle("FPurDept_H", purDepName);
            //采购员
            title.AddTitle("FPurchaser_H", purchaserName);
            //付款单号
            title.AddTitle("FPayNo_H", payNo);
            //销售订单号
            title.AddTitle("FSaleNo_H", saleOrderNo);
            //采购订单号
            title.AddTitle("FPurOrderNo_H", purOrderNo);
            return title;
        }
    }
}

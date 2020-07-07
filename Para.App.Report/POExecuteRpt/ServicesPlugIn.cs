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

namespace Para.App.Report.POExecuteRpt
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("采购订单执行情况表—服务端插件")]
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
        //客户
        string custName = string.Empty;
        //主临时表（存放最终数据）
        string mainTemp = string.Empty;
        //临时表（存放已完成的采购订单ID）
        string finishPOTemp = string.Empty;
        //临时表（存放过滤后的采购订单ID）
        string filterPurIdTemp = string.Empty;
        //临时表（存放应付单全部已生成已审核的采购订单ID）
        string payaTotalGeneratedPurIdTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("采购订单执行情况表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsUIDesignerColumns = false;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.SimpleAllCols = false;
            this.SetDecimalControl();
        }
        //设置精度
        private void SetDecimalControl()
        {
            List<DecimalControlField> list = new List<DecimalControlField>();
            //合同金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPurAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            //合同金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPurAmtLoc",
                DecimalControlFieldName = "FPRECISION"
            });
            //已开票金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FInvoAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            //已开票金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FInvoAmtLoc",
                DecimalControlFieldName = "FPRECISION"
            });
            //已结算金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FSettledAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            //已结算金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FSettledAmtLoc",
                DecimalControlFieldName = "FPRECISION"
            });
            ////未结算金额
            //list.Add(new DecimalControlField
            //{
            //    ByDecimalControlFieldName = "FUnSettledAmt",
            //    DecimalControlFieldName = "FPRECISION"
            //});
            ////未结算金额本位币
            //list.Add(new DecimalControlField
            //{
            //    ByDecimalControlFieldName = "FUnSettledAmtLoc",
            //    DecimalControlFieldName = "FPRECISION"
            //});
            //预付金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPrePayAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            //预付金额本位币
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPrePayAmtLoc",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        //小计，合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            //合同金额
            list.Add(new SummaryField("FPurAmt", BOSEnums.Enu_SummaryType.SUM));
            //合同金额本位币
            list.Add(new SummaryField("FPurAmtLoc", BOSEnums.Enu_SummaryType.SUM));
            //已开票金额
            list.Add(new SummaryField("FInvoAmt", BOSEnums.Enu_SummaryType.SUM));
            //已开票金额本位币
            list.Add(new SummaryField("FInvoAmtLoc", BOSEnums.Enu_SummaryType.SUM));
            //已付结算金额
            list.Add(new SummaryField("FSettledAmt", BOSEnums.Enu_SummaryType.SUM));
            //已付结算金额本位币
            list.Add(new SummaryField("FSettledAmtLoc", BOSEnums.Enu_SummaryType.SUM));
            //未结算金额
            list.Add(new SummaryField("FUnSettledAmt", BOSEnums.Enu_SummaryType.SUM));
            //未结算金额本位币
            list.Add(new SummaryField("FUnSettledAmtLoc", BOSEnums.Enu_SummaryType.SUM));
            //预付金额
            list.Add(new SummaryField("FPrePayAmt", BOSEnums.Enu_SummaryType.SUM));
            //预付金额本位币
            list.Add(new SummaryField("FPrePayAmtLoc", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建主临时表（存放最终数据）
                mainTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#mainTemp", this.CreateMainTemp());
                //创建临时表（存放过滤后的采购订单ID）
                filterPurIdTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#filterPurIdTemp", this.CreateFilterPurIdTemp());
                //创建临时表（存放已结算完成的采购订单ID）
                finishPOTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#finishPOTemp", this.CreateFinishPOTemp());
                //创建临时表（存放应付单全部已生成已审核的采购订单ID）
                payaTotalGeneratedPurIdTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#payaTotalGeneratedPurIdTemp", this.CreatePayaTotalGeneratedPurIdTemp());

                //往临时表插入数据（存放已过滤采购订单ID）
                this.InsertDataToFilterPurIdTemp();
                //往临时表插入数据（存放已完成的采购订单ID）
                this.InsertDataToFinishPOTemp();
                //往临时表插入数据（存放应付单全部已生成已审核的采购订单ID）
                this.InsertDataToPayaTotalGeneratedPurIdTemp();
                //往主临时表插入数据
                this.InsertDataToMainTemp();
                //先更新结算状态 = 未结清
                this.UpdateSettleStatusFromMainTemp();
                //更新未结算金额，如果采购合同已全部生成应付单且应付单已审核，则 未结算金额 = 已开票金额 - 已结算金额 （已开票金额即应付金额）
                this.UpdateNotPaidAmt();
                //更新主临时表中的结算状态和未结算金额：当合同已结算完成时更新未付金额为0
                this.UpdateNotPaidAmtFromMainTemp();
                //排序
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FORGID,FPURNO,FSALENO");
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("	SELECT	");
                sqlBuilder.AppendFormat("		{0}			    --序号\r\n", base.KSQL_SEQ);
                sqlBuilder.AppendLine("		,FORGID			    --组织机构ID	");
                sqlBuilder.AppendLine("		,FPURID			    --采购订单ID	");
                sqlBuilder.AppendLine("		,FSALEORDERID	    --销售订单ID	");
                sqlBuilder.AppendLine("		,FORGNAME		    --组织机构	");
                sqlBuilder.AppendLine("		,FBILLTYPE		    --单据类型	");
                sqlBuilder.AppendLine("		,FPURNO			    --采购订单号	");
                sqlBuilder.AppendLine("		,FSALENO			--销售订单号	");
                sqlBuilder.AppendLine("		,FCUSTNAME			--客户	");
                sqlBuilder.AppendLine("		,FPOCREATEDATE	    --采购创建日期	");
                sqlBuilder.AppendLine("		,FPOAUDITDATE	    --采购审核日期	");
                sqlBuilder.AppendLine("		,FSODELIVERYDATE    --外销交货日期	");
                sqlBuilder.AppendLine("		,FPODELIVERYDATE	--采购交货日期	");
                sqlBuilder.AppendLine("		,FSUPNAME		    --供应商	");
                sqlBuilder.AppendLine("		,FPURDEPNAME		--采购部门	");
                sqlBuilder.AppendLine("		,FPURCHASER		    --采购员	");
                sqlBuilder.AppendLine("		,FCURRENCY		    --币别	");
                sqlBuilder.AppendLine("		,FCURRENCYLOC	    --本位币	");
                sqlBuilder.AppendLine("		,FPURAMT		    --合同金额	");
                sqlBuilder.AppendLine("		,FPURAMTLOC		    --合同金额本位币	");
                sqlBuilder.AppendLine("		,FINVOAMT		    --已开票金额	");
                sqlBuilder.AppendLine("		,FINVOAMTLOC		--已开票金额本位币	");
                sqlBuilder.AppendLine("		,FSETTLEDAMT		--已结算金额	");
                sqlBuilder.AppendLine("		,FSETTLEDAMTLOC     --已结算金额本位币	");
                sqlBuilder.AppendLine("		,FPREPAYAMT 	    --预付金额	");
                sqlBuilder.AppendLine("		,FPREPAYAMTLOC 	    --预付金额本位币	");
                sqlBuilder.AppendLine("		,FUNSETTLEDAMT 	    --未结算金额	");
                sqlBuilder.AppendLine("		,FUNSETTLEDAMTLOC 	--未结算金额本位币	");
                sqlBuilder.AppendLine("		,FSETTLESTATUS   	--结算状态	");
                sqlBuilder.AppendLine("		,FCLOSESTATUS   	--关闭状态	");
                sqlBuilder.AppendLine("		,FCLOSEREMARKS   	--关闭说明	");
                sqlBuilder.AppendLine("		,2 FPRECISION	    --精度	");
                sqlBuilder.AppendFormat("	INTO {0}	\r\n", tableName);
                sqlBuilder.AppendLine("	FROM " + mainTemp + "	");
                sqlBuilder.AppendLine("	WHERE 1 = 1	");
                if (!filter.FilterParameter.FilterString.IsNullOrEmptyOrWhiteSpace())
                {
                    sqlBuilder.AppendLine("	AND " + filter.FilterParameter.FilterString + "   ");
                }
                DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
                //结算状态
                string settleStatus = Convert.ToString(dyFilter["FSettleStatus_Filter"]);
                //结算状态 ≠ 全部
                if (settleStatus != "0")
                {
                    sqlBuilder.AppendLine("	AND FSETTLESTATUS = " + settleStatus + "   ");
                }
                //关闭状态
                string closeStatus = Convert.ToString(dyFilter["FCloseStatus_Filter"]);
                //关闭状态 ≠ 全部
                if (closeStatus != "C")
                {
                    sqlBuilder.AppendLine("	AND FCLOSESTATUS = '" + closeStatus + "'   ");
                }
                //客户
                DynamicObject custObj = dyFilter["FCustId_Filter"] as DynamicObject;
                string custName = custObj == null ? string.Empty : Convert.ToString(custObj["Name"]);
                if (!custName.IsNullOrEmptyOrWhiteSpace())
                {
                    sqlBuilder.AppendLine("	AND FCUSTNAME = '" + custName + "'   ");
                }
                DBUtils.ExecuteDynamicObject(this.Context, sqlBuilder.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, mainTemp);
                DBUtils.DropSessionTemplateTable(base.Context, filterPurIdTemp);
                DBUtils.DropSessionTemplateTable(base.Context, finishPOTemp);
            }
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
                purDepName = Convert.ToString(dyFilter["FPurDepName_Filter"]);
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
                //客户
                DynamicObject custObj = dyFilter["FCustId_Filter"] as DynamicObject;
                custName = custObj == null ? string.Empty : Convert.ToString(custObj["Name"]);
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
            title.AddTitle("FPurDepName_H", purDepName);
            //采购员
            title.AddTitle("FPurchaser_H", purchaserName);
            //组织机构
            title.AddTitle("FOrgName_H", orgName);
            //销售订单号
            title.AddTitle("FSaleNO_H", saleNo);
            //采购订单号
            title.AddTitle("FPurNO_H", purNo);
            //客户
            title.AddTitle("FCustName_H", custName);
            return title;
        }
        //创建主临时表（存放最终数据）
        private string CreateMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FPurId int	");			                //采购订单ID	
            sqlBuilder.AppendLine("	,FSaleOrderId int	");                 //销售订单ID	
            sqlBuilder.AppendLine("	,FOrgId int	");                         //组织机构ID	
            sqlBuilder.AppendLine("	,FOrgName nvarchar(200) ");             //组织机构		
            sqlBuilder.AppendLine("	,FBillType nvarchar(200) ");            //采购订单单据类型		
            sqlBuilder.AppendLine("	,FPurNo nvarchar(200)   ");             //采购订单号	
            sqlBuilder.AppendLine("	,FSaleNo nvarchar(200)  ");             //销售订单号	
            sqlBuilder.AppendLine("	,FCustName nvarchar(500)  ");           //客户	
            sqlBuilder.AppendLine("	,FPOCreateDate datetime  ");            //采购创建日期		
            sqlBuilder.AppendLine("	,FPOAuditDate datetime  ");             //采购审核日期		
            sqlBuilder.AppendLine("	,FSODeliveryDate datetime   ");         //外销交货日期	
            sqlBuilder.AppendLine("	,FPODeliveryDate datetime   ");         //采购交货日期	
            sqlBuilder.AppendLine("	,FSupName nvarchar(300) ");             //供应商		
            sqlBuilder.AppendLine("	,FPurDepName nvarchar(300) ");          //采购部门			
            sqlBuilder.AppendLine("	,FPurchaser nvarchar(300) ");           //采购员		
            sqlBuilder.AppendLine("	,FCurrency nvarchar(20) ");             //币别			
            sqlBuilder.AppendLine("	,FCurrencyLoc nvarchar(20) ");          //本位币		
            sqlBuilder.AppendLine("	,FPurAmt decimal(23, 10)    ");         //合同金额		
            sqlBuilder.AppendLine("	,FPurAmtLoc decimal(23, 10) ");         //合同金额本位币
            sqlBuilder.AppendLine("	,FInvoAmt decimal(23, 10)   ");         //已开票金额
            sqlBuilder.AppendLine("	,FInvoAmtLoc decimal(23, 10)    ");     //已开票金额本位币
            sqlBuilder.AppendLine("	,FSettledAmt decimal(23, 10)    ");     //已结算金额
            sqlBuilder.AppendLine("	,FSettledAmtLoc decimal(23, 10) ");     //已结算金额本位币
            sqlBuilder.AppendLine("	,FPrePayAmt decimal(23, 10) ");         //预付金额
            sqlBuilder.AppendLine("	,FPrePayAmtLoc decimal(23, 10)  ");     //预付金额本位币
            sqlBuilder.AppendLine("	,FUnSettledAmt decimal(23, 10)  ");     //未结算金额
            sqlBuilder.AppendLine("	,FUnSettledAmtLoc decimal(23, 10)   "); //未结算金额本位币            
            sqlBuilder.AppendLine("	,FSettleStatus char(1)   ");            //结算状态            
            sqlBuilder.AppendLine("	,FCLOSESTATUS char(1)   ");             //关闭状态            
            sqlBuilder.AppendLine("	,FCLOSEREMARKS nvarchar(800)   ");      //关闭说明            
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //创建临时表（存放已结算完成的采购订单ID）
        private string CreateFinishPOTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            //采购订单ID
            StringBuilder.AppendLine("FPurId INT");
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //创建临时表（存放应付单全部已生成已审核的采购订单ID）
        private string CreatePayaTotalGeneratedPurIdTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            //采购订单ID
            StringBuilder.AppendLine("FPurId INT");
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //创建临时表（存放过滤后的采购订单ID）
        private string CreateFilterPurIdTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            //采购订单ID
            StringBuilder.AppendLine("FPurId INT");
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //往临时表插入数据（存放已过滤的采购订单ID）
        private void InsertDataToFilterPurIdTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + filterPurIdTemp + " (FPURID)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		PO.FID		FPURID			--采购订单ID	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	--采购订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERFIN POFIN	");
            sqlBuilder.AppendLine("	ON PO.FID = POFIN.FID	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	ON PO.FSALEORDERID = SO.FID	");
            sqlBuilder.AppendLine("	--组织机构	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = ORG_L.FORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP_L.FSUPPLIERID = PO.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--采购部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--业务员	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("	ON OPERATORENTRY.FENTRYID = PO.FPURCHASERID	");
            sqlBuilder.AppendLine("	--员工任岗明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("	ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	WHERE  DATEDIFF(DAY, '" + beginDate + "', PO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, PO.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	AND ORG.FORGID IN (" + orgId + ")   ");
            //采购部门
            if (!purDepName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DEP_L.FNAME LIKE ('%" + purDepName + "%')   ");
            }
            //采购员
            if (!purchaserName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND STAFF_L.FNAME LIKE ('%" + purchaserName + "%')   ");
            }
            //供应商
            if (!supName.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND SUP_L.FNAME LIKE ('%" + supName + "%')   ");
            }
            //销售订单号
            if (!saleNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND SO.FBILLNO LIKE ('%" + saleNo + "%')   ");
            }
            //采购订单号
            if (!purNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND PO.FBILLNO LIKE ('%" + purNo + "%')   ");
            }
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往临时表插入数据（存放已结算完成的采购订单ID）
        private void InsertDataToFinishPOTemp()
        {
            /*
             *合同结算完成标准：
             *	1、采购订单已关闭；
             *	2、采购订单对应的应付单全部生成；
             *	3、应付单全部已审核且完全核销；
             */
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + finishPOTemp + " (FPurId)	");
            sqlBuilder.AppendLine("	--已审核的，已关闭的，应付单已全部下推生成的采购订单	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		PO.FID	");
            sqlBuilder.AppendLine("	FROM T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FID = PO.FID	");
            sqlBuilder.AppendLine("	WHERE PO.FDOCUMENTSTATUS = 'C' AND PO.FCLOSESTATUS = 'B'	");
            sqlBuilder.AppendLine(" AND PO.FID IN (SELECT FPURID FROM " + filterPurIdTemp + ")	");
            sqlBuilder.AppendLine("	GROUP BY PO.FID	");
            sqlBuilder.AppendLine("	--累计入库数量(基本) - 累计退料数量(基本) - 关联应付数量(计价基本) = 0	");
            sqlBuilder.AppendLine("	HAVING SUM(POENTRY_R.FBASESTOCKINQTY) - SUM(POENTRY_R.FBASEMRBQTY) - SUM(POENTRY_R.FBASEAPJOINQTY) = 0	");
            sqlBuilder.AppendLine("	--累计入库数量(基本) = 0 （主要是针对资产采购合同，模具合同等不用入库单的PO）	");
            sqlBuilder.AppendLine("	OR SUM(POENTRY_R.FBASESTOCKINQTY) = 0	");
            sqlBuilder.AppendLine("	INTERSECT	");
            sqlBuilder.AppendLine("	--采购订单对应的应付单全部已经完全核销	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FID	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			PO.FID						--采购订单ID	");
            sqlBuilder.AppendLine("			,PAYABLE.FWRITTENOFFSTATUS	--应付单.付款核销状态	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单	");
            sqlBuilder.AppendLine("		T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("		--采购订单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("		ON POENTRY.FID = PO.FID	");
            sqlBuilder.AppendLine("		--应付单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLEENTRY PAYABLEENTRY	");
            sqlBuilder.AppendLine("		ON PAYABLEENTRY.FORDERENTRYID = POENTRY.FENTRYID	");
            sqlBuilder.AppendLine("		--应付单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("		ON PAYABLE.FID = PAYABLEENTRY.FID	");
            sqlBuilder.AppendLine("     WHERE PO.FID IN (SELECT FPURID FROM " + filterPurIdTemp + ")	");
            sqlBuilder.AppendLine("		GROUP BY PO.FID, PAYABLE.FWRITTENOFFSTATUS	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	GROUP BY T.FID	");
            sqlBuilder.AppendLine("	HAVING COUNT(*) = 1 AND MIN(T.FWRITTENOFFSTATUS) = 'C'	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往临时表插入数据（存放应付单全部已生成已审核的采购订单ID）
        private void InsertDataToPayaTotalGeneratedPurIdTemp()
        {
            /*
             *合同结算完成标准：
             *	1、采购订单已关闭；
             *	2、采购订单对应的应付单全部生成；
             *	3、应付单全部已审核；
             */
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + payaTotalGeneratedPurIdTemp + " (FPurId)	");
            sqlBuilder.AppendLine("	--已审核的，已关闭的，应付单已全部下推生成的采购订单	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		PO.FID	");
            sqlBuilder.AppendLine("	FROM T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FID = PO.FID	");
            sqlBuilder.AppendLine("	WHERE PO.FDOCUMENTSTATUS = 'C' AND PO.FCLOSESTATUS = 'B'	");
            sqlBuilder.AppendLine(" AND PO.FID IN (SELECT FPURID FROM " + filterPurIdTemp + ")	");
            sqlBuilder.AppendLine("	GROUP BY PO.FID	");
            sqlBuilder.AppendLine("	--累计入库数量(基本) - 累计退料数量(基本) - 关联应付数量(计价基本) = 0	");
            sqlBuilder.AppendLine("	HAVING SUM(POENTRY_R.FBASESTOCKINQTY) - SUM(POENTRY_R.FBASEMRBQTY) - SUM(POENTRY_R.FBASEAPJOINQTY) = 0	");
            sqlBuilder.AppendLine("	--累计入库数量(基本) = 0 （主要是针对资产采购合同，模具合同等不用入库单的PO）	");
            sqlBuilder.AppendLine("	OR SUM(POENTRY_R.FBASESTOCKINQTY) = 0	");
            sqlBuilder.AppendLine("	INTERSECT	");
            sqlBuilder.AppendLine("	--采购订单对应的应付单全部已审核	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FID	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			PO.FID						--采购订单ID	");
            sqlBuilder.AppendLine("			,PAYABLE.FDOCUMENTSTATUS	--应付单.单据状态	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单	");
            sqlBuilder.AppendLine("		T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("		--采购订单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("		ON POENTRY.FID = PO.FID	");
            sqlBuilder.AppendLine("		--应付单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLEENTRY PAYABLEENTRY	");
            sqlBuilder.AppendLine("		ON PAYABLEENTRY.FORDERENTRYID = POENTRY.FENTRYID	");
            sqlBuilder.AppendLine("		--应付单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("		ON PAYABLE.FID = PAYABLEENTRY.FID	");
            sqlBuilder.AppendLine("     WHERE PO.FID IN (SELECT FPURID FROM " + filterPurIdTemp + ")	");
            sqlBuilder.AppendLine("		GROUP BY PO.FID, PAYABLE.FDOCUMENTSTATUS	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	GROUP BY T.FID	");
            sqlBuilder.AppendLine("	HAVING COUNT(*) = 1 AND MIN(T.FDOCUMENTSTATUS) = 'C'	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往主临时表插入数据        
        private void InsertDataToMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + mainTemp + " (FOrgId,FPurId,FSaleOrderId,FOrgName,FBillType,FPurNo,FSaleNo,FCustName,FPOCreateDate,FPOAuditDate,FSODeliveryDate,FPODeliveryDate,FSupName,FPurDepName,FPurchaser,	");
            sqlBuilder.AppendLine("	        FCurrency,FCurrencyLoc,FPurAmt,FPurAmtLoc,FInvoAmt,FInvoAmtLoc,FSettledAmt,FSettledAmtLoc,FPrePayAmt,FPrePayAmtLoc,FUnSettledAmt,FUnSettledAmtLoc,FCLOSESTATUS,FCLOSEREMARKS)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		ORG.FORGID							    FORGID			--组织机构ID	");
            sqlBuilder.AppendLine("		,PO.FID								    FPURID			--采购订单ID	");
            sqlBuilder.AppendLine("		,SO.FID								    FSALEORDERID	--销售订单ID	");
            sqlBuilder.AppendLine("		,ORG_L.FNAME						    FORGNAME		--组织机构	");
            sqlBuilder.AppendLine("		,BILLTYPE_L.FNAME						FBILLTYPE		--单据类型	");
            sqlBuilder.AppendLine("		,PO.FBILLNO							    FPURNO			--采购订单号	");
            sqlBuilder.AppendLine("		,SO.FBILLNO							    FSALENO			--销售订单号	");
            sqlBuilder.AppendLine("		,CUSTOMER_L.FNAME					    FCUSTNAME		--客户	");
            sqlBuilder.AppendLine("		,PO.FCREATEDATE 					    FPOCREATEDATE 	--采购创建日期	");
            sqlBuilder.AppendLine("		,PO.FAPPROVEDATE					    FPOAUDITDATE	--采购审核日期	");
            sqlBuilder.AppendLine("		,SO.FDELIVERYDATE_H					    FSODELIVERYDATE	--外销交货日期	");
            sqlBuilder.AppendLine("		,PO.FDELIVERYDATE_H					    FPODELIVERYDATE	--采购交货日期	");
            sqlBuilder.AppendLine("		,SUP_L.FNAME						    FSUPNAME		--供应商	");
            sqlBuilder.AppendLine("		,DEP_L.FNAME						    FPURDEPNAME		--采购部门	");
            sqlBuilder.AppendLine("		,STAFF_L.FNAME						    FPURCHASER		--采购员	");
            sqlBuilder.AppendLine("		,CURR_L1.FNAME						    FCURRENCY		--币别	");
            sqlBuilder.AppendLine("		,CURR_L2.FNAME						    FCURRENCYLOC	--本位币	");
            sqlBuilder.AppendLine("		,ISNULL(POFIN.FBILLALLAMOUNT,0)		    FPURAMT			--合同金额	");
            sqlBuilder.AppendLine("		,ISNULL(POFIN.FBILLALLAMOUNT_LC,0)	    FPURAMTLOC		--合同金额本位币	");
            sqlBuilder.AppendLine("		,ISNULL(PAYA_TEMP.FINVOAMT,0)			FINVOAMTLOC		--已开票金额	");
            sqlBuilder.AppendLine("		,ISNULL(PAYA_TEMP.FINVOAMTLOC,0)		FINVOAMT		--已开票金额本位币	");
            sqlBuilder.AppendLine("		,ISNULL(PAYA_TEMP.FSETTLEDAMT,0)		FSETTLEDAMT		--已结算金额	");
            sqlBuilder.AppendLine("		,ISNULL(PAYA_TEMP.FSETTLEDAMTLOC,0)		FSETTLEDAMTLOC	--已结算金额本位币	");
            sqlBuilder.AppendLine("		,ISNULL(PREPAY_TEMP.FPREPAYAMT,0)		FPREPAYAMT		--预付金额	");
            sqlBuilder.AppendLine("		,ISNULL(PREPAY_TEMP.FPREPAYAMTLOC,0)	FPREPAYAMTLOC	--预付金额本位币	");
            sqlBuilder.AppendLine("		--未结算金额 = 合同金额 - 已结算金额	");
            sqlBuilder.AppendLine("		,ISNULL(POFIN.FBILLALLAMOUNT,0) - ISNULL(PAYA_TEMP.FSETTLEDAMT,0)	");
            sqlBuilder.AppendLine("		    								    FUNSETTLEDAMT	--未结算金额	");
            sqlBuilder.AppendLine("		--未结算金额本位币 = 合同金额本位币 - 已结算金额本位币	");
            sqlBuilder.AppendLine("		,ISNULL(POFIN.FBILLALLAMOUNT_LC,0) - ISNULL(PAYA_TEMP.FSETTLEDAMTLOC,0)	");
            sqlBuilder.AppendLine("											    FUNSETTLEDAMTLOC--未结算金额本位币	");
            sqlBuilder.AppendLine("		,PO.FCLOSESTATUS						FCLOSESTATUS	--采购订单关闭状态	");
            sqlBuilder.AppendLine("		,PO.FCLOSEREMARKS						FCLOSEREMARKS	--采购订单关闭说明	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	--采购订单.财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERFIN POFIN	");
            sqlBuilder.AppendLine("	ON PO.FID = POFIN.FID	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE_L BILLTYPE_L 	");
            sqlBuilder.AppendLine("	ON PO.FBILLTYPEID = BILLTYPE_L.FBILLTYPEID AND BILLTYPE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	LEFT JOIN	");
            sqlBuilder.AppendLine("	(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			T1.FID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(T3.FPAYJOINAMOUNT,0))					FPrePayAmt		--预付金额	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(T3.FPAYJOINAMOUNT,0)*T2.FEXCHANGERATE)	FPrePayAmtloc	--预付金额本位币	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单.表头	");
            sqlBuilder.AppendLine("		T_PUR_POORDER T1	");
            sqlBuilder.AppendLine("		--采购订单.表头_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERFIN T2	");
            sqlBuilder.AppendLine("		ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("		--采购订单.付款计划	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERINSTALLMENT T3	");
            sqlBuilder.AppendLine("		ON T1.FID = T3.FID	");
            sqlBuilder.AppendLine("		GROUP BY T1.FID	");
            sqlBuilder.AppendLine("	) PREPAY_TEMP	");
            sqlBuilder.AppendLine("	ON PREPAY_TEMP.FID = PO.FID	");
            sqlBuilder.AppendLine("	--销售订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDER SO	");
            sqlBuilder.AppendLine("	ON PO.FSALEORDERID = SO.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUSTOMER_L	");
            sqlBuilder.AppendLine("	ON CUSTOMER_L.FCUSTID = SO.FCUSTID AND CUSTOMER_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--组织机构	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = ORG_L.FORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP_L.FSUPPLIERID = PO.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--采购部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--业务员	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_OPERATORENTRY OPERATORENTRY	");
            sqlBuilder.AppendLine("	ON OPERATORENTRY.FENTRYID = PO.FPURCHASERID	");
            sqlBuilder.AppendLine("	--员工任岗明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_STAFF_L STAFF_L	");
            sqlBuilder.AppendLine("	ON STAFF_L.FSTAFFID = OPERATORENTRY.FSTAFFID AND STAFF_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--币别（结算币别）	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CURRENCY_L CURR_L1	");
            sqlBuilder.AppendLine("	ON CURR_L1.FCURRENCYID = POFIN.FSETTLECURRID AND CURR_L1.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--币别（本位币）	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CURRENCY_L CURR_L2	");
            sqlBuilder.AppendLine("	ON CURR_L2.FCURRENCYID = POFIN.FLOCALCURRID AND CURR_L2.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--已开票金额：采购订单对应的已审核的应付金额	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			PO.FID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PAYABLEENTRY.FALLAMOUNTFOR,0))	FINVOAMT		--明细.价税合计(已开票金额)	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PAYABLEENTRY.FALLAMOUNT,0))		FINVOAMTLOC		--明细.价税合计本位币(已开票金额本位币)	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PAYABLEENTRY.FPAYMENTAMOUNT,0)) FSETTLEDAMT		--明细.已结算金额	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(PAYABLEENTRY.FPAYMENTAMOUNT,0) * ISNULL(PAYAFIN.FEXCHANGERATE,0))	");
            sqlBuilder.AppendLine("														FSETTLEDAMTLOC	--明细.已结算金额本位币	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单	");
            sqlBuilder.AppendLine("		T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("		--采购订单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("		ON POENTRY.FID = PO.FID	");
            sqlBuilder.AppendLine("		--应付单.明细	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLEENTRY PAYABLEENTRY	");
            sqlBuilder.AppendLine("		ON PAYABLEENTRY.FORDERENTRYID = POENTRY.FENTRYID	");
            sqlBuilder.AppendLine("		--应付单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLE PAYABLE	");
            sqlBuilder.AppendLine("		ON PAYABLE.FID = PAYABLEENTRY.FID	");
            sqlBuilder.AppendLine("		--应付单.表头_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_AP_PAYABLEFIN PAYAFIN	");
            sqlBuilder.AppendLine("		ON PAYAFIN.FID = PAYABLE.FID	");
            sqlBuilder.AppendLine("		WHERE PAYABLE.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		GROUP BY PO.FID	");
            sqlBuilder.AppendLine("	) PAYA_TEMP	");
            sqlBuilder.AppendLine("	ON PAYA_TEMP.FID = PO.FID	");            
            sqlBuilder.AppendLine("	WHERE  PO.FID IN (SELECT FPURID FROM " + filterPurIdTemp + ")	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //先更新结算状态 = 未结清
        private void UpdateSettleStatusFromMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine(" UPDATE " + mainTemp + "	");
            sqlBuilder.AppendLine(" SET FSETTLESTATUS = '1'	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新未结算金额，如果采购合同已全部生成应付单且应付单已审核，则 未结算金额 = 已开票金额 - 已结算金额 （已开票金额即应付金额）
        private void UpdateNotPaidAmt()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine(" UPDATE T1	");
            sqlBuilder.AppendLine(" --未结算金额 = 已开票金额 - 已结算金额	");
            sqlBuilder.AppendLine(" SET T1.FUNSETTLEDAMT = T1.FINVOAMT - T1.FSETTLEDAMT 	");
            sqlBuilder.AppendLine(" --未结算金额本位币 = 已开票金额本位币 - 已结算金额本位币	");
            sqlBuilder.AppendLine("     ,T1.FUNSETTLEDAMTLOC = T1.FINVOAMTLOC - T1.FSETTLEDAMTLOC 	");
            sqlBuilder.AppendLine(" FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine(" ," + payaTotalGeneratedPurIdTemp + " T2	");
            sqlBuilder.AppendLine(" WHERE T1.FPURID = T2.FPURID	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新主临时表中的结算状态和未结算金额：当合同已完成时更新未结算金额为0
        private void UpdateNotPaidAmtFromMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine(" UPDATE T1	");
            sqlBuilder.AppendLine(" SET T1.FUNSETTLEDAMT = 0,T1.FUNSETTLEDAMTLOC = 0,T1.FSETTLESTATUS = '2'	");
            sqlBuilder.AppendLine(" FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine(" ," + finishPOTemp + " T2	");
            sqlBuilder.AppendLine(" WHERE T1.FPURID = T2.FPURID	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
    }
}

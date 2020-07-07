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

namespace Para.App.Report.ProfitTrackRpt
{
    [HotUpdate]
    [Description("出货利润跟踪表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //查询方式
        string queryStyle = "1";
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //结束日期
        DateTime endDate = DateTime.MinValue;
        //年度
        int year = DateTime.Today.Year;
        //月度
        int month = DateTime.Today.Month;
        //外销发票号
        string outInvoNo = string.Empty;
        //临时表
        string mainTemp = string.Empty;
        string temp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("出货利润跟踪表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsUIDesignerColumns = false;
            this.ReportProperty.IsGroupSummary = false;
            this.ReportProperty.SimpleAllCols = false;
            this.SetDecimalControl();
        }
        //设置精度
        private void SetDecimalControl()
        {
            List<DecimalControlField> list = new List<DecimalControlField>();
            //离岸收入
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FOffshoreIncome",
                DecimalControlFieldName = "FPRECISION"
            });
            //离岸成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FOffshoreCost",
                DecimalControlFieldName = "FPRECISION"
            });
            //离岸利润
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FOffshoreProfit",
                DecimalControlFieldName = "FPRECISION"
            });
            //高山收入(货款)
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPMIncome_M",
                DecimalControlFieldName = "FPRECISION"
            });
            //高山收入(退税)
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPMIncome_R",
                DecimalControlFieldName = "FPRECISION"
            });
            //高山收入
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPMIncome",
                DecimalControlFieldName = "FPRECISION"
            });
            //高山成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPMCost",
                DecimalControlFieldName = "FPRECISION"
            });
            //高山利润
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FPMProfit",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生收入(货款)
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGIncome_M",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生收入(退税)
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGIncome_R",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生收入
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGIncome",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生成本（不含税）
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGCost_NoTax",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生成本
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGCost",
                DecimalControlFieldName = "FPRECISION"
            });
            //阳普生利润
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FEGProfit",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建主临时表
                mainTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#mainTemp", this.CreateMainTemp());
                //创建临时表(存放外销发票号和离岸日期)
                temp = DBUtils.CreateSessionTemplateTable(base.Context, "#temp", this.CreateTemp());
                //往临时表中插入外销发票号和离岸日期
                this.InsertIntoTemp();
                //往主临时表中插入外销发票号和离岸日期
                this.InsertIntoMainTemp();
                //往主临时表插入离岸公司数据
                this.updateOffShoreData();
                //更新主表高山数据
                this.updatePMData();
                //更新阳普生数据
                this.UpdateEGData();
                //更新收入，利润，利润率，插入合计行
                this.FinishTemp();

                //排序
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FLevel,FOutInvoNo,FOffShoreDate,FOffshoreCompany");
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("	SELECT	");
                sqlBuilder.AppendFormat("	{0}			        --序号\r\n", base.KSQL_SEQ);
                sqlBuilder.AppendLine("	,FLevel				    --层级	");
                sqlBuilder.AppendLine("	,FOutInvoNo				--外销发票号	");
                sqlBuilder.AppendLine("	,FCustNum				--客户编码	");
                sqlBuilder.AppendLine("	,FCustName				--客户名称	");
                sqlBuilder.AppendLine("	,FOffShoreDate			--离岸日期	");
                sqlBuilder.AppendLine("	,FOffshoreCompany		--离岸&公司抬头	");
                sqlBuilder.AppendLine("	,FOffshoreIncome		--离岸&收入	");
                sqlBuilder.AppendLine("	,FOffshoreCost			--离岸&成本	");
                sqlBuilder.AppendLine("	,FOffshoreProfit		--离岸&利润	");
                sqlBuilder.AppendLine("	,FOffshoreProfitRate	--离岸&利润率%	");
                sqlBuilder.AppendLine("	,FPMIncome_M			--高山&收入(货款)	");
                sqlBuilder.AppendLine("	,FPMIncome_R			--高山&收入(退税)	");
                sqlBuilder.AppendLine("	,FPMIncome				--高山&收入	");
                sqlBuilder.AppendLine("	,FPMCost				--高山&成本	");
                sqlBuilder.AppendLine("	,FPMProfit				--高山&利润	");
                sqlBuilder.AppendLine("	,FPMProfitRate			--高山&利润率%	");
                sqlBuilder.AppendLine("	,FEGIncome_M			--阳普生&收入(货款)	");
                sqlBuilder.AppendLine("	,FEGIncome_R			--阳普生&收入(退税)	");
                sqlBuilder.AppendLine("	,FEGIncome				--阳普生&收入	");
                sqlBuilder.AppendLine("	,FEGCost_NoTax			--阳普生&成本(不含税)	");
                sqlBuilder.AppendLine("	,FEGCost				--阳普生&成本	");
                sqlBuilder.AppendLine("	,FEGProfit				--阳普生&利润	");
                sqlBuilder.AppendLine("	,FEGProfitRate			--研普生&利润率%	");
                sqlBuilder.AppendLine("	,2 FPRECISION	        --精度	");
                sqlBuilder.AppendFormat("	INTO {0}	\r\n", tableName);
                sqlBuilder.AppendLine("	FROM " + mainTemp + "	");
                sqlBuilder.AppendLine("	WHERE 1 = 1	");
                if (!filter.FilterParameter.FilterString.IsNullOrEmptyOrWhiteSpace())
                {
                    sqlBuilder.AppendLine("	AND " + filter.FilterParameter.FilterString + "   ");
                }
                DBUtils.ExecuteDynamicObject(this.Context, sqlBuilder.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, temp);
                DBUtils.DropSessionTemplateTable(base.Context, mainTemp);
            }
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (dyFilter != null)
            {
                //查询方式
                queryStyle = Convert.ToString(dyFilter["FQueryStyle_Filter"]);
                //起始日期
                beginDate = Convert.ToDateTime(dyFilter["FBeginDate_Filter"]);
                //截止日期
                endDate = Convert.ToDateTime(dyFilter["FEndDate_Filter"]);
                //年度
                year = Convert.ToInt32(dyFilter["FYear_Filter"]);
                //月度
                month = Convert.ToInt32(dyFilter["FMonth_Filter"]);
                //外销发票号
                outInvoNo = Convert.ToString(dyFilter["FOutInvoNo_Filter"]);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //按期间查询
            if (queryStyle == "1")
            {
                //起始日期
                title.AddTitle("FBeginDate", null);
                //结束日期
                title.AddTitle("FEndDate", null);
                //年度
                title.AddTitle("FYear", year.ToString());
                //月度
                title.AddTitle("FMonth", month.ToString());
            }
            else if (queryStyle == "2")
            {
                //起始日期
                title.AddTitle("FBeginDate", beginDate.ToShortDateString());
                //结束日期
                title.AddTitle("FEndDate", endDate.ToShortDateString());
                //年度
                title.AddTitle("FYear", "");
                //月度
                title.AddTitle("FMonth", "");
            }
            return title;
        }
        //创建主临时表
        private string CreateMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FLevel int default (0) ");                          //层级（排序用）
            sqlBuilder.AppendLine("	,FOutInvoNo nvarchar(200) ");                       //外销发票号
            sqlBuilder.AppendLine("	,FCustNum nvarchar(200) ");                         //客户编码
            sqlBuilder.AppendLine("	,FCustName nvarchar(300) ");                        //客户名称
            sqlBuilder.AppendLine("	,FOffShoreDate datetime  ");                        //离岸日期		
            sqlBuilder.AppendLine("	,FOffshoreCompany nvarchar(200) ");                 //离岸公司抬头	
            sqlBuilder.AppendLine("	,FOffshoreIncome decimal(23, 10) default(0)    ");  //离岸收入		
            sqlBuilder.AppendLine("	,FOffshoreCost decimal(23, 10) default(0) ");       //离岸成本
            sqlBuilder.AppendLine("	,FOffshoreProfit decimal(23, 10) default(0)   ");   //离岸利润
            sqlBuilder.AppendLine("	,FOffshoreProfitRate nvarchar(200) ");              //离岸利润率%	

            sqlBuilder.AppendLine("	,FPMIncome_M decimal(23, 10) default(0) ");         //高山收入(货款)
            sqlBuilder.AppendLine("	,FPMIncome_R decimal(23, 10) default(0) ");         //高山收入(退税)
            sqlBuilder.AppendLine("	,FPMIncome decimal(23, 10) default(0)    ");        //高山收入	
            sqlBuilder.AppendLine("	,FPMCost decimal(23, 10) default(0) ");             //高山成本
            sqlBuilder.AppendLine("	,FPMProfit decimal(23, 10) default(0)   ");         //高山利润
            sqlBuilder.AppendLine("	,FPMProfitRate nvarchar(200) ");                    //高山利润率%	    

            sqlBuilder.AppendLine("	,FEGIncome_M decimal(23, 10) default(0)    ");      //阳普生收入(货款)		
            sqlBuilder.AppendLine("	,FEGIncome_R decimal(23, 10) default(0)    ");      //阳普生收入(退税)		
            sqlBuilder.AppendLine("	,FEGIncome decimal(23, 10) default(0)    ");        //阳普生收入		
            sqlBuilder.AppendLine("	,FEGCost_NoTax decimal(23, 10) default(0) ");       //阳普生成本(不含税)
            sqlBuilder.AppendLine("	,FEGCost decimal(23, 10) default(0) ");             //阳普生成本
            sqlBuilder.AppendLine("	,FEGProfit decimal(23, 10) default(0)   ");         //阳普生利润
            sqlBuilder.AppendLine("	,FEGProfitRate nvarchar(200) ");                    //阳普生利润率%	
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //创建临时表(存放外销发票号和离岸日期)
        private string CreateTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FOutInvoNo nvarchar(200) ");            //外销发票号
            sqlBuilder.AppendLine("	,FOffShoreDate datetime  ");            //离岸日期		
            sqlBuilder.AppendLine("	,FCustNum nvarchar(300) ");             //外销发票号
            sqlBuilder.AppendLine("	,FCustName nvarchar(400) ");            //外销发票号
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //往临时表中插入外销发票号，离岸日期和客户
        private void InsertIntoTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + temp + " (FOUTINVONO,FOFFSHOREDATE,FCUSTNUM,FCUSTNAME)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DOC.FBILLNO			FOUTINVONO      --外销发票号	");
            sqlBuilder.AppendLine("		,DOC.FOFFSHOREDATE	FOFFSHOREDATE   --离岸日期	");
            sqlBuilder.AppendLine("		,CUST.FNUMBER	    FCUSTNUM        --客户编码	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME	    FCUSTNAME       --客户名称	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--出口报关单	");
            sqlBuilder.AppendLine("	TPT_FZH_DECALREDOC DOC	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON DOC.FORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = DOC.FCUSID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST_L.FCUSTID = CUST.FCUSTID AND CUST_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	WHERE DOC.FDOCUMENTSTATUS = 'C' AND FISOFFSHORE = '1' AND ORG.FNUMBER IN ('PM','EG')	");
            //查询方式 = 按期间查询
            if (queryStyle == "1")
            {
                sqlBuilder.AppendLine("	AND YEAR(DOC.FOFFSHOREDATE) = '" + year + "' AND MONTH(DOC.FOFFSHOREDATE) = '" + month + "'	");
            }
            //查询方式 = 按日期查询
            else if (queryStyle == "2")
            {
                sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', DOC.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, DOC.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            }
            if (!outInvoNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DOC.FBILLNO LIKE '%" + outInvoNo + "%'	");
            }
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往主临时表中插入外销发票号，离岸日期和客户
        private void InsertIntoMainTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + mainTemp + " (FOUTINVONO,FOFFSHOREDATE,FCUSTNUM,FCUSTNAME)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FOUTINVONO	");
            sqlBuilder.AppendLine("		,FOFFSHOREDATE	");
            sqlBuilder.AppendLine("		,FCUSTNUM	");
            sqlBuilder.AppendLine("		,FCUSTNAME	");
            sqlBuilder.AppendLine("	FROM " + temp + "	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往主临时表插入离岸公司数据
        private void updateOffShoreData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            //更新离岸收入
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FOFFSHOREINCOME = T2.FOFFSHOREINCOME,T1.FOFFSHORECOMPANY = T2.FOFFSHORECOMPANY,T1.FCUSTNUM = T2.FCUSTNUM,T1.FCUSTNAME = T2.FCUSTNAME	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1,	");
            sqlBuilder.AppendLine("	(SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO									FOUTINVONO			--外销发票号	");
            sqlBuilder.AppendLine("		,CUST.FNUMBER									FCUSTNUM			--客户编码	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME									FCUSTNAME			--客户名称	");
            sqlBuilder.AppendLine("		,ORG_L.FNAME									FOFFSHORECOMPANY	--离岸公司抬头	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN OUTSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("			ELSE ROUND((SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE,2)	");
            sqlBuilder.AppendLine("		END												FOFFSHOREINCOME		--离岸公司收入（本位币）	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--销售出库单.表头	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = OUTSTOCK.FOUTINVOICENO	");
            sqlBuilder.AppendLine("	--销售出库单.表头财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("	ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_F.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售退货单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(RETURNSTOCKENTRY_F.FALLAMOUNT,0))		FALLAMOUNT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售退货单.关联表	");
            sqlBuilder.AppendLine("		T_SAL_RETURNSTOCKENTRY_LK RETURNSTOCKENTRY_LK	");
            sqlBuilder.AppendLine("		--销售退货库.明细_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCKENTRY_F RETURNSTOCKENTRY_F	");
            sqlBuilder.AppendLine("		ON RETURNSTOCKENTRY_LK.FENTRYID = RETURNSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--销售退货库.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCK RETURNSTOCK	");
            sqlBuilder.AppendLine("		ON RETURNSTOCK.FID = RETURNSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE RETURNSTOCK.FDOCUMENTSTATUS = 'C' AND RETURNSTOCKENTRY_LK.FSTABLENAME = 'T_SAL_OUTSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) RETURNSTOCK_TEMP	");
            sqlBuilder.AppendLine("	ON RETURNSTOCK_TEMP.FSID = OUTSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON OUTSTOCK.FSTOCKORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = ORG_L.FORGID AND ORG_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = OUTSTOCK.FCUSTOMERID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST_L.FCUSTID = CUST.FCUSTID AND CUST_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = OUTSTOCKFIN.FSETTLECURRID	--汇率.原币 = 出库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = OUTSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, OUTSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, OUTSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('IG','PG')	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,ORG_L.FNAME,CUST.FNUMBER,CUST_L.FNAME,OUTSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //更新离岸成本
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FOffshoreCost = T2.FOffshoreCost	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO																	            --外销发票号	");
            sqlBuilder.AppendLine("		,CASE 	");
            sqlBuilder.AppendLine("	        WHEN INSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("		    ELSE ROUND((SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE, 2)  --当结算币别是外币时，要乘以汇率	");
            sqlBuilder.AppendLine("     END                                                                         FOffshoreCost	--高山成本	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--采购入库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCK INSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = INSTOCK.FOUTINVOICENO_H	");
            sqlBuilder.AppendLine("	--采购入库单.财务信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKFIN INSTOCKFIN	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKFIN.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = INSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("	--采购入库单.明细财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKENTRY_F INSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = INSTOCKFIN.FSETTLECURRID	--汇率.原币 = 入库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = INSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, INSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, INSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	--采购退料单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			MRBENTRY_LK.FSID									        --采购入库单ENTRYID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(MRBENTRY_F.FALLAMOUNT,0))	        FALLAMOUNT	--价税合计	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购退料单.关联表	");
            sqlBuilder.AppendLine("		T_PUR_MRBENTRY_LK MRBENTRY_LK	");
            sqlBuilder.AppendLine("		--采购退料单.明细财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRBENTRY_F MRBENTRY_F	");
            sqlBuilder.AppendLine("		ON MRBENTRY_LK.FENTRYID = MRBENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--采购退料单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRB MRB	");
            sqlBuilder.AppendLine("		ON MRB.FID = MRBENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE MRB.FDOCUMENTSTATUS = 'C' AND MRBENTRY_LK.FSTABLENAME = 'T_STK_INSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY MRBENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) MRB	");
            sqlBuilder.AppendLine("	ON MRB.FSID = INSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	WHERE INSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER IN ('IG','PG')	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,INSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新主表高山数据
        private void updatePMData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            //更新高山货款收入
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FPMIncome_M = T2.FPMINCOME_M	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1,	");
            sqlBuilder.AppendLine("	(SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO									FOUTINVONO			--外销发票号	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN OUTSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("			ELSE ROUND((SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE,2)	");
            sqlBuilder.AppendLine("		END												FPMINCOME_M 		--高山货款收入（本位币）	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--销售出库单.表头	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = OUTSTOCK.FOUTINVOICENO	");
            sqlBuilder.AppendLine("	--销售出库单.表头财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("	ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_F.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售退货单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(RETURNSTOCKENTRY_F.FALLAMOUNT,0))		FALLAMOUNT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售退货单.关联表	");
            sqlBuilder.AppendLine("		T_SAL_RETURNSTOCKENTRY_LK RETURNSTOCKENTRY_LK	");
            sqlBuilder.AppendLine("		--销售退货库.明细_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCKENTRY_F RETURNSTOCKENTRY_F	");
            sqlBuilder.AppendLine("		ON RETURNSTOCKENTRY_LK.FENTRYID = RETURNSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--销售退货库.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCK RETURNSTOCK	");
            sqlBuilder.AppendLine("		ON RETURNSTOCK.FID = RETURNSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE RETURNSTOCK.FDOCUMENTSTATUS = 'C' AND RETURNSTOCKENTRY_LK.FSTABLENAME = 'T_SAL_OUTSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) RETURNSTOCK_TEMP	");
            sqlBuilder.AppendLine("	ON RETURNSTOCK_TEMP.FSID = OUTSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON OUTSTOCK.FSTOCKORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = OUTSTOCKFIN.FSETTLECURRID	--汇率.原币 = 出库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = OUTSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, OUTSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, OUTSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'PM'	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,OUTSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //更新高山退税
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FPMINCOME_R = T2.FPMINCOME_R	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		TT.FOUTINVONO					FOUTINVONO		--外销发票号	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(TT.FPMINCOME_R,0))	FPMINCOME_R		--退税额	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			T.FOUTINVONO									FOUTINVONO	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(COSTMATCH.FTOTALRETURNTAXAMT,0))	FPMINCOME_R	");
            sqlBuilder.AppendLine("		FROM " + temp + " T	");
            sqlBuilder.AppendLine("		--成本匹配单	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CM_COSTMATCHINGBILL COSTMATCH	");
            sqlBuilder.AppendLine("		ON COSTMATCH.FINVOICENO = T.FOUTINVONO	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = COSTMATCH.FORGID	");
            sqlBuilder.AppendLine("		WHERE ORG.FNUMBER = 'PM' AND COSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		GROUP BY T.FOUTINVONO	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			T.FOUTINVONO										FOUTINVONO	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(NEWCOSTMATCH.FTOTALRETURNTAXAMT,0))	FPMINCOME_R	");
            sqlBuilder.AppendLine("		FROM " + temp + " T	");
            sqlBuilder.AppendLine("		--新成本匹配单	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CM_NEWCOSTMATCHINGBILL NEWCOSTMATCH	");
            sqlBuilder.AppendLine("		ON NEWCOSTMATCH.FBILLNO = T.FOUTINVONO	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = NEWCOSTMATCH.FORGID	");
            sqlBuilder.AppendLine("		WHERE ORG.FNUMBER = 'PM' AND NEWCOSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		GROUP BY T.FOUTINVONO	");
            sqlBuilder.AppendLine("	) TT	");
            sqlBuilder.AppendLine("	GROUP BY TT.FOUTINVONO	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //更新高山成本
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FPMCOST = T2.FPMCOST	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO																	    --外销发票号	");
            sqlBuilder.AppendLine("		,CASE 	");
            sqlBuilder.AppendLine("	        WHEN INSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("		    ELSE ROUND((SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE, 2)  --当结算币别是外币时，要乘以汇率	");
            sqlBuilder.AppendLine("	END                                                                             FPMCOST	--高山成本	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--采购入库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCK INSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = INSTOCK.FOUTINVOICENO_H	");
            sqlBuilder.AppendLine("	--采购入库单.财务信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKFIN INSTOCKFIN	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKFIN.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = INSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("	--采购入库单.明细财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKENTRY_F INSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = INSTOCKFIN.FSETTLECURRID	--汇率.原币 = 入库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = INSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, INSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, INSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	--采购退料单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			MRBENTRY_LK.FSID									        --采购入库单ENTRYID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(MRBENTRY_F.FALLAMOUNT,0))	        FALLAMOUNT	--价税合计	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购退料单.关联表	");
            sqlBuilder.AppendLine("		T_PUR_MRBENTRY_LK MRBENTRY_LK	");
            sqlBuilder.AppendLine("		--采购退料单.明细财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRBENTRY_F MRBENTRY_F	");
            sqlBuilder.AppendLine("		ON MRBENTRY_LK.FENTRYID = MRBENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--采购退料单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRB MRB	");
            sqlBuilder.AppendLine("		ON MRB.FID = MRBENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE MRB.FDOCUMENTSTATUS = 'C' AND MRBENTRY_LK.FSTABLENAME = 'T_STK_INSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY MRBENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) MRB	");
            sqlBuilder.AppendLine("	ON MRB.FSID = INSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	WHERE INSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'PM'	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,INSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新阳普生数据
        private void UpdateEGData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            //更新阳普生货款收入
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FEGIncome_M = T2.FEGINCOME_M	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1,	");
            sqlBuilder.AppendLine("	(SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO									FOUTINVONO			--外销发票号	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN OUTSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("			ELSE ROUND((SUM(ISNULL(OUTSTOCKENTRY_F.FALLAMOUNT,0)) - SUM(ISNULL(RETURNSTOCK_TEMP.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE,2)	");
            sqlBuilder.AppendLine("		END												FEGINCOME_M 		--高山货款收入（本位币）	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--销售出库单.表头	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = OUTSTOCK.FOUTINVOICENO	");
            sqlBuilder.AppendLine("	--销售出库单.表头财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKFIN OUTSTOCKFIN	");
            sqlBuilder.AppendLine("	ON OUTSTOCKFIN.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_F.FID = OUTSTOCK.FID	");
            sqlBuilder.AppendLine("	--销售退货单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(RETURNSTOCKENTRY_F.FALLAMOUNT,0))		FALLAMOUNT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--销售退货单.关联表	");
            sqlBuilder.AppendLine("		T_SAL_RETURNSTOCKENTRY_LK RETURNSTOCKENTRY_LK	");
            sqlBuilder.AppendLine("		--销售退货库.明细_财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCKENTRY_F RETURNSTOCKENTRY_F	");
            sqlBuilder.AppendLine("		ON RETURNSTOCKENTRY_LK.FENTRYID = RETURNSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--销售退货库.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_SAL_RETURNSTOCK RETURNSTOCK	");
            sqlBuilder.AppendLine("		ON RETURNSTOCK.FID = RETURNSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE RETURNSTOCK.FDOCUMENTSTATUS = 'C' AND RETURNSTOCKENTRY_LK.FSTABLENAME = 'T_SAL_OUTSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY RETURNSTOCKENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) RETURNSTOCK_TEMP	");
            sqlBuilder.AppendLine("	ON RETURNSTOCK_TEMP.FSID = OUTSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON OUTSTOCK.FSTOCKORGID = ORG.FORGID	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = OUTSTOCKFIN.FSETTLECURRID	--汇率.原币 = 出库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = OUTSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, OUTSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, OUTSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	WHERE OUTSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,OUTSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //更新阳普生退税
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FEGINCOME_R = T2.FEGINCOME_R	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		TT.FOUTINVONO					FOUTINVONO		--外销发票号	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(TT.FEGINCOME_R,0))	FEGINCOME_R		--退税额	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			T.FOUTINVONO									FOUTINVONO	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(COSTMATCH.FTOTALRETURNTAXAMT,0))	FEGINCOME_R	");
            sqlBuilder.AppendLine("		FROM " + temp + " T	");
            sqlBuilder.AppendLine("		--成本匹配单	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CM_COSTMATCHINGBILL COSTMATCH	");
            sqlBuilder.AppendLine("		ON COSTMATCH.FINVOICENO = T.FOUTINVONO	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = COSTMATCH.FORGID	");
            sqlBuilder.AppendLine("		WHERE ORG.FNUMBER = 'EG' AND COSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		GROUP BY T.FOUTINVONO	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			T.FOUTINVONO										FOUTINVONO	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(NEWCOSTMATCH.FTOTALRETURNTAXAMT,0))	    FEGINCOME_R	");
            sqlBuilder.AppendLine("		FROM " + temp + " T	");
            sqlBuilder.AppendLine("		--新成本匹配单	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CM_NEWCOSTMATCHINGBILL NEWCOSTMATCH	");
            sqlBuilder.AppendLine("		ON NEWCOSTMATCH.FBILLNO = T.FOUTINVONO	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = NEWCOSTMATCH.FORGID	");
            sqlBuilder.AppendLine("		WHERE ORG.FNUMBER = 'EG' AND NEWCOSTMATCH.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("		GROUP BY T.FOUTINVONO	");
            sqlBuilder.AppendLine("	) TT	");
            sqlBuilder.AppendLine("	GROUP BY TT.FOUTINVONO	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //更新阳普生成本
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE T1	");
            sqlBuilder.AppendLine("	SET T1.FEGCOST = T2.FEGCOST,T1.FEGCOST_NOTAX = T2.FEGCOST_NOTAX	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + " T1	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T.FOUTINVONO																	            --外销发票号	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN INSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(INSTOCKENTRY_F.FAMOUNT,0) - ISNULL(MRB.FAMOUNT,0))	");
            sqlBuilder.AppendLine("			--当结算币别是外币时，要乘以汇率	");
            sqlBuilder.AppendLine("			ELSE ROUND((SUM(ISNULL(INSTOCKENTRY_F.FAMOUNT,0) - ISNULL(MRB.FAMOUNT,0))) * BD_RATE.FEXCHANGERATE, 2)	");
            sqlBuilder.AppendLine("		END                                                                     FEGCOST_NOTAX	    --阳普生成本(不含税)	");
            sqlBuilder.AppendLine("		,CASE	");
            sqlBuilder.AppendLine("			WHEN INSTOCKFIN.FSETTLECURRID = 1 THEN SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))	");
            sqlBuilder.AppendLine("			--当结算币别是外币时，要乘以汇率	");
            sqlBuilder.AppendLine("			ELSE ROUND((SUM(ISNULL(INSTOCKENTRY_F.FALLAMOUNT,0) - ISNULL(MRB.FALLAMOUNT,0))) * BD_RATE.FEXCHANGERATE, 2)	");
            sqlBuilder.AppendLine("		END                                                                     FEGCOST				--阳普生成本	");
            sqlBuilder.AppendLine("	FROM " + temp + " T	");
            sqlBuilder.AppendLine("	--采购入库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCK INSTOCK	");
            sqlBuilder.AppendLine("	ON T.FOUTINVONO = INSTOCK.FOUTINVOICENO_H	");
            sqlBuilder.AppendLine("	--采购入库单.财务信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKFIN INSTOCKFIN	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKFIN.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = INSTOCK.FSTOCKORGID	");
            sqlBuilder.AppendLine("	--采购入库单.明细财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKENTRY_F INSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("	--采购退料单	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			MRBENTRY_LK.FSID									    --采购入库单ENTRYID	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(MRBENTRY_F.FAMOUNT,0))			FAMOUNT	    --金额	");
            sqlBuilder.AppendLine("			,SUM(ISNULL(MRBENTRY_F.FALLAMOUNT,0))		FALLAMOUNT	--价税合计	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购退料单.关联表	");
            sqlBuilder.AppendLine("		T_PUR_MRBENTRY_LK MRBENTRY_LK	");
            sqlBuilder.AppendLine("		--采购退料单.明细财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRBENTRY_F MRBENTRY_F	");
            sqlBuilder.AppendLine("		ON MRBENTRY_LK.FENTRYID = MRBENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("		--采购退料单.表头	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_MRB MRB	");
            sqlBuilder.AppendLine("		ON MRB.FID = MRBENTRY_F.FID	");
            sqlBuilder.AppendLine("		WHERE MRB.FDOCUMENTSTATUS = 'C' AND MRBENTRY_LK.FSTABLENAME = 'T_STK_INSTOCKENTRY'	");
            sqlBuilder.AppendLine("		GROUP BY MRBENTRY_LK.FSID	");
            sqlBuilder.AppendLine("	) MRB	");
            sqlBuilder.AppendLine("	ON MRB.FSID = INSTOCKENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	--汇率	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_RATE BD_RATE	");
            sqlBuilder.AppendLine("	ON BD_RATE.FCYFORID = INSTOCKFIN.FSETTLECURRID	--汇率.原币 = 入库单.结算币别	");
            sqlBuilder.AppendLine("	AND BD_RATE.FCYTOID = INSTOCKFIN.FLOCALCURRID	--汇率.目标比 = 入库单.本位币	");
            sqlBuilder.AppendLine("	AND BD_RATE.FRATETYPEID = 1						--汇率类型 = 记账汇率	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, BD_RATE.FBEGDATE, INSTOCK.FDATE) >= 0	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, INSTOCK.FDATE,BD_RATE.FENDDATE) >= 0	");
            sqlBuilder.AppendLine("	WHERE INSTOCK.FDOCUMENTSTATUS = 'C' AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("	GROUP BY T.FOUTINVONO,INSTOCKFIN.FSETTLECURRID,BD_RATE.FEXCHANGERATE	");
            sqlBuilder.AppendLine("	) T2	");
            sqlBuilder.AppendLine("	WHERE T1.FOUTINVONO = T2.FOUTINVONO	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新收入，利润，利润率，插入合计行
        private void FinishTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	update ##mainTemp	");
            sqlBuilder.AppendLine("	set --离岸利润 = 离岸收入 - 离岸成本	");
            sqlBuilder.AppendLine("		FOffshoreProfit = FOffshoreIncome - FOffshoreCost	");
            sqlBuilder.AppendLine("		--高山收入 = 高山收入(货款) + 高山收入(退税)	");
            sqlBuilder.AppendLine("		,FPMIncome = FPMIncome_M + FPMIncome_R	");
            sqlBuilder.AppendLine("		--高山利润 = 高山收入(货款) + 高山收入(退税) - 高山成本	");
            sqlBuilder.AppendLine("		,FPMProfit = FPMIncome_M + FPMIncome_R - FPMCost	");
            sqlBuilder.AppendLine("		--阳普生收入 = 阳普生收入(货款) + 阳普生收入(退税)	");
            sqlBuilder.AppendLine("		,FEGIncome = FEGIncome_M + FEGIncome_R	");
            sqlBuilder.AppendLine("		--阳普生利润 = 阳普生收入(货款) + 阳普生收入(退税) - 阳普生成本	");
            sqlBuilder.AppendLine("		,FEGProfit = FEGIncome_M + FEGIncome_R - FEGCost	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //插入高山阳普生合计行数据
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + mainTemp + " (FLevel,FOutInvoNo,FPMIncome_M,FPMIncome_R,FPMIncome,FPMCost,FPMProfit,FEGIncome_M,FEGIncome_R,FEGIncome,FEGCost_NoTax,FEGCost,FEGProfit)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		1				    FLevel	        --层级	");
            sqlBuilder.AppendLine("		,'合计'			    FOutInvoNo	    --合计	");
            sqlBuilder.AppendLine("		,SUM(FPMIncome_M)	FPMIncome_M	    --高山收入(货款)	");
            sqlBuilder.AppendLine("		,SUM(FPMIncome_R)	FPMIncome_R	    --高山收入(退税)	");
            sqlBuilder.AppendLine("		,SUM(FPMIncome)		FPMIncome	    --高山收入	");
            sqlBuilder.AppendLine("		,SUM(FPMCost)		FPMCost		    --高山成本	");
            sqlBuilder.AppendLine("		,SUM(FPMProfit)		FPMProfit	    --高山利润	");
            sqlBuilder.AppendLine("		,SUM(FEGIncome_M)	FEGIncome_M	    --阳普生收入(货款)	");
            sqlBuilder.AppendLine("		,SUM(FEGIncome_R)	FEGIncome_R	    --阳普生收入(退税)	");
            sqlBuilder.AppendLine("		,SUM(FEGIncome)		FEGIncome	    --阳普生收入	");
            sqlBuilder.AppendLine("		,SUM(FEGCost_NoTax)	FEGCost_NoTax	--阳普生成本(不含税)	");
            sqlBuilder.AppendLine("		,SUM(FEGCost)		FEGCost		    --阳普生成本	");
            sqlBuilder.AppendLine("		,SUM(FEGProfit)		FEGProfit	    --阳普生利润	");
            sqlBuilder.AppendLine("	FROM " + mainTemp + "	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            //插入离岸公司数据
            string sql = string.Format(@"
                                        select 
	                                        FOffshoreCompany                        --离岸公司抬头
	                                        ,sum(FOffshoreIncome)   FOffshoreIncome --离岸收入
	                                        ,sum(FOffshoreCost)     FOffshoreCost   --离岸成本
	                                        ,sum(FOffshoreProfit)   FOffshoreProfit --离岸利润
                                        from {0} 
                                        where FOffshoreCompany is not null and FOffshoreCompany <> ''
                                        group by FOffshoreCompany
                                        ", mainTemp);
            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (result != null && result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (i == 0)
                    {
                        string updateSql = string.Format("update {0} set FOffshoreCompany = '{1}',FOffshoreIncome = {2},FOffshoreCost = {3},FOffshoreProfit = {4} where FLevel = 1"
                                                            , mainTemp
                                                            , Convert.ToString(result[i]["FOffshoreCompany"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreIncome"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreCost"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreProfit"]));
                        DBUtils.Execute(this.Context, updateSql);
                    }
                    else
                    {
                        string insertSql = string.Format("insert into {0} (FLevel,FOffshoreCompany,FOffshoreIncome,FOffshoreCost,FOffshoreProfit) values({1},'{2}',{3},{4},{5})"
                                                            , mainTemp
                                                            , i + 1
                                                            , Convert.ToString(result[i]["FOffshoreCompany"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreIncome"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreCost"])
                                                            , Convert.ToDecimal(result[i]["FOffshoreProfit"]));
                        DBUtils.Execute(this.Context, insertSql);
                    }
                }
            }
            //更新利润率
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	update " + mainTemp + "	");
            sqlBuilder.AppendLine("	set --离岸利润率% = 离岸利润 / 离岸收入	");
            sqlBuilder.AppendLine("		FOffshoreProfitRate = case when FOffshoreIncome <> 0 and FOffshoreProfit <> 0 and Convert(decimal(18,2), FOffshoreProfit / FOffshoreIncome * 100) <> 0 then cast(Convert(decimal(18,2), FOffshoreProfit / FOffshoreIncome * 100) AS varchar) + '%' else '' end	");
            sqlBuilder.AppendLine("		--高山利润率% = 高山利润 / 高山收入	");
            sqlBuilder.AppendLine("		,FPMProfitRate = case when FPMIncome <> 0 and FPMProfit <> 0 and Convert(decimal(18,2), FPMProfit / FPMIncome * 100) <> 0 then cast(Convert(decimal(18,2), FPMProfit / FPMIncome * 100) AS varchar) + '%' else '' end	");
            sqlBuilder.AppendLine("		--阳普生利润率% = 阳普生利润 / 阳普生收入	");
            sqlBuilder.AppendLine("		,FEGProfitRate = case when FEGIncome <> 0 and FEGProfit <> 0 and Convert(decimal(18,2), FEGProfit / FEGIncome * 100) <> 0 then cast(Convert(decimal(18,2), FEGProfit / FEGIncome * 100) AS varchar) + '%' else '' end	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
    }
}

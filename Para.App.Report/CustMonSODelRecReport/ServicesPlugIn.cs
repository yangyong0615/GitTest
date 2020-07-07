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

namespace Para.App.Report.CustMonSODelRecReport
{
    [HotUpdate]
    [Description("客户业务往来月度表 - 服务器插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //开始日期
        DateTime beginDate = new DateTime();
        //结束日期
        DateTime endDate = new DateTime();
        //起始年月和截止年月之间的月份数
        int months = 0;
        //主临时表
        string mainTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("客户业务往来月度表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            //false表示用代码构建表头，true表示用BOS构建表头
            this.ReportProperty.IsUIDesignerColumns = false;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.SimpleAllCols = false;
            this.SetDecimalControl();
        }
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(接单)
                string columnNameSO = string.Format("F{0}_{1}_SO", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new SummaryField(columnNameSO, BOSEnums.Enu_SummaryType.SUM));
                //年-月 列(出货)
                string columnNameDel = string.Format("F{0}_{1}_Del", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new SummaryField(columnNameDel, BOSEnums.Enu_SummaryType.SUM));
                //年-月 列(收汇)
                string columnNameRec = string.Format("F{0}_{1}_Rec", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new SummaryField(columnNameRec, BOSEnums.Enu_SummaryType.SUM));
            }
            //累计接单
            list.Add(new SummaryField("FTOTALSOAMT", BOSEnums.Enu_SummaryType.SUM));
            //累计出货
            list.Add(new SummaryField("FTOTALDELAMT", BOSEnums.Enu_SummaryType.SUM));
            //累计收货
            list.Add(new SummaryField("FTOTALRECAMT", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            int width = 80;
            //序号列不需要自己去构建，采用系统标准的
            //ListHeader headChild1 = header.AddChild("FSEQ", new LocaleValue("序号"));
            //headChild1.Width = width;
            //headChild1.Mergeable = false;
            //headChild1.Visible = true;
            ListHeader headChild2 = header.AddChild("FCUSTNUM", new LocaleValue("客户编码"));
            headChild2.Width = width;
            headChild2.Mergeable = false;
            headChild2.Visible = true;
            ListHeader headChild3 = header.AddChild("FCUSTNAME", new LocaleValue("客户名称"));
            headChild3.Width = width;
            headChild3.Mergeable = false;
            headChild3.Visible = true;
            ListHeader headChild4 = header.AddChild("FCUSTTYPE", new LocaleValue("客户类别"));
            headChild4.Width = width;
            headChild4.Mergeable = false;
            headChild4.Visible = true;
            ListHeader headChild5 = header.AddChild("FRECCONDITION", new LocaleValue("账期"));
            headChild5.Width = width;
            headChild5.Mergeable = false;
            headChild5.Visible = true;
            //循环加入日期列
            ListHeader[] listHeader = new ListHeader[months];
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列
                string columnName = string.Format("F{0}_{1}", yearAndMonth.Year, yearAndMonth.Month);
                string title = string.Format("{0}年{1}月", yearAndMonth.Year, yearAndMonth.Month);
                listHeader[i] = header.AddChild();
                listHeader[i].Caption = new LocaleValue(title);
                listHeader[i].Width = width * 3;
                ListHeader headChild11 = listHeader[i].AddChild(string.Format("{0}_SO", columnName), new LocaleValue("接单"), SqlStorageType.SqlDecimal);
                headChild11.Width = width;
                headChild11.Mergeable = true;
                ListHeader headChild12 = listHeader[i].AddChild(string.Format("{0}_Del", columnName), new LocaleValue("出货"), SqlStorageType.SqlDecimal);
                headChild12.Width = width;
                headChild12.Mergeable = true;
                ListHeader headChild13 = listHeader[i].AddChild(string.Format("{0}_Rec", columnName), new LocaleValue("收汇"), SqlStorageType.SqlDecimal);
                headChild13.Width = width;
                headChild13.Mergeable = true;
            }
            ListHeader headChildEnd = header.AddChild();
            headChildEnd.Caption = new LocaleValue("累计（$:万元）");
            headChildEnd.Width = width * 3;
            ListHeader headChildEnd11 = headChildEnd.AddChild("FTOTALSOAMT", new LocaleValue("接单"), SqlStorageType.SqlDecimal);
            headChildEnd11.Width = width;
            headChildEnd11.Mergeable = true;
            ListHeader headChildEnd12 = headChildEnd.AddChild("FTOTALDELAMT", new LocaleValue("出货"), SqlStorageType.SqlDecimal);
            headChildEnd12.Width = width;
            headChildEnd12.Mergeable = true;
            ListHeader headChildEnd13 = headChildEnd.AddChild("FTOTALRECAMT", new LocaleValue("收汇"), SqlStorageType.SqlDecimal);
            headChildEnd13.Width = width;
            headChildEnd13.Mergeable = true;
            return header;
        }
        //设置精度
        private void SetDecimalControl()
        {
            List<DecimalControlField> list = new List<DecimalControlField>();
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列（接单）
                string columnNameSO = string.Format("F{0}_{1}_SO", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new DecimalControlField
                {
                    ByDecimalControlFieldName = columnNameSO,
                    DecimalControlFieldName = "FPRECISION"
                });
                //年-月 列（出货）
                string columnNameDel = string.Format("F{0}_{1}_Del", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new DecimalControlField
                {
                    ByDecimalControlFieldName = columnNameDel,
                    DecimalControlFieldName = "FPRECISION"
                });
                //年-月 列（收汇）
                string columnNameRec = string.Format("F{0}_{1}_Rec", yearAndMonth.Year, yearAndMonth.Month);
                list.Add(new DecimalControlField
                {
                    ByDecimalControlFieldName = columnNameRec,
                    DecimalControlFieldName = "FPRECISION"
                });
            }
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
                //插入接单数据
                this.InsertSOData();
                //插入出货数据
                this.InsertDelData();
                //插入收汇数据
                this.InsertRecData();
                //计算汇总数据
                this.UpdateTotalData();
                //删除未发生数据
                this.DelteNotHappenData();
                //排序按照：出货 倒序，接单 倒序，收汇 倒序
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FTOTALDELAMT DESC,FTOTALSOAMT DESC,FTOTALRECAMT DESC");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("	SELECT	");
                sql.AppendLine("	    " + base.KSQL_SEQ + "		--序号");
                sql.AppendLine("		,FCUSTNUM		            --客户编码	");
                sql.AppendLine("		,FCUSTNAME		            --客户名称	");
                sql.AppendLine("		,FCUSTTYPE		            --客户类别	");
                sql.AppendLine("		,FRECCONDITION		        --账期	");
                for (int i = 0; i < months; i++)
                {
                    DateTime yearAndMonth = beginDate.AddMonths(i);
                    //年-月 列
                    string columnName = string.Format("F{0}_{1}", yearAndMonth.Year, yearAndMonth.Month);
                    sql.AppendFormat("		,{0}_SO		            --年-月(接单)	\r\n", columnName);
                    sql.AppendFormat("		,{0}_Del		        --年-月(出货)	\r\n", columnName);
                    sql.AppendFormat("		,{0}_Rec		        --年-月(收汇)	\r\n", columnName);
                }
                sql.AppendLine("		,FTOTALSOAMT		        --累计接单	");
                sql.AppendLine("		,FTOTALDELAMT		        --累计出货	");
                sql.AppendLine("		,FTOTALRECAMT		        --累计收汇	");
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
            StringBuilder.AppendLine("FSEQ INT");                       //序号
            StringBuilder.AppendLine(",FCUSTNUM NVARCHAR(300)");        //客户编码
            StringBuilder.AppendLine(",FCUSTNAME NVARCHAR(300)");       //客户名称
            StringBuilder.AppendLine(",FCUSTTYPE NVARCHAR(300)");       //客户类别
            StringBuilder.AppendLine(",FRECCONDITION NVARCHAR(300)");   //账期
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(接单)
                string colunmNameSO = string.Format("F{0}_{1}_SO", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder.AppendFormat(",{0} DECIMAL(23, 10) DEFAULT(0)\r\n", colunmNameSO);
                //年-月 列(出货)
                string colunmNameDel = string.Format("F{0}_{1}_Del", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder.AppendFormat(",{0} DECIMAL(23, 10) DEFAULT(0)\r\n", colunmNameDel);
                //年-月 列(收汇)
                string colunmNameRec = string.Format("F{0}_{1}_Rec", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder.AppendFormat(",{0} DECIMAL(23, 10) DEFAULT(0)\r\n", colunmNameRec);
            }
            StringBuilder.AppendLine(",FTOTALSOAMT DECIMAL(23, 10) DEFAULT(0)");    //累计接单
            StringBuilder.AppendLine(",FTOTALDELAMT DECIMAL(23, 10) DEFAULT(0)");   //累计出货
            StringBuilder.AppendLine(",FTOTALRECAMT DECIMAL(23, 10) DEFAULT(0)");   //累计收汇
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
        //插入接单数据
        private void InsertSOData()
        {
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(接单)
                string columnName = string.Format("F{0}_{1}_SO", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("/*dialect*/	");
                sqlBuilder.AppendLine("	UPDATE TEMP	");
                sqlBuilder.AppendLine("		SET TEMP." + columnName + " = T.FAMT	");
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
                sqlBuilder.AppendLine("	AND YEAR(SO.FAPPROVEDATE) = " + yearAndMonth.Year + " AND MONTH(SO.FAPPROVEDATE) = " + yearAndMonth.Month + "	");
                sqlBuilder.AppendLine("	GROUP BY CUST.FNUMBER	");
                sqlBuilder.AppendLine("	) T	");
                sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
                DBUtils.Execute(this.Context, sqlBuilder.ToString());
            }
        }
        //插入出货数据
        private void InsertDelData()
        {
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(出货)
                string columnName = string.Format("F{0}_{1}_DEL", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("/*dialect*/	");
                sqlBuilder.AppendLine("	UPDATE TEMP	");
                sqlBuilder.AppendLine("		SET TEMP." + columnName + " = T.FAMT	");
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
                sqlBuilder.AppendLine("		AND YEAR(OUTSTOCK.FDATE) = " + yearAndMonth.Year + " AND MONTH(OUTSTOCK.FDATE) = " + yearAndMonth.Month + "	");
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
                sqlBuilder.AppendLine("		AND YEAR(DECBILL.FOFFSHOREDATE) = " + yearAndMonth.Year + " AND MONTH(DECBILL.FOFFSHOREDATE) = " + yearAndMonth.Month + "	");
                sqlBuilder.AppendLine("		AND CUST.FNUMBER NOT IN ('PG','IG')	");
                sqlBuilder.AppendLine("		GROUP BY CUST.FNUMBER	");
                sqlBuilder.AppendLine("		) DECTEMP	");
                sqlBuilder.AppendLine("		GROUP BY DECTEMP.FCUSTNUM	");
                sqlBuilder.AppendLine("	) T	");
                sqlBuilder.AppendLine("	WHERE TEMP.FCUSTNUM = T.FCUSTNUM	");
                DBUtils.Execute(this.Context, sqlBuilder.ToString());
            }
        }
        //插入收汇数据
        private void InsertRecData()
        {
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(收汇)
                string columnName = string.Format("F{0}_{1}_REC", yearAndMonth.Year, yearAndMonth.Month);
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("/*dialect*/	");
                sqlBuilder.AppendLine("	UPDATE T1	");
                sqlBuilder.AppendLine("		SET T1." + columnName + " = T2.FAMT	");
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
                sqlBuilder.AppendLine("			AND FSOURCEFROMID = 'AR_RECEIVEBILL'		--源单 = 收款单	");
                //sqlBuilder.AppendLine("			AND FTARGETFROMID = 'AR_RECEIVEBILL'	--目标单 = 收款单	");
                sqlBuilder.AppendLine("			AND ORG.FNUMBER IN ('PM','IG','PG','EG')	");
                sqlBuilder.AppendLine("			AND CUST.FCORRESPONDORGID = 0			--排除内部客户	");
                sqlBuilder.AppendLine("			AND YEAR(RECMacthLog.FVERIFYDATE) = " + yearAndMonth.Year + " AND MONTH(RECMacthLog.FVERIFYDATE) = " + yearAndMonth.Month + "	");
                sqlBuilder.AppendLine("		) T	");
                sqlBuilder.AppendLine("		GROUP BY T.FCUSTNUM	");
                sqlBuilder.AppendLine("	) T2	");
                sqlBuilder.AppendLine("	WHERE T1.FCUSTNUM = T2.FCUSTNUM	");
                DBUtils.Execute(this.Context, sqlBuilder.ToString());
            }
        }
        //计算汇总数据
        private void UpdateTotalData()
        {
            //累计接单
            StringBuilder totalSOAmt = new StringBuilder();
            //累计出货
            StringBuilder totalDelAmt = new StringBuilder();
            //累计收汇
            StringBuilder totalRecAmt = new StringBuilder();
            for (int i = 0; i < months; i++)
            {
                DateTime yearAndMonth = beginDate.AddMonths(i);
                //年-月 列(接单)
                string columnNameSO = string.Format("F{0}_{1}_SO", yearAndMonth.Year, yearAndMonth.Month);
                totalSOAmt.AppendFormat("+{0}", columnNameSO);
                //年-月 列(出货)
                string columnNameDel = string.Format("F{0}_{1}_DEL", yearAndMonth.Year, yearAndMonth.Month);
                totalDelAmt.AppendFormat("+{0}", columnNameDel);
                //年-月 列(接单)
                string columnNameRec = string.Format("F{0}_{1}_REC", yearAndMonth.Year, yearAndMonth.Month);
                totalRecAmt.AppendFormat("+{0}", columnNameRec);
            }
            string sql = string.Format(@"
                                        UPDATE {0}
	                                        SET FTOTALSOAMT = ROUND(({1})/10000,2)	
                                                ,FTOTALDELAMT =  ROUND(({2})/10000,2)	
                                                ,FTOTALRECAMT =  ROUND(({3})/10000,2)	
                                        ", mainTemp, totalSOAmt.Remove(0, 1), totalDelAmt.Remove(0, 1), totalRecAmt.Remove(0, 1));
            DBUtils.Execute(this.Context, sql);
        }
        //删除未发生数据
        private void DelteNotHappenData()
        {
            string sql = string.Format("DELETE FROM {0} WHERE FTOTALSOAMT = 0 AND FTOTALDELAMT = 0 AND FTOTALRECAMT = 0", mainTemp);
            DBUtils.Execute(this.Context, sql);
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //开始日期
                beginDate = Convert.ToDateTime(dyFilter["FBeginDate_F"]);
                //结束日期
                endDate = Convert.ToDateTime(dyFilter["FEndDate_F"]);
                //月份数
                months = endDate.Year * 12 + endDate.Month - beginDate.Year * 12 - beginDate.Month + 1;
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            title.AddTitle("FBeginDate_H", string.Format("{0}年{1}月", beginDate.Year, beginDate.Month));     //开始日期
            title.AddTitle("FEndDate_H", string.Format("{0}年{1}月", endDate.Year, endDate.Month));           //结束日期
            return title;
        }
    }
}

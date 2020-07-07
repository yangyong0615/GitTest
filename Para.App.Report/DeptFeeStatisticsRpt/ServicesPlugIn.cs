using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;

namespace Para.App.Report.DeptFeeStatisticsRpt
{
    [HotUpdate]
    [Description("部门费用统计表 - 服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //组织机构
        string orgIds = string.Empty;
        string orgName = string.Empty;
        //临时表
        string temp = string.Empty;
        //交叉临时表
        string jcTemp = string.Empty;
        //列名（部门）集合
        DynamicObjectCollection depCol;
        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("部门费用统计 表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsUIDesignerColumns = false;//false表示用代码构建表头，true表示用BOS构建表头
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

            this.ReportProperty.DecimalControlFieldList = list;
        }
        //设置报表列名
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            int width = 100;
            ListHeader headChild = header.AddChild("费用项目", new LocaleValue("费用项目"));
            headChild.Width = width;
            headChild.Mergeable = false;
            headChild.Visible = true;
            //循环添加部门            
            ListHeader[] listHeader = new ListHeader[depCol.Count];
            for (int i = 0; i < depCol.Count; i++)
            {
                //部门
                string depName = Convert.ToString(depCol[i]["FDepName"]);
                listHeader[i] = header.AddChild(depName, new LocaleValue(depName), SqlStorageType.SqlDecimal);
                listHeader[i].Width = width;
                listHeader[i].Mergeable = false;
                listHeader[i].Visible = true;
            }
            ListHeader headChildEnd = header.AddChild("合计", new LocaleValue("合计"), SqlStorageType.SqlDecimal);
            headChildEnd.Width = width;
            headChildEnd.Mergeable = false;
            headChildEnd.Visible = true;
            return header;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //base.BuilderReportSqlAndTempTable(filter, tableName);
                //创建临时表
                temp = DBUtils.CreateSessionTemplateTable(base.Context, "#TEMP", this.CreatTemp());
                //往临时表插入数据
                this.InsertDate();
                //列名（部门）集合
                depCol = DBUtils.ExecuteDynamicObject(this.Context, string.Format("SELECT FDepName FROM {0} GROUP BY FDepName", temp));
                //创建交叉临时表
                jcTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#JC_TEMP", this.CreatJCTemp());
                //往交叉表插入数据
                this.InsertJCData();
                //排序
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "序号");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("/*dialect*/	");
                sql.AppendLine("	SELECT	");
                sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
                sql.AppendLine("		,费用项目				");
                for (int i = 0; i < depCol.Count; i++)
                {
                    sql.AppendLine("		," + Convert.ToString(depCol[i]["FDepName"]) + "    ");
                }
                sql.AppendLine("		,合计				");
                sql.AppendLine("		,2 FPRECISION	    --精度	");
                sql.AppendFormat("	INTO {0}	\r\n", tableName);
                sql.AppendLine("	FROM " + jcTemp + "	");
                DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, temp);
                //删除交叉临时表
                DBUtils.DropSessionTemplateTable(base.Context, jcTemp);
            }
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //组织机构
                orgIds = Convert.ToString(dyFilter["MulSelOrgList_Filter"]);
                orgName = this.GetOrgName(orgIds);
                //起始日期
                beginDate = Convert.ToDateTime(dyFilter["BeginDate_Filter"]);
                //截止日期
                endDate = Convert.ToDateTime(dyFilter["EndDate_Filter"]);
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
            title.AddTitle("FOrg_H", orgName);
            return title;
        }
        //获取组织机构名称
        public string GetOrgName(string orgIds)
        {
            string sql = string.Format("/*dialect*/\r\nSELECT FNAME + ',' FROM T_ORG_ORGANIZATIONS_L WHERE FLOCALEID = '2052' AND FORGID IN ({0}) FOR XML PATH('')", orgIds);
            return DBUtils.ExecuteScalar<string>(this.Context, sql, string.Empty, new Kingdee.BOS.SqlParam[0]);
        }
        //创建临时表
        private string CreatTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            StringBuilder.AppendLine("FFeeItem NVARCHAR(200)");     //费用项目名称
            StringBuilder.AppendLine(",FDepName NVARCHAR(200)");    //部门名称
            StringBuilder.AppendLine(",FAmt DECIMAL(23, 10)");      //金额
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //创建交叉临时表
        private string CreatJCTemp()
        {
            StringBuilder StringBuilder = new StringBuilder();
            StringBuilder.AppendLine("(");
            //序号
            StringBuilder.AppendLine("序号 INT");
            //费用项目
            StringBuilder.AppendLine(",费用项目 NVARCHAR(200)");
            for (int i = 0; i < depCol.Count; i++)
            {
                //部门名称
                StringBuilder.AppendLine("," + Convert.ToString(depCol[i]["FDepName"]) + " DECIMAL(23, 10)");
            }
            //金额
            StringBuilder.AppendLine(",合计 DECIMAL(23, 10)");
            StringBuilder.AppendLine(")");
            return StringBuilder.ToString();
        }
        //往临时表插入数据
        private void InsertDate()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	INSERT INTO " + temp + " (FFEEITEM,FDEPNAME,FAMT)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		FFEEITEM,FDEPNAME,SUM(FAMT) FAMT	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	(	");
            sqlBuilder.AppendLine("		--费用报销单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			EXPENSE_L.FNAME				            FFEEITEM	");
            sqlBuilder.AppendLine("			,DEP_L.FNAME				            FDEPNAME	");
            sqlBuilder.AppendLine("			,T2.FTAXSUBMITAMT * T1.FEXCHANGERATE	FAMT	");
            sqlBuilder.AppendLine("		FROM T_ER_EXPENSEREIMB T1	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ER_EXPENSEREIMBENTRY T2	");
            sqlBuilder.AppendLine("		ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("		--费用项目	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_EXPENSE_L EXPENSE_L	");
            sqlBuilder.AppendLine("		ON EXPENSE_L.FEXPID = T2.FEXPID AND EXPENSE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = T2.FEXPENSEDEPTENTRYID	");
            sqlBuilder.AppendLine("		WHERE T1.FDOCUMENTSTATUS = 'C' AND T1.FEXPENSEORGID IN (" + orgIds + ")	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', T1.FDATE) >= 0  AND DATEDIFF(DAY, T1.FDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--付款申请单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			RECPAYPURPOSE_L.FNAME				FFEEITEM	");
            sqlBuilder.AppendLine("			,DEP_L.FNAME						FDEPNAME	");
            sqlBuilder.AppendLine("			,T2.FAPPLYAMOUNTFOR * FEXCHANGERATE	FAMT	");
            sqlBuilder.AppendLine("		FROM T_CN_PAYAPPLY T1	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CN_PAYAPPLYENTRY T2	");
            sqlBuilder.AppendLine("		ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = T2.FCOSTDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		--收付款用途	");
            sqlBuilder.AppendLine("		LEFT JOIN T_CN_RECPAYPURPOSE_L RECPAYPURPOSE_L	");
            sqlBuilder.AppendLine("		ON RECPAYPURPOSE_L.FID = T2.FPAYPURPOSEID AND RECPAYPURPOSE_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		WHERE T1.FDOCUMENTSTATUS = 'C' AND T1.FAPPLYORGID IN (" + orgIds + ")	");
            sqlBuilder.AppendLine("		--单据类型 = 其他付款申请 或 工资付款申请	");
            sqlBuilder.AppendLine("		AND T1.FBILLTYPEID IN ('78acc0ac3462ba8b11e300bc0786706b','5458351793be97')	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', FDATE) >= 0  AND DATEDIFF(DAY, FDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		AND DEP_L.FNAME IS NOT NULL	");
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		--折旧调整单	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			'折旧费'							FFEEITEM	");
            sqlBuilder.AppendLine("			,DEP_L.FNAME						FDEPNAME	");
            sqlBuilder.AppendLine("			,T3.FALLOCVALUE						FAMT	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--折旧调整单	");
            sqlBuilder.AppendLine("		T_FA_DEPRADJUST T1	");
            sqlBuilder.AppendLine("		--折旧汇总	");
            sqlBuilder.AppendLine("		LEFT JOIN T_FA_DEPRADJUSTENTRY T2	");
            sqlBuilder.AppendLine("		ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("		--折旧分配	");
            sqlBuilder.AppendLine("		LEFT JOIN T_FA_DEPRADJUSTDETAIL T3	");
            sqlBuilder.AppendLine("		ON T2.FENTRYID = T3.FENTRYID	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = T3.FUSEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		WHERE T1.FDOCUMENTSTATUS = 'C' AND T1.FOWNERORGID IN (" + orgIds + ")	");
            sqlBuilder.AppendLine("		AND T1.FYEAR >= YEAR('" + beginDate + "') AND T1.FYEAR <= YEAR('" + endDate + "')	");
            sqlBuilder.AppendLine("		AND T1.FPERIOD >= MONTH('" + beginDate + "') AND T1.FPERIOD <= MONTH('" + endDate + "')	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	GROUP BY FFEEITEM,FDEPNAME	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //往交叉表插入数据
        private void InsertJCData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	INSERT INTO " + jcTemp + "  ");
            sqlBuilder.AppendLine(" (序号,费用项目    ");
            for (int i = 0; i < depCol.Count; i++)
            {
                sqlBuilder.AppendLine(" ," + Convert.ToString(depCol[i]["FDepName"]) + "    ");
            }
            sqlBuilder.AppendLine(" ,合计)    ");
            sqlBuilder.AppendLine(" SELECT  ");
            sqlBuilder.AppendLine("     ROW_NUMBER() OVER(ORDER BY FFEEITEM) 序号    ");
            sqlBuilder.AppendLine("     ,FFEEITEM as 费用项目  ");
            for (int i = 0; i < depCol.Count; i++)
            {
                //部门
                string depName = Convert.ToString(depCol[i]["FDepName"]);
                sqlBuilder.AppendLine("     ,MAX(CASE FDEPNAME WHEN '" + depName + "' THEN FAMT ELSE 0 END) " + depName + "");
            }
            sqlBuilder.AppendLine("     ,SUM(FAMT) as 合计  ");
            sqlBuilder.AppendLine(" FROM " + temp + " GROUP BY FFEEITEM ");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
            //插入合计行
            sqlBuilder.Clear();
            sqlBuilder.AppendLine("	INSERT INTO " + jcTemp + "  ");
            sqlBuilder.AppendLine(" (序号,费用项目    ");
            for (int i = 0; i < depCol.Count; i++)
            {
                sqlBuilder.AppendLine(" ," + Convert.ToString(depCol[i]["FDepName"]) + "    ");
            }
            sqlBuilder.AppendLine(" ,合计)    ");
            sqlBuilder.AppendLine(" SELECT  ");
            sqlBuilder.AppendLine("     MAX(序号) + 1 AS 序号    ");
            sqlBuilder.AppendLine("     ,MAX('合计') AS 费用项目  ");
            for (int i = 0; i < depCol.Count; i++)
            {
                //部门
                string depName = Convert.ToString(depCol[i]["FDepName"]);
                sqlBuilder.AppendLine("     ,SUM(" + depName + ") " + depName + "");
            }
            sqlBuilder.AppendLine("     ,SUM(合计) as 合计  ");
            sqlBuilder.AppendLine(" FROM " + jcTemp + " ");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
    }
}

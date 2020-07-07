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

namespace Para.App.Report.SupBusDepStatisReport
{
    [HotUpdate]
    [Description("供应商采购统计表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //组织机构
        string orgName = string.Empty;
        string orgId = string.Empty;
        //事业部-业务部映射表
        string depMatchTemp = string.Empty;
        //供应商排名表
        string rankTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("供应商采购统计表", base.Context.UserLocale.LCID);
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
            //采购金额本位币
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
            //采购金额本位币
            list.Add(new SummaryField("FAmtLC", BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建临时表（事业部-业务部映射表）
                depMatchTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#DepMatchTemp", this.CreateDepMatchTemp());
                //创建临时表（供应商排名表）
                rankTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#RankTemp", this.CreateRankTemp());
                //插入事业部-业务部映射表数据
                this.InsertIntoDepMatchTemp();
                //插入供应商排名数据
                this.InsertIntoRankTemp();
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FRANK,FAMTLC DESC");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("/*dialect*/	");
                sql.AppendLine("	SELECT	");
                sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
                sql.AppendLine("		,FRANK		        --排名	");
                sql.AppendLine("		,FSUPTYPE2			--供应商类别	");
                sql.AppendLine("		,FSUPNUM			--供应商编码	");
                sql.AppendLine("		,FSUPNAME		    --供应商名称	");
                sql.AppendLine("		,FBUSDEPT			--事业部	");
                sql.AppendLine("		,FAMTLC			    --采购金额本位币	");
                sql.AppendLine("		,2 FPRECISION	    --精度	");
                sql.AppendFormat("	INTO {0}	\r\n", tableName);
                sql.AppendLine("	FROM	");
                sql.AppendLine("	(	");
                sql.Append(this.GetSql());
                sql.AppendLine("	) TT	");
                DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());

                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, depMatchTemp);
                DBUtils.DropSessionTemplateTable(base.Context, rankTemp);
            }
        }
        //创建临时表（事业部-业务部映射表）
        private string CreateDepMatchTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FBUSDEPT NVARCHAR(300) ");      //事业部名称			
            sqlBuilder.AppendLine("	,FDEPT NVARCHAR(300)   ");      //部门名称
            sqlBuilder.AppendLine("	,FDEPTID INT   ");              //部门ID      
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //创建临时表（供应商排名表）
        private string CreateRankTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FRANK INT ");                   //供应商排名
            sqlBuilder.AppendLine("	,FSUPNUM NVARCHAR(300)   ");    //供应商编码     
            sqlBuilder.AppendLine("	,FSUPNAME NVARCHAR(300)   ");   //供应商名称    
            sqlBuilder.AppendLine("	,FSUPTYPE2 NVARCHAR(2000)   "); //供应商二级分类    
            sqlBuilder.AppendLine("	,FAMTLC DECIMAL(23,10)   ");    //采购金额    
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //插入事业部-业务部映射表数据
        private void InsertIntoDepMatchTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + depMatchTemp + " (FBUSDEPT,FDEPT,FDEPTID)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T1.FNAME		FBUSDEPT	--事业部	");
            sqlBuilder.AppendLine("		,T2.FNAME		FDEPT		--业务部	");
            sqlBuilder.AppendLine("		,T2.FDEPTID		FDEPTID		--业务部ID	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	(--找出事业部	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DEP_L.FNAME	");
            sqlBuilder.AppendLine("	FROM T_BD_DEPARTMENT DEP	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP.FDEPTID = DEP_L.FDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	WHERE DEP.FDOCUMENTSTATUS = 'C' AND DEP_L.FNAME LIKE '%事业部%'	");
            sqlBuilder.AppendLine("	GROUP BY DEP_L.FNAME	");
            sqlBuilder.AppendLine("	) T1	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L T2	");
            sqlBuilder.AppendLine("	ON T2.FFULLNAME LIKE '%'+T1.FNAME+'%' AND T2.FLOCALEID = 2052	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入供应商排名数据
        private void InsertIntoRankTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	--按采购金额统计供应商排名信息	");
            sqlBuilder.AppendLine("	INSERT INTO " + rankTemp + " (FRANK,FSUPNUM,FSUPNAME,FSUPTYPE2,FAMTLC)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		ROW_NUMBER() OVER(ORDER BY SUM(ISNULL(POFIN.FBILLALLAMOUNT_LC,0)) DESC)	");
            sqlBuilder.AppendLine("													FRANK		--按照采购金额倒序	");
            sqlBuilder.AppendLine("		,SUP.FNUMBER								FSUPNUM	");
            sqlBuilder.AppendLine("		,SUP_L.FNAME								FSUPNAME	");
            sqlBuilder.AppendLine("		,ISNULL(SUPTYPE.FSUPTYPE2,'')				FSUPTYPE2	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(POFIN.FBILLALLAMOUNT_LC,0))		FAMTLC	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERFIN POFIN	");
            sqlBuilder.AppendLine("	ON PO.FID = POFIN.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE BILLTYPE	");
            sqlBuilder.AppendLine("	ON PO.FBILLTYPEID = BILLTYPE.FBILLTYPEID	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON PO.FSUPPLIERID = SUP.FSUPPLIERID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP.FSUPPLIERID = SUP_L.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--获取供应商二级分类	");
            sqlBuilder.AppendLine("	LEFT JOIN (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			SUP.FSUPPLIERID	");
            sqlBuilder.AppendLine("			,FSUPTYPE2 = STUFF(	");
            sqlBuilder.AppendLine("			(SELECT	");
            sqlBuilder.AppendLine("				 DISTINCT '，【'+ T2.FNAME+'】'	");
            sqlBuilder.AppendLine("			FROM TP_T_FSUPPLIERTYPEENTRY T1	");
            sqlBuilder.AppendLine("			LEFT JOIN TP_T_SUPPLIERTYPE_L T2	");
            sqlBuilder.AppendLine("			ON T1.FSUPPLIERTYPE2 = T2.FID AND T2.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("			WHERE SUP.FSUPPLIERID = T1.FSUPPLIERID	");
            sqlBuilder.AppendLine("			FOR XML PATH(''))	");
            sqlBuilder.AppendLine("			,1,1,''	");
            sqlBuilder.AppendLine("			)	");
            sqlBuilder.AppendLine("		FROM T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("		ON SUP.FSUPPLIERID = SUP_L.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	) SUPTYPE	");
            sqlBuilder.AppendLine("	ON SUPTYPE.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("	WHERE PO.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	AND ORG.FNUMBER IN ('PM','EG')	");
            sqlBuilder.AppendLine("	AND SUP.FNUMBER <> 'EG'	");
            sqlBuilder.AppendLine("	AND DEP_L.FNAME NOT LIKE '%义乌办%'	");
            sqlBuilder.AppendLine("	--单据类型：标准采购订单，外贸采购订单'	");
            sqlBuilder.AppendLine("	AND BILLTYPE.FNUMBER IN ('PC','CGDD09_SYS')	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', PO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, PO.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	GROUP BY SUP.FNUMBER,SUP_L.FNAME,SUPTYPE.FSUPTYPE2	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		RANKTEMP.FRANK		--供应商排名	");
            sqlBuilder.AppendLine("		,RANKTEMP.FSUPTYPE2	--供应商二级分类	");
            sqlBuilder.AppendLine("		,RANKTEMP.FSUPNUM	--供应商编码	");
            sqlBuilder.AppendLine("		,RANKTEMP.FSUPNAME	--供应商名称	");
            sqlBuilder.AppendLine("		,MATCH.FBUSDEPT		--事业部	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(POFIN.FBILLALLAMOUNT_LC,0)) FAMTLC	--采购金额本位币	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERFIN POFIN	");
            sqlBuilder.AppendLine("	ON PO.FID = POFIN.FID	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE BILLTYPE	");
            sqlBuilder.AppendLine("	ON PO.FBILLTYPEID = BILLTYPE.FBILLTYPEID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON PO.FSUPPLIERID = SUP.FSUPPLIERID	");
            sqlBuilder.AppendLine("	--业务部-事业部映射表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + depMatchTemp + " MATCH	");
            sqlBuilder.AppendLine("	ON PO.FPURCHASEDEPTID = MATCH.FDEPTID	");
            sqlBuilder.AppendLine("	--供应商排名表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + rankTemp + " RANKTEMP	");
            sqlBuilder.AppendLine("	ON RANKTEMP.FSUPNUM = SUP.FNUMBER	");
            sqlBuilder.AppendLine("	WHERE PO.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	AND ORG.FNUMBER IN ('PM','EG')	");
            sqlBuilder.AppendLine("	AND SUP.FNUMBER <> 'EG'	");
            sqlBuilder.AppendLine("	AND DEP_L.FNAME NOT LIKE '%义乌办%'	");
            sqlBuilder.AppendLine("	--单据类型：标准采购订单，外贸采购订单'	");
            sqlBuilder.AppendLine("	AND BILLTYPE.FNUMBER IN ('PC','CGDD09_SYS')	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', PO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, PO.FAPPROVEDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	GROUP BY RANKTEMP.FRANK,RANKTEMP.FSUPTYPE2,RANKTEMP.FSUPNUM,RANKTEMP.FSUPNAME,MATCH.FBUSDEPT	");
            return sqlBuilder.ToString();
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
            //组织机构
            title.AddTitle("FOrgName_H", orgName);
            return title;
        }
    }
}

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
namespace Para.App.Report.CategoryPurRankRpt
{
    [HotUpdate]
    [Description("品类采购排名表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //组织机构
        string orgName = string.Empty;
        string orgId = string.Empty;
        //定义临时表：存放物料ID和第二级分组
        string matTemp = string.Empty;
        //定义临时表：存放品类排名数据
        string rankTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("品类采购排名表", base.Context.UserLocale.LCID);
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
                //创建临时表（存放物料ID和第二级分组）
                matTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#MatTemp", this.CreateMatTemp());
                //创建临时表（存放品类排名）
                rankTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#RankTemp", this.CreateRankTemp());
                //插入物料数据
                this.InsertIntoMatTemp();
                //插入品类排名数据
                this.InsertIntoRankTemp();        
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FRANK,FAMTLC DESC");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("/*dialect*/	");
                sql.AppendLine("	SELECT	");
                sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
                sql.AppendLine("		,FRANK		        --排名	");
                sql.AppendLine("		,FMATGROUP			--品类	");
                sql.AppendLine("		,FSUPNUM			--供应商编码	");
                sql.AppendLine("		,FSUPNAME		    --供应商名称	");
                sql.AppendLine("		,FAMTLC			    --采购金额本位币	");
                sql.AppendLine("		,2 FPRECISION	    --精度	");
                sql.AppendFormat("	INTO {0}	\r\n", tableName);
                sql.AppendLine("	FROM	");
                sql.AppendLine("	(	");
                sql.Append(this.GetSql());
                sql.AppendLine("	) TT	");
                DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());

                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, matTemp);
                DBUtils.DropSessionTemplateTable(base.Context, rankTemp);
            }
        }
        //创建临时表（存放物料ID和第二级分组）
        private string CreateMatTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FMATNUM NVARCHAR(300)   ");        //物料编码     
            sqlBuilder.AppendLine("	,FMATGROUP NVARCHAR(300)   ");      //第二级分组名称(品类)    
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //创建临时表（存放品类排名数据）
        private string CreateRankTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FMATGROUP NVARCHAR(300)   ");      //第二级分组名称(品类)   
            sqlBuilder.AppendLine("	,FRANK INT ");                     //品类排名 
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //插入物料数据
        private void InsertIntoMatTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            //sqlBuilder.AppendLine(" GO	");
            sqlBuilder.AppendLine("	--判断要创建的函数名是否存在	");
            sqlBuilder.AppendLine("	if exists (select * from sysobjects where xtype='fn' and name='GetSecondGroupId')	");
            //sqlBuilder.AppendLine("	drop function dbo.GetSecondGroupId	");
            sqlBuilder.AppendLine("	drop function GetSecondGroupId	");
            sqlBuilder.AppendLine("GO	");
            sqlBuilder.AppendLine("	--创建函数，获取物料第二级分组的ID（如果获取不到二级分组，则取当前物料分组）	");
            sqlBuilder.AppendLine("	CREATE FUNCTION DBO.GetSecondGroupId(@fullParentPathId VARCHAR(MAX))	");
            sqlBuilder.AppendLine("	RETURNS VARCHAR(MAX)	");
            sqlBuilder.AppendLine("	AS	");
            sqlBuilder.AppendLine("	BEGIN	");
            sqlBuilder.AppendLine("		DECLARE @INDEX1 INT		--标记第一次出现的位置	");
            sqlBuilder.AppendLine("		DECLARE @INDEX2 INT		--标记第二次出现的位置	");
            sqlBuilder.AppendLine("		DECLARE @INDEX3 INT		--标记第三次出现的位置	");
            sqlBuilder.AppendLine("		DECLARE @DATA VARCHAR(MAX) = '0'	");
            sqlBuilder.AppendLine("		SET @INDEX1 = CHARINDEX('.',@fullParentPathId)	");
            sqlBuilder.AppendLine("		SET @INDEX2 = CHARINDEX('.',@fullParentPathId,@INDEX1+1)	");
            sqlBuilder.AppendLine("		SET @INDEX3 = CHARINDEX('.',@fullParentPathId,@INDEX2+1)	");
            sqlBuilder.AppendLine("		IF(@INDEX2 <> 0 AND @INDEX3 <> 0)	");
            sqlBuilder.AppendLine("			SET @DATA = SUBSTRING(@fullParentPathId,@INDEX2+1,@INDEX3-@INDEX2-1)	");
            sqlBuilder.AppendLine("		ELSE IF(@INDEX2 <> 0 AND @INDEX3 = 0)	");
            sqlBuilder.AppendLine("			SET @DATA = RIGHT(@fullParentPathId,LEN(@fullParentPathId)-@INDEX2)	");
            sqlBuilder.AppendLine("		ELSE	");
            sqlBuilder.AppendLine("			SET @DATA = '0'	");
            sqlBuilder.AppendLine("		RETURN @DATA	");
            sqlBuilder.AppendLine("	END;	");
            sqlBuilder.AppendLine("	GO	");
            //DBUtils.Execute(this.Context, sqlBuilder.ToString());
            sqlBuilder.Clear();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + matTemp + " (FMATNUM,FMATGROUP)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T.FNUMBER          FMATNUM	");
            sqlBuilder.AppendLine("		,MATGROUP_L.FNAME   FMATGROUP	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			MAT.FNUMBER	");
            sqlBuilder.AppendLine("			--获取物料第二级分组，如果没有则获取本级分组	");
            sqlBuilder.AppendLine("			,CASE WHEN DBO.GetSecondGroupId(MATGROUP.FFULLPARENTID) = '0' THEN MATGROUP.FID	");
            sqlBuilder.AppendLine("					ELSE DBO.GetSecondGroupId(MATGROUP.FFULLPARENTID)	");
            sqlBuilder.AppendLine("			END MATGROUPID	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单	");
            sqlBuilder.AppendLine("		T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("		ON PO.FID = POENTRY.FID	");
            sqlBuilder.AppendLine("		--单据类型	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BAS_BILLTYPE BILLTYPE	");
            sqlBuilder.AppendLine("		ON PO.FBILLTYPEID = BILLTYPE.FBILLTYPEID	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("		--供应商	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("		ON SUP.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("		--物料	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("		ON MAT.FMATERIALID = POENTRY.FMATERIALID	");
            sqlBuilder.AppendLine("		--物料分组	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_MATERIALGROUP MATGROUP	");
            sqlBuilder.AppendLine("		ON MATGROUP.FID = MAT.FMATERIALGROUP	");
            //添加过滤
            sqlBuilder.AppendLine(this.GetWhereStr());
            sqlBuilder.AppendLine("		GROUP BY MAT.FNUMBER,MATGROUP.FFULLPARENTID,MATGROUP.FID	");
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	--物料分组	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIALGROUP_L MATGROUP_L	");
            sqlBuilder.AppendLine("	ON MATGROUP_L.FID  = T.MATGROUPID AND MATGROUP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	GROUP BY T.FNUMBER,MATGROUP_L.FNAME	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入品类排名数据
        private void InsertIntoRankTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + rankTemp + " (FMATGROUP,FRANK)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		MATTEMP.FMATGROUP	");
            sqlBuilder.AppendLine("		,ROW_NUMBER() OVER(ORDER BY SUM(ISNULL(POENTRY_F.FALLAMOUNT_LC,0)) DESC) FRANK	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("	ON PO.FID = POENTRY.FID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_F POENTRY_F	");
            sqlBuilder.AppendLine("	ON POENTRY_F.FENTRYID = POENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE BILLTYPE	");
            sqlBuilder.AppendLine("	ON PO.FBILLTYPEID = BILLTYPE.FBILLTYPEID	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON SUP.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--物料	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = POENTRY.FMATERIALID	");
            sqlBuilder.AppendLine("	--物料品类表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + matTemp + " MATTEMP	");
            sqlBuilder.AppendLine("	ON MATTEMP.FMATNUM = MAT.FNUMBER	");
            //添加过滤
            sqlBuilder.AppendLine(this.GetWhereStr());
            sqlBuilder.AppendLine("	GROUP BY MATTEMP.FMATGROUP	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		RANKTEMP.FRANK			--品类排名	");
            sqlBuilder.AppendLine("		,RANKTEMP.FMATGROUP		--品类	");
            sqlBuilder.AppendLine("		,SUP.FNUMBER    FSUPNUM			--供应商编码	");
            sqlBuilder.AppendLine("		,SUP_L.FNAME    FSUPNAME		--供应商	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(POENTRY_F.FALLAMOUNT_LC,0))	FAMTLC	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY POENTRY	");
            sqlBuilder.AppendLine("	ON PO.FID = POENTRY.FID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_F POENTRY_F	");
            sqlBuilder.AppendLine("	ON POENTRY_F.FENTRYID = POENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--单据类型	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BAS_BILLTYPE BILLTYPE	");
            sqlBuilder.AppendLine("	ON PO.FBILLTYPEID = BILLTYPE.FBILLTYPEID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON SUP.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP.FSUPPLIERID = SUP_L.FSUPPLIERID AND SUP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--物料	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = POENTRY.FMATERIALID	");
            sqlBuilder.AppendLine("	--物料品类表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + matTemp + " MATTEMP	");
            sqlBuilder.AppendLine("	ON MATTEMP.FMATNUM = MAT.FNUMBER	");
            sqlBuilder.AppendLine("	--品类排名表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + rankTemp + " RANKTEMP	");
            sqlBuilder.AppendLine("	ON RANKTEMP.FMATGROUP = MATTEMP.FMATGROUP	");
            //插入过滤条件
            sqlBuilder.AppendLine(this.GetWhereStr());
            sqlBuilder.AppendLine("	GROUP BY RANKTEMP.FRANK,RANKTEMP.FMATGROUP,SUP.FNUMBER,SUP_L.FNAME	");
            return sqlBuilder.ToString();
        }
        private string GetWhereStr()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	WHERE PO.FDOCUMENTSTATUS = 'C'	");
            sqlBuilder.AppendLine("	AND ORG.FNUMBER IN ('PM','EG')	");
            sqlBuilder.AppendLine("	AND SUP.FNUMBER <> 'EG'	");
            sqlBuilder.AppendLine("	AND DEP_L.FNAME NOT LIKE '%义乌办%'	");
            sqlBuilder.AppendLine("	--单据类型：标准采购订单，外贸采购订单'	");
            sqlBuilder.AppendLine("	AND BILLTYPE.FNUMBER IN ('PC','CGDD09_SYS')	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', PO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY, PO.FAPPROVEDATE, '" + endDate + "') >= 0	");
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

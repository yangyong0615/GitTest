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

namespace Para.App.Report.ExportProductRankReport
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("出口产品排名表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //存放物料报关品名的临时表
        string temp = string.Empty;
        //排名方式
        string rankStyle = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("出口产品排名表", base.Context.UserLocale.LCID);
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
                ByDecimalControlFieldName = "FAmt",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建临时表（存放物料报关品名,客户，部门）
                temp = DBUtils.CreateSessionTemplateTable(base.Context, "#temp", this.CreateTemp());
                //插入数据
                this.InsertIntoTemp();
                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FRANK");
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("/*dialect*/	");
                sql.AppendLine("	SELECT	");
                sql.AppendFormat("		{0}			        --序号\r\n", base.KSQL_SEQ);
                sql.AppendLine("		,FRANK		        --排名	");
                sql.AppendLine("		,FMATNUM			--物料编码	");
                sql.AppendLine("		,FMATNAME			--物料名称	");
                sql.AppendLine("		,FSPECIFICATION		--规格型号	");
                sql.AppendLine("		,FDECNAME			--报关品名	");
                sql.AppendLine("		,FDEP			    --部门	");
                sql.AppendLine("		,FCUST			    --客户	");
                sql.AppendLine("		,FQTY			    --报关数量	");
                sql.AppendLine("		,FAMT			    --报关金额	");
                sql.AppendLine("		,2 FPRECISION	    --精度	");
                sql.AppendFormat("	INTO {0}	\r\n", tableName);
                sql.AppendLine("	FROM	");
                sql.AppendLine("	(	");
                sql.Append(this.GetSql());
                sql.AppendLine("	) TT	");
                DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());

                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, temp);
            }
        }
        //创建临时表（存放物料报关品名，部门，客户）
        private string CreateTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FMATNUM nvarchar(200) ");           //物料编码			
            sqlBuilder.AppendLine("	,FDECNAME nvarchar(2000)   ");      //报关品名      
            sqlBuilder.AppendLine("	,FCUST nvarchar(2000)   ");         //客户      
            sqlBuilder.AppendLine("	,FDEP nvarchar(2000)   ");          //部门      
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //往临时表插入数据
        private void InsertIntoTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine(" INSERT INTO " + temp + " (FMATNUM,FDECNAME,FCUST,FDEP)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		TT.FNUMBER	");
            sqlBuilder.AppendLine("		,FDECNAME = SUBSTRING(	");
            sqlBuilder.AppendLine("					STUFF((SELECT	");
            sqlBuilder.AppendLine("								'，' + B.FDECLARENAME	");
            sqlBuilder.AppendLine("							FROM TPT_FZH_DECALREDOC A	");
            sqlBuilder.AppendLine("							LEFT JOIN TPT_FZH_DECGOODSENTRY B	");
            sqlBuilder.AppendLine("							ON A.FID = B.FID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("							ON MAT.FMATERIALID = B.FGOODSID	");
            sqlBuilder.AppendLine("							WHERE A.FDOCUMENTSTATUS = 'C' AND A.FISOFFSHORE = '1' AND MAT.FNUMBER = TT.FNUMBER	");
            sqlBuilder.AppendLine("							AND DATEDIFF(DAY, '" + beginDate + "', A.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, A.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("							GROUP BY B.FDECLARENAME	");
            sqlBuilder.AppendLine("							for xml path(''))	");
            sqlBuilder.AppendLine("							,1,1,'')	");
            sqlBuilder.AppendLine("					,0,2000)	");
            sqlBuilder.AppendLine("		,FCUST = SUBSTRING(	");
            sqlBuilder.AppendLine("							(SELECT	");
            sqlBuilder.AppendLine("								'【' + CUST_L.FNAME + '】'	");
            sqlBuilder.AppendLine("							FROM TPT_FZH_DecalreDoc A	");
            sqlBuilder.AppendLine("							LEFT JOIN TPT_FZH_DecGoodsEntry B	");
            sqlBuilder.AppendLine("							ON A.FID = B.FID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("							ON MAT.FMATERIALID = B.FGOODSID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("							ON CUST.FCUSTID = A.FCUSID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("							ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("							WHERE A.FDOCUMENTSTATUS = 'C' AND A.FISOFFSHORE = '1' AND MAT.FNUMBER = TT.FNUMBER	");
            sqlBuilder.AppendLine("							AND DATEDIFF(DAY, '" + beginDate + "', A.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, A.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("							GROUP BY CUST_L.FNAME	");
            sqlBuilder.AppendLine("							for xml path(''))	");
            sqlBuilder.AppendLine("					,0,2000)	");
            sqlBuilder.AppendLine("		,FDEP = SUBSTRING(	");
            sqlBuilder.AppendLine("					STUFF((SELECT	");
            sqlBuilder.AppendLine("								'，' + DEP_L.FNAME	");
            sqlBuilder.AppendLine("							FROM TPT_FZH_DecalreDoc A	");
            sqlBuilder.AppendLine("							LEFT JOIN TPT_FZH_DecGoodsEntry B	");
            sqlBuilder.AppendLine("							ON A.FID = B.FID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("							ON MAT.FMATERIALID = B.FGOODSID	");
            sqlBuilder.AppendLine("							LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("							ON DEP_L.FDEPTID = A.FSALEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("							WHERE A.FDOCUMENTSTATUS = 'C' AND A.FISOFFSHORE = '1' AND MAT.FNUMBER = TT.FNUMBER	");
            sqlBuilder.AppendLine("							AND DATEDIFF(DAY, '" + beginDate + "', A.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, A.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("							GROUP BY DEP_L.FNAME	");
            sqlBuilder.AppendLine("							for xml path(''))	");
            sqlBuilder.AppendLine("							,1,1,'')	");
            sqlBuilder.AppendLine("					,0,2000)	");
            sqlBuilder.AppendLine("	FROM (	");
            sqlBuilder.AppendLine("		--找出报关单上所有物料的编码	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			MAT.FNUMBER	");
            sqlBuilder.AppendLine("		FROM TPT_FZH_DecalreDoc T1	");
            sqlBuilder.AppendLine("		LEFT JOIN TPT_FZH_DecGoodsEntry T2	");
            sqlBuilder.AppendLine("		ON T1.FID = T2.FID	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("		ON MAT.FMATERIALID = T2.FGOODSID	");
            sqlBuilder.AppendLine("		WHERE T1.FDOCUMENTSTATUS = 'C' AND T1.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("		AND DATEDIFF(DAY, '" + beginDate + "', T1.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, T1.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("		GROUP BY MAT.FNUMBER	");
            sqlBuilder.AppendLine("	) TT	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("	SELECT	");
            if (rankStyle == "0")
            {
                sqlBuilder.AppendLine("		ROW_NUMBER() OVER(ORDER BY FQTY DESC) FRANK	");
            }
            else
            {
                sqlBuilder.AppendLine("		ROW_NUMBER() OVER(ORDER BY FAMT DESC) FRANK	");
            }
            sqlBuilder.AppendLine("		,FMATNUM	    --物料编码	");
            sqlBuilder.AppendLine("		,FMATNAME	    --物料名称	");
            sqlBuilder.AppendLine("		,FSPECIFICATION	--规格型号	");
            sqlBuilder.AppendLine("		,FDECNAME	    --报关品名	");
            sqlBuilder.AppendLine("		,FDEP		    --部门	");
            sqlBuilder.AppendLine("		,FCUST		    --客户	");
            sqlBuilder.AppendLine("		,FQTY		    --报关数量	");
            sqlBuilder.AppendLine("		,FAMT		    --报关金额	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		MAT.FNUMBER								FMATNUM		    --物料编码	");
            sqlBuilder.AppendLine("		,MAT_L.FNAME							FMATNAME	    --物料名称	");
            sqlBuilder.AppendLine("		,MAT_L.FSPECIFICATION					FSPECIFICATION	--规格型号	");
            sqlBuilder.AppendLine("		,MIN(Temp.FDECNAME)				        FDECNAME	    --报关品名	");
            sqlBuilder.AppendLine("		,MIN(Temp.FDEP)						    FDEP		    --部门	");
            sqlBuilder.AppendLine("		,MIN(Temp.FCUST)					    FCUST		    --客户	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(DOCENTRY.FDECLAREQTY,0))	FQTY		    --报关数量	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(DOCENTRY.FUSDAMT,0))		FAMT		    --报关金额	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--报关单	");
            sqlBuilder.AppendLine("	TPT_FZH_DECALREDOC DOC	");
            sqlBuilder.AppendLine("	LEFT JOIN  TPT_FZH_DECGOODSENTRY DOCENTRY	");
            sqlBuilder.AppendLine("	ON DOC.FID = DOCENTRY.FID	");
            sqlBuilder.AppendLine("	--物料	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = DOCENTRY.FGOODSID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL_L MAT_L	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = MAT_L.FMATERIALID AND MAT_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("	--临时表	");
            sqlBuilder.AppendLine("	LEFT JOIN " + temp + " Temp	");
            sqlBuilder.AppendLine("	ON Temp.FMATNUM = MAT.FNUMBER	");
            sqlBuilder.AppendLine("	WHERE DOC.FDOCUMENTSTATUS = 'C' AND DOC.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', DOC.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, DOC.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            sqlBuilder.AppendLine("	GROUP BY MAT.FNUMBER,MAT_L.FNAME,MAT_L.FSPECIFICATION	");
            sqlBuilder.AppendLine("	) T	");
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
                //排名方式
                rankStyle = Convert.ToString(dyFilter["FRankStyle_Filter"]);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            //排名方式
            title.AddTitle("FRankStyle_H", rankStyle);
            return title;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;

namespace Para.App.Report.SupplierRankingReport
{
    [HotUpdate]
    [Description("供应商排名表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //统计方式
        string statisticsStyle = "0";
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //组织机构
        string orgId = string.Empty;
        //公司事业部
        string company = string.Empty;
        //部门
        string depName = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("供应商排名表", base.Context.UserLocale.LCID);
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
            //金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FAMT",
                DecimalControlFieldName = "FPRECISION"
            });
            this.ReportProperty.DecimalControlFieldList = list;
        }
        //查询脚本
        private string GetSql()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            //sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		ROW_NUMBER() OVER(ORDER BY SUM(ISNULL(FAMT,0)) DESC) FRANKING	--排名	");
            sqlBuilder.AppendLine("		,T.FSUPNUMBER						--供应商编码	");
            sqlBuilder.AppendLine("		,FSUPNAME							--供应商名称	");
            sqlBuilder.AppendLine("		,SUM(ISNULL(FAMT,0)) FAMT			--采购订单.价税合计本位币	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	(	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			SUP.FNUMBER									FSUPNUMBER	--供应商编码	");
            sqlBuilder.AppendLine("			,SUP_L.FNAME								FSUPNAME	--供应商名称	");
            sqlBuilder.AppendLine("			,ISNULL(POFIN.FBILLALLAMOUNT_LC,0)			FAMT		--采购订单.价税合计本位币	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购订单	");
            sqlBuilder.AppendLine("		T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("		--采购订单.表头财务	");
            sqlBuilder.AppendLine("		LEFT JOIN T_PUR_POORDERFIN POFIN	");
            sqlBuilder.AppendLine("		ON PO.FID = POFIN.FID	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = PO.FPURCHASEDEPTID AND DEP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		--供应商	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("		ON SUP.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("		ON SUP_L.FSUPPLIERID = SUP.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = PO.FPURCHASEORGID	");
            sqlBuilder.AppendLine("		WHERE PO.FDOCUMENTSTATUS = 'C' AND DATEDIFF(DAY,'" + beginDate + "',PO.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY,PO.FAPPROVEDATE,'" + endDate + "') >= 0	");
            //统计方式 = 按公司事业部统计
            if (statisticsStyle == "0")
            {
                if (company == "外贸公司")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER IN ('PM','EG')	");
                }
                else if (company == "知客")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER = 'ZK'	");
                }
                else if (company == "千源")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER = 'QY'	");
                }
                else if (company.Contains("事业部"))
                {
                    sqlBuilder.AppendLine("		AND DEP_L.FNAME IN (SELECT DISTINCT FNAME FROM T_BD_DEPARTMENT_L WHERE FFULLNAME LIKE '%" + company.Trim() + "%' )	");
                }
            }
            //统计方式 = 按具体组织和部门统计
            else if (statisticsStyle == "1")
            {
                sqlBuilder.AppendLine("		AND ORG.FORGID IN (" + orgId + ")	");
                sqlBuilder.AppendLine("		AND DEP_L.FNAME IN (" + depName + ")	");
            }
            sqlBuilder.AppendLine("		UNION ALL	");
            sqlBuilder.AppendLine("		SELECT	");
            sqlBuilder.AppendLine("			SUP.FNUMBER									FSUPNUMBER	--供应商编码	");
            sqlBuilder.AppendLine("			,SUP_L.FNAME								FSUPNAME	--供应商名称	");
            sqlBuilder.AppendLine("			,ISNULL(MRBFIN.FBILLALLAMOUNT_LC,0) * -1	FAMT		--采购退料单.价税合计本位币	");
            sqlBuilder.AppendLine("		FROM	");
            sqlBuilder.AppendLine("		--采购退料单	");
            sqlBuilder.AppendLine("		t_PUR_MRB MRB	");
            sqlBuilder.AppendLine("		--采购退料单.表头财务	");
            sqlBuilder.AppendLine("		left join T_PUR_MRBFIN MRBFIN	");
            sqlBuilder.AppendLine("		on MRBFIN.FID = MRB.FID	");
            sqlBuilder.AppendLine("		--部门	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("		ON DEP_L.FDEPTID = MRB.FPURCHASEDEPTID	");
            sqlBuilder.AppendLine("		--供应商	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("		ON SUP.FSUPPLIERID = MRB.FSUPPLIERID	");
            sqlBuilder.AppendLine("		LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("		ON SUP_L.FSUPPLIERID = SUP.FSUPPLIERID AND SUP_L.FLOCALEID = '2052'	");
            sqlBuilder.AppendLine("		--组织	");
            sqlBuilder.AppendLine("		LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("		ON ORG.FORGID = MRB.FSTOCKORGID	");
            sqlBuilder.AppendLine("		WHERE MRB.FDOCUMENTSTATUS = 'C' AND  DATEDIFF(DAY,'" + beginDate + "',MRB.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY,MRB.FAPPROVEDATE,'" + endDate + "') >= 0	");
            //统计方式 = 按公司事业部统计
            if (statisticsStyle == "0")
            {
                if (company == "外贸公司")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER IN ('PM','EG')	");
                }
                else if (company == "知客")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER = 'ZK'	");
                }
                else if (company == "千源")
                {
                    sqlBuilder.AppendLine("		AND ORG.FNUMBER = 'QY'	");
                }
                else if (company.Contains("事业部"))
                {
                    sqlBuilder.AppendLine("		AND DEP_L.FNAME IN (SELECT DISTINCT FNAME FROM T_BD_DEPARTMENT_L WHERE FFULLNAME LIKE '%" + company.Trim() + "%' )	");
                }
            }
            //统计方式 = 按具体组织和部门统计
            else if (statisticsStyle == "1")
            {
                sqlBuilder.AppendLine("		AND ORG.FORGID IN (" + orgId + ")	");
                sqlBuilder.AppendLine("		AND DEP_L.FNAME IN (" + depName + ")	");
            }
            sqlBuilder.AppendLine("	) T	");
            sqlBuilder.AppendLine("	WHERE T.FSUPNUMBER NOT IN('PM','EG')	");
            sqlBuilder.AppendLine("	GROUP BY T.FSUPNUMBER,T.FSUPNAME	");
            return sqlBuilder.ToString();
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FRANKING");
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("/*dialect*/	");
            sql.AppendLine("	SELECT	");
            sql.AppendFormat("		{0}			    --序号\r\n", base.KSQL_SEQ);
            sql.AppendLine("		,FRANKING		--排名	");
            sql.AppendLine("		,FSUPNUMBER		--供应商编码	");
            sql.AppendLine("		,FSUPNAME		--供应商	");
            sql.AppendLine("		,FAMT			--金额	");
            sql.AppendLine("		,2 FPRECISION	--精度	");
            sql.AppendFormat("	INTO {0}	\r\n", tableName);
            sql.AppendLine("	FROM	");
            sql.AppendLine("	(	");
            sql.Append(this.GetSql());
            sql.AppendLine("	) TT	");
            sql.AppendLine("	WHERE FAMT > 0	");
            DBUtils.ExecuteDynamicObject(this.Context, sql.ToString());
        }
        private void FilterParameter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (dyFilter != null)
            {
                //起始日期
                beginDate = Convert.ToDateTime(dyFilter["FBeginDate_Filter"]);
                //截止日期
                endDate = Convert.ToDateTime(dyFilter["FEndDate_Filter"]);
                //统计方式
                statisticsStyle = Convert.ToString(dyFilter["FStatisticsStyle_Filter"]);
                //统计方式 = 按公司事业部统计
                if (statisticsStyle == "0")
                {
                    company = Convert.ToString(dyFilter["FCompany_Filter"]);
                }
                //统计方式 = 按具体组织和部门统计
                else if (statisticsStyle == "1")
                {
                    //组织
                    orgId = Convert.ToString(dyFilter["FMulSelOrgList_Filter"]);
                    //部门
                    depName = Convert.ToString(dyFilter["FDeptItems_Filter"]);
                }
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            return title;
        }
    }
}

using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Para.App.Report.DevliCostAnalysisReport
{
    [HotUpdate]
    [Description("出货成本分析表—服务端插件")]
    public class ServicesPlugIn : SysReportBaseService
    {
        //起始日期
        DateTime beginDate = DateTime.MinValue;
        //截止日期
        DateTime endDate = DateTime.MinValue;
        //事业部
        string busDep = string.Empty;
        //业务部
        string dep = string.Empty;
        //外销发票号
        string invoNo = string.Empty;
        //临时表
        string decTemp = string.Empty;
        //创建临时表存放走阳普生采购平台的外部的供应商
        string supTemp = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.ReportName = new LocaleValue("出货成本分析表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsUIDesignerColumns = true;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.SimpleAllCols = false;
        }
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.FilterParameter(filter);
            using (new SessionScope())
            {
                //创建临时表
                decTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#decTemp", this.CreateDecTemp());
                //创建临时表存放走阳普生采购平台的外部的供应商
                supTemp = DBUtils.CreateSessionTemplateTable(base.Context, "#supTemp", this.CreateSupTemp());
                //插入主数据
                this.InsertIntoDecTemp();
                //给从阳普生采购平台采购的物料行打上标识
                this.UpdatePurFlag();
                //更新临时表中的离岸公司客户和销售价
                this.UpdateOffshoreData();
                //更新没有通过阳普生采购平台采购的物料的成本
                this.UpdateCost1();
                //更新没有通过阳普生采购平台采购的物料的供应商
                this.UpdateSup1();
                //更新通过阳普生采购平台采购的物料的成本
                this.UpdateCost2();
                //插入走阳普生采购平台的外部的供应商
                this.InsertIntoSupTemp();
                //更新通过阳普生采购平台采购的物料的供应商
                this.UpdateSup2();

                base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "FOFFSHOREDATE DESC,FINVONO,FCUSTMATNO");
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("/*dialect*/	");
                sqlBuilder.AppendLine("	SELECT	");
                sqlBuilder.AppendLine("		" + base.KSQL_SEQ + "	--序号	");
                sqlBuilder.AppendLine("		,FINVONO				--外销发票号	");
                sqlBuilder.AppendLine("		,FORGNUM				--组织编码	");
                sqlBuilder.AppendLine("		,FORGNAME				--组织名称	");
                sqlBuilder.AppendLine("		,FCUSTNUM				--客户编码	");
                sqlBuilder.AppendLine("		,FCUST					--客户	");
                sqlBuilder.AppendLine("		,FDEP					--部门	");
                sqlBuilder.AppendLine("		,FOFFSHOREDATE			--离岸日期	");
                sqlBuilder.AppendLine("		,FCUSTMATNO				--客户货号	");
                sqlBuilder.AppendLine("		,FMATNUM				--物料编码	");
                sqlBuilder.AppendLine("		,FMATNAME				--物料名称	");
                sqlBuilder.AppendLine("		,FMATENNAME				--物料英文名	");
                sqlBuilder.AppendLine("		,FSALEPRICE				--销售价(USD)	");
                sqlBuilder.AppendLine("		,FCOSTPRICE				--成本价	");
                sqlBuilder.AppendLine("		,FREFCOST				--BOM成本价	");
                sqlBuilder.AppendLine("		,FSUP					--供应商	");
                sqlBuilder.AppendLine("		,FDELIVERYQTY			--出货数量	");
                sqlBuilder.AppendLine("		,FPIECEQTY				--装箱量	");
                sqlBuilder.AppendLine("		,FLENGTHUNIT			--长度单位	");
                sqlBuilder.AppendLine("		,FCARTONLENGTH			--外箱长	");
                sqlBuilder.AppendLine("		,FCARTONWIDTH			--外箱宽	");
                sqlBuilder.AppendLine("		,FCARTONHEIGHT			--外箱高	");
                sqlBuilder.AppendLine("		,FCARTONVOLM3			--外箱体积M3	");
                sqlBuilder.AppendLine("		,FGROSSWEIGHT_KG		--毛重(KG)	");
                sqlBuilder.AppendLine("		,FNETWEIGHT_KG			--净重(KG)	");
                sqlBuilder.AppendLine("	INTO " + tableName + "	");
                sqlBuilder.AppendLine("	FROM	");
                sqlBuilder.AppendLine("	" + decTemp + "	");
                sqlBuilder.AppendLine("	 TT	");
                DBUtils.ExecuteDynamicObject(this.Context, sqlBuilder.ToString());
                //删除临时表
                DBUtils.DropSessionTemplateTable(base.Context, decTemp);
                DBUtils.DropSessionTemplateTable(base.Context, supTemp);
            }
        }
        //创建临时表
        private string CreateDecTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FINVONO nvarchar(200) ");               //外销发票号
            sqlBuilder.AppendLine("	,FORGNUM nvarchar(200) ");              //组织编码
            sqlBuilder.AppendLine("	,FORGNAME nvarchar(200) ");             //组织名称
            sqlBuilder.AppendLine("	,FCUSTNUM nvarchar(1000) ");            //客户编码
            sqlBuilder.AppendLine("	,FCUST nvarchar(1000) ");               //客户名称
            sqlBuilder.AppendLine("	,FDEP nvarchar(200) ");                 //部门
            sqlBuilder.AppendLine("	,FOFFSHOREDATE datetime  ");            //离岸日期
            sqlBuilder.AppendLine("	,FCUSTMATNO nvarchar(200) ");           //客户货号
            sqlBuilder.AppendLine("	,FMATNUM nvarchar(200) ");              //物料编码
            sqlBuilder.AppendLine("	,FMATNAME nvarchar(200) ");             //物料名称
            sqlBuilder.AppendLine("	,FMATENNAME nvarchar(200) ");           //物料英文名
            sqlBuilder.AppendLine("	,FSALEPRICE decimal(23, 10)   ");       //销售价(USD)
            sqlBuilder.AppendLine("	,FCostPrice decimal(23, 10)   ");       //成本价
            sqlBuilder.AppendLine("	,FREFCOST decimal(23, 10)   ");         //BOM成本价
            sqlBuilder.AppendLine("	,FSup nvarchar(1000) ");                //供应商
            sqlBuilder.AppendLine("	,FDELIVERYQTY decimal(23, 10)   ");     //出货数量
            sqlBuilder.AppendLine("	,FPIECEQTY decimal(23, 10)   ");        //装箱量
            sqlBuilder.AppendLine("	,FLENGTHUNIT nvarchar(200) ");          //长度单位
            sqlBuilder.AppendLine("	,FCARTONLENGTH decimal(23, 10)   ");    //外箱长
            sqlBuilder.AppendLine("	,FCARTONWIDTH decimal(23, 10)   ");     //外箱宽
            sqlBuilder.AppendLine("	,FCARTONHEIGHT decimal(23, 10)   ");    //外箱高
            sqlBuilder.AppendLine("	,FCARTONVOLM3 decimal(23, 10)   ");     //外箱体积M3
            sqlBuilder.AppendLine("	,FGROSSWEIGHT_KG decimal(23, 10)   ");  //毛重(KG)
            sqlBuilder.AppendLine("	,FNETWEIGHT_KG decimal(23, 10)   ");    //净重(KG)
            sqlBuilder.AppendLine("	,FDOCENTRYID int	");			        //报关单ENTRYID
            sqlBuilder.AppendLine("	,FDELENTRYID int	");			        //发货通知单ENTRYID
            sqlBuilder.AppendLine("	,FSOENTRYID int	");			            //销售订单ENTRYID
            sqlBuilder.AppendLine("	,FISPURFROMEG int default 0	");			//当前行是否走阳普生采购平台：是 = 1，否 = 0
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //创建临时表存放走阳普生采购平台的外部的供应商
        private string CreateSupTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("(");
            sqlBuilder.AppendLine("	FDOCENTRYID int	");			        //报关单ENTRYID
            sqlBuilder.AppendLine("	,FSUP nvarchar(2000) ");             //供应商
            sqlBuilder.AppendLine(")");
            return sqlBuilder.ToString();
        }
        //插入主数据
        private void InsertIntoDecTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + decTemp + " (FINVONO,FORGNUM,FORGNAME,FCUSTNUM,FCUST,FDEP,FOFFSHOREDATE,FCUSTMATNO,FMATNUM,FMATNAME,FMATENNAME,FSALEPRICE,FREFCOST,FDELIVERYQTY,FPIECEQTY,FLENGTHUNIT,FCARTONLENGTH,FCARTONWIDTH,FCARTONHEIGHT,FCARTONVOLM3,FGROSSWEIGHT_KG,FNETWEIGHT_KG,FDOCENTRYID,FDELENTRYID,FSOENTRYID)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DOC.FBILLNO					FINVONO				--外销发票号	");
            sqlBuilder.AppendLine("		,ORG.FNUMBER				FORGNUM				--组织编码	");
            sqlBuilder.AppendLine("		,ORG_L.FNAME				FORGNAME			--组织	");
            sqlBuilder.AppendLine("		,CUST.FNUMBER				FCUSTNUM			--客户编码	");
            sqlBuilder.AppendLine("		,CUST_L.FNAME				FCUST				--客户	");
            sqlBuilder.AppendLine("		,DEP_L.FNAME				FDEP				--部门	");
            sqlBuilder.AppendLine("		,DOC.FOFFSHOREDATE			FOFFSHOREDATE		--离岸日期	");
            sqlBuilder.AppendLine("		,DOCENTRY.FCUSNUMBER		FCUSTMATNO			--客户货号	");
            sqlBuilder.AppendLine("		,MAT.FNUMBER				FMATNUM				--物料编码	");
            sqlBuilder.AppendLine("		,MAT_L.FNAME				FMATNAME			--物料名称	");
            sqlBuilder.AppendLine("		,MAT.FENNAME				FMATENNAME			--物料英文名	");
            sqlBuilder.AppendLine("		,DOCENTRY.FPRICE			FSALEPRICE			--销售价(USD)	");
            sqlBuilder.AppendLine("		,MATSTOCK.FREFCOST			FREFCOST			--BOM成本价	");
            sqlBuilder.AppendLine("		,DOCENTRY.FQTY				FDELIVERYQTY		--出货数量	");
            sqlBuilder.AppendLine("		,DELENTRY.FPACKINGQTY		FPIECEQTY			--装箱量	");
            sqlBuilder.AppendLine("		,UNIT_L.FNAME				FLENGTHUNIT			--长度单位	");
            sqlBuilder.AppendLine("		,DELENTRY.FCARTONLENGTH		FCARTONLENGTH		--外箱长	");
            sqlBuilder.AppendLine("		,DELENTRY.FCARTONWIDTH		FCARTONWIDTH		--外箱宽	");
            sqlBuilder.AppendLine("		,DELENTRY.FCARTONHEIGHT		FCARTONHEIGHT		--外箱高	");
            sqlBuilder.AppendLine("		,DELENTRY.FCARTONVOLM3		FCARTONVOLM3		--外箱体积M3	");
            sqlBuilder.AppendLine("		,DELENTRY.FGROSSWEIGHT_KG	FGROSSWEIGHT_KG		--毛重(KG)	");
            sqlBuilder.AppendLine("		,DELENTRY.FNETWEIGHT_KG		FNETWEIGHT_KG		--净重(KG)	");
            sqlBuilder.AppendLine("		,DOCENTRY.FENTRYID			FDOCENTRYID			--报关单ENTRYID	");
            sqlBuilder.AppendLine("		,DELENTRY.FENTRYID			FDELENTRYID			--发货通知单ENTRYID	");
            sqlBuilder.AppendLine("		,DOCENTRY.FSOENTRYID		FSOENTRYID			--销售订单ENTRYID	");
            sqlBuilder.AppendLine("	FROM	");
            sqlBuilder.AppendLine("	--报关单	");
            sqlBuilder.AppendLine("	TPT_FZH_DECALREDOC DOC	");
            sqlBuilder.AppendLine("	--报关单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN TPT_FZH_DECGOODSENTRY DOCENTRY	");
            sqlBuilder.AppendLine("	ON DOCENTRY.FID = DOC.FID	");
            sqlBuilder.AppendLine("	--发货通知单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_DELIVERYNOTICEENTRY DELENTRY	");
            sqlBuilder.AppendLine("	ON DOCENTRY.FSRCENTRYID = DELENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = DOC.FORGID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS_L ORG_L	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = ORG_L.FORGID AND ORG_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = DOC.FCUSID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--部门	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_DEPARTMENT_L DEP_L	");
            sqlBuilder.AppendLine("	ON DEP_L.FDEPTID = DOC.FSALEDEPTID AND DEP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--物料	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL MAT	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = DOCENTRY.FGOODSID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIAL_L MAT_L	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = MAT_L.FMATERIALID AND MAT_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	--物料.库存	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_MATERIALSTOCK MATSTOCK	");
            sqlBuilder.AppendLine("	ON MAT.FMATERIALID = MATSTOCK.FMATERIALID	");
            sqlBuilder.AppendLine("	--单位	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_UNIT_L UNIT_L	");
            sqlBuilder.AppendLine("	ON UNIT_L.FUNITID = DELENTRY.FLENGTHUNIT AND UNIT_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	WHERE DOC.FDOCUMENTSTATUS = 'C' AND DOC.FISOFFSHORE = '1'	");
            sqlBuilder.AppendLine("	AND DATEDIFF(DAY, '" + beginDate + "', DOC.FOFFSHOREDATE) >= 0 AND DATEDIFF(DAY, DOC.FOFFSHOREDATE, '" + endDate + "') >= 0	");
            //事业部
            if (!busDep.IsNullOrEmptyOrWhiteSpace() && busDep != "全部")
            {
                sqlBuilder.AppendLine("	AND DEP_L.FFULLNAME LIKE '%" + busDep + "%'	");
            }
            //业务部
            if (!dep.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DEP_L.FNAME IN (" + dep + ")	");
            }
            //外销发票号
            if (!invoNo.IsNullOrEmptyOrWhiteSpace())
            {
                sqlBuilder.AppendLine("	AND DOC.FBILLNO LIKE '%" + invoNo + "%'	");
            }
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //给从阳普生采购平台采购的物料行打上标识
        private void UpdatePurFlag()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET DECTEMP.FISPURFROMEG = 1	");
            sqlBuilder.AppendLine("	FROM ##decTemp DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID		FDOCENTRYID		--报关单ENTRYID	");
            sqlBuilder.AppendLine("	FROM ##decTemp DECTEMP	");
            sqlBuilder.AppendLine("	--采购订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FDEMANDBILLENTRYID = DECTEMP.FSOENTRYID	");
            sqlBuilder.AppendLine("	--采购订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDER PO	");
            sqlBuilder.AppendLine("	ON PO.FID = POENTRY_R.FID	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER SUP	");
            sqlBuilder.AppendLine("	ON SUP.FSUPPLIERID = PO.FSUPPLIERID	");
            sqlBuilder.AppendLine("	WHERE SUP.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) TEMP	");
            sqlBuilder.AppendLine("	WHERE TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新临时表中的离岸公司客户和销售价
        private void UpdateOffshoreData()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET	DECTEMP.FCUSTNUM = LA_TEMP.FCUSTNUM	");
            sqlBuilder.AppendLine("		,DECTEMP.FCUST = LA_TEMP.FCUST	");
            sqlBuilder.AppendLine("		,DECTEMP.FSALEPRICE = LA_TEMP.FSALEPRICE	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID					FDOCENTRYID	");
            sqlBuilder.AppendLine("		,MAX(CUST.FNUMBER)					FCUSTNUM	");
            sqlBuilder.AppendLine("		,MAX(CUST_L.FNAME)					FCUST	");
            sqlBuilder.AppendLine("		,MAX(LA_SOENTRY_F.FTAXPRICE)		FSALEPRICE	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--高山销售订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERENTRY PM_SOENTRY	");
            sqlBuilder.AppendLine("	ON PM_SOENTRY.FENTRYID = DECTEMP.FSOENTRYID	");
            sqlBuilder.AppendLine("	--离岸采购订单.明细_关联信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R LA_POENTRY_R	");
            sqlBuilder.AppendLine("	ON LA_POENTRY_R.FENTRYID = PM_SOENTRY.FPOENTRYID	");
            sqlBuilder.AppendLine("	--离岸销售订单.明细_财务信息	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERENTRY_F LA_SOENTRY_F	");
            sqlBuilder.AppendLine("	ON LA_POENTRY_R.FDEMANDBILLENTRYID = LA_SOENTRY_F.FENTRYID	");
            sqlBuilder.AppendLine("	--离岸销售订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDER LA_SO	");
            sqlBuilder.AppendLine("	ON LA_SO.FID = LA_SOENTRY_F.FID	");
            sqlBuilder.AppendLine("	--客户	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER CUST	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = LA_SO.FCUSTID	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_CUSTOMER_L CUST_L	");
            sqlBuilder.AppendLine("	ON CUST.FCUSTID = CUST_L.FCUSTID AND CUST_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	WHERE DECTEMP.FCUSTNUM IN ('PG','IG') AND DECTEMP.FORGNUM = 'PM'	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) LA_TEMP	");
            sqlBuilder.AppendLine("	WHERE LA_TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新没有通过阳普生采购平台采购的物料的成本
        private void UpdateCost1()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET DECTEMP.FCOSTPRICE = ROUND(TEMP.FCOSTPRICE,2)	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID												--报关单EntryId	");
            sqlBuilder.AppendLine("		,AVG(ISNULL(OUTSTOCKENTRY_F.FCOSTPRICE,0))*1.13	FCOSTPRICE	    --成本价（本位币）	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--发货通知单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_DELIVERYNOTICEENTRY DELENTRY	");
            sqlBuilder.AppendLine("	ON DELENTRY.FENTRYID = DECTEMP.FDELENTRYID	");
            sqlBuilder.AppendLine("	--销售出库单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY OUTSTOCKENTRY	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY.FSOURCEENTRYID = DELENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_关联	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_R OUTSTOCKENTRY_R	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_R.FENTRYID = OUTSTOCKENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_F.FENTRYID = OUTSTOCKENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	WHERE DECTEMP.FISPURFROMEG = 0 AND OUTSTOCKENTRY_R.FSRCTYPE = 'SAL_DELIVERYNOTICE'	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) TEMP	");
            sqlBuilder.AppendLine("	WHERE TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	AND DECTEMP.FISPURFROMEG = 0	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新没有通过阳普生采购平台采购的物料的供应商
        private void UpdateSup1()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	--更新没有通过阳普生采购平台采购的物料的供应商	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET DECTEMP.FSUP = TEMP.FSUP	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID		FDOCENTRYID		--报关单ENTRYID	");
            sqlBuilder.AppendLine("		,MAX(SUP_L.FNAME)		FSUP			--供应商	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--采购订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FDEMANDBILLENTRYID = DECTEMP.FSOENTRYID	");
            sqlBuilder.AppendLine("	--采购入库单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKENTRY INSTOCKENTRY	");
            sqlBuilder.AppendLine("	ON INSTOCKENTRY.FPOORDERENTRYID = POENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("	--采购入库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCK INSTOCK	");
            sqlBuilder.AppendLine("	ON INSTOCK.FID = INSTOCKENTRY.FID	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP_L.FSUPPLIERID = INSTOCK.FSUPPLIERID AND SUP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	WHERE DECTEMP.FISPURFROMEG = 0	");
            sqlBuilder.AppendLine("	AND (INSTOCK.FOUTINVOICENO_H = '' OR INSTOCK.FOUTINVOICENO_H = DECTEMP.FINVONO) 	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) TEMP	");
            sqlBuilder.AppendLine("	WHERE TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	AND DECTEMP.FISPURFROMEG = 0	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新通过阳普生采购平台采购的物料的成本
        private void UpdateCost2()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET DECTEMP.FCOSTPRICE = ROUND(TEMP.FCOSTPRICE,2)	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID												--报关单EntryId	");
            sqlBuilder.AppendLine("		,AVG(ISNULL(OUTSTOCKENTRY_F.FCOSTPRICE,0))*1.13     FCOSTPRICE	--成本价	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--采购订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FDEMANDBILLENTRYID = DECTEMP.FSOENTRYID	");
            sqlBuilder.AppendLine("	--阳普生销售订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERENTRY EG_SOENTRY	");
            sqlBuilder.AppendLine("	ON EG_SOENTRY.FPOENTRYID = POENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("	--阳普生销售订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDER EG_SO	");
            sqlBuilder.AppendLine("	ON EG_SO.FID = EG_SOENTRY.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = EG_SO.FSALEORGID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_关联	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_R OUTSTOCKENTRY_R	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_R.FSOENTRYID = EG_SOENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--销售出库单.明细_财务	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCKENTRY_F OUTSTOCKENTRY_F	");
            sqlBuilder.AppendLine("	ON OUTSTOCKENTRY_F.FENTRYID = OUTSTOCKENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("	--销售出库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_OUTSTOCK OUTSTOCK	");
            sqlBuilder.AppendLine("	ON OUTSTOCK.FID = OUTSTOCKENTRY_F.FID	");
            sqlBuilder.AppendLine("	WHERE DECTEMP.FISPURFROMEG = 1 AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("	AND OUTSTOCKENTRY_R.FSRCTYPE = 'SAL_SaleOrder'	");
            sqlBuilder.AppendLine("	AND OUTSTOCK.FOUTINVOICENO = DECTEMP.FINVONO	");
            sqlBuilder.AppendLine("	GROUP BY DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) TEMP	");
            sqlBuilder.AppendLine("	WHERE TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	AND DECTEMP.FISPURFROMEG = 1	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //插入走阳普生采购平台的外部的供应商
        private void InsertIntoSupTemp()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	INSERT INTO " + supTemp + " (FDOCENTRYID,FSUP)	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		DECTEMP.FDOCENTRYID				--报关单EntryId	");
            sqlBuilder.AppendLine("		,SUP_L.FNAME			FSUP	--供应商	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	--采购订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R POENTRY_R	");
            sqlBuilder.AppendLine("	ON POENTRY_R.FDEMANDBILLENTRYID = DECTEMP.FSOENTRYID	");
            sqlBuilder.AppendLine("	--阳普生销售订单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDERENTRY EG_SOENTRY	");
            sqlBuilder.AppendLine("	ON EG_SOENTRY.FPOENTRYID = POENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("	--阳普生销售订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_SAL_ORDER EG_SO	");
            sqlBuilder.AppendLine("	ON EG_SO.FID = EG_SOENTRY.FID	");
            sqlBuilder.AppendLine("	--组织	");
            sqlBuilder.AppendLine("	LEFT JOIN T_ORG_ORGANIZATIONS ORG	");
            sqlBuilder.AppendLine("	ON ORG.FORGID = EG_SO.FSALEORGID	");
            sqlBuilder.AppendLine("	--阳普生采购订单.明细_关联	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDERENTRY_R EG_POENTRY_R	");
            sqlBuilder.AppendLine("	ON EG_POENTRY_R.FDEMANDBILLENTRYID = EG_SOENTRY.FENTRYID	");
            sqlBuilder.AppendLine("	--阳普生采购订单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_PUR_POORDER EG_PO	");
            sqlBuilder.AppendLine("	ON EG_POENTRY_R.FID = EG_PO.FID	");
            sqlBuilder.AppendLine("	--采购入库单.明细	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCKENTRY EG_INSTOCKENTRY	");
            sqlBuilder.AppendLine("	ON EG_INSTOCKENTRY.FPOORDERENTRYID = EG_POENTRY_R.FENTRYID	");
            sqlBuilder.AppendLine("	--采购入库单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_STK_INSTOCK EG_INSTOCK	");
            sqlBuilder.AppendLine("	ON EG_INSTOCK.FID = EG_INSTOCKENTRY.FID	");
            sqlBuilder.AppendLine("	--供应商	");
            sqlBuilder.AppendLine("	LEFT JOIN T_BD_SUPPLIER_L SUP_L	");
            sqlBuilder.AppendLine("	ON SUP_L.FSUPPLIERID = EG_INSTOCK.FSUPPLIERID AND SUP_L.FLOCALEID = 2052	");
            sqlBuilder.AppendLine("	WHERE DECTEMP.FISPURFROMEG = 1 AND ORG.FNUMBER = 'EG'	");
            sqlBuilder.AppendLine("	AND FISINVOMSGCANCLE = '0'      --开票信息作废 = false	");
            sqlBuilder.AppendLine("	AND EG_PO.FISCANCEL = '0'       --是否作废 = false	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
        }
        //更新通过阳普生采购平台采购的物料的供应商
        private void UpdateSup2()
        {
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	UPDATE DECTEMP	");
            sqlBuilder.AppendLine("	SET DECTEMP.FSUP = TEMP.FSUP	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " DECTEMP	");
            sqlBuilder.AppendLine("	,(	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("		T.FDOCENTRYID						--报关单EntryId	");
            sqlBuilder.AppendLine("		,FSUP = stuff((select distinct '，'+FSUP from " + supTemp + " WHERE FDOCENTRYID = T.FDOCENTRYID FOR XML PATH('')), 1, 1, '')	");
            sqlBuilder.AppendLine("	FROM " + decTemp + " T	");
            sqlBuilder.AppendLine("	GROUP BY T.FDOCENTRYID	");
            sqlBuilder.AppendLine("	) TEMP	");
            sqlBuilder.AppendLine("	WHERE TEMP.FDOCENTRYID = DECTEMP.FDOCENTRYID	");
            sqlBuilder.AppendLine("	AND DECTEMP.FISPURFROMEG = 1	");
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
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
                //事业部
                busDep = Convert.ToString(dyFilter["FBusDep_Filter"]);
                //业务部
                dep = Convert.ToString(dyFilter["FDep_Filter"]);
                //外销发票号
                invoNo = Convert.ToString(dyFilter["FInvoNo_Filter"]);
            }
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles title = new ReportTitles();
            //起始日期
            title.AddTitle("FBeginDate_H", beginDate.ToShortDateString());
            //截止日期
            title.AddTitle("FEndDate_H", endDate.ToShortDateString());
            //事业部
            title.AddTitle("FBusDep_H", busDep);
            return title;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Util;

namespace Para.App.Report.STK_StockDetailRpt
{
    [HotUpdate]
    [Description("物料收发明细表(扩展标准产品报表) - 服务器插件")]
    public class StockDetailRpt : Kingdee.K3.SCM.App.Stock.Report.StockDetailRpt
    {
        string[] temps;
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //创建临时表
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
            temps = dbService.CreateTemporaryTableName(this.Context, 1);
            string temp = temps[0];
            //调用基类方法，获取初步查询结果到临时表
            base.BuilderReportSqlAndTempTable(filter, temp);
            //对标准报表所查询的数据进行加工
            string sql = string.Format(@"/*dialect*/
                                        SELECT 
	                                        T1.*
	                                        ,T2.FOLDNUMBER	                    --旧物料编码
	                                        ,T2.FCUSTPRODUCTNO  FCUSTMATNO	    --客户货号
	                                        ,T2.FQYPACKINGQTY   FPACKINGQTY   	--装箱量
                                            ,CASE
                                                WHEN T2.FQYPACKINGQTY = 0 THEN 0
                                                ELSE CEILING(T1.FSTOCKJCQTY / T2.FQYPACKINGQTY)
                                            END                 FJCCARTONQTY    --结存箱数 = 结存数量(库存) / 装箱量
                                        INTO {0}
                                        FROM {1} T1
                                        LEFT JOIN T_BD_MATERIAL T2
                                        ON T1.FMATERIALID = T2.FMATERIALID
                                        ", tableName, temp);
            DBUtils.Execute(this.Context, sql);
        }
        public override void CloseReport()
        {
            base.CloseReport();
            //删除临时表
            if (temps.IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }
            else
            {
                IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
                dbService.DeleteTemporaryTableName(this.Context, temps);
            }
        }
        protected override DataTable GetReportData(string tablename, Kingdee.BOS.Core.Report.IRptParams filter)
        {
            DataTable dtPageData = base.GetReportData(tablename, filter);
            for (int i = 0; i < dtPageData.Rows.Count; i++)
            {
                //结存数量
                decimal JCQty = Convert.ToDecimal(dtPageData.Rows[i]["FSTOCKJCQTY"]);
                //装箱量
                decimal packingQty = Convert.ToDecimal(dtPageData.Rows[i]["FPACKINGQTY"]);
                //结存箱数
                dtPageData.Rows[i]["FJCCARTONQTY"] = packingQty == 0M ? 0M : Math.Ceiling(JCQty / packingQty);
            }
            return dtPageData;
        }
    }
}

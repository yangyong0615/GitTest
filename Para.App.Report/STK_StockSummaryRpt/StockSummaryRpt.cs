using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Util;

namespace Para.App.Report.STK_StockSummaryRpt
{
    [HotUpdate]
    [Description("物料收发汇总表(扩展标准产品报表) - 服务器插件")]
    public class StockSummaryRpt : Kingdee.K3.SCM.App.Stock.Report.StockSummaryRpt
    {
        string[] customRptTableNames;
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //创建临时表
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
            customRptTableNames = dbService.CreateTemporaryTableName(this.Context, 1);
            string temp = customRptTableNames[0];
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
            if (customRptTableNames.IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }
            else
            {
                IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
                dbService.DeleteTemporaryTableName(this.Context, customRptTableNames);
            }
        }
    }
}

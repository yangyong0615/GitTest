using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Util;

namespace Para.App.Report.HS_INOUTSTOCKDETAILRPT
{
    [HotUpdate]
    [Description("存货收发存明细表(扩展标准产品报表) - 服务器插件")]
    public class InOutStockDetailService : Kingdee.K3.FIN.HS.App.Report.InOutStockDetailService
    {
//        string[] temps;
//        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
//        {
//            //创建临时表
//            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
//            temps = dbService.CreateTemporaryTableName(this.Context, 1);
//            string temp = temps[0];
//            //调用基类方法，获取初步查询结果到临时表
//            base.BuilderReportSqlAndTempTable(filter, temp);
//            //对标准报表所查询的数据进行加工
//            string sql = string.Format(@"/*dialect*/
//                                        SELECT 
//	                                        T1.*	                                        
//	                                        ,T2.FCUSTPRODUCTNO  FCUSTMATNO	    --客户货号
//                                        INTO {0}
//                                        FROM {1} T1
//                                        LEFT JOIN T_BD_MATERIAL T2
//                                        ON T1.FMATERIALBASEID = T2.FMATERIALID
//                                        ", tableName, temp);
//            DBUtils.Execute(this.Context, sql);
//        }
//        public override void CloseReport()
//        {
//            base.CloseReport();
//            //删除临时表
//            if (temps.IsNullOrEmptyOrWhiteSpace())
//            {
//                return;
//            }
//            else
//            {
//                IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
//                dbService.DeleteTemporaryTableName(this.Context, temps);
//            }
//        }
    }
}

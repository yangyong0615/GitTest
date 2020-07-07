using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;

namespace Para.App.Report.AP_MaturedDebt
{
    [HotUpdate]
    [Description("到期债务表(扩展标准产品报表) - 服务器插件")]
    public class ServicesPlugIn : Kingdee.K3.FIN.AP.App.Report.MaturedDebtService
    {
        string[] temps;
        //开始日期（审核）
        DateTime beginAuditDate = DateTime.MinValue;
        //结束日期（审核）
        DateTime endAuditDate = DateTime.MinValue;
        public override Kingdee.BOS.Core.Report.ReportHeader GetReportHeaders(Kingdee.BOS.Core.Report.IRptParams filter)
        {

            ReportHeader header = base.GetReportHeaders(filter);
            header.AddChild("FAuditDate", new LocaleValue("审核日期"), SqlStorageType.SqlSmalldatetime, true);
            return header;
        }
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //创建临时表
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<IDBService>();
            temps = dbService.CreateTemporaryTableName(this.Context, 1);
            string temp = temps[0];
            //调用基类方法，获取初步查询结果到临时表
            base.BuilderReportSqlAndTempTable(filter, temp);
            //对标准报表所查询的数据进行加工
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("/*dialect*/	");
            sqlBuilder.AppendLine("	SELECT	");
            sqlBuilder.AppendLine("	    T1.*	");
            sqlBuilder.AppendLine("	    ,CASE	");
            sqlBuilder.AppendLine("	        WHEN T1.FFORMID = 'AP_Payable' THEN T2.FAPPROVEDATE	");
            sqlBuilder.AppendLine("	        WHEN T1.FFORMID = 'AP_OtherPayable' THEN T3.FAPPROVEDATE	");
            sqlBuilder.AppendLine("	        ELSE NULL	");
            sqlBuilder.AppendLine("	    END                 FAUDITDATE    --审核日期	");
            sqlBuilder.AppendLine("	INTO " + tableName + "	");
            sqlBuilder.AppendLine("	FROM " + temp + " T1	");
            sqlBuilder.AppendLine("	--应付单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_AP_PAYABLE T2	");
            sqlBuilder.AppendLine("	ON T2.FID = T1.FID	");
            sqlBuilder.AppendLine("	--其他应付单	");
            sqlBuilder.AppendLine("	LEFT JOIN T_AP_OTHERPAYABLE T3	");
            sqlBuilder.AppendLine("	ON T3.FID = T1.FID	");
            sqlBuilder.AppendLine("	WHERE 1 = 1	");
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (filter.FilterParameter.CustomFilter != null)
            {
                //起始日期
                beginAuditDate = Convert.ToDateTime(dyFilter["FBeginAuditDate_Filter"]);
                //截止日期
                endAuditDate = Convert.ToDateTime(dyFilter["FEndAuditDate_Filter"]);
                if (beginAuditDate != DateTime.MinValue && endAuditDate != DateTime.MinValue)
                {
                    sqlBuilder.AppendLine("	AND CASE WHEN T1.FFORMID = 'AP_Payable' AND DATEDIFF(DAY, '" + beginAuditDate + "', T2.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY,  T2.FAPPROVEDATE, '" + endAuditDate + "') >= 0 THEN 1	");
                    sqlBuilder.AppendLine("	WHEN T1.FFORMID = 'AP_OtherPayable' AND DATEDIFF(DAY, '" + beginAuditDate + "', T3.FAPPROVEDATE) >= 0 AND DATEDIFF(DAY,  T3.FAPPROVEDATE, '" + endAuditDate + "') >= 0 THEN 1 ELSE 0 END = 1	");
                }
                if (beginAuditDate != DateTime.MinValue && endAuditDate == DateTime.MinValue)
                {
                    sqlBuilder.AppendLine("	AND CASE WHEN T1.FFORMID = 'AP_Payable' AND DATEDIFF(DAY, '" + beginAuditDate + "', T2.FAPPROVEDATE) >= 0 THEN 1	");
                    sqlBuilder.AppendLine("	WHEN T1.FFORMID = 'AP_OtherPayable' AND DATEDIFF(DAY, '" + beginAuditDate + "', T3.FAPPROVEDATE) >= 0 THEN 1 ELSE 0 END = 1	");
                }
                if (beginAuditDate == DateTime.MinValue && endAuditDate != DateTime.MinValue)
                {
                    sqlBuilder.AppendLine("	AND CASE WHEN T1.FFORMID = 'AP_Payable' AND DATEDIFF(DAY,  T2.FAPPROVEDATE, '" + endAuditDate + "') >= 0 THEN 1	");
                    sqlBuilder.AppendLine("	WHEN T1.FFORMID = 'AP_OtherPayable' AND DATEDIFF(DAY,  T3.FAPPROVEDATE, '" + endAuditDate + "') >= 0 THEN 1 ELSE 0 END = 1	");
                }
            }
            DBUtils.Execute(this.Context, sqlBuilder.ToString());
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
    }
}

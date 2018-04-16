using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmRecorder
{
    class MyDbLib
    {
        private static void LogError(SqlException ex)
        {
            if (ex != null)
                Win32Helper.LogHelper.AppendToLogfile("SQL-error.txt", ex.ToString());
        }

        public static OmmcMesDataSet.AlarmRecorderListDataTable AlarmRecorderList(string PC_ID)
        {
            try
            {
                using (var ta = new OmmcMesDataSetTableAdapters.AlarmRecorderListTableAdapter())
                {
                    return ta.GetData(PC_ID);
                }
            }
            catch (SqlException ex)
            {
                LogError(ex);
                return null;
            }
        }

        public static OmmcMesDataSet.PLC_TAG_LOOKUP_PLC_IPRow PLC_TAG_LOOKUP_PLC_IP(string WW_ITEM_NAME)
        {
            try
            {
                using (var ta = new OmmcMesDataSetTableAdapters.PLC_TAG_LOOKUP_PLC_IPTableAdapter())
                {
                    var dt = ta.GetData(WW_ITEM_NAME);
                    if (dt != null && dt.Rows.Count > 0)
                        return dt[0];
                }
            }
            catch (SqlException ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static OmmcMesDataSet.PLC_TAG_LOOKUP_WW_ITEM_NAMERow PLC_TAG_LOOKUP_WW_ITEM_NAME(string PLC_IP, string TAG_ID)
        {
            try
            {
                using (var ta = new OmmcMesDataSetTableAdapters.PLC_TAG_LOOKUP_WW_ITEM_NAMETableAdapter())
                {
                    var dt = ta.GetData(PLC_IP, TAG_ID);
                    if (dt != null && dt.Rows.Count > 0)
                        return dt[0];
                }
            }
            catch (SqlException ex)
            {
                LogError(ex);
            }
            return null;
        }

    }
}

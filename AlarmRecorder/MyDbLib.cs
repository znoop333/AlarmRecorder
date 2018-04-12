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

    }
}

using AlarmRecorder.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Win32Helper;

namespace AlarmRecorder
{
    public partial class Form1 : Form
    {
        private string PC_ID;
        RMQManager rmq;
        private SynchronizationContext _syncContext = null;
        OmmcMesDataSet.AlarmRecorderListDataTable AlarmList;
        int LastCorrelationId = 0;
        string WWPrefix = "DDESuite_CLX02.MTqStopper02.";

        private BindingSource SBind;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _syncContext = WindowsFormsSynchronizationContext.Current;
            PC_ID = Settings.Default.PC_ID;

            Directory.CreateDirectory(Settings.Default.LOG_PATH);

            rmq = new RMQManager();
            rmq.HostName = Settings.Default.RMQHostName;
            rmq.VirtualHost = Settings.Default.RMQVirtualHost;
            rmq.UserName = Settings.Default.RMQUserName;
            rmq.Password = Settings.Default.RMQPassword;
            rmq.Port = Settings.Default.RMQPort;
            rmq.InboundExchangeName = Settings.Default.RMQInboundExchangeName;
            rmq.OutboundExchangeName = Settings.Default.RMQOutboundExchangeName;
            rmq.PC_ID = Settings.Default.PC_ID;


            rmq.MessageArrived += Rmq_MessageArrived;
            rmq.LogMessage += Rmq_LogMessage;

            bool connected = await rmq.Connect();
            if (connected)
            {
                if (!rmq.Subscribe())
                    LogToGUI("Failed to Subscribe!");
            }
            else
                LogToGUI("Failed to Connect!");

            AlarmList = MyDbLib.AlarmRecorderList(PC_ID);
            SubscribeAlarms();
            RereadTags();

            SetupDataGridView();
            RefreshData();
        }

        private void SetupDataGridView()
        {
            SBind = new BindingSource();
            SBind.DataSource = AlarmList;

            this.dataGridViewAlarms.AutoGenerateColumns = false;
            this.dataGridViewAlarms.DataSource = SBind;

            var c = new DataGridViewTextBoxColumn()
            {
                Name = "TAG_ID",
                HeaderText = "Tag",
                DataPropertyName = "TAG_ID",
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            dataGridViewAlarms.Columns.Add(c);

            c = new DataGridViewTextBoxColumn()
            {
                Name = "DESCRIPTION",
                HeaderText = "Descr.",
                DataPropertyName = "DESCRIPTION",
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            dataGridViewAlarms.Columns.Add(c);

            c = new DataGridViewTextBoxColumn()
            {
                Name = "Value",
                HeaderText = "Value",
                DataPropertyName = "Value",
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            dataGridViewAlarms.Columns.Add(c);

        }
        
        void RefreshData()
        {
            this.dataGridViewAlarms.Refresh();
        }


        private void SubscribeAlarms()
        {
            if (AlarmList == null || AlarmList.Rows.Count == 0)
                return;

            var pi = new ProgramInfo();

            foreach (var dr in AlarmList)
            {
                var m = new RMQWonderwareAdapter.RmqCommandMessage();
                m.Command = "SUBSCRIBE";
                m.RequesterName = "AlarmRecorder";
                m.RequesterIP = pi.IP;
                m.TagName = this.WWPrefix + dr.TAG_ID;
                m.Once = false;
                m.CorrelationId = Guid.NewGuid().ToString();

                LogToGUI(String.Format("Subscribing to " + m.TagName + ", CorrelationId:" + m.CorrelationId));

                rmq.PutMessage(PC_ID, JsonConvert.SerializeObject(m), m.CorrelationId);
            }


        }

        private void LogToGUI(string s)
        {
            if (textBoxLog.Created && !textBoxLog.IsDisposed)
                LogHelper.AppendToTextbox(textBoxLog, s);

            LogHelper.AppendToLogfile(FilenameForLog("main"), s);
        }

        public static string FilenameForLog(string filename)
        {
            return String.Format("{0}\\{1}\\{2}-{3}.log", Application.StartupPath, Settings.Default.LOG_PATH, filename, DateTime.Now.ToString("yyyyMMdd"));
        }

        private void Rmq_LogMessage(object sender, string e)
        {
            _syncContext.Post(
                        delegate
                        {
                            Console.WriteLine(e);
                            LogToGUI(e);
                        }, null);
        }

        private void Rmq_MessageArrived(object sender, RMQManager.EventArgsMessageArrived e)
        {
            _syncContext.Post(
                        delegate
                        {
                            Console.WriteLine(e.ToString());
                            HandleMessage(sender, e);
                        }, null);
        }

        private void HandleMessage(object sender, RMQManager.EventArgsMessageArrived e)
        {
            string s;
            RMQWonderwareAdapter.RmqResponseMessage ParsedMessage;
            try
            {
                ParsedMessage = JsonConvert.DeserializeObject<RMQWonderwareAdapter.RmqResponseMessage>(e.OriginalMessageString);
            }
            catch (JsonReaderException jre)
            {
                s = "JSON parsing error! " + jre.ToString();
                LogToGUI(s);
                Win32Helper.LogHelper.AppendToLogfile(FilenameForLog("JSON"), s);
                return;
            }

            var cnt = AlarmList.Count(dr => WWPrefix + dr.TAG_ID == ParsedMessage.TagName );
            if(cnt == 0)
            {
                LogToGUI("Ignoring event for " + ParsedMessage.TagName + " with value " + ParsedMessage.Value);
                return;
            }

            AlarmList.Where(dr => WWPrefix + dr.TAG_ID == ParsedMessage.TagName).ToList().ForEach( dr => dr.Value = ParsedMessage.Value == null ? "" : ParsedMessage.Value == "True" ? "1" : "0" );
            this.RefreshData();

            s = "Received JSON: " + e.OriginalMessageString;
            //LogToGUI(s);
            Win32Helper.LogHelper.AppendToLogfile(FilenameForLog("JSON"), s);

            switch (ParsedMessage.Command)
            {
                case "DataChange":
                    LogToGUI("DataChange occurred on tag " + ParsedMessage.TagName + " with value " + ParsedMessage.Value);

                    break;
                case "LastValue":
                    LogToGUI("LastValue on tag " + ParsedMessage.TagName + " with value " + ParsedMessage.Value);

                    break;
                default:
                    LogToGUI("Unknown command " + ParsedMessage.Command);
                    break;
            }

            LogHelper.FlushTextbox(textBoxLog);

        }

        private void timerFlushTextboxes_Tick(object sender, EventArgs e)
        {
            LogHelper.FlushTextbox(textBoxLog);
        }

        private void timerCloseLogFiles_Tick(object sender, EventArgs e)
        {
            LogHelper.CloseLogFiles();
        }

        private void timerFlushLogs_Tick(object sender, EventArgs e)
        {
            LogHelper.FlushLogFiles();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var fn = FilenameForLog("main");
            LogHelper.AppendToLogfile(fn, "Form1_FormClosing");
            LogHelper.FlushLogFiles();
            LogHelper.CloseLogFiles();

            Console.WriteLine("Before Disconnect ");

            //await Task.Delay(5000);
            //Environment.Exit(0);
            //Environment.FailFast("");

            // is channel.BasicCancel() causing a hang here? see https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/341
            //await rmq.Unsubscribe();
            await rmq.Disconnect();

            // this line is never reached!
            Console.WriteLine("After Disconnect ");

            LogHelper.AppendToLogfile(fn, "RMQ exited");

            LogHelper.FlushLogFiles();
            LogHelper.CloseLogFiles();
        }

        private void dataGridViewAlarms_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            int r = e.RowIndex, c = e.ColumnIndex;

            // don't try to format the dgv until all of the columns are loaded
            if (dataGridViewAlarms.Rows[r].Cells.Count < 3)
                return;

            string TAG_ID = dataGridViewAlarms.Rows[r].Cells["TAG_ID"].Value as string;
            string Value = dataGridViewAlarms.Rows[r].Cells["Value"].Value as string;
            var ColumnName = this.dataGridViewAlarms.Columns[e.ColumnIndex].Name;

            switch (ColumnName)
            {
                case "TAG_ID":
                case "Value":
                    if (Value == "0")
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.BackColor = Color.Blue;
                    }
                    else if (Value == "1")
                    {
                        e.CellStyle.ForeColor = Color.Yellow;
                        e.CellStyle.BackColor = Color.Red;
                    }
                    break;
            }


        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();

        }

        private void RereadTags()
        {
            var pi = new ProgramInfo();

            // request the latest value, just in case they were already advised once
            foreach (var dr in AlarmList)
            {
                var m = new RMQWonderwareAdapter.RmqCommandMessage();
                m.Command = "READ";
                m.RequesterName = "AlarmRecorder";
                m.RequesterIP = pi.IP;
                m.TagName = this.WWPrefix + dr.TAG_ID;
                m.Once = false;
                m.CorrelationId = Guid.NewGuid().ToString();

                LogToGUI(String.Format("Reading " + m.TagName + ", CorrelationId:" + m.CorrelationId));

                rmq.PutMessage(PC_ID, JsonConvert.SerializeObject(m), m.CorrelationId);
            }
        }

        private void reReadTagsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RereadTags();
        }
    }
}

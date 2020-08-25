using System;
using System.Management;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace MonitorSwitch
{
    public partial class MonitorSwitch : Form
    {
        String targetUsbHubDeviceID = @"USB\VID_1A40&PID_0101";
        Dictionary<String, Boolean> monitorConfig = new Dictionary<String, Boolean>();
        ManagementEventWatcher insertWatcher = null;
        ManagementEventWatcher removeWatcher = null;

        public MonitorSwitch()
        {
            InitializeComponent();
            targetUsbHubDeviceID = ConfigurationManager.AppSettings.Get("UsbHubDeviceID");
            monitorConfig.Add(@"\\.\DISPLAY1", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display1Enabled")));
            monitorConfig.Add(@"\\.\DISPLAY2", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display2Enabled")));
            monitorConfig.Add(@"\\.\DISPLAY3", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display3Enabled")));
            monitorConfig.Add(@"\\.\DISPLAY4", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display4Enabled")));
            monitorConfig.Add(@"\\.\DISPLAY5", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display5Enabled")));
            monitorConfig.Add(@"\\.\DISPLAY6", Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Display6Enabled")));
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            String deviceID = (String)instance.Properties["DeviceID"].Value;
            if (deviceID.Contains(targetUsbHubDeviceID))
            {
                WinAPI.SetDisplayMonitors(true, monitorConfig);
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            String deviceID = (String)instance.Properties["DeviceID"].Value;
            if (deviceID.Contains(targetUsbHubDeviceID))
            {
                WinAPI.SetDisplayMonitors(false, monitorConfig);
            }
        }

        private void MonitorSwitch_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                this.notifyIcon1.Visible = true;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Start();
        }

        private void Start()
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");

            this.insertWatcher = new ManagementEventWatcher(insertQuery);
            this.insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            this.insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            this.removeWatcher = new ManagementEventWatcher(removeQuery);
            this.removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            this.removeWatcher.Start();

            this.btnStop.Enabled = true;
            this.btnStart.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.insertWatcher.Stop();
            this.removeWatcher.Stop();

            this.btnStart.Enabled = true;
            this.btnStop.Enabled = false;
        }

        private void MonitorSwitch_Load(object sender, EventArgs e)
        {
            notifyIcon1.Visible = true;
            
            this.ShowInTaskbar = false;
            this.Hide();
            Start();
            this.WindowState = FormWindowState.Minimized;

        }
    }
}

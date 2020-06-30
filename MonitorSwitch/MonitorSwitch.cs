using System;
using System.Management;
using System.Windows.Forms;

namespace MonitorSwitch
{
    public partial class MonitorSwitch : Form
    {
        String targetUsbHubDeviceID = @"USB\VID_1A40&PID_0101";
        ManagementEventWatcher insertWatcher = null;
        ManagementEventWatcher removeWatcher = null;

        public MonitorSwitch()
        {
            InitializeComponent();
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            String deviceID = (String)instance.Properties["DeviceID"].Value;
            if (deviceID.Contains(targetUsbHubDeviceID))
            {
                WinAPI.SetDisplayMonitors(true);
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            String deviceID = (String)instance.Properties["DeviceID"].Value;
            if (deviceID.Contains(targetUsbHubDeviceID))
            {
                WinAPI.SetDisplayMonitors(false);
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

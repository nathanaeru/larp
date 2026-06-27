using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Reflection;
using System.Threading;

namespace Larp
{
    static class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "LARPDNSSwitcherMutex";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                return;
            }

            try 
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Crash Details:\n\n" + ex.Message, "LARP Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (createdNew)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }

    public class TrayApp : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private AppWindow appWindow;
        
        public TrayApp()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "LARP DNS Switcher";
            trayIcon.Visible = true;
            trayIcon.MouseClick += new MouseEventHandler(TrayIcon_MouseClick);
            
            appWindow = new AppWindow(this);
            UpdateTrayIcon(appWindow.IsDnsOn, appWindow.SystemAccent);
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (appWindow.Visible)
                {
                    appWindow.Hide();
                }
                else
                {
                    Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                    appWindow.Location = new Point(workingArea.Right - appWindow.Width - 10, workingArea.Bottom - appWindow.Height - 10);
                    appWindow.Show();
                    appWindow.Activate();
                }
            }
        }

        public void UpdateTrayIcon(bool isOn, Color accent)
        {
            Image img = AppWindow.LoadEmbeddedImage(isOn ? "CONNECT.png" : "DISCONNECT.png");
            
            if (img != null)
            {
                using (Bitmap bmp = new Bitmap(img))
                {
                    if (trayIcon.Icon != null) trayIcon.Icon.Dispose();
                    trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }
            else
            {
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    if (isOn)
                    {
                        using (SolidBrush b = new SolidBrush(accent))
                            g.FillEllipse(b, 2, 2, 12, 12);
                    }
                    else
                    {
                        using (Pen p = new Pen(Color.FromArgb(166, 173, 200), 2))
                            g.DrawEllipse(p, 3, 3, 10, 10);
                    }
                }
                if (trayIcon.Icon != null) trayIcon.Icon.Dispose();
                trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            }
        }

        public void ExitApp()
        {
            if (trayIcon != null) trayIcon.Visible = false;
            Application.Exit();
        }
    }

    public class PowerButton : Control
    {
        public bool IsOn { get; set; }
        public Color AccentColor { get; set; }
        public Color BgOff { get; set; }
        public Color IconOff { get; set; }

        public PowerButton()
        {
            this.BgOff = Color.FromArgb(49, 50, 68);
            this.IconOff = Color.FromArgb(166, 173, 200);
            
            this.Size = new Size(130, 130);
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(5, 5, this.Width - 10, this.Height - 10);
            using (SolidBrush brush = new SolidBrush(IsOn ? AccentColor : BgOff))
            {
                g.FillEllipse(brush, rect);
            }

            using (Pen pen = new Pen(IsOn ? Color.White : IconOff, 7))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                int cx = this.Width / 2;
                int cy = this.Height / 2;
                int radius = 24;

                Rectangle iconRect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
                g.DrawArc(pen, iconRect, -50, 280);
                g.DrawLine(pen, cx, cy - radius - 8, cx, cy + 4);
            }
        }
    }

    public class AppWindow : Form
    {
        private TrayApp parentApp;
        public bool IsDnsOn = false;
        public Color SystemAccent;
        private Color currentBorderColor;
        
        private string currentProvider = "Cloudflare";
        private string customDns = "9.9.9.9,149.112.112.112";
        private readonly string SettingsFile = Path.Combine(Application.StartupPath, "LarpSettings.txt");
        
        private PictureBox headerImage;
        private Label statusLabel;
        private Label subStatusLabel;
        private PowerButton powerBtn;
        private Panel settingsPanel;
        private ComboBox providerDropdown;
        private TextBox customDnsInput;
        private CheckBox startupCheckbox;
        private Button exitBtn;

        public AppWindow(TrayApp parent)
        {
            parentApp = parent;
            SystemAccent = GetSystemAccentColor();
            LoadSettings();
            CheckCurrentDnsState();
            InitializeComponent();
            ApplyTheme();
            
            UpdateUIState(); 
            
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
        }

        public static Image LoadEmbeddedImage(string imageName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // Tries the exact filename first, then falls back to the namespace prefix
                var stream = assembly.GetManifestResourceStream(imageName);
                if (stream == null) stream = assembly.GetManifestResourceStream("Larp." + imageName);
                
                if (stream != null) 
                {
                    return Image.FromStream(stream);
                }
            }
            catch { }
            return null;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                SystemAccent = GetSystemAccentColor();
                ApplyTheme();
                UpdateUIState();
            }
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value != null) return ((int)value == 0);
                    }
                }
            }
            catch { }
            return true; 
        }

        private Color GetSystemAccentColor()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("ColorizationColor");
                        if (value != null)
                        {
                            int colorCode = (int)value;
                            return Color.FromArgb(255, (byte)(colorCode >> 16), (byte)(colorCode >> 8), (byte)colorCode);
                        }
                    }
                }
            }
            catch { }
            return Color.FromArgb(203, 166, 247); 
        }

        private Color Blend(Color front, Color back, double amount)
        {
            byte r = (byte)((front.R * amount) + back.R * (1 - amount));
            byte g = (byte)((front.G * amount) + back.G * (1 - amount));
            byte b = (byte)((front.B * amount) + back.B * (1 - amount));
            return Color.FromArgb(255, r, g, b);
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Font = new Font("Segoe UI", 9.5f);
            this.Width = 320;
            this.Height = 525; 
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Deactivate += new EventHandler((s, e) => this.Hide());
            this.Paint += new PaintEventHandler(DrawBorder);

            // Apply App Icon
            Image appIconImg = LoadEmbeddedImage("ICON.png");
            if (appIconImg != null)
            {
                using (Bitmap bmp = new Bitmap(appIconImg))
                {
                    this.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }

            headerImage = new PictureBox();
            headerImage.Width = this.Width;
            headerImage.Height = 75;
            headerImage.Location = new Point(0, 20);
            headerImage.SizeMode = PictureBoxSizeMode.Zoom;
            this.Controls.Add(headerImage);

            powerBtn = new PowerButton();
            powerBtn.Location = new Point((this.Width - powerBtn.Width) / 2, 110);
            powerBtn.Click += new EventHandler(PowerBtn_Click);
            this.Controls.Add(powerBtn);

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.Width = this.Width;
            statusLabel.Height = 40; 
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Font = new Font("Segoe UI", 16f, FontStyle.Regular);
            statusLabel.Location = new Point(0, 250);
            this.Controls.Add(statusLabel);

            subStatusLabel = new Label();
            subStatusLabel.AutoSize = false;
            subStatusLabel.Width = this.Width;
            subStatusLabel.Height = 25; 
            subStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            subStatusLabel.Font = new Font("Segoe UI", 9.5f);
            subStatusLabel.Location = new Point(0, 290);
            this.Controls.Add(subStatusLabel);

            settingsPanel = new Panel();
            settingsPanel.Width = 280;
            settingsPanel.Height = 120;
            settingsPanel.Location = new Point(20, 330);
            this.Controls.Add(settingsPanel);

            providerDropdown = new ComboBox();
            providerDropdown.DropDownStyle = ComboBoxStyle.DropDownList;
            providerDropdown.Items.Add("Cloudflare");
            providerDropdown.Items.Add("Google");
            providerDropdown.Items.Add("Custom");
            providerDropdown.Location = new Point(15, 15);
            providerDropdown.Size = new Size(250, 30);
            providerDropdown.FlatStyle = FlatStyle.Flat;
            providerDropdown.SelectedItem = currentProvider; 
            providerDropdown.SelectedIndexChanged += new EventHandler(ProviderChanged);
            settingsPanel.Controls.Add(providerDropdown);

            customDnsInput = new TextBox();
            customDnsInput.Location = new Point(15, 55);
            customDnsInput.Size = new Size(250, 30);
            customDnsInput.BorderStyle = BorderStyle.FixedSingle;
            customDnsInput.Text = customDns;
            customDnsInput.TextChanged += new EventHandler((s, e) => { customDns = customDnsInput.Text; SaveSettings(); });
            settingsPanel.Controls.Add(customDnsInput);

            startupCheckbox = new CheckBox();
            startupCheckbox.Text = "Run on system startup";
            startupCheckbox.Location = new Point(15, 90);
            startupCheckbox.AutoSize = true;
            startupCheckbox.Checked = CheckStartup();
            startupCheckbox.CheckedChanged += new EventHandler(ToggleStartup);
            settingsPanel.Controls.Add(startupCheckbox);

            exitBtn = new Button();
            exitBtn.Text = "Quit Application";
            exitBtn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            exitBtn.ForeColor = Color.White;
            exitBtn.FlatStyle = FlatStyle.Flat;
            exitBtn.FlatAppearance.BorderSize = 0;
            exitBtn.Size = new Size(280, 38);
            exitBtn.Location = new Point(20, 465);
            exitBtn.Cursor = Cursors.Hand;
            exitBtn.Click += new EventHandler((s, e) => parentApp.ExitApp());
            this.Controls.Add(exitBtn);
        }

        private void ApplyTheme()
        {
            bool isDark = IsSystemDarkTheme();
            
            Color baseBg = isDark ? Blend(SystemAccent, Color.Black, 0.12) : Blend(SystemAccent, Color.White, 0.05);
            Color surface0 = isDark ? Blend(SystemAccent, Color.Black, 0.20) : Blend(SystemAccent, Color.White, 0.15);
            Color textMain = isDark ? Color.White : Color.FromArgb(40, 40, 40);
            Color subText = isDark ? Blend(SystemAccent, Color.Gray, 0.3) : Blend(SystemAccent, Color.DimGray, 0.3);

            this.BackColor = baseBg;
            this.ForeColor = textMain;
            currentBorderColor = surface0;

            Image newHeader = LoadEmbeddedImage(isDark ? "LIGHT.png" : "DARK.png");
            if (newHeader != null)
            {
                if (headerImage.Image != null) headerImage.Image.Dispose();
                headerImage.Image = newHeader;
            }
            
            settingsPanel.BackColor = surface0;
            providerDropdown.BackColor = baseBg;
            providerDropdown.ForeColor = textMain;
            customDnsInput.BackColor = baseBg;
            customDnsInput.ForeColor = textMain;
            startupCheckbox.ForeColor = subText;

            exitBtn.BackColor = SystemAccent;
            powerBtn.AccentColor = SystemAccent;
            powerBtn.BgOff = surface0;
            powerBtn.IconOff = subText;
            
            this.Invalidate(); 
            powerBtn.Invalidate(); 
        }

        private void DrawBorder(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, currentBorderColor, ButtonBorderStyle.Solid);
        }

        private void ProviderChanged(object sender, EventArgs e)
        {
            currentProvider = providerDropdown.SelectedItem.ToString();
            SaveSettings();
            CheckCurrentDnsState();
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            powerBtn.IsOn = IsDnsOn;
            powerBtn.Invalidate(); 

            bool isDark = IsSystemDarkTheme();
            Color textMain = isDark ? Color.White : Color.FromArgb(40, 40, 40);
            Color subText = isDark ? Blend(SystemAccent, Color.Gray, 0.3) : Blend(SystemAccent, Color.DimGray, 0.3);

            if (IsDnsOn)
            {
                statusLabel.Text = "Connected";
                statusLabel.ForeColor = textMain;
                subStatusLabel.Text = string.Format("Your DNS queries are private ({0})", currentProvider);
                subStatusLabel.ForeColor = subText;
            }
            else
            {
                statusLabel.Text = "Disconnected";
                statusLabel.ForeColor = subText;
                subStatusLabel.Text = "System is using unprotected ISP DNS";
                subStatusLabel.ForeColor = subText;
            }

            bool isCustom = (currentProvider == "Custom");
            customDnsInput.Visible = isCustom;
            
            parentApp.UpdateTrayIcon(IsDnsOn, SystemAccent);
        }

        private void PowerBtn_Click(object sender, EventArgs e)
        {
            this.Hide(); 
            ApplyDns(!IsDnsOn);
        }

        private void ApplyDns(bool turnOn)
        {
            string psCommand;
            if (turnOn)
            {
                string dnsIps = currentProvider == "Cloudflare" ? "1.1.1.1,1.0.0.1" :
                                currentProvider == "Google" ? "8.8.8.8,8.8.4.4" : customDnsInput.Text;
                
                psCommand = string.Format("Get-NetAdapter | Where-Object {{$_.Status -eq 'Up'}} | Set-DnsClientServerAddress -ServerAddresses {0}; Clear-DnsClientCache", dnsIps);
            }
            else
            {
                psCommand = "Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ResetServerAddresses; Clear-DnsClientCache";
            }

            try
            {
                string fullCommand = string.Format("-NoProfile -WindowStyle Hidden -Command \"{0}\"", psCommand);
                ProcessStartInfo psi = new ProcessStartInfo("powershell", fullCommand);
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                
                Process proc = Process.Start(psi);
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    IsDnsOn = turnOn;
                }
            }
            catch (System.ComponentModel.Win32Exception) { }
            
            UpdateUIState();
        }

        private void CheckCurrentDnsState()
        {
            try
            {
                string targetDns = currentProvider == "Cloudflare" ? "1.1.1.1" :
                                   currentProvider == "Google" ? "8.8.8.8" : 
                                   customDns.Split(',')[0].Trim();

                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPInterfaceProperties ipProps = ni.GetIPProperties();
                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            if (dns.ToString() == targetDns)
                            {
                                IsDnsOn = true;
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
            IsDnsOn = false;
        }

        private void ToggleStartup(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (startupCheckbox.Checked) rk.SetValue("Larp", Application.ExecutablePath);
            else rk.DeleteValue("Larp", false);
        }

        private bool CheckStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            return rk.GetValue("Larp") != null;
        }

        private void SaveSettings()
        {
            File.WriteAllText(SettingsFile, currentProvider + "|" + customDns);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                string[] parts = File.ReadAllText(SettingsFile).Split('|');
                if (parts.Length == 2)
                {
                    currentProvider = parts[0];
                    customDns = parts[1];
                }
            }
        }
    }
}
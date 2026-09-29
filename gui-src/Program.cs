using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("Scrcpy 可视化控制台")]
[assembly: AssemblyDescription("适用于 scrcpy 的无控制台 Windows 图形化启动器")]
[assembly: AssemblyCompany("Local")]
[assembly: AssemblyProduct("Scrcpy GUI")]
[assembly: AssemblyVersion("1.7.0.0")]
[assembly: AssemblyFileVersion("1.7.0.0")]

namespace ScrcpyGui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly string baseDir;
        private readonly string scrcpyPath;
        private readonly string adbPath;
        private readonly List<ScrcpySession> sessions = new List<ScrcpySession>();

        private ComboBox deviceBox;
        private TextBox wirelessBox;
        private ComboBox sizeBox;
        private ComboBox bitrateBox;
        private ComboBox fpsBox;
        private ComboBox codecBox;
        private ComboBox orientationBox;
        private CheckBox fullscreenBox;
        private CheckBox topmostBox;
        private CheckBox screenOffBox;
        private CheckBox stayAwakeBox;
        private CheckBox touchesBox;
        private CheckBox noAudioBox;
        private CheckBox noControlBox;
        private CheckBox audioDupBox;
        private CheckBox recordBox;
        private TextBox recordPathBox;
        private TextBox extraArgsBox;
        private TextBox commandPreview;
        private TextBox logBox;
        private Label statusLabel;
        private Button startButton;
        private Button stopButton;
        private Button shortcutButton;
        private Button longShotButton;
        private Button refreshButton;
        private Button connectButton;

        private static readonly Color WindowColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color PrimaryColor = Color.FromArgb(33, 105, 255);
        private static readonly Color TextColor = Color.FromArgb(32, 39, 50);
        private static readonly Color MutedColor = Color.FromArgb(102, 112, 128);
        private static readonly Font UiFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

        public MainForm()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            scrcpyPath = Path.Combine(baseDir, "scrcpy.exe");
            adbPath = Path.Combine(baseDir, "adb.exe");

            Text = "Scrcpy 可视化控制台";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 700);
            Size = new Size(980, 760);
            BackColor = WindowColor;
            ForeColor = TextColor;
            Font = UiFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            try
            {
                if (File.Exists(scrcpyPath))
                    Icon = Icon.ExtractAssociatedIcon(scrcpyPath);
            }
            catch { }

            BuildInterface();
            WireEvents();
            UpdateCommandPreview();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!File.Exists(scrcpyPath) || !File.Exists(adbPath))
            {
                MessageBox.Show(this, "请把 ScrcpyGUI.exe 放在 scrcpy.exe 和 adb.exe 所在目录。",
                    "缺少运行文件", MessageBoxButtons.OK, MessageBoxIcon.Error);
                startButton.Enabled = false;
                refreshButton.Enabled = false;
                connectButton.Enabled = false;
                return;
            }
            RefreshDevices();
        }

        private void BuildInterface()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 18, 22, 18),
                ColumnCount = 1,
                RowCount = 5,
                BackColor = WindowColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = WindowColor };
            var title = new Label
            {
                Text = "Scrcpy 可视化控制台",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "连接 Android 设备，按需配置并静默启动",
                AutoSize = true,
                Font = UiFont,
                ForeColor = MutedColor,
                Location = new Point(2, 36)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            var connectionCard = MakeCard("设备连接");
            var connectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 30, 14, 10),
                ColumnCount = 6,
                RowCount = 2
            };
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            connectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            connectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            connectionCard.Controls.Add(connectionLayout);

            connectionLayout.Controls.Add(MakeLabel("设备"), 0, 0);
            deviceBox = MakeCombo();
            deviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            connectionLayout.Controls.Add(deviceBox, 1, 0);
            refreshButton = MakeButton("刷新", false);
            connectionLayout.Controls.Add(refreshButton, 2, 0);
            connectionLayout.Controls.Add(MakeLabel("无线地址"), 3, 0);
            wirelessBox = MakeTextBox();
            wirelessBox.Text = "192.168.1.100:5555";
            connectionLayout.Controls.Add(wirelessBox, 4, 0);
            connectButton = MakeButton("连接", false);
            connectionLayout.Controls.Add(connectButton, 5, 0);

            statusLabel = new Label
            {
                Text = "正在检查设备…",
                AutoSize = true,
                ForeColor = MutedColor,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 0)
            };
            connectionLayout.SetColumnSpan(statusLabel, 6);
            connectionLayout.Controls.Add(statusLabel, 0, 1);
            root.Controls.Add(connectionCard, 0, 1);

            var options = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 12, 0, 10)
            };
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.Controls.Add(options, 0, 2);

            var qualityCard = MakeCard("画面与性能");
            qualityCard.Margin = new Padding(0, 0, 7, 0);
            var quality = MakeOptionsTable();
            qualityCard.Controls.Add(quality);
            quality.Controls.Add(MakeLabel("最大边长"), 0, 0);
            sizeBox = MakeCombo("原始分辨率", "1920", "1600", "1280", "1024", "720");
            quality.Controls.Add(sizeBox, 1, 0);
            quality.Controls.Add(MakeLabel("视频码率"), 0, 1);
            bitrateBox = MakeCombo("自动", "4M", "8M", "12M", "16M", "24M");
            bitrateBox.SelectedIndex = 2;
            quality.Controls.Add(bitrateBox, 1, 1);
            quality.Controls.Add(MakeLabel("最高帧率"), 0, 2);
            fpsBox = MakeCombo("自动", "30", "60", "90", "120");
            fpsBox.SelectedIndex = 2;
            quality.Controls.Add(fpsBox, 1, 2);
            quality.Controls.Add(MakeLabel("视频编码"), 0, 3);
            codecBox = MakeCombo("H.264（兼容）", "H.265", "AV1", "VP8", "VP9");
            quality.Controls.Add(codecBox, 1, 3);
            quality.Controls.Add(MakeLabel("画面方向"), 0, 4);
            orientationBox = MakeCombo("跟随设备", "0°", "90°", "180°", "270°");
            quality.Controls.Add(orientationBox, 1, 4);

            fullscreenBox = MakeCheckBox("全屏启动");
            topmostBox = MakeCheckBox("窗口置顶");
            var qualityChecks = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            qualityChecks.Controls.Add(fullscreenBox);
            qualityChecks.Controls.Add(topmostBox);
            quality.SetColumnSpan(qualityChecks, 2);
            quality.Controls.Add(qualityChecks, 0, 5);
            options.Controls.Add(qualityCard, 0, 0);

            var behaviorCard = MakeCard("控制与输出");
            behaviorCard.Margin = new Padding(7, 0, 0, 0);
            var behavior = MakeOptionsTable();
            behaviorCard.Controls.Add(behavior);

            screenOffBox = MakeCheckBox("启动后关闭手机屏幕");
            stayAwakeBox = MakeCheckBox("保持设备唤醒");
            stayAwakeBox.Checked = true;
            touchesBox = MakeCheckBox("显示物理触摸点");
            noAudioBox = MakeCheckBox("禁用音频转发");
            noControlBox = MakeCheckBox("只读（禁止控制）");
            audioDupBox = MakeCheckBox("手机同时播放声音");
            var checks = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 2, 0, 0)
            };
            checks.Controls.Add(screenOffBox);
            checks.Controls.Add(stayAwakeBox);
            checks.Controls.Add(touchesBox);
            checks.Controls.Add(noAudioBox);
            checks.Controls.Add(noControlBox);
            checks.Controls.Add(audioDupBox);
            behavior.SetColumnSpan(checks, 2);
            behavior.SetRowSpan(checks, 3);
            behavior.Controls.Add(checks, 0, 0);

            behavior.Controls.Add(MakeLabel("录屏"), 0, 3);
            var recordPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            recordPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
            recordPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            recordPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            recordBox = new CheckBox { Dock = DockStyle.Fill, Margin = new Padding(3, 7, 0, 0) };
            recordPathBox = MakeTextBox();
            recordPathBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "scrcpy-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".mp4");
            var browseButton = MakeButton("选择", false);
            recordPanel.Controls.Add(recordBox, 0, 0);
            recordPanel.Controls.Add(recordPathBox, 1, 0);
            recordPanel.Controls.Add(browseButton, 2, 0);
            behavior.Controls.Add(recordPanel, 1, 3);

            behavior.Controls.Add(MakeLabel("额外参数"), 0, 4);
            extraArgsBox = MakeTextBox();
            extraArgsBox.PlaceholderTextCompat("例如：--window-title=我的手机");
            behavior.Controls.Add(extraArgsBox, 1, 4);
            options.Controls.Add(behaviorCard, 1, 0);

            browseButton.Click += delegate
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "MP4 视频|*.mp4|MKV 视频|*.mkv|所有文件|*.*";
                    dialog.FileName = Path.GetFileName(recordPathBox.Text);
                    dialog.InitialDirectory = Path.GetDirectoryName(recordPathBox.Text);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        recordPathBox.Text = dialog.FileName;
                        recordBox.Checked = true;
                    }
                }
            };

            var outputCard = MakeCard("启动命令与运行日志");
            var outputLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 28, 14, 10),
                ColumnCount = 1,
                RowCount = 2
            };
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            commandPreview = MakeTextBox();
            commandPreview.ReadOnly = true;
            commandPreview.BackColor = Color.FromArgb(246, 248, 252);
            logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ForeColor = MutedColor,
                Font = new Font("Consolas", 8.5F),
                Margin = new Padding(3, 5, 3, 0)
            };
            outputLayout.Controls.Add(commandPreview, 0, 0);
            outputLayout.Controls.Add(logBox, 0, 1);
            outputCard.Controls.Add(outputLayout);
            root.Controls.Add(outputCard, 0, 3);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                Padding = new Padding(0, 10, 0, 0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
            stopButton = MakeButton("停止全部", false);
            stopButton.Enabled = false;
            shortcutButton = MakeButton("窗口快捷操作", false);
            shortcutButton.Enabled = false;
            longShotButton = MakeButton("手机长截图", false);
            startButton = MakeButton("启动 Scrcpy", true);
            actions.Controls.Add(stopButton, 1, 0);
            actions.Controls.Add(shortcutButton, 2, 0);
            actions.Controls.Add(longShotButton, 3, 0);
            actions.Controls.Add(startButton, 4, 0);
            root.Controls.Add(actions, 0, 4);
        }

        private static Panel MakeCard(string title)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(14, 8)
            });
            return panel;
        }

        private static TableLayoutPanel MakeOptionsTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 34, 14, 10),
                ColumnCount = 2,
                RowCount = 6
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return table;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = MutedColor,
                Margin = new Padding(3, 0, 3, 0)
            };
        }

        private static TextBox MakeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiFont,
                Margin = new Padding(3, 4, 3, 4)
            };
        }

        private static ComboBox MakeCombo(params string[] items)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiFont,
                Margin = new Padding(3, 4, 3, 4)
            };
            if (items != null && items.Length > 0)
            {
                combo.Items.AddRange(items);
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        private static CheckBox MakeCheckBox(string text)
        {
            return new CheckBox
            {
                Text = text,
                AutoSize = true,
                ForeColor = TextColor,
                Margin = new Padding(4, 5, 18, 4)
            };
        }

        private static Button MakeButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 211, 220);
            button.BackColor = primary ? PrimaryColor : Color.White;
            button.ForeColor = primary ? Color.White : TextColor;
            return button;
        }

        private void WireEvents()
        {
            refreshButton.Click += delegate { RefreshDevices(); };
            connectButton.Click += delegate { ConnectWireless(); };
            startButton.Click += delegate { StartScrcpy(); };
            stopButton.Click += delegate { StopAllSessions(); };
            shortcutButton.Click += delegate { OpenShortcutPanel(); };
            longShotButton.Click += delegate { OpenLongScreenshot(); };

            foreach (Control control in FindAllControls(this))
            {
                if (control is ComboBox)
                    ((ComboBox)control).SelectedIndexChanged += delegate { UpdateCommandPreview(); };
                else if (control is CheckBox)
                    ((CheckBox)control).CheckedChanged += delegate { UpdateCommandPreview(); };
                else if (control is TextBox && control != logBox && control != commandPreview)
                    ((TextBox)control).TextChanged += delegate { UpdateCommandPreview(); };
            }
        }

        private static IEnumerable<Control> FindAllControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control nested in FindAllControls(child))
                    yield return nested;
            }
        }

        private void RefreshDevices()
        {
            SetStatus("正在刷新设备…", MutedColor);
            refreshButton.Enabled = false;
            Task.Factory.StartNew(delegate { return RunHiddenCapture(adbPath, "devices -l", 8000); })
                .ContinueWith(delegate(Task<string> task)
                {
                    refreshButton.Enabled = true;
                    if (task.IsFaulted)
                    {
                        SetStatus("刷新失败：" + task.Exception.GetBaseException().Message, Color.Firebrick);
                        return;
                    }

                    string previous = deviceBox.SelectedItem == null ? null : deviceBox.SelectedItem.ToString();
                    deviceBox.Items.Clear();
                    string[] lines = task.Result.Replace("\r", "").Split('\n');
                    foreach (string raw in lines)
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("List of devices"))
                            continue;
                        string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[1] == "device")
                        {
                            string serial = parts[0];
                            string model = FindToken(parts, "model:");
                            deviceBox.Items.Add(model.Length > 0 ? serial + "  ·  " + model.Replace('_', ' ') : serial);
                        }
                    }

                    if (deviceBox.Items.Count == 0)
                    {
                        SetStatus("未发现可用设备，请连接 USB 或使用无线地址。", Color.FromArgb(190, 115, 0));
                    }
                    else
                    {
                        int previousIndex = previous == null ? -1 : deviceBox.Items.IndexOf(previous);
                        deviceBox.SelectedIndex = previousIndex >= 0 ? previousIndex : 0;
                        SetStatus("已发现 " + deviceBox.Items.Count + " 台设备", Color.FromArgb(29, 135, 78));
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static string FindToken(string[] parts, string prefix)
        {
            foreach (string part in parts)
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return part.Substring(prefix.Length);
            return string.Empty;
        }

        private void ConnectWireless()
        {
            string address = wirelessBox.Text.Trim();
            if (address.Length == 0)
            {
                MessageBox.Show(this, "请输入设备 IP，例如 192.168.1.100:5555。", "无线连接",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            connectButton.Enabled = false;
            SetStatus("正在连接 " + address + "…", MutedColor);
            Task.Factory.StartNew(delegate { return RunHiddenCapture(adbPath, "connect " + Quote(address), 12000); })
                .ContinueWith(delegate(Task<string> task)
                {
                    connectButton.Enabled = true;
                    if (task.IsFaulted)
                    {
                        SetStatus("无线连接失败：" + task.Exception.GetBaseException().Message, Color.Firebrick);
                        return;
                    }
                    AppendLog(task.Result.Trim());
                    SetStatus(task.Result.Trim(), task.Result.IndexOf("connected", StringComparison.OrdinalIgnoreCase) >= 0
                        ? Color.FromArgb(29, 135, 78) : Color.FromArgb(190, 115, 0));
                    RefreshDevices();
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void StartScrcpy()
        {
            try
            {
                List<string> args = BuildArguments();
                var info = new ProcessStartInfo
                {
                    FileName = scrcpyPath,
                    Arguments = string.Join(" ", args.ToArray()),
                    WorkingDirectory = baseDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                var process = new Process { StartInfo = info, EnableRaisingEvents = true };
                ScrcpySession session = null;
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) AppendLog(e.Data); };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) AppendLog(e.Data); };
                process.Exited += delegate(object sender, EventArgs e)
                {
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (session != null) sessions.Remove(session);
                            stopButton.Enabled = sessions.Count > 0;
                            shortcutButton.Enabled = sessions.Count > 0;
                            SetStatus("Scrcpy 会话已结束（退出码 " + process.ExitCode + "）", MutedColor);
                            process.Dispose();
                        });
                    }
                    catch { }
                };
                if (!process.Start())
                    throw new InvalidOperationException("无法启动 scrcpy.exe");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                session = new ScrcpySession(process, GetSelectedSerial());
                sessions.Add(session);
                stopButton.Enabled = true;
                shortcutButton.Enabled = true;
                SetStatus("Scrcpy 已启动（PID " + process.Id + "）", Color.FromArgb(29, 135, 78));
                AppendLog("启动: scrcpy.exe " + info.Arguments);
            }
            catch (Exception ex)
            {
                SetStatus("启动失败：" + ex.Message, Color.Firebrick);
                MessageBox.Show(this, ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> BuildArguments()
        {
            var args = new List<string>();
            string selectedSerial = GetSelectedSerial();
            if (selectedSerial.Length > 0)
            {
                args.Add("--serial");
                args.Add(Quote(selectedSerial));
            }

            string size = Selected(sizeBox);
            if (size != "原始分辨率") args.Add("--max-size=" + size);
            string bitRate = Selected(bitrateBox);
            if (bitRate != "自动") args.Add("--video-bit-rate=" + bitRate);
            string fps = Selected(fpsBox);
            if (fps != "自动") args.Add("--max-fps=" + fps);

            string codec = Selected(codecBox);
            if (codec.StartsWith("H.265")) args.Add("--video-codec=h265");
            else if (codec.StartsWith("AV1")) args.Add("--video-codec=av1");
            else if (codec.StartsWith("VP8")) args.Add("--video-codec=vp8");
            else if (codec.StartsWith("VP9")) args.Add("--video-codec=vp9");

            string orientation = Selected(orientationBox);
            if (orientation != "跟随设备") args.Add("--display-orientation=" + orientation.Replace("°", ""));
            if (fullscreenBox.Checked) args.Add("--fullscreen");
            if (topmostBox.Checked) args.Add("--always-on-top");
            if (screenOffBox.Checked) args.Add("--turn-screen-off");
            if (stayAwakeBox.Checked) args.Add("--stay-awake");
            if (touchesBox.Checked) args.Add("--show-touches");
            if (noAudioBox.Checked) args.Add("--no-audio");
            if (noControlBox.Checked) args.Add("--no-control");
            if (audioDupBox.Checked && !noAudioBox.Checked)
            {
                args.Add("--audio-source=playback");
                args.Add("--audio-dup");
            }
            if (recordBox.Checked)
            {
                string recordPath = recordPathBox.Text.Trim();
                if (recordPath.Length == 0)
                    throw new InvalidOperationException("启用录屏后必须选择保存路径。");
                string directory = Path.GetDirectoryName(Path.GetFullPath(recordPath));
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                args.Add("--record=" + Quote(recordPath));
            }
            if (extraArgsBox.Text.Trim().Length > 0)
                args.Add(extraArgsBox.Text.Trim());
            return args;
        }

        private string GetSelectedSerial()
        {
            if (deviceBox.SelectedItem == null) return string.Empty;
            string selected = deviceBox.SelectedItem.ToString();
            return selected.Split(new[] { "  ·  " }, StringSplitOptions.None)[0].Trim();
        }

        private void OpenLongScreenshot()
        {
            string serial = GetSelectedSerial();
            if (serial.Length == 0)
            {
                MessageBox.Show(this, "请先连接并选择一台设备。", "手机长截图",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dialog = new LongScreenshotForm(adbPath, serial))
                dialog.ShowDialog(this);
        }

        private void OpenShortcutPanel()
        {
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (sessions[i].Process.HasExited) sessions.RemoveAt(i);
                }
                catch { sessions.RemoveAt(i); }
            }
            stopButton.Enabled = sessions.Count > 0;
            shortcutButton.Enabled = sessions.Count > 0;
            if (sessions.Count == 0)
            {
                MessageBox.Show(this, "请先用本程序启动一个 Scrcpy 窗口。", "窗口快捷操作",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new ScrcpyShortcutForm(sessions.ToArray(), ExecuteShortcut))
                dialog.ShowDialog(this);
        }

        private void ExecuteShortcut(ScrcpySession session, ScrcpyShortcut shortcut)
        {
            try
            {
                ScrcpyWindowController.Send(session.Process, shortcut.Key, shortcut.Shift);
                SetStatus("已发送：" + shortcut.Text + " → " + session, Color.FromArgb(29, 135, 78));
                AppendLog("快捷操作: " + shortcut.Text + " (" + shortcut.DisplayKeys + ") → PID " +
                    session.Process.Id);
            }
            catch (Exception ex)
            {
                SetStatus("快捷操作失败：" + ex.Message, Color.Firebrick);
                MessageBox.Show(this, ex.Message, "窗口快捷操作", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Selected(ComboBox box)
        {
            return box.SelectedItem == null ? string.Empty : box.SelectedItem.ToString();
        }

        private void UpdateCommandPreview()
        {
            if (commandPreview == null) return;
            try
            {
                commandPreview.Text = "scrcpy.exe " + string.Join(" ", BuildArguments().ToArray());
            }
            catch
            {
                commandPreview.Text = "scrcpy.exe";
            }
        }

        private void StopAllSessions()
        {
            int stopped = 0;
            ScrcpySession[] copy = sessions.ToArray();
            foreach (ScrcpySession session in copy)
            {
                try
                {
                    Process process = session.Process;
                    if (!process.HasExited)
                    {
                        process.Kill();
                        stopped++;
                    }
                }
                catch { }
            }
            sessions.Clear();
            stopButton.Enabled = false;
            shortcutButton.Enabled = false;
            SetStatus("已停止 " + stopped + " 个 Scrcpy 会话", MutedColor);
        }

        private static string RunHiddenCapture(string fileName, string arguments, int timeoutMs)
        {
            var info = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("进程启动失败。");
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(timeoutMs))
                {
                    process.Kill();
                    throw new TimeoutException("命令执行超时。");
                }
                return (stdout + Environment.NewLine + stderr).Trim();
            }
        }

        private static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private void AppendLog(string line)
        {
            if (IsDisposed || line == null) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((MethodInvoker)delegate { AppendLog(line); }); } catch { }
                return;
            }
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line + Environment.NewLine);
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }
    }

    internal sealed class ScrcpySession
    {
        public readonly Process Process;
        public readonly string Serial;
        public readonly DateTime StartedAt;

        public ScrcpySession(Process process, string serial)
        {
            Process = process;
            Serial = string.IsNullOrEmpty(serial) ? "默认设备" : serial;
            StartedAt = DateTime.Now;
        }

        public override string ToString()
        {
            int processId;
            try { processId = Process.Id; }
            catch { processId = 0; }
            return Serial + "  ·  PID " + processId + "  ·  " + StartedAt.ToString("HH:mm:ss");
        }
    }

    internal sealed class ScrcpyShortcut
    {
        public readonly string Text;
        public readonly Keys Key;
        public readonly bool Shift;

        public ScrcpyShortcut(string text, Keys key, bool shift)
        {
            Text = text;
            Key = key;
            Shift = shift;
        }

        public string DisplayKeys
        {
            get { return "Alt+" + (Shift ? "Shift+" : string.Empty) + Key; }
        }
    }

    internal sealed class ScrcpyShortcutForm : Form
    {
        private readonly ComboBox sessionBox;
        private readonly Action<ScrcpySession, ScrcpyShortcut> execute;
        private readonly ToolTip toolTip = new ToolTip();

        public ScrcpyShortcutForm(ScrcpySession[] sessions,
            Action<ScrcpySession, ScrcpyShortcut> execute)
        {
            this.execute = execute;
            Text = "Scrcpy 已启动窗口快捷操作";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(780, 500);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Microsoft YaHei UI", 9F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 14),
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            header.Controls.Add(new Label
            {
                Text = "目标窗口",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.FromArgb(102, 112, 128)
            }, 0, 0);
            sessionBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 3, 3, 5)
            };
            sessionBox.Items.AddRange(sessions);
            if (sessionBox.Items.Count > 0) sessionBox.SelectedIndex = sessionBox.Items.Count - 1;
            header.Controls.Add(sessionBox, 1, 0);
            var description = new Label
            {
                Text = "点击按钮后会激活目标 Scrcpy 窗口并发送官方快捷键。",
                AutoSize = true,
                ForeColor = Color.FromArgb(102, 112, 128),
                Anchor = AnchorStyles.Left
            };
            header.SetColumnSpan(description, 2);
            header.Controls.Add(description, 0, 1);
            root.Controls.Add(header, 0, 0);

            var shortcuts = new[]
            {
                new ScrcpyShortcut("全屏", Keys.F, false),
                new ScrcpyShortcut("显示左旋", Keys.Left, false),
                new ScrcpyShortcut("显示右旋", Keys.Right, false),
                new ScrcpyShortcut("水平翻转", Keys.Left, true),
                new ScrcpyShortcut("垂直翻转", Keys.Up, true),
                new ScrcpyShortcut("暂停画面", Keys.Z, false),
                new ScrcpyShortcut("恢复画面", Keys.Z, true),
                new ScrcpyShortcut("重置视频", Keys.R, true),
                new ScrcpyShortcut("窗口 1:1", Keys.G, false),
                new ScrcpyShortcut("适应内容", Keys.W, false),
                new ScrcpyShortcut("主页", Keys.H, false),
                new ScrcpyShortcut("返回", Keys.B, false),
                new ScrcpyShortcut("最近任务", Keys.S, false),
                new ScrcpyShortcut("菜单", Keys.M, false),
                new ScrcpyShortcut("音量 +", Keys.Up, false),
                new ScrcpyShortcut("音量 -", Keys.Down, false),
                new ScrcpyShortcut("电源", Keys.P, false),
                new ScrcpyShortcut("关闭手机屏", Keys.O, false),
                new ScrcpyShortcut("打开手机屏", Keys.O, true),
                new ScrcpyShortcut("旋转设备", Keys.R, false),
                new ScrcpyShortcut("展开通知", Keys.N, false),
                new ScrcpyShortcut("收起通知", Keys.N, true),
                new ScrcpyShortcut("复制", Keys.C, false),
                new ScrcpyShortcut("粘贴", Keys.V, false),
                new ScrcpyShortcut("文本粘贴", Keys.V, true),
                new ScrcpyShortcut("FPS 计数", Keys.I, false)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                ColumnCount = 5,
                RowCount = 6,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            for (int column = 0; column < 5; column++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            for (int row = 0; row < 6; row++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666F));

            for (int i = 0; i < shortcuts.Length; i++)
            {
                ScrcpyShortcut shortcut = shortcuts[i];
                var button = new Button
                {
                    Text = shortcut.Text,
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(32, 39, 50),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3),
                    Tag = shortcut
                };
                button.FlatAppearance.BorderColor = Color.FromArgb(205, 211, 220);
                button.Click += ShortcutClick;
                toolTip.SetToolTip(button, shortcut.DisplayKeys);
                grid.Controls.Add(button, i % 5, i / 5);
            }
            root.Controls.Add(grid, 0, 1);

            root.Controls.Add(new Label
            {
                Text = "提示：按钮使用 Scrcpy 默认快捷修饰键 Left Alt；若在额外参数中修改了 --shortcut-mod，请恢复默认值。",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(102, 112, 128)
            }, 0, 2);
        }

        private void ShortcutClick(object sender, EventArgs e)
        {
            ScrcpySession session = sessionBox.SelectedItem as ScrcpySession;
            ScrcpyShortcut shortcut = ((Control)sender).Tag as ScrcpyShortcut;
            if (session == null || shortcut == null) return;
            execute(session, shortcut);
        }
    }

    internal static class ScrcpyWindowController
    {
        private const uint KeyUp = 0x0002;
        private const int RestoreWindow = 9;

        private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

        public static void Send(Process process, Keys key, bool shift)
        {
            if (process == null || process.HasExited)
                throw new InvalidOperationException("目标 Scrcpy 会话已经结束。");

            IntPtr window = FindWindow(process);
            if (window == IntPtr.Zero)
                throw new InvalidOperationException("未找到目标 Scrcpy 窗口，请确认画面窗口已经打开。");

            ShowWindowAsync(window, RestoreWindow);
            SetForegroundWindow(window);
            Thread.Sleep(90);
            if (GetForegroundWindow() != window)
            {
                SetForegroundWindow(window);
                Thread.Sleep(90);
            }
            if (GetForegroundWindow() != window)
                throw new InvalidOperationException("无法激活目标 Scrcpy 窗口，请先点击一次该窗口后重试。");

            Press(Keys.LMenu, false);
            if (shift) Press(Keys.LShiftKey, false);
            Press(key, false);
            Press(key, true);
            if (shift) Press(Keys.LShiftKey, true);
            Press(Keys.LMenu, true);
        }

        private static void Press(Keys key, bool release)
        {
            keybd_event((byte)key, 0, release ? KeyUp : 0, UIntPtr.Zero);
        }

        private static IntPtr FindWindow(Process process)
        {
            process.Refresh();
            IntPtr mainWindow = process.MainWindowHandle;
            if (mainWindow != IntPtr.Zero) return mainWindow;

            IntPtr found = IntPtr.Zero;
            uint targetId = (uint)process.Id;
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);
                if (processId == targetId && IsWindowVisible(window))
                {
                    found = window;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }
    }

    internal sealed class LongScreenshotForm : Form
    {
        private readonly string adbPath;
        private readonly string serial;
        private readonly NumericUpDown pagesBox;
        private readonly NumericUpDown swipeBox;
        private readonly NumericUpDown waitBox;
        private readonly NumericUpDown topCropBox;
        private readonly NumericUpDown bottomCropBox;
        private readonly CheckBox nativeAutoSaveBox;
        private readonly ProgressBar progressBar;
        private readonly Label progressLabel;
        private readonly Button startButton;
        private readonly Button fallbackButton;
        private readonly Button cancelButton;
        private volatile bool cancelRequested;
        private bool isRunning;

        public LongScreenshotForm(string adbPath, string serial)
        {
            this.adbPath = adbPath;
            this.serial = serial;

            Text = "手机长截图";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 520);
            BackColor = Color.FromArgb(245, 247, 250);
            ForeColor = Color.FromArgb(32, 39, 50);
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 18, 22, 18),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Fill };
            header.Controls.Add(new Label
            {
                Text = "自动滚动并拼接手机画面",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
                Location = new Point(0, 0)
            });
            header.Controls.Add(new Label
            {
                Text = "开始前请让要截取的页面停在最顶部，并避免触碰手机。",
                AutoSize = true,
                ForeColor = Color.FromArgb(102, 112, 128),
                Location = new Point(2, 36)
            });
            root.Controls.Add(header, 0, 0);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 12),
                ColumnCount = 3,
                RowCount = 7
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            for (int i = 0; i < 6; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(fields);
            root.Controls.Add(card, 0, 1);

            fields.Controls.Add(FieldLabel("当前设备"), 0, 0);
            var deviceLabel = new Label
            {
                Text = serial,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Consolas", 9F),
                ForeColor = Color.FromArgb(33, 105, 255)
            };
            fields.SetColumnSpan(deviceLabel, 2);
            fields.Controls.Add(deviceLabel, 1, 0);

            fields.Controls.Add(FieldLabel("原生模式"), 0, 1);
            nativeAutoSaveBox = new CheckBox
            {
                Text = "自动滚动到底并点击保存（取消勾选可在手机上手动操作）",
                Dock = DockStyle.Fill,
                Checked = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(29, 135, 78),
                Margin = new Padding(3, 7, 3, 4)
            };
            fields.SetColumnSpan(nativeAutoSaveBox, 2);
            fields.Controls.Add(nativeAutoSaveBox, 1, 1);

            fields.Controls.Add(FieldLabel("最大拼接屏数"), 0, 2);
            pagesBox = NumberBox(2, 99, 99, 1);
            fields.Controls.Add(pagesBox, 1, 2);
            fields.Controls.Add(HintLabel("到达底部会提前结束"), 2, 2);

            fields.Controls.Add(FieldLabel("每次滑动距离"), 0, 3);
            swipeBox = NumberBox(20, 60, 35, 5);
            fields.Controls.Add(swipeBox, 1, 3);
            fields.Controls.Add(HintLabel("屏幕高度 %"), 2, 3);

            fields.Controls.Add(FieldLabel("滚动等待时间"), 0, 4);
            waitBox = NumberBox(300, 2500, 800, 100);
            fields.Controls.Add(waitBox, 1, 4);
            fields.Controls.Add(HintLabel("毫秒"), 2, 4);

            fields.Controls.Add(FieldLabel("裁剪系统栏"), 0, 5);
            var cropPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            cropPanel.Controls.Add(HintLabel("顶部"));
            topCropBox = NumberBox(0, 25, 3, 1);
            topCropBox.Width = 70;
            cropPanel.Controls.Add(topCropBox);
            cropPanel.Controls.Add(HintLabel("%    底部"));
            bottomCropBox = NumberBox(0, 25, 5, 1);
            bottomCropBox.Width = 70;
            cropPanel.Controls.Add(bottomCropBox);
            cropPanel.Controls.Add(HintLabel("%"));
            fields.SetColumnSpan(cropPanel, 2);
            fields.Controls.Add(cropPanel, 1, 5);

            var tips = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Color.FromArgb(102, 112, 128),
                Text = "推荐使用手机原生长截图；下列滚动参数仅供“电脑兼容拼接”模式使用。手机原图会保留在手机相册。",
                Padding = new Padding(0, 8, 0, 0)
            };
            fields.SetColumnSpan(tips, 3);
            fields.Controls.Add(tips, 0, 6);

            var progressPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 4) };
            progressLabel = new Label
            {
                Text = "准备就绪",
                AutoSize = true,
                ForeColor = Color.FromArgb(102, 112, 128),
                Location = new Point(0, 10)
            };
            progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Location = new Point(0, 36),
                Width = 574,
                Height = 16,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            progressPanel.Controls.Add(progressLabel);
            progressPanel.Controls.Add(progressBar);
            root.Controls.Add(progressPanel, 0, 2);

            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
            cancelButton = FlatButton("取消", false);
            cancelButton.Enabled = false;
            fallbackButton = FlatButton("电脑兼容拼接", false);
            startButton = FlatButton("手机原生长截图", true);
            actions.Controls.Add(cancelButton, 1, 0);
            actions.Controls.Add(fallbackButton, 2, 0);
            actions.Controls.Add(startButton, 3, 0);
            root.Controls.Add(actions, 0, 3);

            startButton.Click += delegate { StartNativeCapture(); };
            fallbackButton.Click += delegate { StartCapture(); };
            cancelButton.Click += delegate
            {
                cancelRequested = true;
                cancelButton.Enabled = false;
                progressLabel.Text = "正在停止，将已截取内容复制到剪贴板…";
            };
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (!isRunning) return;
                cancelRequested = true;
                e.Cancel = true;
                progressLabel.Text = "请稍候，正在结束当前截图任务…";
            };
        }

        private static Label FieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.FromArgb(78, 88, 104)
            };
        }

        private static Label HintLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.FromArgb(130, 139, 152),
                Margin = new Padding(5, 8, 3, 0)
            };
        }

        private static TextBox FieldTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9F),
                Margin = new Padding(3, 7, 3, 6)
            };
        }

        private static NumericUpDown NumberBox(decimal min, decimal max, decimal value, decimal increment)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Increment = increment,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9F),
                Margin = new Padding(3, 7, 3, 6)
            };
        }

        private static Button FlatButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Color.FromArgb(33, 105, 255) : Color.White,
                ForeColor = primary ? Color.White : Color.FromArgb(32, 39, 50),
                Font = new Font("Microsoft YaHei UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 211, 220);
            return button;
        }

        private void StartCapture()
        {
            var settings = new LongScreenshotSettings
            {
                MaxFrames = (int)pagesBox.Value,
                SwipePercent = (int)swipeBox.Value,
                WaitMilliseconds = (int)waitBox.Value,
                TopCropPercent = (int)topCropBox.Value,
                BottomCropPercent = (int)bottomCropBox.Value
            };

            cancelRequested = false;
            isRunning = true;
            SetInputsEnabled(false);
            startButton.Enabled = false;
            fallbackButton.Enabled = false;
            cancelButton.Enabled = true;
            progressBar.Value = 0;
            progressLabel.Text = "正在截取第 1 屏…";

            Task.Factory.StartNew(delegate { return CaptureAndStitch(settings); })
                .ContinueWith(delegate(Task<LongScreenshotResult> task)
                {
                    isRunning = false;
                    SetInputsEnabled(true);
                    startButton.Enabled = true;
                    fallbackButton.Enabled = true;
                    cancelButton.Enabled = false;

                    if (task.IsFaulted)
                    {
                        string message = task.Exception.GetBaseException().Message;
                        progressLabel.Text = "失败：" + message;
                        MessageBox.Show(this, message, "长截图失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    LongScreenshotResult result = task.Result;
                    try
                    {
                        CopyImageToClipboard(result.Image);
                        progressBar.Value = 100;
                        progressLabel.Text = "长截图已复制到剪贴板";
                        string messageText = "长截图已复制到 Windows 剪贴板，可直接按 Ctrl+V 粘贴。" +
                            "\r\n\r\n共拼接 " + result.FrameCount + " 屏，图片尺寸 " +
                            result.Width + " × " + result.Height + "。" +
                            "\r\n自动识别固定顶部 " + result.FixedTop + " px、固定底部 " +
                            result.FixedBottom + " px。";
                        if (result.Warning.Length > 0)
                            messageText += "\r\n\r\n提示：" + result.Warning;
                        MessageBox.Show(this, messageText, "手机长截图", MessageBoxButtons.OK,
                            result.Warning.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        progressLabel.Text = "复制到剪贴板失败：" + ex.Message;
                        MessageBox.Show(this, "长图已经生成，但复制到剪贴板失败：\r\n" + ex.Message,
                            "手机长截图", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        result.Image.Dispose();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void StartNativeCapture()
        {
            bool autoSave = nativeAutoSaveBox.Checked;
            cancelRequested = false;
            isRunning = true;
            SetInputsEnabled(false);
            startButton.Enabled = false;
            fallbackButton.Enabled = false;
            cancelButton.Enabled = true;
            progressBar.Value = 5;
            progressLabel.Text = "正在调用手机系统截图…";

            Task.Factory.StartNew(delegate { return CaptureNativeLongScreenshot(autoSave); })
                .ContinueWith(delegate(Task<NativeScreenshotResult> task)
                {
                    isRunning = false;
                    SetInputsEnabled(true);
                    startButton.Enabled = true;
                    fallbackButton.Enabled = true;
                    cancelButton.Enabled = false;

                    if (task.IsFaulted)
                    {
                        string message = task.Exception.GetBaseException().Message;
                        progressLabel.Text = "失败：" + message;
                        MessageBox.Show(this, message, "手机原生长截图",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    NativeScreenshotResult result = task.Result;
                    try
                    {
                        CopyImageToClipboard(result.Image);
                        progressBar.Value = 100;
                        progressLabel.Text = "手机长图已复制到电脑剪贴板";
                        MessageBox.Show(this,
                            "手机生成的长图已复制到 Windows 剪贴板，可直接按 Ctrl+V 粘贴。" +
                            "\r\n\r\n图片尺寸：" + result.Image.Width + " × " + result.Image.Height +
                            "\r\n手机原图：" + result.PhonePath +
                            "\r\n电脑端未保存文件。",
                            "手机原生长截图", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        progressLabel.Text = "复制到剪贴板失败：" + ex.Message;
                        MessageBox.Show(this, "手机长图已经生成，但复制到电脑剪贴板失败：\r\n" + ex.Message,
                            "手机原生长截图", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        result.Image.Dispose();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private NativeScreenshotResult CaptureNativeLongScreenshot(bool autoSave)
        {
            Dictionary<string, long> baseline = ListDeviceScreenshotFiles();
            int screenWidth;
            int screenHeight;
            bool useFlymeFastPath = IsFlymeDevice();
            bool enteredByFastPath = false;
            using (Bitmap screenBeforeCapture = CaptureDeviceScreen())
            {
                screenWidth = screenBeforeCapture.Width;
                screenHeight = screenBeforeCapture.Height;

                RunAdbCommand("shell input keycombination -t 1400 KEYCODE_POWER KEYCODE_VOLUME_DOWN", 8000);
                ReportProgress(14, "已打开系统截图，正在点击“截长图”…");

                // Flyme 会从 adb screencap 中隐藏系统截图浮层，无法靠截图像素判断它是否出现。
                // 截图按键释放后直接点击已验证的 scroll_marker 区域，不再等待画面差异或 UIAutomator。
                if (useFlymeFastPath)
                {
                    int buttonX = (int)Math.Round(screenWidth * 0.500);
                    int buttonY = (int)Math.Round(screenHeight * 0.803);
                    RunAdbCommand("shell input tap " + buttonX + " " + buttonY, 5000);
                    enteredByFastPath = true;
                }
            }

            bool alreadyInLongMode = enteredByFastPath;
            if (!enteredByFastPath)
                alreadyInLongMode = EnterNativeLongScreenshot(screenWidth, screenHeight, 12000);
            if (autoSave)
            {
                ReportProgress(25, "手机正在自动滚动长截图，完成后将自动保存…");
                if (useFlymeFastPath && enteredByFastPath)
                    WaitForFlymeBottomAndSave(screenWidth, screenHeight, 150000);
                else
                    WaitForNativeScrollAndSave(screenWidth, screenHeight, 150000, alreadyInLongMode);
            }
            else
            {
                ReportProgress(25, "请在手机上手动滚动和调整范围，完成后点击“保存”…");
            }

            string phonePath;
            Bitmap longImage = WaitForSavedLongScreenshot(baseline, screenWidth, screenHeight,
                180000, out phonePath);
            return new NativeScreenshotResult { Image = longImage, PhonePath = phonePath };
        }

        private bool IsFlymeDevice()
        {
            try
            {
                string manufacturer = RunAdbText("shell getprop ro.product.manufacturer", 4000);
                if (manufacturer.IndexOf("meizu", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                string display = RunAdbText("shell getprop ro.build.display.id", 4000);
                return display.IndexOf("flyme", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private void WaitForFlymeBottomAndSave(int screenWidth, int screenHeight, int timeoutMs)
        {
            Stopwatch timer = Stopwatch.StartNew();
            bool screenshotUiSeen = false;
            int missingUiCount = 0;
            while (timer.ElapsedMilliseconds < timeoutMs && !cancelRequested)
            {
                try
                {
                    string xml = DumpUiHierarchy();
                    bool reachedBottom =
                        xml.IndexOf("已到达底部", StringComparison.Ordinal) >= 0 ||
                        xml.IndexOf("已达底部", StringComparison.Ordinal) >= 0;
                    bool screenshotUi =
                        xml.IndexOf("com.flyme.systemuiex", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        xml.IndexOf("bg_panel_save", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (reachedBottom)
                    {
                        int saveX;
                        int saveY;
                        if (!TryFindUiControlByResourceId(xml, "bg_panel_save", out saveX, out saveY))
                        {
                            saveX = (int)Math.Round(screenWidth * 0.836);
                            saveY = (int)Math.Round(screenHeight * 0.891);
                        }
                        ReportProgress(78, "检测到“已到达底部”，正在点击“保存”…");
                        RunAdbCommand("shell input tap " + saveX + " " + saveY, 5000);
                        return;
                    }

                    if (screenshotUi)
                    {
                        screenshotUiSeen = true;
                        missingUiCount = 0;
                        int progress = 25 + Math.Min(48,
                            (int)(timer.ElapsedMilliseconds * 48L / timeoutMs));
                        ReportProgress(progress, "手机系统正在自动滚动，等待“已到达底部”…");
                    }
                    else if (screenshotUiSeen)
                    {
                        // 用户可能已经手动保存；连续两次看不到截图界面后交给文件检测。
                        missingUiCount++;
                        if (missingUiCount >= 2)
                        {
                            ReportProgress(82, "检测到手机截图界面已关闭，正在读取长图…");
                            return;
                        }
                    }
                }
                catch { }
                Thread.Sleep(80);
            }

            if (cancelRequested)
            {
                try { RunAdbCommand("shell input keyevent BACK", 5000); } catch { }
                throw new OperationCanceledException("长截图已取消。");
            }
            throw new TimeoutException("等待手机显示“已到达底部”超时，未自动点击保存。你仍可在手机上手动保存。");
        }

        private bool EnterNativeLongScreenshot(int screenWidth, int screenHeight, int timeoutMs)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < timeoutMs && !cancelRequested)
            {
                try
                {
                    string xml = DumpUiHierarchy();
                    if (xml.IndexOf("轻点图片停止自动滚动", StringComparison.Ordinal) >= 0 ||
                        xml.IndexOf("上下滑动调整截图长度", StringComparison.Ordinal) >= 0)
                    {
                        ReportProgress(22, "已检测到手机进入长截图模式…");
                        return true;
                    }

                    int buttonX;
                    int buttonY;
                    if (TryFindUiControl(xml,
                        new[] { "截长图", "长截图", "滚动截屏", "长截屏" }, out buttonX, out buttonY))
                    {
                        RunAdbCommand("shell input tap " + buttonX + " " + buttonY, 5000);
                        return false;
                    }
                }
                catch { }
                Thread.Sleep(300);
            }

            if (cancelRequested)
            {
                try { RunAdbCommand("shell input keyevent BACK", 5000); } catch { }
                throw new OperationCanceledException("长截图已取消。");
            }

            // Flyme 的截图层偶尔不会及时暴露给 UIAutomator，使用已验证的相对位置后备。
            int fallbackX = (int)Math.Round(screenWidth * 0.535);
            int fallbackY = (int)Math.Round(screenHeight * 0.803);
            ReportProgress(18, "未读到按钮文字，正在使用魅族截图界面的后备位置…");
            RunAdbCommand("shell input tap " + fallbackX + " " + fallbackY, 5000);
            return false;
        }

        private bool WaitForUiControl(string[] labels, int timeoutMs, out int centerX, out int centerY)
        {
            centerX = 0;
            centerY = 0;
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < timeoutMs && !cancelRequested)
            {
                try
                {
                    string xml = DumpUiHierarchy();
                    if (TryFindUiControl(xml, labels, out centerX, out centerY))
                        return true;
                }
                catch { }
                Thread.Sleep(350);
            }
            if (cancelRequested)
            {
                try { RunAdbCommand("shell input keyevent BACK", 5000); } catch { }
                throw new OperationCanceledException("长截图已取消。");
            }
            return false;
        }

        private static bool TryFindUiControl(string xml, string[] labels, out int centerX, out int centerY)
        {
            centerX = 0;
            centerY = 0;
            foreach (Match node in Regex.Matches(xml, "<node[^>]+>"))
            {
                bool labelMatched = false;
                foreach (string label in labels)
                {
                    if (node.Value.IndexOf("text=\"" + label + "\"", StringComparison.Ordinal) >= 0 ||
                        node.Value.IndexOf("content-desc=\"" + label + "\"", StringComparison.Ordinal) >= 0)
                    {
                        labelMatched = true;
                        break;
                    }
                }
                if (!labelMatched) continue;
                Match bounds = Regex.Match(node.Value,
                    "bounds=\"\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]\"");
                if (!bounds.Success) continue;
                centerX = (int.Parse(bounds.Groups[1].Value) + int.Parse(bounds.Groups[3].Value)) / 2;
                centerY = (int.Parse(bounds.Groups[2].Value) + int.Parse(bounds.Groups[4].Value)) / 2;
                return true;
            }
            return false;
        }

        private static bool TryFindUiControlByResourceId(string xml, string resourceIdSuffix,
            out int centerX, out int centerY)
        {
            centerX = 0;
            centerY = 0;
            foreach (Match node in Regex.Matches(xml, "<node[^>]+>"))
            {
                if (node.Value.IndexOf("resource-id=\"", StringComparison.Ordinal) < 0 ||
                    node.Value.IndexOf(resourceIdSuffix, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                Match bounds = Regex.Match(node.Value,
                    "bounds=\"\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]\"");
                if (!bounds.Success) continue;
                centerX = (int.Parse(bounds.Groups[1].Value) + int.Parse(bounds.Groups[3].Value)) / 2;
                centerY = (int.Parse(bounds.Groups[2].Value) + int.Parse(bounds.Groups[4].Value)) / 2;
                return true;
            }
            return false;
        }

        private void WaitForNativeScrollAndSave(int screenWidth, int screenHeight, int timeoutMs,
            bool alreadyInLongMode)
        {
            Stopwatch timer = Stopwatch.StartNew();
            bool enteredLongScreenshot = alreadyInLongMode;
            bool retriedLongButton = false;
            int exitedUiCount = 0;

            while (timer.ElapsedMilliseconds < timeoutMs)
            {
                if (cancelRequested)
                {
                    try { RunAdbCommand("shell input keyevent BACK", 5000); } catch { }
                    throw new OperationCanceledException("长截图已取消。");
                }

                try
                {
                    string xml = DumpUiHierarchy();
                    bool isScrolling = xml.IndexOf("轻点图片停止自动滚动", StringComparison.Ordinal) >= 0;
                    bool isAdjusting =
                        xml.IndexOf("上下滑动调整截图长度", StringComparison.Ordinal) >= 0 ||
                        xml.IndexOf("已到达底部", StringComparison.Ordinal) >= 0 ||
                        xml.IndexOf("已达底部", StringComparison.Ordinal) >= 0;

                    if (isScrolling)
                    {
                        enteredLongScreenshot = true;
                        exitedUiCount = 0;
                        int progress = 25 + Math.Min(48, (int)(timer.ElapsedMilliseconds * 48L / timeoutMs));
                        ReportProgress(progress, "手机系统正在自动滚动长截图…");
                        Thread.Sleep(300);
                        continue;
                    }

                    if (isAdjusting || (enteredLongScreenshot &&
                        xml.IndexOf("保存", StringComparison.Ordinal) >= 0))
                    {
                        enteredLongScreenshot = true;
                        int saveX;
                        int saveY;
                        if (!TryFindUiControlByResourceId(xml, "bg_panel_save", out saveX, out saveY) &&
                            !TryFindUiControl(xml, new[] { "保存", "完成" }, out saveX, out saveY))
                        {
                            saveX = (int)Math.Round(screenWidth * 0.836);
                            saveY = (int)Math.Round(screenHeight * 0.891);
                        }
                        ReportProgress(78, "自动滚动已结束，正在点击“保存”…");
                        RunAdbCommand("shell input tap " + saveX + " " + saveY, 5000);
                        return;
                    }

                    // 用户可能提前手动点击“保存”；截图界面连续两次消失后直接进入文件检测。
                    if (enteredLongScreenshot &&
                        xml.IndexOf("截长图", StringComparison.Ordinal) < 0 &&
                        xml.IndexOf("保存", StringComparison.Ordinal) < 0)
                    {
                        exitedUiCount++;
                        if (exitedUiCount >= 2)
                        {
                            ReportProgress(82, "检测到手机截图界面已关闭，正在读取长图…");
                            return;
                        }
                    }
                    else
                    {
                        exitedUiCount = 0;
                    }

                    // 后备位置第一次没有点中时，再用文字坐标补点一次。
                    if (!enteredLongScreenshot && !retriedLongButton && timer.ElapsedMilliseconds > 5000)
                    {
                        int retryX;
                        int retryY;
                        if (TryFindUiControl(xml,
                            new[] { "截长图", "长截图", "滚动截屏", "长截屏" }, out retryX, out retryY))
                        {
                            RunAdbCommand("shell input tap " + retryX + " " + retryY, 5000);
                            retriedLongButton = true;
                        }
                    }
                }
                catch { }
                Thread.Sleep(400);
            }

            // 极长或无限滚动页面达到时限后，停止在当前长度并保存。
            if (enteredLongScreenshot)
            {
                ReportProgress(75, "页面较长，正在停止滚动并保存当前范围…");
                RunAdbCommand("shell input tap " + (screenWidth / 2) + " " +
                    (int)Math.Round(screenHeight * 0.45), 5000);
                Thread.Sleep(800);
                int saveX;
                int saveY;
                string finalXml = string.Empty;
                try { finalXml = DumpUiHierarchy(); } catch { }
                if (!TryFindUiControlByResourceId(finalXml, "bg_panel_save", out saveX, out saveY) &&
                    !TryFindUiControl(finalXml, new[] { "保存", "完成" }, out saveX, out saveY))
                {
                    saveX = (int)Math.Round(screenWidth * 0.836);
                    saveY = (int)Math.Round(screenHeight * 0.891);
                }
                RunAdbCommand("shell input tap " + saveX + " " + saveY, 5000);
                return;
            }

            throw new InvalidOperationException(
                "未能进入手机长截图模式。请确认截图浮层中显示“截长图”，或取消自动模式后手动操作。");
        }

        private string DumpUiHierarchy()
        {
            const string dumpPath = "/data/local/tmp/scrcpy_gui_ui.xml";
            return RunAdbText("shell uiautomator dump --compressed " + dumpPath +
                " >/dev/null 2>&1 && cat " + dumpPath, 12000);
        }

        private Bitmap WaitForSavedLongScreenshot(Dictionary<string, long> baseline,
            int screenWidth, int screenHeight, int timeoutMs, out string phonePath)
        {
            phonePath = string.Empty;
            var previousSizes = new Dictionary<string, long>(StringComparer.Ordinal);
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < timeoutMs)
            {
                if (cancelRequested)
                {
                    try { RunAdbCommand("shell input keyevent BACK", 5000); } catch { }
                    throw new OperationCanceledException("长截图已取消。");
                }

                Dictionary<string, long> current = ListDeviceScreenshotFiles();
                foreach (KeyValuePair<string, long> item in current)
                {
                    long oldSize;
                    bool changed = !baseline.TryGetValue(item.Key, out oldSize) || oldSize != item.Value;
                    if (!changed || item.Value <= 0 || Path.GetFileName(item.Key).StartsWith(".pending-"))
                        continue;

                    long previousSize;
                    bool stable = previousSizes.TryGetValue(item.Key, out previousSize) && previousSize == item.Value;
                    previousSizes[item.Key] = item.Value;
                    if (!stable) continue;

                    Bitmap candidate = null;
                    try
                    {
                        candidate = ReadDeviceImage(item.Key);
                        double normalHeight = screenHeight * (candidate.Width / (double)screenWidth);
                        // 魅族允许只比单屏多截取很短的一段，因此只要求明显高于单屏。
                        if (candidate.Height > normalHeight + 8)
                        {
                            phonePath = item.Key;
                            return candidate;
                        }
                    }
                    catch { }
                    if (candidate != null) candidate.Dispose();
                }

                int progress = 25 + Math.Min(65, (int)(timer.ElapsedMilliseconds * 65L / timeoutMs));
                ReportProgress(progress, "等待你在手机上完成长截图并点击“保存”…");
                Thread.Sleep(650);
            }
            throw new TimeoutException("等待手机保存长截图超时。请重新触发，完成后在手机上点击“保存”。");
        }

        private Dictionary<string, long> ListDeviceScreenshotFiles()
        {
            var result = new Dictionary<string, long>(StringComparer.Ordinal);
            string[] directories =
            {
                "/sdcard/Pictures/Screenshots",
                "/sdcard/DCIM/Screenshots",
                "/sdcard/DCIM/Screenshot"
            };
            foreach (string directory in directories)
            {
                try
                {
                    string output = RunAdbText("shell find " + directory +
                        " -maxdepth 1 -type f -exec stat -c %s:%n {} \\;", 12000);
                    foreach (string rawLine in output.Replace("\r", string.Empty).Split('\n'))
                    {
                        int separator = rawLine.IndexOf(':');
                        if (separator <= 0) continue;
                        long size;
                        if (!long.TryParse(rawLine.Substring(0, separator).Trim(), out size)) continue;
                        string path = rawLine.Substring(separator + 1).Trim();
                        if (path.Length > 0) result[path] = size;
                    }
                }
                catch { }
            }
            return result;
        }

        private Bitmap ReadDeviceImage(string path)
        {
            var info = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = "-s " + Quote(serial) + " exec-out cat " + Quote(path),
                WorkingDirectory = Path.GetDirectoryName(adbPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("无法启动 adb.exe。");
                using (var data = new MemoryStream())
                {
                    process.StandardOutput.BaseStream.CopyTo(data);
                    string error = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(30000))
                    {
                        process.Kill();
                        throw new TimeoutException("读取手机长图超时。");
                    }
                    if (process.ExitCode != 0 || data.Length == 0)
                        throw new InvalidOperationException("读取手机长图失败：" + error.Trim());
                    data.Position = 0;
                    using (var decoded = new Bitmap(data))
                        return new Bitmap(decoded);
                }
            }
        }

        private void SetInputsEnabled(bool enabled)
        {
            pagesBox.Enabled = enabled;
            swipeBox.Enabled = enabled;
            waitBox.Enabled = enabled;
            topCropBox.Enabled = enabled;
            bottomCropBox.Enabled = enabled;
            nativeAutoSaveBox.Enabled = enabled;
        }

        private LongScreenshotResult CaptureAndStitch(LongScreenshotSettings settings)
        {
            var frames = new List<Bitmap>();
            var warnings = new List<string>();
            bool reachedBottom = false;
            int stagnantCaptures = 0;
            try
            {
                Bitmap firstRaw = CaptureDeviceScreen();
                try { frames.Add(CropFrame(firstRaw, settings.TopCropPercent, settings.BottomCropPercent)); }
                finally { firstRaw.Dispose(); }

                int attempts = 0;
                while (frames.Count < settings.MaxFrames && !cancelRequested &&
                    attempts < settings.MaxFrames + 4)
                {
                    attempts++;
                    ReportProgress((frames.Count * 80) / settings.MaxFrames,
                        "正在滚动并截取第 " + (frames.Count + 1) + " 屏…");

                    int screenWidth = frames[0].Width;
                    int fullHeight = (int)Math.Round(frames[0].Height * 100.0 /
                        (100 - settings.TopCropPercent - settings.BottomCropPercent));
                    int x = screenWidth / 2;
                    int startY = (int)(fullHeight * 0.85);
                    int endY = (int)(fullHeight * (0.85 - settings.SwipePercent / 100.0));
                    // 较慢的拖动可抑制 Android 列表惯性，避免一次跳过尚未截取的内容。
                    RunAdbCommand("shell input swipe " + x + " " + startY + " " + x + " " + endY + " 650", 8000);
                    Thread.Sleep(settings.WaitMilliseconds);

                    Bitmap raw = CaptureDeviceScreen();
                    Bitmap next;
                    try { next = CropFrame(raw, settings.TopCropPercent, settings.BottomCropPercent); }
                    finally { raw.Dispose(); }

                    if (next.Size != frames[0].Size)
                    {
                        next.Dispose();
                        throw new InvalidOperationException("截图过程中手机方向或分辨率发生了变化，请保持方向不变后重试。");
                    }

                    if (FramesNearStagnant(frames[frames.Count - 1], next))
                    {
                        next.Dispose();
                        stagnantCaptures++;
                        if (stagnantCaptures >= 2)
                        {
                            reachedBottom = true;
                            break;
                        }
                        ReportProgress((frames.Count * 80) / settings.MaxFrames,
                            "画面未移动，正在再次确认是否到达底部…");
                        continue;
                    }

                    stagnantCaptures = 0;
                    frames.Add(next);
                }

                ReportProgress(84, "正在识别固定顶部和固定底部…");
                int fixedTop;
                int fixedBottom;
                DetectFixedRegions(frames, out fixedTop, out fixedBottom);
                int viewportHeight = frames[0].Height - fixedTop - fixedBottom;
                if (viewportHeight < frames[0].Height * 40 / 100)
                {
                    warnings.Add("固定区域识别结果异常，已改为整屏拼接。");
                    fixedTop = 0;
                    fixedBottom = 0;
                    viewportHeight = frames[0].Height;
                }

                var shifts = new List<int>();
                var shiftHistory = new List<int>();
                int expectedShift = Math.Max(1, viewportHeight * settings.SwipePercent / 100);
                for (int i = 1; i < frames.Count; i++)
                {
                    using (Bitmap previousViewport = ExtractRegion(frames[i - 1], fixedTop, viewportHeight))
                    using (Bitmap nextViewport = ExtractRegion(frames[i], fixedTop, viewportHeight))
                    {
                        double overlapScore;
                        double confidence;
                        int historyShift = MedianOfRecent(shiftHistory, 3);
                        int shift = FindVerticalShiftRobust(previousViewport, nextViewport,
                            expectedShift, historyShift, reachedBottom && i == frames.Count - 1,
                            out overlapScore, out confidence);

                        // 重复表单卡片可能产生多个相似候选。弱匹配若明显偏离历史滚动量，
                        // 使用最近三次的中位数，避免一次错误匹配吞掉整段中间内容。
                        if (historyShift > 0 && Math.Abs(shift - historyShift) > viewportHeight * 18 / 100 &&
                            overlapScore > 5.0)
                        {
                            warnings.Add("第 " + (i + 1) + " 屏匹配不唯一，已使用近期滚动距离校正。");
                            shift = historyShift;
                        }
                        shifts.Add(shift);
                        shiftHistory.Add(shift);
                        if (overlapScore > 22 || confidence < 0.004)
                            warnings.Add("第 " + (i + 1) + " 屏的滚动区重叠相似度较低，拼接处可能需要检查。");
                    }
                }

                ReportProgress(92, "正在生成长图并准备剪贴板…");
                int totalHeight = frames[0].Height;
                foreach (int shift in shifts) totalHeight += shift;
                if (totalHeight > 60000)
                    throw new InvalidOperationException("生成图片过长，请减少最大拼接屏数后重试。");

                var output = new Bitmap(frames[0].Width, totalHeight, PixelFormat.Format24bppRgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(output))
                    {
                        graphics.Clear(Color.White);
                        int targetY = 0;

                        // 固定顶部只取第一屏一次。
                        if (fixedTop > 0)
                        {
                            DrawRegion(graphics, frames[0], 0, fixedTop, targetY);
                            targetY += fixedTop;
                        }

                        // 第一屏的完整滚动视口。
                        DrawRegion(graphics, frames[0], fixedTop, viewportHeight, targetY);
                        targetY += viewportHeight;

                        // 后续屏只追加滚动视口底部的新内容，不包含固定底栏。
                        for (int i = 1; i < frames.Count; i++)
                        {
                            int shift = shifts[i - 1];
                            int sourceY = fixedTop + viewportHeight - shift;
                            DrawRegion(graphics, frames[i], sourceY, shift, targetY);
                            targetY += shift;
                        }

                        // 固定底部只取最后一屏一次。
                        if (fixedBottom > 0)
                            DrawRegion(graphics, frames[frames.Count - 1],
                                fixedTop + viewportHeight, fixedBottom, targetY);
                    }
                }
                catch
                {
                    output.Dispose();
                    throw;
                }

                if (!reachedBottom && !cancelRequested && frames.Count == settings.MaxFrames)
                    warnings.Add("已达到最大拼接屏数，页面可能仍有更多内容。可调大屏数后重新截取。");

                return new LongScreenshotResult
                {
                    Image = output,
                    FrameCount = frames.Count,
                    Width = frames[0].Width,
                    Height = totalHeight,
                    FixedTop = fixedTop,
                    FixedBottom = fixedBottom,
                    Cancelled = cancelRequested,
                    Warning = string.Join(" ", warnings.ToArray())
                };
            }
            finally
            {
                foreach (Bitmap frame in frames) frame.Dispose();
            }
        }

        private Bitmap CaptureDeviceScreen()
        {
            string arguments = "-s " + Quote(serial) + " exec-out screencap -p";
            var info = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(adbPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("无法启动 adb.exe。");
                using (var data = new MemoryStream())
                {
                    process.StandardOutput.BaseStream.CopyTo(data);
                    string error = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(15000))
                    {
                        process.Kill();
                        throw new TimeoutException("获取手机截图超时。");
                    }
                    if (process.ExitCode != 0 || data.Length == 0)
                        throw new InvalidOperationException("ADB 截图失败：" + error.Trim());
                    data.Position = 0;
                    using (var decoded = new Bitmap(data))
                        return new Bitmap(decoded);
                }
            }
        }

        private void RunAdbCommand(string command, int timeoutMs)
        {
            RunAdbText(command, timeoutMs);
        }

        private string RunAdbText(string command, int timeoutMs)
        {
            var info = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = "-s " + Quote(serial) + " " + command,
                WorkingDirectory = Path.GetDirectoryName(adbPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("无法启动 adb.exe。");
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(timeoutMs))
                {
                    process.Kill();
                    throw new TimeoutException("手机滚动命令执行超时。");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("手机滚动失败：" + (error.Length > 0 ? error : output).Trim());
                return (output + Environment.NewLine + error).Trim();
            }
        }

        private static Bitmap CropFrame(Bitmap source, int topPercent, int bottomPercent)
        {
            int top = source.Height * topPercent / 100;
            int bottom = source.Height * bottomPercent / 100;
            int height = source.Height - top - bottom;
            if (height < 100) throw new InvalidOperationException("顶部和底部裁剪比例过大。");
            var result = new Bitmap(source.Width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(result))
                graphics.DrawImage(source, new Rectangle(0, 0, result.Width, result.Height),
                    new Rectangle(0, top, source.Width, height), GraphicsUnit.Pixel);
            return result;
        }

        private static Bitmap ExtractRegion(Bitmap source, int y, int height)
        {
            var result = new Bitmap(source.Width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(result))
                graphics.DrawImage(source, new Rectangle(0, 0, result.Width, result.Height),
                    new Rectangle(0, y, source.Width, height), GraphicsUnit.Pixel);
            return result;
        }

        private static void DrawRegion(Graphics graphics, Bitmap source, int sourceY, int height, int targetY)
        {
            if (height <= 0) return;
            graphics.DrawImage(source,
                new Rectangle(0, targetY, source.Width, height),
                new Rectangle(0, sourceY, source.Width, height),
                GraphicsUnit.Pixel);
        }

        private static void DetectFixedRegions(List<Bitmap> frames, out int fixedTop, out int fixedBottom)
        {
            fixedTop = 0;
            fixedBottom = 0;
            if (frames.Count < 2) return;

            const int sampleWidth = 160;
            int sampleHeight = Math.Max(120, frames[0].Height * sampleWidth / frames[0].Width);
            var samples = new List<Bitmap>();
            try
            {
                foreach (Bitmap frame in frames)
                    samples.Add(ResizeForCompare(frame, sampleWidth, sampleHeight));

                var rowScores = new double[sampleHeight];
                for (int pair = 1; pair < samples.Count; pair++)
                {
                    Bitmap previous = samples[pair - 1];
                    Bitmap next = samples[pair];
                    for (int y = 0; y < sampleHeight; y++)
                    {
                        long difference = 0;
                        int count = 0;
                        for (int x = sampleWidth * 5 / 100; x < sampleWidth * 95 / 100; x += 4)
                        {
                            Color a = previous.GetPixel(x, y);
                            Color b = next.GetPixel(x, y);
                            difference += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                            count += 3;
                        }
                        double score = difference / (double)Math.Max(1, count);
                        // 固定边缘必须在每一对画面中都稳定，因此保留最大差异而不是平均值。
                        if (score > rowScores[y]) rowScores[y] = score;
                    }
                }

                // 五行中位数滤波，去除状态图标、光标或抗锯齿造成的单行尖峰。
                var smooth = new double[sampleHeight];
                for (int y = 0; y < sampleHeight; y++)
                {
                    var window = new List<double>();
                    for (int offset = -2; offset <= 2; offset++)
                    {
                        int row = y + offset;
                        if (row < 0 || row >= sampleHeight) continue;
                        window.Add(rowScores[row]);
                    }
                    window.Sort();
                    smooth[y] = window[window.Count / 2];
                }

                double[] sorted = (double[])smooth.Clone();
                Array.Sort(sorted);
                double movingLevel = sorted[sorted.Length * 3 / 4];
                double threshold = Math.Max(2.2, Math.Min(10.0, movingLevel * 0.24));

                int topRows = FindFixedTopBoundary(smooth, threshold, sampleHeight * 40 / 100);
                int bottomRows = FindFixedBottomBoundary(smooth, threshold, sampleHeight * 30 / 100);
                int minimum = Math.Max(4, sampleHeight * 2 / 100);
                if (topRows < minimum) topRows = 0;
                if (bottomRows < minimum) bottomRows = 0;

                fixedTop = (int)Math.Round(topRows * frames[0].Height / (double)sampleHeight);
                fixedBottom = (int)Math.Round(bottomRows * frames[0].Height / (double)sampleHeight);
            }
            finally
            {
                foreach (Bitmap sample in samples) sample.Dispose();
            }
        }

        private static int FindFixedTopBoundary(double[] scores, double threshold, int limit)
        {
            const int requiredRun = 5;
            int highRun = 0;
            for (int y = 0; y < Math.Min(limit, scores.Length); y++)
            {
                highRun = scores[y] > threshold ? highRun + 1 : 0;
                if (highRun >= requiredRun) return Math.Max(0, y - requiredRun + 1);
            }
            return 0;
        }

        private static int FindFixedBottomBoundary(double[] scores, double threshold, int limit)
        {
            const int requiredRun = 5;
            int highRun = 0;
            int minY = Math.Max(0, scores.Length - limit);
            for (int y = scores.Length - 1; y >= minY; y--)
            {
                highRun = scores[y] > threshold ? highRun + 1 : 0;
                if (highRun >= requiredRun)
                {
                    int firstMovingRowFromBottom = y + requiredRun - 1;
                    return Math.Max(0, scores.Length - firstMovingRowFromBottom - 1);
                }
            }
            return 0;
        }

        private static bool FramesNearStagnant(Bitmap previous, Bitmap next)
        {
            if (previous.Size != next.Size) return false;
            const int sampleWidth = 160;
            int sampleHeight = Math.Max(100, previous.Height * sampleWidth / previous.Width);
            using (Bitmap a = ResizeForCompare(previous, sampleWidth, sampleHeight))
            using (Bitmap b = ResizeForCompare(next, sampleWidth, sampleHeight))
            {
                int stableRows = 0;
                int rowCount = 0;
                double total = 0;
                for (int y = sampleHeight * 3 / 100; y < sampleHeight * 97 / 100; y += 2)
                {
                    double rowDifference = SampledRowDifference(a, b, y, y);
                    if (rowDifference <= 2.0) stableRows++;
                    total += rowDifference;
                    rowCount++;
                }
                if (rowCount == 0) return false;
                double stableRatio = stableRows / (double)rowCount;
                double meanDifference = total / rowCount;
                return stableRatio >= 0.90 || meanDifference <= 1.8;
            }
        }

        private static double SampledRowDifference(Bitmap previous, Bitmap next,
            int previousY, int nextY)
        {
            int startX = previous.Width * 5 / 100;
            int endX = previous.Width * 94 / 100;
            int stepX = Math.Max(2, previous.Width / 48);
            long difference = 0;
            long samples = 0;
            for (int x = startX; x < endX; x += stepX)
            {
                Color a = previous.GetPixel(x, previousY);
                Color b = next.GetPixel(x, nextY);
                difference += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                samples += 3;
            }
            return samples == 0 ? double.MaxValue : difference / (double)samples;
        }

        private static int FindVerticalShiftRobust(Bitmap previous, Bitmap next,
            int expectedShift, int historyShift, bool allowShortShift,
            out double bestScore, out double confidence)
        {
            // 设计参考 MIT 项目 scrollshot / screenStitch 的动态区域、边缘特征和重叠模板思路；
            // 此处为适配 ADB 手机纵向滚动、重复表单和单文件 WinForms 的独立实现。
            const int sampleWidth = 180;
            int sampleHeight = Math.Max(120, previous.Height * sampleWidth / previous.Width);
            using (Bitmap a = ResizeForCompare(previous, sampleWidth, sampleHeight))
            using (Bitmap b = ResizeForCompare(next, sampleWidth, sampleHeight))
            {
                double[,] edgeA = BuildEdgeMap(a);
                double[,] edgeB = BuildEdgeMap(b);
                int predictedPixels = historyShift > 0 ? historyShift : expectedShift;
                int absoluteMin = Math.Max(2, sampleHeight * 2 / 100);
                int absoluteMax = Math.Min(sampleHeight - 12, sampleHeight * 82 / 100);
                int predicted = Math.Max(absoluteMin, Math.Min(absoluteMax,
                    (int)Math.Round(predictedPixels * sampleHeight / (double)previous.Height)));
                int minShift = allowShortShift ? absoluteMin :
                    Math.Max(absoluteMin, (int)Math.Round(predicted * 0.55));
                int maxShift = Math.Min(absoluteMax, (int)Math.Round(predicted * 1.45));

                int bestShift = minShift;
                double bestRank = double.MinValue;
                double bestCorrelation = double.MinValue;
                double secondRank = double.MinValue;

                for (int shift = minShift; shift <= maxShift; shift += 2)
                {
                    double correlation = EdgeCorrelation(edgeA, edgeB, shift);
                    double proximityPenalty = 0.080 * Math.Abs(shift - predicted) / sampleHeight;
                    double rank = correlation - proximityPenalty;
                    if (rank > bestRank)
                    {
                        if (Math.Abs(shift - bestShift) >= 3) secondRank = bestRank;
                        bestRank = rank;
                        bestCorrelation = correlation;
                        bestShift = shift;
                    }
                    else if (Math.Abs(shift - bestShift) >= 3 && rank > secondRank)
                    {
                        secondRank = rank;
                    }
                }

                // 在粗搜索峰值附近逐像素细化。
                int refineStart = Math.Max(minShift, bestShift - 3);
                int refineEnd = Math.Min(maxShift, bestShift + 3);
                for (int shift = refineStart; shift <= refineEnd; shift++)
                {
                    double correlation = EdgeCorrelation(edgeA, edgeB, shift);
                    double proximityPenalty = 0.080 * Math.Abs(shift - predicted) / sampleHeight;
                    double rank = correlation - proximityPenalty;
                    if (rank > bestRank)
                    {
                        bestRank = rank;
                        bestCorrelation = correlation;
                        bestShift = shift;
                    }
                }

                bestScore = DifferenceScore(a, b, bestShift);
                confidence = secondRank == double.MinValue ? 1.0 : bestRank - secondRank;
                int result = (int)Math.Round(bestShift * previous.Height / (double)sampleHeight);

                // 边缘证据不足时，优先采用已知的滑动量，避免空白或重复卡片匹配到极端位置。
                if (bestCorrelation < 0.42 || bestScore > 36)
                {
                    int fallback = historyShift > 0 ? historyShift : expectedShift;
                    result = Math.Max(1, Math.Min(previous.Height - 1, fallback));
                }
                return Math.Max(1, Math.Min(previous.Height - 1, result));
            }
        }

        private static double[,] BuildEdgeMap(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            var gray = new double[height, width];
            var edge = new double[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = image.GetPixel(x, y);
                    gray[y, x] = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
                }
            }
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double dx = gray[y, x + 1] - gray[y, x - 1];
                    double dy = gray[y + 1, x] - gray[y - 1, x];
                    edge[y, x] = Math.Min(255.0, Math.Abs(dx) + Math.Abs(dy));
                }
            }
            return edge;
        }

        private static double EdgeCorrelation(double[,] previous, double[,] next, int shift)
        {
            int height = previous.GetLength(0);
            int width = previous.GetLength(1);
            int overlap = height - shift;
            if (overlap < 12) return -1.0;
            int startY = Math.Max(1, overlap * 3 / 100);
            int endY = Math.Min(overlap - 1, overlap * 97 / 100);
            int startX = Math.Max(1, width * 5 / 100);
            int endX = Math.Min(width - 1, width * 94 / 100);
            double sumAB = 0;
            double sumAA = 0;
            double sumBB = 0;
            for (int y = startY; y < endY; y += 2)
            {
                int previousY = y + shift;
                for (int x = startX; x < endX; x += 2)
                {
                    double a = previous[previousY, x];
                    double b = next[y, x];
                    sumAB += a * b;
                    sumAA += a * a;
                    sumBB += b * b;
                }
            }
            double denominator = Math.Sqrt(sumAA * sumBB);
            return denominator <= 0.0001 ? -1.0 : sumAB / denominator;
        }

        private static int MedianOfRecent(List<int> values, int count)
        {
            if (values.Count == 0) return 0;
            int start = Math.Max(0, values.Count - count);
            var recent = new List<int>();
            for (int i = start; i < values.Count; i++) recent.Add(values[i]);
            recent.Sort();
            return recent[recent.Count / 2];
        }

        private static Bitmap ResizeForCompare(Bitmap source, int width, int height)
        {
            var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            }
            return result;
        }

        // shift 表示上一屏内容在下一屏中向上移动的像素数。
        private static double DifferenceScore(Bitmap previous, Bitmap next, int shift)
        {
            int overlap = previous.Height - shift;
            if (overlap < previous.Height / 8) return double.MaxValue;
            int startY = Math.Max(2, previous.Height * 12 / 100);
            int endY = overlap - Math.Max(2, previous.Height * 5 / 100);
            if (endY <= startY) return double.MaxValue;
            int startX = previous.Width * 7 / 100;
            int endX = previous.Width * 93 / 100;
            int stepX = Math.Max(2, previous.Width / 32);
            int stepY = Math.Max(2, previous.Height / 120);
            long difference = 0;
            long samples = 0;
            for (int y = startY; y < endY; y += stepY)
            {
                int previousY = y + shift;
                for (int x = startX; x < endX; x += stepX)
                {
                    Color a = previous.GetPixel(x, previousY);
                    Color b = next.GetPixel(x, y);
                    difference += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                    samples += 3;
                }
            }
            return samples == 0 ? double.MaxValue : difference / (double)samples;
        }

        private static void CopyImageToClipboard(Bitmap image)
        {
            ExternalException lastError = null;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(image, true);
                    return;
                }
                catch (ExternalException ex)
                {
                    lastError = ex;
                    Thread.Sleep(120);
                }
            }
            throw new InvalidOperationException("剪贴板正被其他程序占用，请稍后重试。", lastError);
        }

        private void ReportProgress(int value, string text)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, value));
                    progressLabel.Text = text;
                });
            }
            catch { }
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private sealed class LongScreenshotSettings
        {
            public int MaxFrames;
            public int SwipePercent;
            public int WaitMilliseconds;
            public int TopCropPercent;
            public int BottomCropPercent;
        }

        private sealed class LongScreenshotResult
        {
            public Bitmap Image;
            public int FrameCount;
            public int Width;
            public int Height;
            public int FixedTop;
            public int FixedBottom;
            public bool Cancelled;
            public string Warning;
        }

        private sealed class NativeScreenshotResult
        {
            public Bitmap Image;
            public string PhonePath;
        }
    }

    internal static class TextBoxExtensions
    {
        // .NET Framework WinForms does not provide PlaceholderText, so use a tooltip instead.
        public static void PlaceholderTextCompat(this TextBox box, string text)
        {
            var tip = new ToolTip();
            tip.SetToolTip(box, text);
        }
    }
}

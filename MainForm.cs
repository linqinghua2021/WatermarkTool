using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WatermarkTool
{
    public class MainForm : Form
    {
        private TextBox txtSource, txtOutput, txtText;
        private Button btnBrowseSource, btnBrowseOutput, btnColor, btnProcess, btnShortcut;
        private ComboBox cmbFont;
        private NumericUpDown numFontSize, numAlpha, numRotation, numRows, numCols, numGapX, numGapY, numStartX, numStartY;
        private RadioButton radCenter, radTile;
        private ToggleSwitch toggleDarkMode;
        private Label lblStatus, lblEditWarning;
        private ProgressBar progressBar;
        private readonly List<AnimeCard> cards = new List<AnimeCard>();
        private AnimeCard fileCard;
        private MascotCard mascotCard;
        private HeroPanel heroPanel;

        private Color chosenColor = Color.FromArgb(98, 187, 174);
        private bool darkMode;
        private const string AppVersion = "1.2";
        private const string AppDisplayName = "文档水印工具";

        private readonly Font uiFont = new Font("Microsoft YaHei UI", 9F);
        private readonly Color coral = Color.FromArgb(238, 107, 139);
        private readonly Color mint = Color.FromArgb(98, 187, 174);
        private readonly Color violet = Color.FromArgb(112, 92, 198);

        private Color TextColor { get { return darkMode ? Color.FromArgb(239, 235, 255) : Color.FromArgb(46, 42, 68); } }
        private Color MutedColor { get { return darkMode ? Color.FromArgb(186, 180, 214) : Color.FromArgb(103, 97, 128); } }
        private Color FieldBack { get { return darkMode ? Color.FromArgb(43, 39, 60) : Color.FromArgb(255, 254, 250); } }
        private Color CardBack { get { return darkMode ? Color.FromArgb(224, 36, 32, 52) : Color.FromArgb(246, 255, 255, 250); } }
        private Color CardBorder { get { return darkMode ? Color.FromArgb(130, 112, 92, 198) : Color.FromArgb(180, 206, 220, 214); } }

        public MainForm()
        {
            Text = AppDisplayName + " v" + AppVersion + "  |  关于：MasterLin 开发";
            Size = new Size(980, 840);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            DoubleBuffered = true;
            Font = uiFont;
            string iconPath = GetAssetPath("app_icon.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            darkMode = IsSystemDarkMode();
            BuildUi();
            toggleDarkMode.Checked = darkMode;
            WireEvents();
            LoadFonts();
            ApplyTheme();
            UpdateLayoutUI();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color a = darkMode ? Color.FromArgb(29, 27, 43) : Color.FromArgb(254, 246, 241);
            Color b = darkMode ? Color.FromArgb(18, 42, 48) : Color.FromArgb(236, 250, 247);
            using (var brush = new LinearGradientBrush(ClientRectangle, a, b, LinearGradientMode.ForwardDiagonal))
                e.Graphics.FillRectangle(brush, ClientRectangle);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(darkMode ? Color.FromArgb(70, 98, 187, 174) : Color.FromArgb(58, 112, 92, 198), 2))
            {
                e.Graphics.DrawEllipse(pen, new Rectangle(780, 22, 146, 146));
                e.Graphics.DrawEllipse(pen, new Rectangle(18, 604, 96, 96));
            }
        }

        private void BuildUi()
        {
            int margin = 24;
            heroPanel = new HeroPanel(GetAssetPath("anime_header.png")) { Location = new Point(margin, 18), Size = new Size(932, 154) };
            Controls.Add(heroPanel);

            heroPanel.Controls.Add(new Label
            {
                Text = AppDisplayName,
                Location = new Point(30, 24),
                Size = new Size(300, 40),
                Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            heroPanel.Controls.Add(new Label
            {
                Text = "v" + AppVersion + " | Word / PDF 水印处理",
                Location = new Point(34, 70),
                Size = new Size(280, 24),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            heroPanel.Controls.Add(new Label
            {
                Text = "图片上方水印 | 无需 Office | 柔光主题",
                Location = new Point(34, 102),
                Size = new Size(330, 24),
                BackColor = Color.Transparent
            });
            toggleDarkMode = new ToggleSwitch
            {
                Text = "黑暗模式",
                Location = new Point(772, 94),
                Size = new Size(138, 34),
                BackColor = Color.Transparent
            };
            heroPanel.Controls.Add(toggleDarkMode);

            fileCard = AddCard("文件", margin, 192, 600, 126);
            txtSource = MakeTextBox(new Point(96, 40), new Size(376, 25));
            btnBrowseSource = MakeButton("浏览", new Point(488, 36), new Size(84, 32), false);
            txtOutput = MakeTextBox(new Point(96, 80), new Size(376, 25));
            btnBrowseOutput = MakeButton("另存为", new Point(488, 76), new Size(84, 32), false);
            fileCard.Controls.AddRange(new Control[] {
                MakeLabel("源文件", 24, 44, 64), txtSource, btnBrowseSource,
                MakeLabel("输出到", 24, 84, 64), txtOutput, btnBrowseOutput,
                MakeLabel("可直接拖入 Word/PDF", 388, 14, 190)
            });

            mascotCard = new MascotCard(GetAssetPath("anime_mascot.png")) { Location = new Point(648, 192), Size = new Size(308, 356) };
            Controls.Add(mascotCard);

            var textCard = AddCard("水印文字", margin, 334, 600, 140);
            txtText = MakeTextBox(new Point(96, 42), new Size(476, 54));
            txtText.Multiline = true;
            txtText.ScrollBars = ScrollBars.Vertical;
            txtText.Text = "此资料仅供项目使用\r\n复印无效";
            cmbFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(96, 106), Size = new Size(210, 26), Font = uiFont };
            numFontSize = MakeNumber(new Point(386, 106), 5, 300, 22);
            textCard.Controls.AddRange(new Control[] {
                MakeLabel("文字", 24, 48, 64), txtText,
                MakeLabel("字体", 24, 110, 64), cmbFont,
                MakeLabel("字号", 338, 110, 42), numFontSize
            });

            var settingsCard = AddCard("外观与布局", margin, 490, 600, 218);
            settingsCard.Controls.AddRange(new Control[] {
                MakeSectionLabel("外观", 24, 42),
                MakeSectionLabel("布局", 302, 42)
            });

            btnColor = MakeButton("更换颜色", new Point(94, 72), new Size(112, 32), false);
            numAlpha = MakeNumber(new Point(94, 116), 0, 255, 128);
            numRotation = MakeNumber(new Point(94, 158), -180, 180, -45);
            settingsCard.Controls.AddRange(new Control[] {
                MakeLabel("颜色", 24, 78, 58), btnColor,
                MakeLabel("透明度", 24, 120, 58), numAlpha,
                MakeLabel("旋转", 24, 162, 58), numRotation
            });

            radCenter = new RadioButton { Text = "居中", Location = new Point(356, 70), Size = new Size(72, 24), Checked = false, BackColor = Color.Transparent };
            radTile = new RadioButton { Text = "平铺", Location = new Point(438, 70), Size = new Size(72, 24), Checked = true, BackColor = Color.Transparent };
            numRows = MakeNumber(new Point(360, 104), 1, 20, 3);
            numCols = MakeNumber(new Point(516, 104), 1, 20, 2);
            numGapX = MakeNumber(new Point(360, 138), 10, 2000, 250);
            numGapY = MakeNumber(new Point(516, 138), 10, 2000, 280);
            numStartX = MakeNumber(new Point(360, 172), -1000, 2000, 0);
            numStartY = MakeNumber(new Point(516, 172), -1000, 2000, 100);
            settingsCard.Controls.AddRange(new Control[] {
                radCenter, radTile,
                MakeLabel("行数", 302, 108, 50), numRows,
                MakeLabel("列数", 458, 108, 50), numCols,
                MakeLabel("左右距", 302, 142, 50), numGapX,
                MakeLabel("上下距", 458, 142, 50), numGapY,
                MakeLabel("左偏移", 302, 176, 50), numStartX,
                MakeLabel("上偏移", 458, 176, 50), numStartY
            });

            lblEditWarning = new Label
            {
                Text = "提示：加水印后的文档主要用于分发/打印，可能不适合继续编辑，请保留源文件。",
                Location = new Point(672, 560),
                Size = new Size(260, 54),
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 8.5F)
            };
            Controls.Add(lblEditWarning);

            btnProcess = MakeButton("开始添加水印", new Point(672, 622), new Size(260, 46), true);
            btnProcess.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            Controls.Add(btnProcess);

            btnShortcut = MakeButton("添加桌面快捷方式", new Point(672, 682), new Size(260, 38), false);
            Controls.Add(btnShortcut);

            progressBar = new ProgressBar { Location = new Point(margin, 732), Size = new Size(932, 16) };
            lblStatus = new Label { Location = new Point(margin, 752), Size = new Size(932, 24), Text = "准备就绪", BackColor = Color.Transparent };
            Controls.AddRange(new Control[] { progressBar, lblStatus });
        }

        private AnimeCard AddCard(string title, int x, int y, int w, int h)
        {
            var card = new AnimeCard(title) { Location = new Point(x, y), Size = new Size(w, h) };
            cards.Add(card);
            Controls.Add(card);
            return card;
        }

        private string GetAssetPath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", fileName);
        }

        private void WireEvents()
        {
            btnBrowseSource.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "Word/PDF (*.docx;*.pdf)|*.docx;*.pdf|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf|所有文件 (*.*)|*.*" })
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        txtSource.Text = dlg.FileName;
                        AutoSetOutput();
                        PromptReadyToStart();
                    }
            };
            btnBrowseOutput.Click += (s, e) =>
            {
                string ext = Path.GetExtension(txtSource.Text).ToLower();
                string filter = ext == ".pdf" ? "PDF (*.pdf)|*.pdf" : "Word (*.docx)|*.docx";
                using (var dlg = new SaveFileDialog { Filter = filter, FileName = txtOutput.Text })
                    if (dlg.ShowDialog() == DialogResult.OK) txtOutput.Text = dlg.FileName;
            };
            btnColor.Click += (s, e) =>
            {
                using (var dlg = new ColorDialog { Color = chosenColor, FullOpen = true })
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        chosenColor = dlg.Color;
                        UpdateColorButton();
                    }
            };
            toggleDarkMode.CheckedChanged += (s, e) =>
            {
                darkMode = toggleDarkMode.Checked;
                ApplyTheme();
            };
            radCenter.CheckedChanged += (s, e) => UpdateLayoutUI();
            radTile.CheckedChanged += (s, e) => UpdateLayoutUI();
            btnProcess.Click += BtnProcess_Click;
            btnShortcut.Click += BtnShortcut_Click;
            txtSource.TextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtOutput.Text)) AutoSetOutput();
                if (File.Exists(txtSource.Text)) PromptReadyToStart();
            };

            EnableFileDrop(this);
            EnableFileDrop(fileCard);
            EnableFileDrop(txtSource);
        }

        private void EnableFileDrop(Control control)
        {
            control.AllowDrop = true;
            control.DragEnter += (s, e) =>
            {
                e.Effect = GetDroppedSupportedFile(e.Data) == null ? DragDropEffects.None : DragDropEffects.Copy;
            };
            control.DragDrop += (s, e) =>
            {
                string file = GetDroppedSupportedFile(e.Data);
                if (file == null)
                {
                    lblStatus.Text = "请拖入 .docx 或 .pdf 文件。";
                    return;
                }
                txtSource.Text = file;
                AutoSetOutput();
                PromptReadyToStart();
            };
        }

        private string GetDroppedSupportedFile(IDataObject data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop)) return null;
            var files = data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return null;
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (File.Exists(file) && (ext == ".docx" || ext == ".pdf")) return file;
            }
            return null;
        }

        private void LoadFonts()
        {
            foreach (var ff in FontFamily.Families) cmbFont.Items.Add(ff.Name);
            if (cmbFont.Items.Contains("Microsoft YaHei UI")) cmbFont.SelectedItem = "Microsoft YaHei UI";
            else if (cmbFont.Items.Contains("微软雅黑")) cmbFont.SelectedItem = "微软雅黑";
            else if (cmbFont.Items.Count > 0) cmbFont.SelectedIndex = 0;
        }

        private void ApplyTheme()
        {
            BackColor = darkMode ? Color.FromArgb(29, 27, 43) : Color.FromArgb(248, 249, 252);
            foreach (var card in cards) card.SetTheme(darkMode, CardBack, CardBorder, mint, violet);
            if (heroPanel != null) heroPanel.DarkMode = darkMode;
            if (mascotCard != null) mascotCard.SetTheme(darkMode);

            ApplyThemeToControls(Controls);
            UpdateColorButton();
            Invalidate(true);
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is Label)
                {
                    c.ForeColor = c.Parent == heroPanel && c.Text.Contains("Word") ? violet : (c.Parent == heroPanel && c.Text.Contains("图片") ? MutedColor : TextColor);
                    c.BackColor = Color.Transparent;
                }
                else if (c is TextBox)
                {
                    c.ForeColor = TextColor;
                    c.BackColor = FieldBack;
                }
                else if (c is ComboBox || c is NumericUpDown)
                {
                    c.ForeColor = TextColor;
                    c.BackColor = FieldBack;
                }
                else if (c is RadioButton)
                {
                    c.ForeColor = TextColor;
                    c.BackColor = Color.Transparent;
                }
                else if (c is ToggleSwitch)
                {
                    ((ToggleSwitch)c).SetTheme(darkMode, mint, coral, violet, TextColor, MutedColor);
                }

                if (c == lblStatus || c == lblEditWarning) c.ForeColor = MutedColor;
                if (c == btnBrowseSource || c == btnBrowseOutput) ThemeButton((Button)c, false);
                if (c == btnProcess) ThemeButton((Button)c, true);
                if (c == btnShortcut) ThemeButton((Button)c, false);
                if (c.HasChildren) ApplyThemeToControls(c.Controls);
            }
        }

        private bool IsSystemDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value == 0;
                }
            }
            catch
            {
            }
            return false;
        }

        private Label MakeLabel(string text, int x, int y, int width)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 20), BackColor = Color.Transparent, Font = uiFont };
        }

        private Label MakeSectionLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(120, 22),
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
        }

        private TextBox MakeTextBox(Point location, Size size)
        {
            return new TextBox { Location = location, Size = size, BorderStyle = BorderStyle.FixedSingle, Font = uiFont };
        }

        private NumericUpDown MakeNumber(Point location, int min, int max, int value)
        {
            return new NumericUpDown { Location = location, Size = new Size(72, 24), Minimum = min, Maximum = max, Value = value, DecimalPlaces = 0, Font = uiFont };
        }

        private Button MakeButton(string text, Point location, Size size, bool primary)
        {
            var btn = new Button { Text = text, TextAlign = ContentAlignment.MiddleCenter, Location = location, Size = size, FlatStyle = FlatStyle.Flat, Font = uiFont, Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
            ThemeButton(btn, primary);
            return btn;
        }

        private void ThemeButton(Button btn, bool primary)
        {
            if (btn == btnColor) return;
            btn.ForeColor = primary ? Color.White : (darkMode ? Color.FromArgb(220, 214, 255) : violet);
            btn.BackColor = primary ? coral : (darkMode ? Color.FromArgb(48, 43, 67) : Color.FromArgb(255, 252, 246));
            btn.FlatAppearance.BorderSize = primary ? 0 : 1;
            btn.FlatAppearance.BorderColor = darkMode ? Color.FromArgb(94, 82, 132) : Color.FromArgb(210, 223, 218);
            btn.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(220, 83, 119) : (darkMode ? Color.FromArgb(58, 53, 78) : Color.FromArgb(238, 248, 245));
            btn.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(188, 64, 99) : (darkMode ? Color.FromArgb(67, 61, 91) : Color.FromArgb(225, 240, 236));
        }

        private void UpdateColorButton()
        {
            if (btnColor == null) return;
            btnColor.UseVisualStyleBackColor = false;
            btnColor.BackColor = chosenColor;
            btnColor.ForeColor = ReadableText(chosenColor);
            btnColor.FlatAppearance.BorderSize = 2;
            btnColor.FlatAppearance.BorderColor = darkMode ? Color.FromArgb(235, 230, 255) : Color.FromArgb(44, 38, 64);
            btnColor.FlatAppearance.MouseOverBackColor = chosenColor;
            btnColor.FlatAppearance.MouseDownBackColor = chosenColor;
        }

        private Color ReadableText(Color color)
        {
            double brightness = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
            return brightness < 150 ? Color.White : Color.FromArgb(44, 38, 64);
        }

        private void UpdateLayoutUI()
        {
            bool tiled = radTile.Checked;
            numRows.Enabled = tiled;
            numCols.Enabled = tiled;
            numGapX.Enabled = tiled;
            numGapY.Enabled = tiled;
            numStartX.Enabled = tiled;
            numStartY.Enabled = tiled;
        }

        private void AutoSetOutput()
        {
            if (string.IsNullOrEmpty(txtSource.Text)) return;
            string dir = Path.GetDirectoryName(txtSource.Text);
            string name = Path.GetFileNameWithoutExtension(txtSource.Text);
            string ext = Path.GetExtension(txtSource.Text);
            if (ext.Equals(".doc", StringComparison.OrdinalIgnoreCase)) ext = ".docx";
            txtOutput.Text = Path.Combine(dir, name + "_水印" + ext);
        }

        private void PromptReadyToStart()
        {
            if (lblStatus != null)
            {
                string name = Path.GetFileName(txtSource.Text);
                lblStatus.Text = "已选择文件：" + name + "，请点击“开始添加水印”。";
            }
        }

        private void BtnShortcut_Click(object sender, EventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktop, AppDisplayName + "_v" + AppVersion + ".lnk");
                string iconPath = GetAssetPath("app_icon.ico");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Application.ExecutablePath });
                shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { AppDomain.CurrentDomain.BaseDirectory });
                shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { AppDisplayName + " v" + AppVersion });
                if (File.Exists(iconPath))
                    shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
                shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);

                lblStatus.Text = "桌面快捷方式已创建：" + shortcutPath;
                MessageBox.Show("桌面快捷方式已创建。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "创建快捷方式失败";
                MessageBox.Show("创建快捷方式失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtSource.Text)) { MessageBox.Show("源文件不存在。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (string.IsNullOrEmpty(txtOutput.Text)) { MessageBox.Show("请指定输出路径。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (string.IsNullOrWhiteSpace(txtText.Text)) { MessageBox.Show("水印文字不能为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            var settings = new WatermarkSettings
            {
                Text = txtText.Text,
                FontName = (cmbFont.SelectedItem != null ? cmbFont.SelectedItem.ToString() : "Microsoft YaHei UI"),
                FontSize = (float)numFontSize.Value,
                FontColor = chosenColor,
                Transparency = (int)numAlpha.Value,
                Rotation = (float)numRotation.Value,
                IsTiled = radTile.Checked,
                Rows = (int)numRows.Value,
                Cols = (int)numCols.Value,
                GapX = (float)numGapX.Value,
                GapY = (float)numGapY.Value,
                StartX = (float)numStartX.Value,
                StartY = (float)numStartY.Value
            };

            string ext = Path.GetExtension(txtSource.Text).ToLower();
            lblStatus.Text = "正在处理，请稍候...";
            progressBar.Style = ProgressBarStyle.Marquee;
            btnProcess.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                if (ext == ".docx") DocxWatermarker.Apply(txtSource.Text, txtOutput.Text, settings);
                else if (ext == ".pdf") PdfWatermarker.Apply(txtSource.Text, txtOutput.Text, settings);
                else throw new Exception("不支持的文件格式。请选择 .docx 或 .pdf 文件。");

                lblStatus.Text = "完成：" + txtOutput.Text + "。请保留源文件，输出文档可能不适合继续编辑。";
                MessageBox.Show("水印添加成功！\n\n输出文件：\n" + txtOutput.Text + "\n\n提示：加水印后的文档主要用于分发/打印，可能不适合继续编辑，请保留源文件。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "处理失败";
                MessageBox.Show("处理时出错：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                btnProcess.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }

    internal class HeroPanel : Panel
    {
        private readonly Image image;
        public bool DarkMode { get; set; }

        public HeroPanel(string imagePath)
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            if (File.Exists(imagePath)) image = Image.FromFile(imagePath);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = UiShapes.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 22))
            {
                e.Graphics.SetClip(path);
                if (image != null)
                {
                    float scale = Math.Max(Width / (float)image.Width, Height / (float)image.Height);
                    int drawW = (int)(image.Width * scale);
                    int drawH = (int)(image.Height * scale);
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(image, new Rectangle(Width - drawW, (Height - drawH) / 2, drawW, drawH));
                }
                else
                {
                    using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(255, 244, 239), Color.FromArgb(230, 247, 242), LinearGradientMode.Horizontal))
                        e.Graphics.FillRectangle(brush, ClientRectangle);
                }

                Color veilA = DarkMode ? Color.FromArgb(216, 24, 22, 36) : Color.FromArgb(245, 255, 250, 244);
                Color veilB = DarkMode ? Color.FromArgb(70, 24, 22, 36) : Color.FromArgb(25, 255, 255, 255);
                using (var veil = new LinearGradientBrush(ClientRectangle, veilA, veilB, LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(veil, ClientRectangle);
                e.Graphics.ResetClip();
                using (var pen = new Pen(DarkMode ? Color.FromArgb(110, 112, 92, 198) : Color.FromArgb(170, 255, 255, 255), 1))
                    e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal class MascotCard : Panel
    {
        private readonly Image mascot;
        private bool darkMode;

        public MascotCard(string imagePath)
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            if (File.Exists(imagePath)) mascot = Image.FromFile(imagePath);
        }

        public void SetTheme(bool dark)
        {
            darkMode = dark;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color a = darkMode ? Color.FromArgb(236, 39, 35, 57) : Color.FromArgb(252, 255, 251, 244);
            Color b = darkMode ? Color.FromArgb(236, 30, 43, 54) : Color.FromArgb(252, 235, 249, 255);
            using (var path = UiShapes.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 20))
            using (var brush = new LinearGradientBrush(ClientRectangle, a, b, LinearGradientMode.Vertical))
            using (var pen = new Pen(darkMode ? Color.FromArgb(130, 112, 92, 198) : Color.FromArgb(180, 206, 220, 214), 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            if (mascot != null)
            {
                var rect = new Rectangle(34, 16, Width - 68, Width - 68);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(mascot, rect);
            }

            using (var font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold))
            using (var brush = new SolidBrush(darkMode ? Color.FromArgb(239, 235, 255) : Color.FromArgb(46, 42, 68)))
                e.Graphics.DrawString("水印小助手", font, brush, new PointF(28, 270));
            using (var font = new Font("Microsoft YaHei UI", 9F))
            using (var brush = new SolidBrush(darkMode ? Color.FromArgb(186, 180, 214) : Color.FromArgb(103, 97, 128)))
                e.Graphics.DrawString("文件处理已准备好", font, brush, new PointF(30, 306));
        }
    }

    internal class AnimeCard : Panel
    {
        private readonly string title;
        private Color cardBack = Color.FromArgb(246, 255, 255, 250);
        private Color border = Color.FromArgb(180, 206, 220, 214);
        private Color tagA = Color.FromArgb(98, 187, 174);
        private Color tagB = Color.FromArgb(112, 92, 198);

        public AnimeCard(string title)
        {
            this.title = title;
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Padding = new Padding(12, 34, 12, 12);
        }

        public void SetTheme(bool dark, Color back, Color borderColor, Color accentA, Color accentB)
        {
            cardBack = back;
            border = borderColor;
            tagA = accentA;
            tagB = accentB;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 10, Width - 1, Height - 11);
            using (var path = UiShapes.RoundRect(rect, 16))
            using (var brush = new SolidBrush(cardBack))
            using (var pen = new Pen(border, 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            using (var tag = UiShapes.RoundRect(new Rectangle(18, 0, 138, 30), 15))
            using (var brush = new LinearGradientBrush(new Rectangle(18, 0, 138, 30), tagA, tagB, LinearGradientMode.Horizontal))
                e.Graphics.FillPath(brush, tag);

            using (var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
                e.Graphics.DrawString(title, font, brush, new PointF(34, 6));
        }
    }

    internal class ToggleSwitch : Control
    {
        private bool isChecked;
        private bool darkMode;
        private Color mint = Color.FromArgb(98, 187, 174);
        private Color coral = Color.FromArgb(238, 107, 139);
        private Color violet = Color.FromArgb(112, 92, 198);
        private Color textColor = Color.FromArgb(46, 42, 68);
        private Color mutedColor = Color.FromArgb(103, 97, 128);

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public void SetTheme(bool dark, Color mintColor, Color coralColor, Color violetColor, Color text, Color muted)
        {
            darkMode = dark;
            mint = mintColor;
            coral = coralColor;
            violet = violetColor;
            textColor = text;
            mutedColor = muted;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var track = new Rectangle(0, 3, 60, Height - 7);
            Color trackA = Checked ? violet : (darkMode ? Color.FromArgb(64, 58, 84) : Color.FromArgb(244, 241, 250));
            Color trackB = Checked ? mint : (darkMode ? Color.FromArgb(48, 43, 67) : Color.FromArgb(232, 244, 241));
            using (var path = UiShapes.RoundRect(track, track.Height / 2))
            using (var brush = new LinearGradientBrush(track, trackA, trackB, LinearGradientMode.Horizontal))
            using (var pen = new Pen(Checked ? Color.FromArgb(180, 255, 255, 255) : (darkMode ? Color.FromArgb(104, 94, 138) : Color.FromArgb(210, 223, 218)), 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            int knobSize = Height - 12;
            int knobX = Checked ? track.Right - knobSize - 4 : track.Left + 4;
            var knob = new Rectangle(knobX, 6, knobSize, knobSize);
            using (var brush = new SolidBrush(Checked ? coral : Color.FromArgb(255, 252, 246)))
            using (var pen = new Pen(Checked ? Color.FromArgb(255, 220, 232) : Color.FromArgb(220, 210, 235), 1))
            {
                e.Graphics.FillEllipse(brush, knob);
                e.Graphics.DrawEllipse(pen, knob);
            }

            using (var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(Checked ? textColor : mutedColor))
            {
                e.Graphics.DrawString(Text, font, brush, new PointF(70, 8));
            }
        }
    }

    internal static class UiShapes
    {
        public static GraphicsPath RoundRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

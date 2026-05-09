$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

function New-AppName {
    -join ([char[]]@(0x6587, 0x6863, 0x6C34, 0x5370, 0x5DE5, 0x5177))
}

$appName = New-AppName
$appVersion = "1.2"
$appExeName = "$appName`_v$appVersion.exe"
$setupExeName = "$appName`_v$appVersion`_Setup.exe"
$setupExePath = Join-Path $scriptDir $setupExeName

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw ".NET Framework C# compiler was not found."
}

$frameworkDir = Split-Path -Parent $csc
$wpfLibDir = Join-Path $frameworkDir "WPF"

Write-Host "Building app: $appExeName" -ForegroundColor Cyan

$compileArgs = @(
    "/nologo",
    "/codepage:65001",
    "/target:winexe",
    "/win32icon:assets\app_icon.ico",
    "/out:$appExeName",
    "/lib:$wpfLibDir",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Xml.dll",
    "/r:System.Xml.Linq.dll",
    "/r:System.Core.dll",
    "/r:WindowsBase.dll",
    "/r:PdfSharp.dll",
    "Program.cs",
    "WatermarkSettings.cs",
    "WatermarkBitmapGenerator.cs",
    "DocxWatermarker.cs",
    "PdfWatermarker.cs",
    "MainForm.cs"
)

& $csc @compileArgs
if ($LASTEXITCODE -ne 0) {
    throw "Application compilation failed."
}

$buildDir = Join-Path $scriptDir ".installer_build"
if (Test-Path $buildDir) {
    Remove-Item -LiteralPath $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir | Out-Null

$installerSource = Join-Path $buildDir "WatermarkToolInstaller.cs"

$payload = @(
    @{ Source = $appExeName; Target = $appExeName; Resource = "payload_app_exe" },
    @{ Source = "PdfSharp.dll"; Target = "PdfSharp.dll"; Resource = "payload_pdfsharp" },
    @{ Source = "System.IO.Compression.dll"; Target = "System.IO.Compression.dll"; Resource = "payload_compression" },
    @{ Source = "System.IO.Compression.FileSystem.dll"; Target = "System.IO.Compression.FileSystem.dll"; Resource = "payload_compression_fs" },
    @{ Source = "assets\anime_header.png"; Target = "assets\anime_header.png"; Resource = "payload_anime_header" },
    @{ Source = "assets\anime_icons.png"; Target = "assets\anime_icons.png"; Resource = "payload_anime_icons" },
    @{ Source = "assets\anime_mascot.png"; Target = "assets\anime_mascot.png"; Resource = "payload_anime_mascot" },
    @{ Source = "assets\app_icon.ico"; Target = "assets\app_icon.ico"; Resource = "payload_app_icon" }
)

foreach ($item in $payload) {
    $sourcePath = Join-Path $scriptDir $item.Source
    if (-not (Test-Path $sourcePath)) {
        throw "Required payload file is missing: $($item.Source)"
    }
}

$payloadLines = ($payload | ForEach-Object {
    '            new PayloadFile("' + $_.Resource + '", "' + ($_.Target -replace '\\', '\\') + '"),'
}) -join "`r`n"

$installerCode = @"
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class InstallerProgram
{
    private const string AppVersion = "$appVersion";

    private static readonly PayloadFile[] PayloadFiles = new PayloadFile[]
    {
$payloadLines
    };

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }

    private static string NewAppName()
    {
        return new string(new char[] { '\u6587', '\u6863', '\u6c34', '\u5370', '\u5de5', '\u5177' });
    }

    private sealed class InstallerForm : Form
    {
        private readonly string appName = NewAppName();
        private readonly string defaultInstallDir;
        private TextBox txtInstallDir;
        private Button btnBrowse;
        private Button btnInstall;
        private Button btnCancel;
        private Label lblStatus;
        private ProgressBar progressBar;

        public InstallerForm()
        {
            defaultInstallDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                appName);

            Text = appName + " v" + AppVersion + " Setup";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 388);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(248, 250, 252);
            Icon = LoadInstallerIcon();

            BuildUi();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Rectangle(0, 0, ClientSize.Width, 150),
                Color.FromArgb(28, 92, 114),
                Color.FromArgb(86, 126, 176),
                LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, 0, 0, ClientSize.Width, 150);
            }

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
            {
                e.Graphics.FillEllipse(brush, ClientSize.Width - 180, -60, 240, 240);
                e.Graphics.FillEllipse(brush, 390, 70, 170, 170);
            }
        }

        private void BuildUi()
        {
            Label title = new Label
            {
                Text = appName,
                Location = new Point(34, 28),
                Size = new Size(420, 36),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold)
            };
            Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "v" + AppVersion + " \u5b89\u88c5\u7a0b\u5e8f",
                Location = new Point(38, 70),
                Size = new Size(420, 24),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(230, 245, 250, 255),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            Controls.Add(subtitle);

            Label hint = new Label
            {
                Text = "\u8bf7\u9009\u62e9\u5b89\u88c5\u76ee\u5f55\uff0c\u5b89\u88c5\u5668\u4f1a\u521b\u5efa\u684c\u9762\u548c\u5f00\u59cb\u83dc\u5355\u5feb\u6377\u65b9\u5f0f\uff0c\u5e76\u6ce8\u518c\u5378\u8f7d\u9879\u3002",
                Location = new Point(38, 103),
                Size = new Size(520, 24),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(220, 255, 255, 255)
            };
            Controls.Add(hint);

            Panel panel = new Panel
            {
                Location = new Point(34, 172),
                Size = new Size(552, 140),
                BackColor = Color.White
            };
            panel.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.FromArgb(218, 226, 235)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            Controls.Add(panel);

            Label installTo = new Label
            {
                Text = "\u5b89\u88c5\u4f4d\u7f6e",
                Location = new Point(22, 20),
                Size = new Size(120, 22),
                ForeColor = Color.FromArgb(42, 52, 67),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            panel.Controls.Add(installTo);

            txtInstallDir = new TextBox
            {
                Location = new Point(24, 52),
                Size = new Size(392, 26),
                Text = defaultInstallDir,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(txtInstallDir);

            btnBrowse = MakeButton("\u6d4f\u89c8...", new Point(428, 50), new Size(96, 30), false);
            btnBrowse.Click += Browse_Click;
            panel.Controls.Add(btnBrowse);

            lblStatus = new Label
            {
                Text = "\u51c6\u5907\u5b89\u88c5",
                Location = new Point(24, 96),
                Size = new Size(500, 22),
                ForeColor = Color.FromArgb(88, 100, 116)
            };
            panel.Controls.Add(lblStatus);

            progressBar = new ProgressBar
            {
                Location = new Point(34, 326),
                Size = new Size(552, 10),
                Style = ProgressBarStyle.Blocks
            };
            Controls.Add(progressBar);

            btnCancel = MakeButton("\u53d6\u6d88", new Point(390, 350), new Size(92, 32), false);
            btnCancel.Click += delegate { Close(); };
            Controls.Add(btnCancel);

            btnInstall = MakeButton("\u5b89\u88c5", new Point(494, 350), new Size(92, 32), true);
            btnInstall.Click += Install_Click;
            Controls.Add(btnInstall);
        }

        private Button MakeButton(string text, Point location, Size size, bool primary)
        {
            Button button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(190, 202, 216);
            button.BackColor = primary ? Color.FromArgb(48, 112, 168) : Color.White;
            button.ForeColor = primary ? Color.White : Color.FromArgb(42, 52, 67);
            return button;
        }

        private Icon LoadInstallerIcon()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload_app_icon"))
                {
                    if (stream == null) return null;
                    return new Icon(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "\u9009\u62e9\u5b89\u88c5\u76ee\u5f55";
                dialog.SelectedPath = Directory.Exists(txtInstallDir.Text) ? txtInstallDir.Text : Path.GetDirectoryName(defaultInstallDir);
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtInstallDir.Text = Path.Combine(dialog.SelectedPath, appName);
                }
            }
        }

        private void Install_Click(object sender, EventArgs e)
        {
            string installDir = txtInstallDir.Text.Trim();
            if (installDir.Length == 0)
            {
                MessageBox.Show(this, "\u8bf7\u9009\u62e9\u5b89\u88c5\u76ee\u5f55\u3002", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToggleInstalling(true);

            try
            {
                Directory.CreateDirectory(installDir);
                Directory.CreateDirectory(Path.Combine(installDir, "assets"));

                progressBar.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "\u6b63\u5728\u590d\u5236\u7a0b\u5e8f\u6587\u4ef6...";
                Refresh();

                foreach (PayloadFile file in PayloadFiles)
                {
                    WriteResource(file.ResourceName, Path.Combine(installDir, file.TargetPath));
                }

                lblStatus.Text = "\u6b63\u5728\u521b\u5efa\u5feb\u6377\u65b9\u5f0f\u548c\u5378\u8f7d\u9879...";
                Refresh();

                string exeName = appName + "_v" + AppVersion + ".exe";
                string exePath = Path.Combine(installDir, exeName);
                string iconPath = Path.Combine(installDir, "assets", "app_icon.ico");
                CreateShortcut(appName, exePath, installDir, iconPath);
                WriteUninstaller(appName, installDir, exePath);

                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = 100;
                lblStatus.Text = "\u5b89\u88c5\u5b8c\u6210";
                MessageBox.Show(this, "\u5b89\u88c5\u5b8c\u6210\u3002\u684c\u9762\u548c\u5f00\u59cb\u83dc\u5355\u5feb\u6377\u65b9\u5f0f\u5df2\u521b\u5efa\u3002", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                lblStatus.Text = "\u5b89\u88c5\u5931\u8d25";
                ToggleInstalling(false);
                MessageBox.Show(this, "\u5b89\u88c5\u5931\u8d25\uff1a" + Environment.NewLine + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleInstalling(bool installing)
        {
            txtInstallDir.Enabled = !installing;
            btnBrowse.Enabled = !installing;
            btnInstall.Enabled = !installing;
            btnCancel.Enabled = !installing;
            Cursor = installing ? Cursors.WaitCursor : Cursors.Default;
        }
    }

    private static void WriteResource(string resourceName, string outputPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream input = assembly.GetManifestResourceStream(resourceName))
        {
            if (input == null) throw new InvalidOperationException("Missing installer resource: " + resourceName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (FileStream output = File.Create(outputPath))
            {
                input.CopyTo(output);
            }
        }
    }

    private static void CreateShortcut(string appName, string exePath, string installDir, string iconPath)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);

        string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), appName + ".lnk");
        SaveShortcut(shellType, shell, desktopShortcut, exePath, installDir, iconPath, appName);

        string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), appName);
        Directory.CreateDirectory(startMenuDir);
        SaveShortcut(shellType, shell, Path.Combine(startMenuDir, appName + ".lnk"), exePath, installDir, iconPath, appName);
    }

    private static void SaveShortcut(Type shellType, object shell, string shortcutPath, string exePath, string installDir, string iconPath, string appName)
    {
        object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
        Type shortcutType = shortcut.GetType();
        shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { exePath });
        shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { installDir });
        shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { appName + " v" + AppVersion });
        if (File.Exists(iconPath))
        {
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
        }
        shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
    }

    private static void WriteUninstaller(string appName, string installDir, string exePath)
    {
        string uninstallScript = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp",
            "uninstall_" + Guid.NewGuid().ToString("N") + ".cmd");
        string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), appName + ".lnk");
        string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), appName);
        string regKey = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\" + appName;

        string script =
            "@echo off\r\n" +
            "timeout /t 1 /nobreak >nul\r\n" +
            "del \"" + desktopShortcut + "\" >nul 2>nul\r\n" +
            "rmdir /s /q \"" + startMenuDir + "\" >nul 2>nul\r\n" +
            "reg delete \"" + regKey + "\" /f >nul 2>nul\r\n" +
            "rmdir /s /q \"" + installDir + "\" >nul 2>nul\r\n" +
            "del \"%~f0\" >nul 2>nul\r\n";
        File.WriteAllText(uninstallScript, script, Encoding.Default);

        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + appName);
        if (key == null) return;
        using (key)
        {
            key.SetValue("DisplayName", appName, RegistryValueKind.String);
            key.SetValue("DisplayVersion", AppVersion, RegistryValueKind.String);
            key.SetValue("Publisher", "MasterLin", RegistryValueKind.String);
            key.SetValue("DisplayIcon", exePath, RegistryValueKind.String);
            key.SetValue("InstallLocation", installDir, RegistryValueKind.String);
            key.SetValue("UninstallString", "\"" + uninstallScript + "\"", RegistryValueKind.String);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
    }

    private sealed class PayloadFile
    {
        public readonly string ResourceName;
        public readonly string TargetPath;

        public PayloadFile(string resourceName, string targetPath)
        {
            ResourceName = resourceName;
            TargetPath = targetPath;
        }
    }
}
"@

[System.IO.File]::WriteAllText($installerSource, $installerCode, [System.Text.Encoding]::UTF8)

$resourceArgs = @()
foreach ($item in $payload) {
    $resourceArgs += "/resource:$(Join-Path $scriptDir $item.Source),$($item.Resource)"
}

Write-Host "Building installer: $setupExeName" -ForegroundColor Cyan
& $csc /nologo /codepage:65001 /target:winexe /win32icon:assets\app_icon.ico "/out:$setupExePath" /r:System.Windows.Forms.dll /r:System.Drawing.dll $resourceArgs $installerSource
if ($LASTEXITCODE -ne 0) {
    throw "Installer compilation failed."
}

Remove-Item -LiteralPath $buildDir -Recurse -Force -ErrorAction SilentlyContinue

$sizeMb = [Math]::Round((Get-Item $setupExePath).Length / 1MB, 2)
Write-Host "Created installer: $setupExeName ($sizeMb MB)" -ForegroundColor Green

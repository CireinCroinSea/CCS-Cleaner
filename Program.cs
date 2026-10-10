using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CireCleaner;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CleanerForm());
    }
}

internal sealed record ScanEntry(string Category, string Name, string Path, long Size, bool IsDirectory, string Kind = "file");
internal sealed record InstalledApp(string Name, string Publisher, string Version, string InstallLocation, string UninstallString, string QuietUninstallString, bool IsSystem, bool IsGame, string RegistryKey);

internal sealed class CleanerForm : Form
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    private void ApplyDarkTitleBar()
    {
        try
        {
            int enabled = 1;
            int hr = DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
            if (hr != 0)
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref enabled, sizeof(int));
        }
        catch { }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyDarkTitleBar();
    }

    private readonly ComboBox drive = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 85 };
    private readonly ComboBox minimum = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly TextBox nameFilter = new() { Width = 155 };
    private readonly ListView results = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, CheckBoxes = true, HideSelection = false };
    private readonly ToolStripStatusLabel status = new("Готово. Выбери диск и действие.");
    private readonly Color panelColor = Color.FromArgb(30, 30, 38);
    private readonly Color controlColor = Color.FromArgb(47, 48, 59);
    private readonly Color accent = Color.FromArgb(105, 185, 255);
    private readonly List<ScanEntry> entries = new();
    private bool appMode;
    private int appFontSize = 9;
    private string selectedTheme = "Тёмная";
    private Color systemColor = Color.LightGreen;
    private Color gameColor = Color.Khaki;
    private Color unknownColor = Color.LightCoral;
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CireCleaner", "settings.txt");

    public CleanerForm()
    {
        Text = "Cire Cleaner";
        Size = new Size(1200, 760);
        MinimumSize = new Size(980, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 20, 26);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(layout);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = panelColor, Padding = new Padding(12, 8, 8, 4) };
        layout.Controls.Add(top, 0, 0);

        var headingRow = new FlowLayoutPanel { Location = new Point(12, 7), Size = new Size(900, 34), AutoSize = false, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
        var appTitle = new Label { Text = "CIRE CLEANER", AutoSize = true, Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold), ForeColor = accent, Margin = new Padding(0, 0, 16, 0) };
        headingRow.Controls.Add(appTitle);
        var authorPrefix = new Label { Text = "Автор:", AutoSize = true, ForeColor = Color.Gainsboro, Margin = new Padding(0, 9, 4, 0) };
        headingRow.Controls.Add(authorPrefix);
        var authorLink = new LinkLabel { Text = "cireincroinsea", AutoSize = true, Font = new Font("Segoe UI", 9), LinkColor = accent, ActiveLinkColor = Color.White, Margin = new Padding(0, 9, 12, 0) };
        authorLink.LinkClicked += (_, _) => OpenUrl("https://t.me/cireincroinsea");
        headingRow.Controls.Add(authorLink);
        top.Controls.Add(headingRow);

        var filtersRow = new FlowLayoutPanel { Location = new Point(12, 43), Size = new Size(1050, 34), AutoSize = false, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
        filtersRow.Controls.Add(new Label { Text = "Диск:", AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
        foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType is DriveType.Fixed or DriveType.Removable))
            drive.Items.Add(d.Name);
        if (drive.Items.Count == 0) drive.Items.Add(@"C:\");
        drive.SelectedIndex = 0;
        StyleCombo(drive);
        filtersRow.Controls.Add(drive);

        filtersRow.Controls.Add(new Label { Text = "Размер:", AutoSize = true, Margin = new Padding(8, 8, 2, 0) });
        foreach (var v in new[] { "100 МБ", "500 МБ", "1 ГБ", "5 ГБ" }) minimum.Items.Add(v);
        minimum.SelectedIndex = 1;
        StyleCombo(minimum);
        filtersRow.Controls.Add(minimum);

        filtersRow.Controls.Add(new Label { Text = "Имя:", AutoSize = true, Margin = new Padding(8, 8, 2, 0) });
        nameFilter.PlaceholderText = "Имя содержит...";
        nameFilter.BackColor = controlColor;
        nameFilter.ForeColor = Color.White;
        nameFilter.BorderStyle = BorderStyle.FixedSingle;
        filtersRow.Controls.Add(nameFilter);
        top.Controls.Add(filtersRow);

        var buttonsRow = new FlowLayoutPanel { Location = new Point(12, 81), Size = new Size(1140, 40), AutoSize = false, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        buttonsRow.Controls.Add(MakeButton("Поиск на диске", 135, (_, _) => ScanLargeFiles()));
        buttonsRow.Controls.Add(MakeButton("Выбрать", 100, (_, _) => { foreach (ListViewItem i in results.Items) i.Checked = true; }));
        buttonsRow.Controls.Add(MakeButton("Снять", 105, (_, _) => { foreach (ListViewItem i in results.Items) i.Checked = false; }));
        buttonsRow.Controls.Add(MakeButton("Удалить", 155, (_, _) => DeleteSelected()));
        buttonsRow.Controls.Add(MakeButton("Очистка мусора", 145, (_, _) => ScanJunk()));
        buttonsRow.Controls.Add(MakeButton("Программы", 120, (_, _) => ShowApps()));
        buttonsRow.Controls.Add(MakeButton("Настройки", 120, (_, _) => ShowSettings()));
        top.Controls.Add(buttonsRow);

        results.BackColor = Color.FromArgb(12, 12, 15);
        results.ForeColor = Color.White;
        results.OwnerDraw = true;
        results.DrawColumnHeader += (_, e) =>
        {
            using var bg = new SolidBrush(Color.FromArgb(12, 12, 15));
            using var fg = new SolidBrush(Color.White);
            e.Graphics.FillRectangle(bg, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, results.Font, e.Bounds, Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using var pen = new Pen(Color.FromArgb(55, 55, 65));
            e.Graphics.DrawRectangle(pen, e.Bounds);
        };
        results.DrawItem += (_, e) => { if (e.Item != null) e.DrawDefault = true; };
        results.DrawSubItem += (_, e) => { e.DrawDefault = true; };
        results.Columns.Add("Категория / статус", 155);
        results.Columns.Add("Название", 230);
        results.Columns.Add("Размер / версия", 115);
        results.Columns.Add("Полный путь / издатель", 650);
        results.DoubleClick += (_, _) => OpenSelectedLocation();
        layout.Controls.Add(results, 0, 1);
        results.HandleCreated += (_, _) => { try { SetWindowTheme(results.Handle, "DarkMode_Explorer", null); } catch { } };
        results.HandleCreated += (_, _) => { try { SetWindowTheme(results.Handle, "DarkMode_ItemsView", null); } catch { } };

        var strip = new StatusStrip { BackColor = panelColor, ForeColor = Color.White, SizingGrip = false };
        strip.Items.Add(status);
        layout.Controls.Add(strip, 0, 2);
        LoadSettings();
        ApplyTheme();
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath)) return;
            foreach (string line in File.ReadAllLines(settingsPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                switch (parts[0])
                {
                    case "FontSize": if (int.TryParse(parts[1], out int fs)) appFontSize = Math.Clamp(fs, 8, 16); break;
                    case "Theme": selectedTheme = parts[1]; break;
                    case "SystemColor": if (ColorTranslator.FromHtml(parts[1]) is Color sc && sc.A != 0) systemColor = sc; break;
                    case "GameColor": if (ColorTranslator.FromHtml(parts[1]) is Color gc && gc.A != 0) gameColor = gc; break;
                    case "UnknownColor": if (ColorTranslator.FromHtml(parts[1]) is Color uc && uc.A != 0) unknownColor = uc; break;
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllLines(settingsPath, new[]
            {
                "FontSize=" + appFontSize,
                "Theme=" + selectedTheme,
                "SystemColor=" + ColorTranslator.ToHtml(systemColor),
                "GameColor=" + ColorTranslator.ToHtml(gameColor),
                "UnknownColor=" + ColorTranslator.ToHtml(unknownColor)
            });
        }
        catch (Exception ex) { MessageBox.Show("Не удалось сохранить настройки: " + ex.Message); }
    }

    private void ApplyTheme()
    {
        var themes = new Dictionary<string, (Color Back, Color Panel, Color Control, Color Text, Color Accent)>
        {
            ["Тёмная"] = (Color.FromArgb(20,20,26), Color.FromArgb(30,30,38), Color.FromArgb(47,48,59), Color.White, Color.FromArgb(105,185,255)),
            ["Чёрная"] = (Color.FromArgb(0,0,0), Color.FromArgb(8,8,8), Color.FromArgb(25,25,25), Color.White, Color.FromArgb(0,200,255)),
            ["Графит"] = (Color.FromArgb(28,30,34), Color.FromArgb(38,41,47), Color.FromArgb(55,58,66), Color.White, Color.FromArgb(120,190,255)),
            ["Синяя"] = (Color.FromArgb(12,22,40), Color.FromArgb(20,35,58), Color.FromArgb(32,52,80), Color.White, Color.FromArgb(100,180,255)),
            ["Фиолетовая"] = (Color.FromArgb(28,16,38), Color.FromArgb(43,27,57), Color.FromArgb(61,39,78), Color.White, Color.FromArgb(200,145,255)),
            ["Изумрудная"] = (Color.FromArgb(12,30,25), Color.FromArgb(20,45,37), Color.FromArgb(33,63,52), Color.White, Color.FromArgb(100,230,175)),
            ["Бордовая"] = (Color.FromArgb(35,14,20), Color.FromArgb(52,22,30), Color.FromArgb(73,34,43), Color.White, Color.FromArgb(255,140,160)),
            ["Светлая"] = (Color.FromArgb(235,238,244), Color.FromArgb(218,223,232), Color.FromArgb(200,207,219), Color.FromArgb(25,28,35), Color.FromArgb(20,100,190)),
            ["Песочная"] = (Color.FromArgb(40,34,25), Color.FromArgb(57,49,36), Color.FromArgb(78,67,48), Color.FromArgb(250,240,220), Color.FromArgb(240,190,100))
        };
        if (!themes.TryGetValue(selectedTheme, out var t)) t = themes["Тёмная"];
        BackColor = t.Back; ForeColor = t.Text;
        foreach (Control c in ControlsRecursive(this))
        {
            if (c is Panel or FlowLayoutPanel or TableLayoutPanel) c.BackColor = t.Panel;
            else if (c is Button b) { b.BackColor = t.Control; b.ForeColor = t.Text; b.FlatAppearance.BorderColor = Color.FromArgb(85,85,95); }
            else if (c is TextBox tb) { tb.BackColor = t.Control; tb.ForeColor = t.Text; }
            else if (c is ComboBox cb) { cb.BackColor = t.Control; cb.ForeColor = t.Text; }
            else if (c is Label l) { l.ForeColor = t.Text; }
            else if (c is LinkLabel ll) { ll.LinkColor = t.Accent; ll.ActiveLinkColor = t.Text; }
        }
        results.BackColor = Color.Black;
        results.ForeColor = Color.White;
        Font = new Font("Segoe UI", appFontSize);
        foreach (Control c in ControlsRecursive(this))
        {
            if (c is not Form) c.Font = new Font(c.Font.FontFamily, appFontSize, c.Font.Style);
        }
        Invalidate(true);
    }

    private IEnumerable<Control> ControlsRecursive(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in ControlsRecursive(child)) yield return nested;
        }
    }

    private void ShowSettings()
    {
        using var dlg = new Form { Text = "Настройки Cire Cleaner", Size = new Size(440, 430), StartPosition = FormStartPosition.CenterParent, BackColor = panelColor, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        dlg.Controls.Add(flow);
        flow.Controls.Add(new Label { Text = "Размер текста", AutoSize = true, ForeColor = Color.White });
        var fontNum = new NumericUpDown { Minimum = 8, Maximum = 16, Value = appFontSize, Width = 100 };
        flow.Controls.Add(fontNum);
        flow.Controls.Add(new Label { Text = "Тема интерфейса (9 вариантов)", AutoSize = true, ForeColor = Color.White, Margin = new Padding(3,12,3,3) });
        var themeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
        themeBox.Items.AddRange(new object[] { "Тёмная", "Чёрная", "Графит", "Синяя", "Фиолетовая", "Изумрудная", "Бордовая", "Светлая", "Песочная" });
        themeBox.SelectedItem = themeBox.Items.Contains(selectedTheme) ? selectedTheme : "Тёмная";
        flow.Controls.Add(themeBox);

        Button colorButton(string text, Color current, Action<Color> setColor)
        {
            var b = new Button { Text = text + " — выбрать цвет", Width = 260, Height = 32, BackColor = current, ForeColor = current.GetBrightness() < 0.5 ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat, Margin = new Padding(3,8,3,2) };
            b.Click += (_, _) => { using var cd = new ColorDialog { Color = current, FullOpen = true }; if (cd.ShowDialog(dlg) == DialogResult.OK) { setColor(cd.Color); b.BackColor = cd.Color; b.ForeColor = cd.Color.GetBrightness() < 0.5 ? Color.White : Color.Black; } };
            flow.Controls.Add(b); return b;
        }
        colorButton("Системные", systemColor, c => systemColor = c);
        colorButton("Игры", gameColor, c => gameColor = c);
        colorButton("Неизвестные", unknownColor, c => unknownColor = c);
        var save = new Button { Text = "Сохранить", Width = 120, Height = 34, BackColor = controlColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(3,14,3,3) };
        save.Click += (_, _) => { appFontSize = (int)fontNum.Value; selectedTheme = themeBox.SelectedItem?.ToString() ?? "Тёмная"; SaveSettings(); ApplyTheme(); dlg.Close(); };
        flow.Controls.Add(save);
        dlg.ShowDialog(this);
    }

    private void StyleCombo(ComboBox box) { box.BackColor = controlColor; box.ForeColor = Color.White; box.Margin = new Padding(4, 5, 4, 0); }
    private Button MakeButton(string text, int width, EventHandler click)
    {
        var b = new Button { Text = text, Width = width, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = controlColor, ForeColor = Color.White, Margin = new Padding(3, 4, 3, 0), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(80, 85, 100); b.Click += click; return b;
    }
    private string SelectedRoot => Path.GetPathRoot(drive.SelectedItem?.ToString() ?? @"C:\") ?? @"C:\";
    private long MinimumBytes => minimum.SelectedIndex switch { 0 => 100L * 1024 * 1024, 1 => 500L * 1024 * 1024, 2 => 1024L * 1024 * 1024, 3 => 5L * 1024 * 1024 * 1024, _ => 500L * 1024 * 1024 };

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024 * 1024):N2} ГБ";
        if (bytes >= 1024L * 1024) return $"{bytes / (1024d * 1024):N2} МБ";
        if (bytes >= 1024) return $"{bytes / 1024d:N0} КБ";
        return $"{bytes} Б";
    }
    private void SetStatus(string text) { status.Text = text; Application.DoEvents(); }
    private void ClearResults() { results.Items.Clear(); entries.Clear(); appMode = false; }
    private void AddEntry(ScanEntry e)
    {
        entries.Add(e);
        var row = new ListViewItem(e.Category) { Tag = e };
        row.SubItems.Add(e.Name); row.SubItems.Add(e.IsDirectory ? "Папка" : FormatSize(e.Size)); row.SubItems.Add(e.Path);
        results.Items.Add(row);
    }

    // Never show high-risk Windows files as candidates, even if the user scans C:.
    private bool IsProtected(string path, string root)
    {
        string full;
        try { full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar); } catch { return true; }
        string[] blockedNames = ["Windows", "Program Files", "Program Files (x86)", "ProgramData", "System Volume Information", "$Recycle.Bin", "Recovery", "PerfLogs", "Windows.old", "Documents and Settings", "Boot", "EFI"];
        foreach (string name in blockedNames)
        {
            string blocked = Path.GetFullPath(Path.Combine(root, name)).TrimEnd(Path.DirectorySeparatorChar);
            if (full.Equals(blocked, StringComparison.OrdinalIgnoreCase) || full.StartsWith(blocked + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return true;
        }
        string[] exactSystemFiles = ["hiberfil.sys", "pagefile.sys", "swapfile.sys", "memory.dmp", "bootmgr", "bootnxt"];
        if (exactSystemFiles.Contains(Path.GetFileName(full), StringComparer.OrdinalIgnoreCase)) return true;
        if (full.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) ||
            full.Contains(@"\Windows\Installer\", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private bool IsExcludedFromLargeFileScan(string path, string root)
    {
        if (IsProtected(path, root)) return true;
        string full;
        try { full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar); }
        catch { return true; }

        string[] excludedSegments =
        [
            @"\AppData",
            @"\Program Files",
            @"\Program Files (x86)",
            @"\ProgramData",
            @"\Windows"
        ];
        foreach (string segment in excludedSegments)
            if (full.Contains(segment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                full.EndsWith(segment, StringComparison.OrdinalIgnoreCase)) return true;

        // Do not follow reparse points/junctions; they can redirect scans into protected or repeated trees.
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0) return true;
        }
        catch { }
        return false;
    }

    private void ScanLargeFiles()
    {
        string root = SelectedRoot;
        if (!Directory.Exists(root)) { SetStatus("Диск недоступен."); return; }
        if (MessageBox.Show($"Искать файлы от {FormatSize(MinimumBytes)} на {root}?\nКритические системные файлы (включая hiberfil.sys/pagefile.sys/swapfile.sys), системные папки, Program Files и AppData исключаются. Личные файлы могут попасть в список.", "Поиск на диске", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        ClearResults(); SetStatus($"Сканирование {root}...");
        string needle = nameFilter.Text.Trim(); int count = 0;
        foreach (string file in EnumerateFilesSafely(root))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length < MinimumBytes || IsExcludedFromLargeFileScan(file, root)) continue;
                if (needle.Length > 0 && !info.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)) continue;
                AddEntry(new ScanEntry("Большой файл", info.Name, file, info.Length, false)); count++;
                if (count % 100 == 0) SetStatus($"Найдено файлов: {count}");
            }
            catch { }
        }
        SetStatus($"Поиск завершён. Найдено файлов: {results.Items.Count}. Двойной щелчок по строке открывает расположение.");
    }

    private IEnumerable<string> EnumerateFilesSafely(string root)
    {
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count > 0)
        {
            string dir = pending.Pop();
            if (IsExcludedFromLargeFileScan(dir, root)) continue;
            string[] files = Array.Empty<string>(), dirs = Array.Empty<string>();
            try { files = Directory.GetFiles(dir); } catch { }
            try { dirs = Directory.GetDirectories(dir); } catch { }
            foreach (string f in files) yield return f;
            foreach (string d in dirs) if (!IsExcludedFromLargeFileScan(d, root)) pending.Push(d);
        }
    }

    private void ScanJunk()
    {
        ClearResults(); SetStatus("Поиск временного мусора и пустых папок...");
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tempRoots = new (string Path, string Name)[]
        {
            (Environment.GetEnvironmentVariable("TEMP") ?? Path.Combine(local, "Temp"), "TEMP пользователя"),
            (Path.Combine(local, "Temp"), "TEMP пользователя"),
            (Path.Combine(local, "CrashDumps"), "Дампы сбоев"),
            (Path.Combine(local, @"Microsoft\Windows\WER\ReportQueue"), "Отчёты ошибок"),
            (Path.Combine(local, @"Microsoft\Windows\WER\ReportArchive"), "Архив отчётов"),
            (Path.Combine(local, "D3DSCache"), "Кэш DirectX"),
            (Path.Combine(local, @"NVIDIA\DXCache"), "Кэш шейдеров NVIDIA"),
            (Path.Combine(local, @"NVIDIA\GLCache"), "Кэш OpenGL NVIDIA"),
            (Path.Combine(local, @"AMD\DxCache"), "Кэш шейдеров AMD"),
            (Path.Combine(local, @"AMD\GLCache"), "Кэш OpenGL AMD")
        }.DistinctBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var root in tempRoots)
        {
            if (!Directory.Exists(root.Path)) continue;
            foreach (string file in EnumerateFilesUnderKnownTemp(root.Path))
            {
                try { var info = new FileInfo(file); AddEntry(new ScanEntry(root.Name, info.Name, file, info.Length, false)); } catch { }
            }
        }
        string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
        try
        {
            if (Directory.Exists(explorer))
                foreach (string f in Directory.GetFiles(explorer).Where(f => Path.GetFileName(f).StartsWith("thumbcache_", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).StartsWith("iconcache_", StringComparison.OrdinalIgnoreCase)))
                { try { var fi = new FileInfo(f); AddEntry(new ScanEntry("Кэш значков", fi.Name, f, fi.Length, false)); } catch { } }
        } catch { }

        string[] userRoots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        ];
        foreach (string folder in userRoots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string dir in EnumerateDirectoriesSafely(folder))
            {
                try
                {
                    if (Directory.EnumerateFileSystemEntries(dir).Any()) continue;
                    AddEntry(new ScanEntry("Пустая папка", Path.GetFileName(dir), dir, 0, true));
                }
                catch { }
            }
        }
        SetStatus($"Найдено объектов: {results.Items.Count}. Найдены известные кэши/временные файлы и пустые папки; всё проверяется вручную.");
    }

    private IEnumerable<string> EnumerateFilesUnderKnownTemp(string root)
    {
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count > 0)
        {
            string dir = pending.Pop();
            try
            {
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
            }
            catch { continue; }
            string[] files = Array.Empty<string>(), dirs = Array.Empty<string>();
            try { files = Directory.GetFiles(dir); } catch { }
            try { dirs = Directory.GetDirectories(dir); } catch { }
            foreach (string f in files)
            {
                bool isReparsePoint;
                try { isReparsePoint = (File.GetAttributes(f) & FileAttributes.ReparsePoint) != 0; }
                catch { continue; }
                if (!isReparsePoint) yield return f;
            }
            foreach (string d in dirs)
            {
                try { if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) == 0) pending.Push(d); } catch { }
            }
        }
    }

    private IEnumerable<string> EnumerateDirectoriesSafely(string root)
    {
        var stack = new Stack<string>();
        try { foreach (string d in Directory.GetDirectories(root)) stack.Push(d); } catch { }
        while (stack.Count > 0)
        {
            string dir = stack.Pop();
            if (dir.Contains(@"\AppData\", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
            }
            catch { continue; }
            yield return dir;
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    try { if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) == 0) stack.Push(d); } catch { }
                }
            }
            catch { }
        }
    }

    private bool IsInsideDirectory(string path, string directory)
    {
        try
        {
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            string fullDir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            return fullPath.Equals(fullDir, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private bool IsKnownSafeCleanupFile(ScanEntry entry)
    {
        if (entry.IsDirectory) return false;
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] roots =
        [
            Environment.GetEnvironmentVariable("TEMP") ?? Path.Combine(local, "Temp"),
            Path.Combine(local, "Temp"),
            Path.Combine(local, "CrashDumps"),
            Path.Combine(local, @"Microsoft\Windows\WER\ReportQueue"),
            Path.Combine(local, @"Microsoft\Windows\WER\ReportArchive"),
            Path.Combine(local, "D3DSCache"),
            Path.Combine(local, @"NVIDIA\DXCache"),
            Path.Combine(local, @"NVIDIA\GLCache"),
            Path.Combine(local, @"AMD\DxCache"),
            Path.Combine(local, @"AMD\GLCache"),
            Path.Combine(local, @"Microsoft\Windows\Explorer")
        ];
        bool categoryAllowed = entry.Category is "TEMP пользователя" or "Дампы сбоев" or "Отчёты ошибок" or "Архив отчётов" or "Кэш DirectX" or "Кэш шейдеров NVIDIA" or "Кэш шейдеров AMD" or "Кэш значков";
        return categoryAllowed && roots.Any(r => !string.IsNullOrWhiteSpace(r) && IsInsideDirectory(entry.Path, r));
    }

    private void DeleteSelected()
    {
        if (appMode) { UninstallSelectedApps(); return; }
        var selected = results.CheckedItems.Cast<ListViewItem>().ToList();
        if (selected.Count == 0) { MessageBox.Show("Сначала отметь объекты галочками."); return; }
        long total = selected.Where(i => i.Tag is ScanEntry e && !e.IsDirectory).Sum(i => ((ScanEntry)i.Tag!).Size);
        if (MessageBox.Show($"Выбрано объектов: {selected.Count}\nРазмер файлов: {FormatSize(total)}\n\nУдалить? Это необратимо. Папки удаляются только если всё ещё пусты.", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        int deleted = 0;
        foreach (ListViewItem row in selected)
        {
            if (row.Tag is not ScanEntry e) continue;
            try
            {
                string root = Path.GetPathRoot(e.Path) ?? SelectedRoot;
                bool safeCleanupFile = IsKnownSafeCleanupFile(e);
                if (!safeCleanupFile && IsProtected(e.Path, root)) continue;
                if (!safeCleanupFile && e.Category != "Пустая папка" && e.Category != "Большой файл") continue;
                if (!e.IsDirectory && File.Exists(e.Path)) File.Delete(e.Path);
                else if (e.IsDirectory && e.Category == "Пустая папка" && Directory.Exists(e.Path) &&
                         !IsProtected(e.Path, root) && !Directory.EnumerateFileSystemEntries(e.Path).Any()) Directory.Delete(e.Path);
                else continue;
                results.Items.Remove(row); deleted++;
            }
            catch { }
        }
        SetStatus($"Удалено объектов: {deleted}");
        MessageBox.Show($"Удалено объектов: {deleted}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenSelectedLocation()
    {
        if (results.SelectedItems.Count == 0) return;
        if (appMode) { return; }
        if (results.SelectedItems[0].Tag is not ScanEntry e) return;
        try
        {
            if (e.IsDirectory && Directory.Exists(e.Path)) Process.Start(new ProcessStartInfo("explorer.exe", $"\"{e.Path}\"") { UseShellExecute = true });
            else if (File.Exists(e.Path)) Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{e.Path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show("Не удалось открыть расположение: " + ex.Message); }
    }

    private void ShowApps()
    {
        ClearResults(); appMode = true;
        results.Columns[0].Text = "Категория";
        results.Columns[1].Text = "Программа";
        results.Columns[2].Text = "Версия";
        results.Columns[3].Text = "Издатель / путь";
        SetStatus("Загрузка списка установленных программ...");
        var apps = ReadInstalledApps();
        foreach (var app in apps.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            string category = app.IsSystem ? "● Системное" : app.IsGame ? "● Игра" : "● Неизвестное";
            var row = new ListViewItem(category) { Tag = app };
            row.ForeColor = app.IsSystem ? systemColor : app.IsGame ? gameColor : unknownColor;
            row.SubItems.Add(app.Name);
            row.SubItems.Add(app.Version);
            row.SubItems.Add(string.IsNullOrWhiteSpace(app.Publisher) ? app.InstallLocation : app.Publisher);
            results.Items.Add(row);
        }
        SetStatus($"Программ найдено: {apps.Count}. Отметь только те, которые точно хочешь удалить.");
    }

    private List<InstalledApp> ReadInstalledApps()
    {
        var output = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
        string[] paths =
        [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];
        foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (string sub in paths)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                    using var uninstall = baseKey.OpenSubKey(sub);
                    if (uninstall == null) continue;
                    foreach (string keyName in uninstall.GetSubKeyNames())
                    {
                        try
                        {
                            using var key = uninstall.OpenSubKey(keyName);
                            if (key == null) continue;
                            string name = Convert.ToString(key.GetValue("DisplayName")) ?? "";
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            string publisher = Convert.ToString(key.GetValue("Publisher")) ?? "";
                            string version = Convert.ToString(key.GetValue("DisplayVersion")) ?? "";
                            string location = Convert.ToString(key.GetValue("InstallLocation")) ?? "";
                            string uninstallCmd = Convert.ToString(key.GetValue("UninstallString")) ?? "";
                            string quiet = Convert.ToString(key.GetValue("QuietUninstallString")) ?? "";
                            bool system = Convert.ToInt32(key.GetValue("SystemComponent", 0)) == 1 ||
                                Convert.ToString(key.GetValue("WindowsInstaller")) == "1" ||
                                publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Microsoft Visual C++", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Windows Driver", StringComparison.OrdinalIgnoreCase);
                            bool game = name.Contains("game", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("xbox", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("epic", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("genshin", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("игр", StringComparison.OrdinalIgnoreCase) ||
                                publisher.Contains("Valve", StringComparison.OrdinalIgnoreCase) ||
                                publisher.Contains("Riot", StringComparison.OrdinalIgnoreCase);
                            var app = new InstalledApp(name, publisher, version, location, uninstallCmd, quiet, system, game, $"{hive}\\{sub}\\{keyName}");
                            output[name + "|" + publisher] = app;
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        return output.Values.ToList();
    }

    private void UninstallSelectedApps()
    {
        var selected = results.CheckedItems.Cast<ListViewItem>().Select(i => i.Tag).OfType<InstalledApp>().ToList();
        if (selected.Count == 0) { MessageBox.Show("Отметь программы для удаления."); return; }
        if (selected.Any(a => a.IsSystem))
        {
            MessageBox.Show("В выборе есть системная/компонентная программа (зелёная). Удали отметку с неё. Эта кнопка не удаляет системные компоненты.", "Защита системных компонентов", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var names = string.Join(Environment.NewLine, selected.Take(10).Select(a => "• " + a.Name));
        if (MessageBox.Show($"Будет запущен штатный деинсталлятор выбранных программ:\n\n{names}\n\nЕсли программ больше 10, остальные тоже выбраны.\nПриложение не может безопасно гарантировать удаление всех пользовательских данных и общих компонентов. Продолжить?", "Удаление программ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        foreach (var app in selected)
        {
            string command = string.IsNullOrWhiteSpace(app.QuietUninstallString) ? app.UninstallString : app.QuietUninstallString;
            if (string.IsNullOrWhiteSpace(command))
            {
                MessageBox.Show($"Для «{app.Name}» не найден штатный деинсталлятор. Удали её через Параметры Windows.");
                continue;
            }
            try
            {
                // Use the registered uninstaller, not recursive deletion of installation folders.
                if (command.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase) || command.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c " + command) { UseShellExecute = false });
                else
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c " + command) { UseShellExecute = false });
            }
            catch (Exception ex) { MessageBox.Show($"Не удалось запустить деинсталлятор {app.Name}: {ex.Message}"); }
        }
        MessageBox.Show("Запущены штатные деинсталляторы. Заверши удаление в их окнах; затем обнови список программ.", "Деинсталляция", MessageBoxButtons.OK, MessageBoxIcon.Information);
        ShowApps();
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}

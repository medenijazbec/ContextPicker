using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ContextPicker
{
    public partial class Form1 : Form
    {
        private const int MaxContexts = 5;
        private readonly List<FolderContext> folderContexts = new();
        private string? exportPath = null;

        public Form1()
        {
            InitializeComponent();
            AddFolderContextUI();
            UpdateExportPathLabel();
        }

        #region Folder Context Management

        private void AddFolderContextUI(string? initialFolder = null)
        {
            if (folderContexts.Count >= MaxContexts)
            {
                MessageBox.Show($"A maximum of {MaxContexts} folders is allowed.", "Limit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Calculate next panel's top
            int top = 0;
            foreach (Control c in panelFolders.Controls)
                if (c is Panel) top = Math.Max(top, c.Bottom + 10);

            // Folder path
            var txt = new TextBox
            {
                Width = 240,
                Left = 0,
                Top = 0,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            if (!string.IsNullOrEmpty(initialFolder))
                txt.Text = initialFolder;

            // Browse
            var btn = new Button
            {
                Text = "Browse...",
                Width = 70,
                Left = txt.Right + 5,
                Top = 0
            };

            // Remove
            var btnRemove = new Button
            {
                Text = "Remove",
                Width = 65,
                Left = btn.Right + 5,
                Top = 0
            };

            // Name label
            var lblName = new Label
            {
                Text = "Name:",
                AutoSize = true,
                Left = btnRemove.Right + 15,
                Top = 4
            };

            // Dump name TextBox
            var txtDumpName = new TextBox
            {
                Width = 100,
                Left = lblName.Right + 5,
                Top = 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // TreeView
            var tree = new TreeView
            {
                CheckBoxes = true,
                Width = panelFolders.Width - 10,
                Height = 180,
                Left = 0,
                Top = txt.Height + 4,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var container = new Panel
            {
                Width = panelFolders.Width - 25,
                Height = txt.Height + 4 + tree.Height + 4,
                Left = 0,
                Top = top,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txt.Parent = container;
            btn.Parent = container;
            btnRemove.Parent = container;
            lblName.Parent = container;
            txtDumpName.Parent = container;
            tree.Parent = container;

            container.Controls.Add(txt);
            container.Controls.Add(btn);
            container.Controls.Add(btnRemove);
            container.Controls.Add(lblName);
            container.Controls.Add(txtDumpName);
            container.Controls.Add(tree);

            // Layout horizontally
            btn.Left = txt.Right + 5;
            btn.Top = txt.Top;
            btnRemove.Left = btn.Right + 15;
            btnRemove.Top = txt.Top;
            lblName.Left = btnRemove.Right + 40;
            lblName.Top = txt.Top + 4;
            txtDumpName.Left = lblName.Right + 15;
            txtDumpName.Top = txt.Top;

            panelFolders.Controls.Add(container);

            RelayoutFolderPanels();

            var fc = new FolderContext
            {
                Panel = container,
                TextBox = txt,
                BrowseButton = btn,
                RemoveButton = btnRemove,
                TreeView = tree,
                NameBox = txtDumpName,
                CheckedStates = new Dictionary<string, bool>()
            };
            folderContexts.Add(fc);

            btn.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (Directory.Exists(txt.Text)) fbd.SelectedPath = txt.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txt.Text = fbd.SelectedPath;
                    LoadDirectoryTree(fc, fbd.SelectedPath);
                }
            };
            txt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (Directory.Exists(txt.Text))
                        LoadDirectoryTree(fc, txt.Text);
                    else
                        MessageBox.Show("Directory does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnRemove.Click += (s, e) =>
            {
                fc.Dispose();
                folderContexts.Remove(fc);
                panelFolders.Controls.Remove(container);
                RelayoutFolderPanels();
                UpdateAddFolderButtonState();
            };
            tree.BeforeExpand += (s, e) => Tree_BeforeExpand(fc, e);
            TreeViewEventHandler treeAfterCheckHandler = null;
            treeAfterCheckHandler = (s, e) => Tree_AfterCheckRecursive(fc, e, treeAfterCheckHandler);
            tree.AfterCheck += treeAfterCheckHandler;

            if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                LoadDirectoryTree(fc, initialFolder);

            UpdateAddFolderButtonState();
        }

        // Relayout all folder panels
        private void RelayoutFolderPanels()
        {
            int top = 0;
            foreach (Control c in panelFolders.Controls)
            {
                c.Top = top;
                c.Width = panelFolders.ClientSize.Width - 25;
                top = c.Bottom + 10;
            }
        }

        private void UpdateAddFolderButtonState()
        {
            int left = MaxContexts - folderContexts.Count;
            btnAddFolder.Enabled = folderContexts.Count < MaxContexts;
            btnAddFolder.Text = $"Add Another Folder ({left} left)";
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            AddFolderContextUI();
        }

        #endregion

        #region Directory Tree Logic

        private void LoadDirectoryTree(FolderContext fc, string rootPath)
        {
            try
            {
                fc.CurrentRootPath = rootPath;
                fc.CheckedStates.Clear();
                fc.TreeView.Nodes.Clear();

                var rootNode = CreateDirectoryNode(rootPath, true);
                fc.TreeView.Nodes.Add(rootNode);
                rootNode.Expand();

                SetupWatcher(fc, rootPath);

                lblStatus.Text = $"Folder loaded: {rootPath}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
            }
        }

        // Recursively creates directory node and all children (to support check/uncheck of non-expanded nodes)
        private TreeNode CreateDirectoryNode(string fullPath, bool loadChildren = false)
        {
            if (Directory.Exists(fullPath))
            {
                var dirInfo = new DirectoryInfo(fullPath);
                var node = new TreeNode(dirInfo.Name)
                {
                    Tag = fullPath,
                    ToolTipText = fullPath
                };
                if (loadChildren)
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(fullPath))
                        {
                            var di = new DirectoryInfo(dir);
                            if ((di.Attributes & FileAttributes.Hidden) == 0)
                                node.Nodes.Add(CreateDirectoryNode(dir, false));
                        }
                        foreach (var file in Directory.GetFiles(fullPath))
                        {
                            var fi = new FileInfo(file);
                            if ((fi.Attributes & FileAttributes.Hidden) == 0)
                                node.Nodes.Add(CreateDirectoryNode(file, false));
                        }
                    }
                    catch { }
                }
                else
                {
                    node.Nodes.Add(new TreeNode("Loading..."));
                }
                return node;
            }
            else if (File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                return new TreeNode(fi.Name)
                {
                    Tag = fullPath,
                    ToolTipText = fullPath
                };
            }
            else
            {
                return new TreeNode("???")
                {
                    Tag = fullPath
                };
            }
        }

        private void Tree_BeforeExpand(FolderContext fc, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                string dirPath = node.Tag.ToString();
                try
                {
                    foreach (var dir in Directory.GetDirectories(dirPath))
                    {
                        var di = new DirectoryInfo(dir);
                        if ((di.Attributes & FileAttributes.Hidden) == 0)
                            node.Nodes.Add(CreateDirectoryNode(dir, false));
                    }
                    foreach (var file in Directory.GetFiles(dirPath))
                    {
                        var fi = new FileInfo(file);
                        if ((fi.Attributes & FileAttributes.Hidden) == 0)
                            node.Nodes.Add(CreateDirectoryNode(file, false));
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error loading node: " + ex.Message;
                }
            }
        }

        // Universal: checking or unchecking a folder auto-expands and auto-checks all children recursively, regardless of expansion
        private void Tree_AfterCheckRecursive(FolderContext fc, TreeViewEventArgs e, TreeViewEventHandler handler)
        {
            fc.TreeView.AfterCheck -= handler;
            try
            {
                string path = GetNodePath(e.Node);
                if (Directory.Exists(path))
                {
                    // Ensure all children are loaded so they can be checked recursively
                    EnsureChildrenLoaded(e.Node, path);
                }
                // Recursively check/uncheck all descendants in tree and UI
                SetNodeCheckedRecursive(fc, e.Node, e.Node.Checked);

                // Update parent node states if needed
                UpdateParentCheckedState(fc, e.Node.Parent);

                // Save checked state
                fc.CheckedStates[path] = e.Node.Checked;
            }
            finally
            {
                fc.TreeView.AfterCheck += handler;
            }
        }

        // Loads all child nodes (if not yet loaded), so SetNodeCheckedRecursive always works
        private void EnsureChildrenLoaded(TreeNode node, string path)
        {
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                try
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var di = new DirectoryInfo(dir);
                        if ((di.Attributes & FileAttributes.Hidden) == 0)
                            node.Nodes.Add(CreateDirectoryNode(dir, false));
                    }
                    foreach (var file in Directory.GetFiles(path))
                    {
                        var fi = new FileInfo(file);
                        if ((fi.Attributes & FileAttributes.Hidden) == 0)
                            node.Nodes.Add(CreateDirectoryNode(file, false));
                    }
                }
                catch { }
            }
            // Recursively ensure children are loaded too
            foreach (TreeNode child in node.Nodes)
            {
                string childPath = GetNodePath(child);
                if (Directory.Exists(childPath))
                    EnsureChildrenLoaded(child, childPath);
            }
        }

        private void SetNodeCheckedRecursive(FolderContext fc, TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Checked != isChecked)
                    child.Checked = isChecked;
                fc.CheckedStates[GetNodePath(child)] = isChecked;
                SetNodeCheckedRecursive(fc, child, isChecked);
            }
        }

        private void UpdateParentCheckedState(FolderContext fc, TreeNode node)
        {
            if (node == null) return;
            bool allChecked = true, anyChecked = false;
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Checked) anyChecked = true;
                else allChecked = false;
            }
            bool shouldCheck = allChecked || anyChecked;
            if (node.Checked != shouldCheck)
            {
                node.Checked = shouldCheck;
                fc.CheckedStates[GetNodePath(node)] = shouldCheck;
                UpdateParentCheckedState(fc, node.Parent);
            }
        }

        private string GetNodePath(TreeNode node) => node.Tag?.ToString() ?? string.Empty;

        #endregion

        #region FileSystemWatcher Logic

        private void SetupWatcher(FolderContext fc, string path)
        {
            fc.DisposeWatcher();
            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName
            };
            watcher.Created += (s, e) => Watcher_Created(fc, e);
            watcher.Renamed += (s, e) => Watcher_Created(fc, e);
            watcher.EnableRaisingEvents = true;
            fc.Watcher = watcher;
        }

        private void Watcher_Created(FolderContext fc, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                BeginInvoke(new Action(() =>
                {
                    SaveCheckedStates(fc, fc.TreeView.Nodes.Count > 0 ? fc.TreeView.Nodes[0] : null);
                    LoadDirectoryTree(fc, fc.CurrentRootPath);
                    RestoreCheckedStates(fc, fc.TreeView.Nodes.Count > 0 ? fc.TreeView.Nodes[0] : null);
                    lblStatus.Text = $"Folder added: {e.FullPath}";
                }));
            }
        }

        private void SaveCheckedStates(FolderContext fc, TreeNode? node)
        {
            if (node == null) return;
            fc.CheckedStates[GetNodePath(node)] = node.Checked;
            foreach (TreeNode child in node.Nodes)
                SaveCheckedStates(fc, child);
        }
        private void RestoreCheckedStates(FolderContext fc, TreeNode? node)
        {
            if (node == null) return;
            if (fc.CheckedStates.TryGetValue(GetNodePath(node), out bool chk))
                node.Checked = chk;
            foreach (TreeNode child in node.Nodes)
                RestoreCheckedStates(fc, child);
        }

        #endregion

        #region Export Path UI

        private void btnExportPath_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (!string.IsNullOrEmpty(exportPath) && Directory.Exists(exportPath))
                fbd.SelectedPath = exportPath;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                exportPath = fbd.SelectedPath;
                UpdateExportPathLabel();
            }
        }

        private void UpdateExportPathLabel()
        {
            lblExportPath.Text = "Export Folder: " + (exportPath ?? "(none)");
        }

        #endregion

        #region Generate Context/CSV

        private async void btnGenerateContext_Click(object sender, EventArgs e)
        {
            if (folderContexts.Count == 0)
            {
                MessageBox.Show("No folders loaded!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(exportPath) || !Directory.Exists(exportPath))
            {
                MessageBox.Show("Please set a valid export folder first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnGenerateContext.Enabled = false;
            progressBar.Value = 0;
            lblStatus.Text = "Processing...";

            int contextsTotal = folderContexts.Count;
            int contextsDone = 0;

            var outFiles = new List<string>();
            var tasks = new List<System.Threading.Tasks.Task>();

            foreach (var fc in folderContexts)
            {
                // Collect checked files
                var selectedFiles = new List<string>();
                if (fc.TreeView.Nodes.Count > 0)
                    GetCheckedFiles(fc, fc.TreeView.Nodes[0], selectedFiles);

                if (selectedFiles.Count == 0) { contextsTotal--; continue; }

                // Get name from user
                string baseName = fc.NameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    // If not set, use folder name
                    baseName = fc.CurrentRootPath != null
                        ? new DirectoryInfo(fc.CurrentRootPath).Name
                        : "Context";
                }
                // Sanitize filename
                foreach (char c in Path.GetInvalidFileNameChars())
                    baseName = baseName.Replace(c, '_');

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outFile = Path.Combine(exportPath, $"{baseName}_ContextDump_{timestamp}.txt");
                outFiles.Add(outFile);

                // Process this folder context asynchronously
                var fcRef = fc;
                var filesRef = selectedFiles;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    using var sw = new StreamWriter(outFile, false, Encoding.UTF8);
                    sw.WriteLine($"# EXPORT DIRECTORY: {fcRef.CurrentRootPath}");
                    foreach (var file in filesRef)
                    {
                        sw.WriteLine($"==== FILE: {file} ====");
                        try
                        {
                            string content = File.ReadAllText(file);
                            sw.WriteLine(content);
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine($"[Error reading file: {ex.Message}]");
                        }
                        sw.WriteLine(new string('-', 80));
                    }
                    // Progress bar per context
                    this.Invoke(() =>
                    {
                        contextsDone++;
                        progressBar.Value = Math.Min(progressBar.Maximum, (int)(contextsDone * 100.0 / contextsTotal));
                        lblStatus.Text = $"Processed {contextsDone}/{contextsTotal} context(s)...";
                    });
                }));
            }

            try
            {
                await System.Threading.Tasks.Task.WhenAll(tasks);

                lblStatus.Text = "All contexts generated!";
                progressBar.Value = 100;
                txtOutputFile.Text = string.Join("; ", outFiles);
                if (outFiles.Count > 0)
                    OpenContainingFolder(exportPath);
                else
                    MessageBox.Show("No files selected in any context.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
            }
            btnGenerateContext.Enabled = true;
        }

        // Recursively collects all checked files under the checked nodes (including all subfiles for checked folders)
        private void GetCheckedFiles(FolderContext fc, TreeNode node, List<string> files)
        {
            string path = GetNodePath(node);
            if (node.Checked)
            {
                if (File.Exists(path))
                {
                    files.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    // Only add the folder path itself (if you want), and then check children
                    foreach (TreeNode child in node.Nodes)
                        GetCheckedFiles(fc, child, files);
                }
            }
            else
            {
                foreach (TreeNode child in node.Nodes)
                    GetCheckedFiles(fc, child, files);
            }
        }


        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (folderContexts.Count == 0)
            {
                MessageBox.Show("No folders loaded!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(exportPath) || !Directory.Exists(exportPath))
            {
                MessageBox.Show("Please set a valid export folder first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string csvFile = Path.Combine(exportPath, $"Selections_{timestamp}.csv");

            var checkedNodes = new List<(string path, string type, string contextName)>();
            foreach (var fc in folderContexts)
            {
                string contextName = fc.NameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(contextName) && !string.IsNullOrEmpty(fc.CurrentRootPath))
                    contextName = new DirectoryInfo(fc.CurrentRootPath).Name;
                if (fc.TreeView.Nodes.Count > 0)
                    GetCheckedNodes(fc, fc.TreeView.Nodes[0], checkedNodes, contextName);
            }

            try
            {
                using var sw = new StreamWriter(csvFile, false, Encoding.UTF8);
                sw.WriteLine("ExportPath,ContextName,Path,Type");
                foreach (var (path, type, contextName) in checkedNodes)
                {
                    sw.WriteLine($"\"{exportPath.Replace("\"", "\"\"")}\",\"{contextName.Replace("\"", "\"\"")}\",\"{path.Replace("\"", "\"\"")}\",{type}");
                }
                lblStatus.Text = "Selection exported!";
                OpenContainingFolder(exportPath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"CSV Export Error: {ex.Message}";
            }
        }

        private void GetCheckedNodes(FolderContext fc, TreeNode node, List<(string, string, string)> checkedNodes, string contextName)
        {
            string path = GetNodePath(node);
            if (node.Checked)
            {
                string type = Directory.Exists(path) ? "Folder" : "File";
                checkedNodes.Add((path, type, contextName));
                // Only recurse if not a file
                if (Directory.Exists(path))
                {
                    foreach (TreeNode child in node.Nodes)
                        GetCheckedNodes(fc, child, checkedNodes, contextName);
                }
            }
            else
            {
                foreach (TreeNode child in node.Nodes)
                    GetCheckedNodes(fc, child, checkedNodes, contextName);
            }
        }


        private void OpenContainingFolder(string dir)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            }
            catch { }
        }

        private void importSection_Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select a CSV Export File";
                    ofd.Filter = "CSV Export (*.csv)|*.csv|All Files (*.*)|*.*";
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;

                    var csvPath = ofd.FileName;
                    // Read all lines and parse
                    var lines = File.ReadAllLines(csvPath);
                    if (lines.Length < 2)
                    {
                        MessageBox.Show("CSV is empty or invalid.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // We'll parse all rows into: ContextName, RootFolder, Paths
                    // Group rows by (ContextName, RootFolder)
                    var sectionDict = new Dictionary<(string, string), List<(string Path, string Type)>>();

                    for (int i = 1; i < lines.Length; i++) // skip header
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Parse CSV, handling quoted values and commas in paths
                        string[] cols = ParseCsvRow(line);
                        if (cols.Length < 4) continue;

                        string exportFolder = cols[0].Trim('"');
                        string contextName = cols[1].Trim('"');
                        string path = cols[2].Trim('"');
                        string type = cols[3].Trim('"');

                        // Only consider valid file/folder entries
                        if (string.IsNullOrWhiteSpace(contextName) || string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(type))
                            continue;

                        // Root is always the very first entry for each context (or the shortest path)
                        // We'll treat the shortest path as the root
                        var sectionKey = (contextName, FindRootFromContext(contextName, path, sectionDict));

                        // If not in dict yet, store initial root as this path
                        if (!sectionDict.ContainsKey(sectionKey))
                            sectionDict[sectionKey] = new List<(string, string)>();

                        sectionDict[sectionKey].Add((path, type));
                    }

                    // For each context, add as a new folder context UI and set checks
                    foreach (var kv in sectionDict)
                    {
                        string contextName = kv.Key.Item1;
                        string rootFolder = kv.Key.Item2;
                        var allEntries = kv.Value;

                        // Skip if already loaded
                        if (folderContexts.Any(fc => string.Equals(fc.CurrentRootPath, rootFolder, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        // Verify root exists
                        if (!Directory.Exists(rootFolder))
                        {
                            MessageBox.Show(
                                $"The root folder '{rootFolder}' for context '{contextName}' does not exist. Skipping.",
                                "Import Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            continue;
                        }

                        // Add the section UI
                        AddFolderContextUI(rootFolder);
                        // Find the just-added FolderContext
                        var fc = folderContexts.LastOrDefault();
                        if (fc == null)
                            continue;
                        // Set the context name if present
                        if (!string.IsNullOrWhiteSpace(contextName))
                            fc.NameBox.Text = contextName;

                        // Wait for the tree to load (ensures UI thread)
                        fc.TreeView.Invoke(new Action(() =>
                        {
                            // Build a hashset for fast path lookup
                            var checkedSet = new HashSet<string>(
                                allEntries.Where(e => e.Type == "File" || e.Type == "Folder").Select(e => e.Path),
                                StringComparer.OrdinalIgnoreCase);

                            // Recursively check nodes based on imported CSV paths
                            CheckNodesByPath(fc.TreeView.Nodes, checkedSet);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to import sections:\n" + ex.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Helper: Extract root (shortest path) per context group
        private string FindRootFromContext(string contextName, string path, Dictionary<(string, string), List<(string, string)>> dict)
        {
            // See if any section with this context exists: find the shortest path
            var matches = dict.Keys.Where(k => k.Item1 == contextName).ToList();
            if (matches.Count == 0)
                return path;
            var shortest = matches.MinBy(k => k.Item2.Length);
            if (path.Length < shortest.Item2.Length)
                return path;
            return shortest.Item2;
        }

        // Helper: CSV parsing supporting quoted commas
        private string[] ParseCsvRow(string line)
        {
            var values = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            values.Add(sb.ToString());
            return values.ToArray();
        }

        // Recursively check all nodes matching any path in checkedSet
        private void CheckNodesByPath(TreeNodeCollection nodes, HashSet<string> checkedSet)
        {
            foreach (TreeNode node in nodes)
            {
                string nodePath = node.Tag?.ToString() ?? "";
                if (checkedSet.Contains(nodePath))
                {
                    node.Checked = true;
                }
                // Recurse
                if (node.Nodes.Count > 0)
                    CheckNodesByPath(node.Nodes, checkedSet);
            }
        }

    }

    internal class FolderContext : IDisposable
    {
        public Panel Panel;
        public TextBox TextBox;
        public Button BrowseButton;
        public Button RemoveButton;
        public TreeView TreeView;
        public TextBox NameBox;
        public string? CurrentRootPath = null;
        public Dictionary<string, bool> CheckedStates = new();
        public FileSystemWatcher? Watcher = null;

        public void Dispose()
        {
            DisposeWatcher();
            Panel.Dispose();
        }
        public void DisposeWatcher()
        {
            if (Watcher != null)
            {
                Watcher.EnableRaisingEvents = false;
                Watcher.Dispose();
                Watcher = null;
            }
        }
    }
}
#endregion

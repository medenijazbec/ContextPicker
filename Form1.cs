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
                Height = 330,
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

                // Show the selected folder as the only root, but only its contents are visible under it
                var rootNode = new TreeNode(new DirectoryInfo(rootPath).Name)
                {
                    Tag = rootPath,
                    ToolTipText = rootPath
                };

                // Add immediate subfolders and files as children
                try
                {
                    foreach (var dir in Directory.GetDirectories(rootPath))
                    {
                        var di = new DirectoryInfo(dir);
                        if ((di.Attributes & FileAttributes.Hidden) == 0)
                            rootNode.Nodes.Add(CreateDirectoryNode(dir, false));
                    }
                    foreach (var file in Directory.GetFiles(rootPath))
                    {
                        var fi = new FileInfo(file);
                        if ((fi.Attributes & FileAttributes.Hidden) == 0)
                            rootNode.Nodes.Add(CreateDirectoryNode(file, false));
                    }
                }
                catch { /* silently ignore errors */ }

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

        // Only ever called for children, never for the root itself
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
                    // Add dummy node for expansion
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

            // ----- contextIgnore Export Mode -----
            if (contextIgnoreCheckBox.Checked)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                foreach (var fc in folderContexts)
                {
                    string contextName = string.IsNullOrWhiteSpace(fc.NameBox.Text)
                        ? Path.GetFileName(fc.CurrentRootPath)
                        : fc.NameBox.Text;

                    if (string.IsNullOrEmpty(fc.CurrentRootPath) || !Directory.Exists(fc.CurrentRootPath))
                        continue;

                    // 1. Gather all files and folders (relative to root)
                    var allPaths = new List<string>();
                    GetAllRelativePaths(fc.CurrentRootPath, fc.CurrentRootPath, allPaths);

                    // 2. Gather all checked paths (relative to root)
                    var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (fc.TreeView.Nodes.Count > 0)
                        GetCheckedRelativePaths(fc.CurrentRootPath, fc.TreeView.Nodes[0], checkedPaths);

                    // 3. Not checked = to be ignored (relative paths)
                    var notCheckedPaths = allPaths.Where(p => !checkedPaths.Contains(p)).ToList();

                    // 4. Write .contextIgnore file (root as first line, then ignore list)
                    string ignoreFile = Path.Combine(exportPath, $"{contextName}_contextIgnore_{timestamp}.contextIgnore");
                    using (var sw = new StreamWriter(ignoreFile, false, Encoding.UTF8))
                    {
                        sw.WriteLine(fc.CurrentRootPath); // Root as first line
                        foreach (var p in notCheckedPaths)
                            sw.WriteLine(p);
                    }
                    lblStatus.Text = $"Exported .contextIgnore for \"{contextName}\"!";
                }
                OpenContainingFolder(exportPath);
                return;
            }

            // ----- Standard CSV Export (Unchanged) -----
            string timestampCsv = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string csvFile = Path.Combine(exportPath, $"Selections_{timestampCsv}.csv");

            var checkedNodes = new List<(string path, string type, string contextName)>();

            foreach (var fc in folderContexts)
            {
                string contextName = string.IsNullOrWhiteSpace(fc.NameBox.Text)
                    ? Path.GetFileName(fc.CurrentRootPath)
                    : fc.NameBox.Text;

                if (fc.TreeView.Nodes.Count > 0)
                {
                    foreach (TreeNode node in fc.TreeView.Nodes)
                    {
                        CollectExplicitlyCheckedNodes(node, checkedNodes, contextName);
                    }
                }
            }

            using var swCsv = new StreamWriter(csvFile, false, Encoding.UTF8);
            swCsv.WriteLine("ExportPath,ContextName,Path,Type");

            foreach (var (path, type, contextName) in checkedNodes)
            {
                swCsv.WriteLine($"\"{exportPath.Replace("\"", "\"\"")}\",\"{contextName.Replace("\"", "\"\"")}\",\"{path.Replace("\"", "\"\"")}\",{type}");
            }

            lblStatus.Text = "Selection exported!";
            OpenContainingFolder(exportPath);
        }


        // Recursively collect all relative paths (file & folder) from rootPath
        private void GetAllRelativePaths(string rootPath, string current, List<string> relPaths)
        {
            foreach (var dir in Directory.GetDirectories(current))
            {
                var di = new DirectoryInfo(dir);
                if ((di.Attributes & FileAttributes.Hidden) != 0)
                    continue;
                string rel = Path.GetRelativePath(rootPath, dir);
                relPaths.Add(rel);
                GetAllRelativePaths(rootPath, dir, relPaths);
            }
            foreach (var file in Directory.GetFiles(current))
            {
                var fi = new FileInfo(file);
                if ((fi.Attributes & FileAttributes.Hidden) != 0)
                    continue;
                string rel = Path.GetRelativePath(rootPath, file);
                relPaths.Add(rel);
            }
        }

        // Recursively collect checked paths (as relative to root)
        private void GetCheckedRelativePaths(string rootPath, TreeNode node, HashSet<string> relPaths)
        {
            string path = GetNodePath(node);
            if (node.Checked)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    string rel = Path.GetRelativePath(rootPath, path);
                    if (!string.IsNullOrEmpty(rel) && rel != "." && rel != string.Empty)
                        relPaths.Add(rel);
                }
            }

            foreach (TreeNode child in node.Nodes)
                GetCheckedRelativePaths(rootPath, child, relPaths);
        }

        // Recursively check or uncheck all nodes
        private void CheckAllRecursive(TreeNodeCollection nodes, bool isChecked)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = isChecked;
                CheckAllRecursive(node.Nodes, isChecked);
            }
        }

        // Uncheck all nodes whose relative path is in .contextIgnore set
        private void UncheckIgnoredPaths(TreeNodeCollection nodes, string rootPath, HashSet<string> ignoreSet)
        {
            foreach (TreeNode node in nodes)
            {
                string path = GetNodePath(node);
                string rel = Path.GetRelativePath(rootPath, path);
                if (ignoreSet.Contains(rel))
                    node.Checked = false;

                UncheckIgnoredPaths(node.Nodes, rootPath, ignoreSet);
            }
        }





        private void CollectAllCheckedChildren(TreeNode node, List<(string path, string type, string contextName)> checkedNodes, string contextName)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Checked)
                {
                    string path = child.Tag?.ToString() ?? "";
                    string type = Directory.Exists(path) ? "Folder" : "File";
                    checkedNodes.Add((path, type, contextName));

                    CollectAllCheckedChildren(child, checkedNodes, contextName);
                }
            }
        }

        private void CollectExplicitlyCheckedNodes(TreeNode node, List<(string path, string type, string contextName)> checkedNodes, string contextName)
        {
            string path = node.Tag?.ToString() ?? "";

            if (node.Checked && (node.Parent == null || !node.Parent.Checked))
            {
                string type = Directory.Exists(path) ? "Folder" : "File";
                checkedNodes.Add((path, type, contextName));

                // Recursively add all checked child nodes
                CollectAllCheckedChildren(node, checkedNodes, contextName);
            }
            else
            {
                // Continue recursion
                foreach (TreeNode child in node.Nodes)
                {
                    CollectExplicitlyCheckedNodes(child, checkedNodes, contextName);
                }
            }
        }


        private void GetCheckedNodes(TreeNodeCollection nodes, List<(string path, string type, string contextName)> checkedNodes, string contextName)
        {
            foreach (TreeNode node in nodes)
            {
                string path = node.Tag?.ToString() ?? string.Empty;
                bool explicitlyChecked = node.Checked && (node.Parent == null || !node.Parent.Checked);

                if (explicitlyChecked)
                {
                    string type = Directory.Exists(path) ? "Folder" : "File";
                    checkedNodes.Add((path, type, contextName));
                }

                GetCheckedNodes(node.Nodes, checkedNodes, contextName);
            }
        }



        private void importSection_Button_Click(object sender, EventArgs e)
        {
            // If contextIgnore mode is checked, use .contextIgnore logic
            if (contextIgnoreCheckBox.Checked)
            {
                using OpenFileDialog ofd = new()
                {
                    Title = "Select one or more .contextIgnore Files",
                    Filter = "ContextIgnore (*.contextIgnore)|*.contextIgnore|All Files (*.*)|*.*",
                    Multiselect = true
                };

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                int importedCount = 0;

                foreach (var file in ofd.FileNames)
                {
                    var ignoreFileLines = File.ReadAllLines(file);
                    if (ignoreFileLines.Length == 0)
                    {
                        MessageBox.Show($"{Path.GetFileName(file)} is empty or invalid.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    string importedRoot = ignoreFileLines[0].Trim();
                    if (!Directory.Exists(importedRoot))
                    {
                        MessageBox.Show($"Root folder '{importedRoot}' (from {Path.GetFileName(file)}) does not exist.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    var ignoreLines = ignoreFileLines.Skip(1)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Use context name from file name
                    string importedContextName = Path.GetFileNameWithoutExtension(
                        Path.GetFileNameWithoutExtension(Path.GetFileName(file)).Replace("_contextIgnore_", "_").Trim('_')
                    );

                    AddFolderContextUI(importedRoot);
                    var fc = folderContexts.Last();
                    fc.NameBox.Text = importedContextName;

                    // By default, check EVERYTHING except those listed in .contextIgnore
                    fc.TreeView.Invoke(() =>
                    {
                        CheckAllRecursive(fc.TreeView.Nodes, true);
                        UncheckIgnoredPaths(fc.TreeView.Nodes, importedRoot, ignoreLines);
                    });

                    importedCount++;
                }

                if (importedCount > 0)
                    lblStatus.Text = $".contextIgnore import: {importedCount} context(s) loaded.";
                else
                    lblStatus.Text = "No .contextIgnore files imported.";
                return;
            }

            // --- Default CSV import logic (unchanged) ---
            using OpenFileDialog csvOfd = new()
            {
                Title = "Select a CSV Export File",
                Filter = "CSV Export (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (csvOfd.ShowDialog() != DialogResult.OK)
                return;

            var lines = File.ReadAllLines(csvOfd.FileName);
            if (lines.Length < 2)
            {
                MessageBox.Show("CSV is empty or invalid.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var groupedEntries = lines.Skip(1)
                .Select(ParseCsvRow)
                .Where(cols => cols.Length == 4)
                .Select(cols => new
                {
                    ExportFolder = cols[0].Trim('"'),
                    ContextName = cols[1].Trim('"'),
                    Path = cols[2].Trim('"'),
                    Type = cols[3].Trim('"')
                })
                .GroupBy(x => x.ContextName)
                .ToList();

            foreach (var group in groupedEntries)
            {
                string contextName = group.Key;
                string contextRoot = group.First().Path;

                if (!Directory.Exists(contextRoot))
                {
                    MessageBox.Show($"Root folder '{contextRoot}' for context '{contextName}' does not exist.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                AddFolderContextUI(contextRoot);
                var fc = folderContexts.Last();
                fc.NameBox.Text = contextName;

                fc.TreeView.Invoke(() =>
                {
                    ClearAllChecks(fc.TreeView.Nodes);

                    var pathsToCheck = new HashSet<string>(group.Select(x => x.Path), StringComparer.OrdinalIgnoreCase);
                    CheckOnlyExactNodes(fc.TreeView.Nodes, pathsToCheck);
                });
            }
        }





        private void CheckOnlyExactNodes(TreeNodeCollection nodes, HashSet<string> pathsToCheck)
        {
            foreach (TreeNode node in nodes)
            {
                string nodePath = node.Tag?.ToString() ?? "";
                node.Checked = pathsToCheck.Contains(nodePath);

                CheckOnlyExactNodes(node.Nodes, pathsToCheck);

                if (!node.Checked && node.Nodes.Cast<TreeNode>().Any(child => child.Checked))
                {
                    node.Checked = true;
                }
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

        private void ClearAllChecks(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = false;
                ClearAllChecks(node.Nodes);
            }
        }
        private void CheckOnlySpecifiedNodes(TreeNodeCollection nodes, HashSet<string> pathsToCheck)
        {
            foreach (TreeNode node in nodes)
            {
                string nodePath = node.Tag?.ToString() ?? "";
                node.Checked = pathsToCheck.Contains(nodePath);

                // Expand nodes if necessary
                if (node.Nodes.Count > 0)
                    CheckOnlySpecifiedNodes(node.Nodes, pathsToCheck);

                // Ensure parent nodes are partially checked
                if (!node.Checked && node.Nodes.Cast<TreeNode>().Any(child => child.Checked))
                {
                    node.Checked = true;
                }
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

        private static string[] ParseCsvRow(string line)
        {
            var values = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
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

        private void contextIgnoreCheckBox_CheckedChanged(object sender, EventArgs e)
        {

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

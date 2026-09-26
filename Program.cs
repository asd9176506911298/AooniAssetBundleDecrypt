using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

// Explicit aliases in case any other reference in the project pulls in
// System.Net.Mime.MediaTypeNames.Application or System.Reflection.Emit.Label.
using Application = System.Windows.Forms.Application;
using Label = System.Windows.Forms.Label;
using TextBox = System.Windows.Forms.TextBox;
using Button = System.Windows.Forms.Button;
using CheckBox = System.Windows.Forms.CheckBox;
using ProgressBar = System.Windows.Forms.ProgressBar;
using Form = System.Windows.Forms.Form;
using MessageBox = System.Windows.Forms.MessageBox;
using Ookii.Dialogs.WinForms;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBoxButtons = System.Windows.Forms.MessageBoxButtons;
using MessageBoxIcon = System.Windows.Forms.MessageBoxIcon;
using ScrollBars = System.Windows.Forms.ScrollBars;
using FormStartPosition = System.Windows.Forms.FormStartPosition;

namespace AooniBatchDecryptor
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

    public class MainForm : Form
    {
        private TextBox txtInput;
        private TextBox txtOutput;
        private const string DefaultKey = "PWmmzQxSjLC8354U5CEWn5U35JaGYZrd";

        private TextBox txtKey;
        private Button btnUnlockKey;
        private Button btnResetKey;
        private CheckBox chkRecursive;
        private CheckBox chkKeepExt;
        private Button btnBrowseInput;
        private Button btnBrowseOutput;
        private Button btnStart;
        private ProgressBar progressBar;
        private TextBox txtLog;
        private Label lblStatus;

        public MainForm()
        {
            Text = "Aooni AssetBundle Batch Decryptor";
            Width = 780;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 9F);

            var lblInput = new Label { Text = "Input Folder:", Left = 15, Top = 20, Width = 200 };
            txtInput = new TextBox { Left = 15, Top = 45, Width = 620, Text = @"E:\Steam\steamapps\common\Aooni\Aooni_Data\StreamingAssets\aa\StandaloneWindows64" };
            btnBrowseInput = new Button { Text = "Browse...", Left = 645, Top = 43, Width = 100 };
            btnBrowseInput.Click += (s, e) => BrowseFolder(txtInput);

            var lblOutput = new Label { Text = "Output Folder:", Left = 15, Top = 80, Width = 200 };
            txtOutput = new TextBox { Left = 15, Top = 105, Width = 620, Text = @"E:\AooniDecrypted" };
            btnBrowseOutput = new Button { Text = "Browse...", Left = 645, Top = 103, Width = 100 };
            btnBrowseOutput.Click += (s, e) => BrowseFolder(txtOutput);

            var lblKey = new Label { Text = "Key (UTF-8, 32 characters):", Left = 15, Top = 140, Width = 220 };
            txtKey = new TextBox
            {
                Left = 15,
                Top = 165,
                Width = 520,
                Text = DefaultKey,
                ReadOnly = true,
                TabStop = false,
                BackColor = System.Drawing.SystemColors.Control,
                Cursor = System.Windows.Forms.Cursors.Default
            };
            btnUnlockKey = new Button { Text = "Edit", Left = 545, Top = 163, Width = 90 };
            btnUnlockKey.Click += BtnUnlockKey_Click;

            btnResetKey = new Button { Text = "Reset", Left = 645, Top = 163, Width = 100 };
            btnResetKey.Click += BtnResetKey_Click;

            chkRecursive = new CheckBox { Text = "Include subfolders (recursive)", Left = 15, Top = 200, Width = 220, Checked = true };
            chkKeepExt = new CheckBox { Text = "Force .bundle extension on output", Left = 250, Top = 200, Width = 280, Checked = true };

            btnStart = new Button { Text = "Start Batch Decrypt", Left = 15, Top = 235, Width = 200, Height = 35 };
            btnStart.Click += BtnStart_Click;

            lblStatus = new Label { Left = 230, Top = 243, Width = 500, Text = "Ready." };

            progressBar = new ProgressBar { Left = 15, Top = 280, Width = 730, Height = 20 };

            txtLog = new TextBox
            {
                Left = 15,
                Top = 310,
                Width = 730,
                Height = 190,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9F)
            };

            Controls.Add(lblInput);
            Controls.Add(txtInput);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(txtOutput);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblKey);
            Controls.Add(txtKey);
            Controls.Add(btnUnlockKey);
            Controls.Add(btnResetKey);
            Controls.Add(chkRecursive);
            Controls.Add(chkKeepExt);
            Controls.Add(btnStart);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(txtLog);
        }

        private void BrowseFolder(TextBox target)
        {
            // VistaFolderBrowserDialog gives the modern Explorer-style folder picker
            // (same look as .NET 5+'s built-in FolderBrowserDialog), instead of the
            // old tree-view dialog that ships with .NET Framework's FolderBrowserDialog.
            using (var dlg = new VistaFolderBrowserDialog())
            {
                dlg.UseDescriptionForTitle = true;
                dlg.Description = "Select Folder";

                if (Directory.Exists(target.Text))
                    dlg.SelectedPath = target.Text;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    target.Text = dlg.SelectedPath;
            }
        }

        private void BtnUnlockKey_Click(object sender, EventArgs e)
        {
            if (txtKey.ReadOnly)
            {
                var confirm = MessageBox.Show(
                    this,
                    "This key is preset to match the game's decryption key. Only change it if you know what you're doing.\n\nUnlock for editing?",
                    "Confirm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                    return;

                txtKey.ReadOnly = false;
                txtKey.TabStop = true;
                txtKey.BackColor = System.Drawing.SystemColors.Window;
                txtKey.Cursor = System.Windows.Forms.Cursors.IBeam;
                txtKey.Focus();
                txtKey.SelectAll();
                btnUnlockKey.Text = "Lock";
            }
            else
            {
                txtKey.ReadOnly = true;
                txtKey.TabStop = false;
                txtKey.BackColor = System.Drawing.SystemColors.Control;
                txtKey.Cursor = System.Windows.Forms.Cursors.Default;
                btnUnlockKey.Text = "Edit";
            }
        }

        private void BtnResetKey_Click(object sender, EventArgs e)
        {
            txtKey.Text = DefaultKey;
            txtKey.ReadOnly = true;
            txtKey.TabStop = false;
            txtKey.BackColor = System.Drawing.SystemColors.Control;
            txtKey.Cursor = System.Windows.Forms.Cursors.Default;
            btnUnlockKey.Text = "Edit";
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message)));
                return;
            }
            txtLog.AppendText(message + Environment.NewLine);
        }

        private void SetStatus(string message)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => SetStatus(message)));
                return;
            }
            lblStatus.Text = message;
        }

        private void SetProgress(int value, int max)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() => SetProgress(value, max)));
                return;
            }
            progressBar.Maximum = Math.Max(max, 1);
            progressBar.Value = Math.Min(value, progressBar.Maximum);
        }

        private void SetUiEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetUiEnabled(enabled)));
                return;
            }
            btnStart.Enabled = enabled;
            btnBrowseInput.Enabled = enabled;
            btnBrowseOutput.Enabled = enabled;
            txtInput.Enabled = enabled;
            txtOutput.Enabled = enabled;
            txtKey.Enabled = enabled;
            chkRecursive.Enabled = enabled;
            chkKeepExt.Enabled = enabled;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            string inputFolder = txtInput.Text.Trim();
            string outputFolder = txtOutput.Text.Trim();
            string keyText = txtKey.Text;

            if (!Directory.Exists(inputFolder))
            {
                MessageBox.Show(this, "Input folder does not exist. Please check the path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show(this, "Please specify an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            byte[] key = Encoding.UTF8.GetBytes(keyText);
            if (key.Length != 32)
            {
                MessageBox.Show(this, "Key must be 32 bytes (32 characters in UTF-8 encoding).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Directory.CreateDirectory(outputFolder);
            txtLog.Clear();
            SetUiEnabled(false);

            bool recursive = chkRecursive.Checked;
            bool keepExt = chkKeepExt.Checked;

            var thread = new Thread(() => RunBatch(inputFolder, outputFolder, key, recursive, keepExt))
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void RunBatch(string inputFolder, string outputFolder, byte[] key, bool recursive, bool keepExt)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files;
            try
            {
                files = Directory.GetFiles(inputFolder, "*.bundle", searchOption);
            }
            catch (Exception ex)
            {
                Log("Failed to read folder: " + ex.Message);
                SetUiEnabled(true);
                return;
            }

            int total = files.Length;
            int ok = 0, skipped = 0, failed = 0;

            Log($"Found {total} file(s). Starting processing...");
            SetProgress(0, total);

            for (int i = 0; i < total; i++)
            {
                string srcPath = files[i];
                string relative = srcPath.Substring(inputFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destPath = Path.Combine(outputFolder, relative);
                if (keepExt && !destPath.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
                {
                    destPath += ".bundle";
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    var result = DecryptOne(srcPath, destPath, key);
                    switch (result)
                    {
                        case DecryptResult.Decrypted:
                            Log("[Decrypted] " + relative);
                            ok++;
                            break;
                        case DecryptResult.CopiedRaw:
                            Log("[Unencrypted / offset restored] " + relative);
                            ok++;
                            break;
                        case DecryptResult.NotBundle:
                            Log("[Skipped, not a recognized format] " + relative);
                            skipped++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log("[Failed] " + relative + " -> " + ex.Message);
                    failed++;
                }

                SetProgress(i + 1, total);
            }

            SetStatus($"Done. Success: {ok}, Skipped: {skipped}, Failed: {failed} (Total: {total}).");
            Log("=== All files processed ===");
            Log($"Success: {ok}   Skipped: {skipped}   Failed: {failed}");
            SetUiEnabled(true);
        }

        private enum DecryptResult
        {
            Decrypted,
            CopiedRaw,
            NotBundle
        }

        /// <summary>
        /// Mirrors the original game logic in BlueAssetBundleProvider.BlueAssetBundleResource.Load:
        /// - 1st byte == 0: the data itself is not encrypted; two XOR-obfuscated int32 values encode
        ///   the actual data offset. We compute the real offset and copy the remaining bytes as-is,
        ///   which is the original AssetBundle content.
        /// - 1st byte != 0: encrypted via BinaryFilter (skip 1 flag byte -> 32-byte IV -> Rijndael-256 CBC).
        /// </summary>
        private static DecryptResult DecryptOne(string srcPath, string destPath, byte[] key)
        {
            using (var src = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (src.Length < 1)
                    return DecryptResult.NotBundle;

                int flag = src.ReadByte();
                if (flag < 0)
                    return DecryptResult.NotBundle;

                if (flag == 0)
                {
                    // Unencrypted format: read two little-endian int32 values.
                    byte[] buf4 = new byte[4];

                    if (src.Read(buf4, 0, 4) != 4) return DecryptResult.NotBundle;
                    int num2 = BitConverter.ToInt32(buf4, 0);

                    if (src.Read(buf4, 0, 4) != 4) return DecryptResult.NotBundle;
                    int num3raw = BitConverter.ToInt32(buf4, 0);
                    int num3 = num3raw ^ num2;

                    long dataLength = src.Length - num3 - 9;
                    if (dataLength < 0)
                        return DecryptResult.NotBundle;

                    src.Seek(9 + num3, SeekOrigin.Begin);

                    using (var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    {
                        CopyExactly(src, dest, dataLength);
                    }
                    return DecryptResult.CopiedRaw;
                }
                else
                {
                    // Encrypted format: position is already past the 1 flag byte (ReadByte advanced it).
                    using (var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    using (var rijndael = new RijndaelManaged
                    {
                        Padding = PaddingMode.Zeros,
                        Mode = CipherMode.CBC,
                        KeySize = 256,
                        BlockSize = 256,
                        Key = key
                    })
                    {
                        byte[] iv = new byte[rijndael.IV.Length]; // 32 bytes
                        int readIv = src.Read(iv, 0, iv.Length);
                        if (readIv != iv.Length)
                            return DecryptResult.NotBundle;

                        rijndael.IV = iv;

                        using (var transform = rijndael.CreateDecryptor())
                        using (var cs = new CryptoStream(src, transform, CryptoStreamMode.Read))
                        {
                            cs.CopyTo(dest);
                        }
                    }
                    return DecryptResult.Decrypted;
                }
            }
        }

        private static void CopyExactly(Stream src, Stream dest, long length)
        {
            byte[] buffer = new byte[81920];
            long remaining = length;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = src.Read(buffer, 0, toRead);
                if (read <= 0) break;
                dest.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}

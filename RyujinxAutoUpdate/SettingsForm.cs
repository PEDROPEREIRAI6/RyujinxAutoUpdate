﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RyujinxAutoUpdate
{
    public partial class SettingsForm : Form
    {
        private static string[] branches;
        private static string currentBranch;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            this.Icon = new Icon("Images/settings.ico");
            Settings.UpdateValues();
            ShouldOpenDefaultHomebrewCheck.Checked = Settings.SHOULD_OPEN_DEFAULT_HOMEBREW;
            ShowRyujinxConsoleCheck.Checked        = Settings.SHOW_RYUJINX_CONSOLE;
            WriteRyujinxLogCheck.Checked           = Settings.WRITE_RYUJINX_LOG;
            ShowBuildConsoleCheck.Checked          = Settings.SHOW_BUILD_CONSOLE;
            WriteBuildLogCheck.Checked             = Settings.WRITE_BUILD_LOG;
            DefaultAppPath.Text                    = Settings.DEFAULT_HOMEBREW_APP;
            IconSize.Text                          = Settings.GAMELIST_ICON_SIZE;
            GetMetadataCDN.Checked                 = Settings.GET_METADATA_FROM_CDN;
            UseNucleus.Checked                     = Settings.USE_NUCLEUS;

            if (branches == null)      branches      = GitParser.GitRemoteBranches(MainForm.RyujinxDownloadPath);
            if (currentBranch == null) currentBranch = GitParser.GitCurrentBranch (MainForm.RyujinxDownloadPath);

            CurrentBranchLabel.Text += " " + currentBranch;

            if (branches != null)
            {
                foreach (string s in branches)
                {
                    if (s != currentBranch)
                    {
                        listView1.Items.Add(new ListViewItem
                        {
                            Text = s
                        });
                    }
                }
            }
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            if (IconSize.Items.Contains(IconSize.Text))
            {
                Settings.UpdateINI();
                MainForm.ReloadIconSize();
                MainForm.ReloadGameList();
            }
            else
            {
                MessageBox.Show("Game List Icon Size is not of a proper value.", "Error");
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void ShouldOpenDefaultHomebrewCheck_CheckedChanged(object sender, EventArgs e)
        {
            DefaultApp.Enabled                    = ShouldOpenDefaultHomebrewCheck.Checked;
            DefaultAppPath.Enabled                = ShouldOpenDefaultHomebrewCheck.Checked;
            Settings.SHOULD_OPEN_DEFAULT_HOMEBREW = ShouldOpenDefaultHomebrewCheck.Checked;
        }

        private void ShowRyujinxConsoleCheck_CheckedChanged(object sender, EventArgs e)
        {
            Settings.SHOW_RYUJINX_CONSOLE =  ShowRyujinxConsoleCheck.Checked;
            WriteRyujinxLogCheck.Enabled  = !ShowRyujinxConsoleCheck.Checked;
        }

        private void WriteRyujinxLogCheck_CheckedChanged(object sender, EventArgs e)
        {
            Settings.WRITE_RYUJINX_LOG      =  WriteRyujinxLogCheck.Checked;
            ShowRyujinxConsoleCheck.Enabled = !WriteRyujinxLogCheck.Checked;
        }

        private void ShowBuildConsoleCheck_CheckedChanged(object sender, EventArgs e)
        {
            Settings.SHOW_BUILD_CONSOLE =  ShowBuildConsoleCheck.Checked;
            WriteBuildLogCheck.Enabled  = !ShowBuildConsoleCheck.Checked;
        }

        private void WriteBuildLogCheck_CheckedChanged(object sender, EventArgs e)
        {
            Settings.WRITE_BUILD_LOG      =  WriteBuildLogCheck.Checked;
            ShowBuildConsoleCheck.Enabled = !WriteBuildLogCheck.Checked;
        }

        private void UseNucleus_CheckedChanged(object sender, EventArgs e)
        {
            Settings.USE_NUCLEUS = UseNucleus.Checked;
        }

        private void GetMetadataCDN_CheckedChanged(object sender, EventArgs e)
        {
            if (GetMetadataCDN.Checked == true)
            {
                if (!File.Exists("keys.txt") && !File.Exists(NintendoCDN.CertPath))
                {
                    MessageBox.Show("keys.txt and " + NintendoCDN.CertPath + " are missing!", "Error");
                    GetMetadataCDN.Checked = false;
                    return;
                }
                else if (!File.Exists(NintendoCDN.CertPath))
                {
                    MessageBox.Show(NintendoCDN.CertPath + " is missing!", "Error");
                    GetMetadataCDN.Checked = false;
                    return;
                }
                else if (!File.Exists("keys.txt"))
                {
                    MessageBox.Show("keys.txt is missing!", "Error");
                    GetMetadataCDN.Checked = false;
                    return;
                }
            }

            Settings.GET_METADATA_FROM_CDN = GetMetadataCDN.Checked;
        }

        private void GameListIconSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox) sender;

            Settings.GAMELIST_ICON_SIZE = comboBox.Text;
        }

        private void DefaultApp_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = "Select a Default NRO"
            };

            fileDialog.Filter = "Homebrew Game (*.nro)|*.nro";

            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                Settings.DEFAULT_HOMEBREW_APP = fileDialog.FileName;
                DefaultAppPath.Text = fileDialog.FileName;
                fileDialog.Dispose();
            }
            else if (result == DialogResult.Cancel)
            {
                fileDialog.Dispose();
                return;
            }
        }

        private void GitLoginButton_Click(object sender, EventArgs e)
        {
            GitParser.GitLoggedIn(GitLoginUsername.Text, GitLoginEmail.Text);
        }

        private void ListView1_ItemActivate(object sender, EventArgs e)
        {
            string Branch = ((ListView)sender).SelectedItems[0].Text;

            DialogResult res = MessageBox.Show("Do you want to attempt to merge the branch \"" + Branch + "\"?", "Are you sure?", MessageBoxButtons.YesNo);

            if (res == DialogResult.Yes)
            {
                Process Git = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "-C \"" + MainForm.RyujinxDownloadPath + "\" fetch",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                Git.Start();

                Git.WaitForExit();

                if (Git.ExitCode != 0)
                {
                    DialogResult res1 = MessageBox.Show("The fetch failed!  Git exited with Code: " + Git.ExitCode, "Error", MessageBoxButtons.OK);
                    if (res1 == DialogResult.OK)
                    {
                        Git.Dispose();
                        return;
                    }
                }

                Git.StartInfo.Arguments = "-C \"" + MainForm.RyujinxDownloadPath + "\" pull origin " + Branch;

                Git.Start();

                Git.WaitForExit();

                if (Git.ExitCode != 0)
                {
                    int pullExit = Git.ExitCode;

                    Git.StartInfo.Arguments = "-C \"" + MainForm.RyujinxDownloadPath + "\" merge --abort";

                    Git.Start();

                    Git.WaitForExit();

                    if (Git.ExitCode != 0)
                    {
                        DialogResult res1 = MessageBox.Show("The merge abort failed!  Git exited with Code: " + Git.ExitCode, "Error", MessageBoxButtons.OK);
                        if (res1 == DialogResult.OK)
                        {
                            Git.Dispose();
                            return; // Abort!  This must be serious!
                        }
                    }

                    DialogResult res2 = MessageBox.Show("The merge failed!  Git exited with Code: " + pullExit, "Error", MessageBoxButtons.OK);
                    if (res2 == DialogResult.OK)
                    {
                        Git.Dispose();
                        return;
                    }
                    Git.Dispose();
                }

                DialogResult res3 = MessageBox.Show("The merge was a Success!  When you click OK, we will attempt to build Ryujinx.", "Success", MessageBoxButtons.OK);
                if (res3 == DialogResult.OK)
                {
                    Git.Dispose();
                    Process proc = new Process();

                    proc.StartInfo.FileName        = "dotnet";
                    proc.StartInfo.Arguments       = "build -c Release \"" + MainForm.RyujinxDownloadPath + "\\Ryujinx\"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow  = !Settings.SHOW_BUILD_CONSOLE;

                    proc.StartInfo.RedirectStandardError  = Settings.WRITE_BUILD_LOG;
                    proc.StartInfo.RedirectStandardOutput = Settings.WRITE_BUILD_LOG;

                    string LOG = "";

                    if (Settings.WRITE_BUILD_LOG)
                    {
                        proc.OutputDataReceived += (s, ev) => LOG += ev.Data + '\n';
                        proc.ErrorDataReceived  += (s, ev) => LOG += ev.Data + '\n';
                    }

                    // Build Ryujinx
                    Stopwatch TimeTook = new Stopwatch();

                    try
                    {
                        proc.Start();
                        TimeTook.Start();
                        if (Settings.WRITE_BUILD_LOG)
                        {
                            proc.BeginOutputReadLine();
                            proc.BeginErrorReadLine();
                        }
                    }
                    catch (Exception) // Make sure they have the .Net SDK Installed!
                    {
                        DialogResult dialogResult = MessageBox.Show("You need the .Net SDK installed to use this function!", "Error", MessageBoxButtons.OK);
                        if (DialogResult == DialogResult.OK)
                        {
                            return;
                        }
                    }

                    proc.WaitForExit();

                    TimeTook.Stop();

                    if (proc.ExitCode != 0) // Make sure nothing went wrong!
                    {
                        DialogResult res4 = MessageBox.Show("Something went wrong!  Dotnet Exit Code: " + proc.ExitCode);
                        if (res4 == DialogResult.OK)
                        {
                            proc.Dispose();
                            return;
                        }
                    }

                    if (Settings.WRITE_BUILD_LOG) using (StreamWriter sw = File.CreateText(MainForm.BuildLogFilePath)) sw.Write(LOG);

                    DialogResult res5 = MessageBox.Show("Ryujinx Build finished in " + ((double)(TimeTook.ElapsedMilliseconds) / 1000) + " Second(s).", "Success", MessageBoxButtons.OK);
                    if (res5 == DialogResult.OK)
                    {
                        return;
                    }
                }
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;

namespace LiveLogViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            try
            {
                string[] fileNamesToLog = new string[6];

                System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                String directory = Directory.GetCurrentDirectory();

                fileNamesToLog[0] = directory + "\\" + "log-file-AWSGlaicierTool.txt";
                fileNamesToLog[1] = directory + "\\" + "log-file-SlideLog.txt";
                fileNamesToLog[2] = directory + "\\" + "log-DPImageServiceHost.txt";
                fileNamesToLog[3] = directory + "\\" + "AperioReader.log";

                //Scan through all files selected in the fialog
                foreach (string File in fileNamesToLog)
                {
                    //Create the suffix required if the file is accessed across the network
                    string Suffix = "";

                    //Check to see if the file is already being monitored
                    if (CheckForExistingTab(File)) continue;

                    //Check to see if the file is being access across the network
                    Suffix = CheckForNetworkShare(File);

                    //If we can't find the file display an error message otherwise add the tab
                    if (!System.IO.File.Exists(File))
                    {
                        Console.WriteLine(string.Format("File not found '{0}'", File), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else AddNewTab(File, Suffix);
                }
            }
            catch (Exception ex)
            {
            }

        }

        internal static bool Frozen = false; //If false then TextBoxes will scroll to the bottom when text is appended

        private void AddButton_Click(object sender, EventArgs e)
        {
            //Show the OpenFileDialog
            if (LogOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Return if no files are selected
                if (LogOpenFileDialog.FileNames == null) return;

                //Scan through all files selected in the fialog
                foreach (string File in LogOpenFileDialog.FileNames)
                {
                    //Create the suffix required if the file is accessed across the network
                    string Suffix = "";

                    //Check to see if the file is already being monitored
                    if (CheckForExistingTab(File)) continue;

                    //Check to see if the file is being access across the network
                    Suffix = CheckForNetworkShare(File);

                    //If we can't find the file display an error message otherwise add the tab
                    if (!System.IO.File.Exists(File))
                    {
                        Console.WriteLine(string.Format("File not found '{0}'", File), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else AddNewTab(File, Suffix);
                }
            }
        }

        private void DeleteButton_Click(System.Object sender, System.EventArgs e)
        {
            //Delete the currently selected tab
            DeleteTab(MainTabControl.SelectedIndex);
        }

        private void FreezeButton_Click(object sender, EventArgs e)
        {
            //Toggle the freeze function
            switch (Frozen)
            {
                case false:
                    Frozen = true;
                    FreezeButton.Text = "Unfreeze";
                    break;
                case true:
                    Frozen = false;
                    FreezeButton.Text = "Freeze";
                    break;
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            //Clear the TextBox of the currently selected tab
            if (MainTabControl.SelectedTab != null)
                ((LogTabPage)MainTabControl.SelectedTab).TextBox.Clear();
        }

        private void AddNewTab(string FileName, string Suffix = "")
        {
            //Create a new LogTabPage and add it to the TabControl
            LogTabPage Page = new LogTabPage(FileName, Suffix);
            MainTabControl.TabPages.Add(Page);

            //Enable the controls
            ClearButton.Enabled = true;
            FreezeButton.Enabled = true;
            DeleteButton.Enabled = true;

            //Subscribe to the changed event
            Page.Watcher.Changed += new FileSystemEventHandler(Watcher_Changed);

            //Select the new page
            Page.Select();
        }

        private void DeleteTab(int i)
        {
            //Create a reference to the tab that will be deleted
            LogTabPage Page = (LogTabPage)MainTabControl.TabPages[i];

            //Unsubscribe from the event
            Page.Watcher.Changed -= new FileSystemEventHandler(Watcher_Changed);

            //Remove the page from the TabControl
            MainTabControl.TabPages.Remove(Page);

            //Dispose the objects
            Page.Watcher.Dispose();
            Page.Dispose();

            //If there are no tabs left then disable the controls and clear the LastUpdatedLabel
            if (MainTabControl.TabCount == 0) { ClearButton.Enabled = false; FreezeButton.Enabled = false; DeleteButton.Enabled = false; LastUpdatedLabel.Text = ""; }
        }

        //Occurs when the File being watched has changed
        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //Invoke the LastUpdate method if required
            if (this.InvokeRequired) this.Invoke(new Action(delegate() { LastUpdate(e); }));
            else LastUpdate(e);
        }

        private void LastUpdate(FileSystemEventArgs e)
        {
            //Notify the user which file was last updated and when
            LastUpdatedLabel.Text = string.Format("{0} at {1}", Path.GetFileName(e.FullPath), DateTime.Now.ToLongTimeString());
        }

        private static string CheckForNetworkShare(string File)
        {
            //Check to see if the file is being accessed over a network, if not return an empty string
            if (File.Substring(0, 2) == "\\\\")
            {
                string[] SplitString = Regex.Split(File, "\\\\");

                //Return the name/IP of the remote PC
                if (SplitString.Length > 2) return string.Format("on {0}", SplitString[2]);
            }
            return "";
        }

        private bool CheckForExistingTab(string File)
        {
            //Loop through the LogTabPages
            foreach (LogTabPage Page in MainTabControl.TabPages)
            {
                //If the file is being monitored then notify the user and return true
                if (Page.Watcher.FileName == File)
                {
                    Console.WriteLine(string.Format("File already being monitored '{0}'", File));
                    return true;
                }
            }
            //File isn't being monitored already return false
            return false;
        }
    }
}
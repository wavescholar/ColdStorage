using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

namespace LiveLogViewer
{
    class LogTabPage : TabPage
    {
        //The textbox where the log will be displayed
        internal RichTextBox TextBox = new RichTextBox();
        //The LogWatcher that monitors the file
        internal LogWatcher Watcher;

        //Constructor for the LogTabPage
        public LogTabPage(string FileName, string Suffix)
        {
            //Display the filename on the Tab
            this.Text = Path.GetFileName(string.Format("{0} {1}", FileName, Suffix));

            //Configure the TextBox
            TextBox.Dock = DockStyle.Fill;
            TextBox.BackColor = Color.White;
            TextBox.ReadOnly = true;

            //Add the TextBox to the LogTabPage
            this.Controls.Add(TextBox);

            //Create the LogWatcher
            CreateWatcher(FileName);
        }

        private void CreateWatcher(string FileName)
        {
            Watcher = new LogWatcher(FileName);

            //Set the directory of the file to monitor 
            Watcher.Path = Path.GetDirectoryName(FileName);

            //Raise events when the LastWrite or Size attribute is changed
            Watcher.NotifyFilter = (NotifyFilters.LastWrite | NotifyFilters.Size);

            //Filter out events for only this file
            Watcher.Filter = Path.GetFileName(FileName);

            //Subscribe to the event
            Watcher.TextChanged += new LogWatcher.LogWatcherEventHandler(Watcher_Changed);

            //Enable the event
            Watcher.EnableRaisingEvents = true;
        }

        //Occurs when the file is changed
        void Watcher_Changed(object sender, LogWatcherEventArgs e)
        {
            //Invoke the AppendText method if required
            if (TextBox.InvokeRequired) this.Invoke(new Action(delegate() { AppendText(e.Contents); }));
            else AppendText(e.Contents);
        }

        private void AppendText(string Text)
        {
            //Append the new text to the TextBox
            TextBox.Text += Text;

            //If the Frozen function isn't enabled then scroll to the bottom of the TextBox
            if (!MainForm.Frozen)
            {
                TextBox.SelectionStart = TextBox.Text.Length;
                TextBox.SelectionLength = 0;
                TextBox.ScrollToCaret();
            }
        }
    }
}


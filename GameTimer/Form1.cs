using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameTimer
{
    public partial class Form1 : Form
    {
        /* Borderless window dragging */
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        /* */

        private string currentGame;
        private TimerConfig config;

        private DateTime startTime;

        public TimeSpan CurrentTime 
        { 
            get { return DateTime.Now - startTime; } 
        }
        public string CurrentTimeString
        {
            get { return ((int)CurrentTime.TotalHours).ToString("D2") + CurrentTime.ToString(TimerConfig.outputFormat); }
        }
        public long CurrentTimeTicks
        {
            get { return CurrentTime.Ticks; }
        }
        
        Font fontTimer = new Font("Courier New", 20);
        Font fontName = new Font("Courier New", 14, FontStyle.Italic);
        Brush brush = new SolidBrush(Color.White);
        Brush brushComplete = new SolidBrush(Color.Green);
        Brush brushPause = new SolidBrush(Color.Yellow);

        private void StartTimers()
        {
            timerUpdateDisplay.Start();
            timerUpdate.Start();
            timerSave.Start();
        }

        private void StopTimers()
        {
            timerUpdateDisplay.Stop();
            timerUpdate.Stop();
            timerSave.Stop();
        }

        public Form1()
        {
            config = new TimerConfig(TimerConfig.fileName);

            InitializeComponent();
            StartTimers();
            markAsCompleteToolStripMenuItem.Enabled = false;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            // to enable dragging the entire window
            if(e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // runs every 10 ms
        private void timerUpdateDisplay_Tick(object sender, EventArgs e)
        {
            if(markAsCompleteToolStripMenuItem.Checked || pauseToolStripMenuItem.Checked)
            {
                startTime = DateTime.Now - TimeSpan.Parse(config.GetEntryTime(currentGame));
            }

            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            if(config != null && currentGame != null)
            {
                SizeF textMeasure = e.Graphics.MeasureString(currentGame, fontName);

                if(textMeasure.Width < Width)
                {
                    e.Graphics.DrawString(currentGame, fontName, brush, 1.0f, 5.0f);
                }
                else
                {
                    e.Graphics.ScaleTransform((Width - 18.0f) / textMeasure.Width, 1.0f);
                    e.Graphics.DrawString(currentGame, fontName, brush, 1.0f, 5.0f);
                }
                e.Graphics.ResetTransform();

                Brush brushToUse = brush;
                if(markAsCompleteToolStripMenuItem.Checked)
                {
                    brushToUse = brushComplete;
                }
                else if(pauseToolStripMenuItem.Checked)
                {
                    brushToUse = brushPause;
                }

                e.Graphics.DrawString(CurrentTimeString, fontTimer, brushToUse, 1.0f, 30.0f);

                Console.WriteLine(CurrentTimeString);

                markAsCompleteToolStripMenuItem.Enabled = true;
                pauseToolStripMenuItem.Enabled = true;

                if(markAsCompleteToolStripMenuItem.Checked)
                {
                    pauseToolStripMenuItem.Enabled = false;
                }
                else if(pauseToolStripMenuItem.Checked)
                {
                    //markAsCompleteToolStripMenuItem.Enabled = false;
                }
            }
            else
            {
                // draw nothing

                markAsCompleteToolStripMenuItem.Enabled = false;
                pauseToolStripMenuItem.Enabled = false;
            }
        }

        // runs every second
        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Save();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(this, new Point(e.X, e.Y));
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentGame = null;
            pauseToolStripMenuItem.Checked = false;
            pauseToolStripMenuItem.Enabled = false;
            markAsCompleteToolStripMenuItem.Checked = false;
            markAsCompleteToolStripMenuItem.Enabled = false;

            //Close();
            //Environment.Exit(0);
        }

        // runs every minute
        private void timerSave_Tick(object sender, EventArgs e)
        {
            Save();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save("Saved");
        }

        private void launchTimesxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(TimerConfig.fileName);
        }

        private void markAsCompleteToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if(config == null || currentGame == null) return;

            config.SetComplete(currentGame, markAsCompleteToolStripMenuItem.Checked);
            UpdateTime();

            if(markAsCompleteToolStripMenuItem.Checked)
            {
                //pauseToolStripMenuItem.Enabled = false;
                timerSave.Stop();
            }
            else
            {
                timerSave.Start();
            }

            Save();
            Invalidate();
        }

        private void pauseToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if(config == null) return;

            UpdateTime();

            if(pauseToolStripMenuItem.Checked)
            {
                timerSave.Stop();
            }
            else
            {
                timerSave.Start();
            }

            Save();
            Invalidate();
        }

        private void reloadTimesxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopTimers();

            config.LoadConfig(TimerConfig.fileName);
            MessageBox.Show("Reloaded", "Config");
            startTime = DateTime.Now - TimeSpan.Parse(config.GetEntryTime(currentGame));

            StartTimers();
        }

        private void Save(string message="")
        {
            if(currentGame != null)
            {
                if(config.ContainsKey(currentGame))
                {
                    config.SaveConfig(TimerConfig.fileName, currentGame);
                    if(message != "")
                    {
                        MessageBox.Show(this, message, "Save");
                    }
                }
            }
        }

        private void UpdateTime()
        {
            if(currentGame == null) return;

            // config.PrintEntries();
            config.UpdateTime(currentGame, CurrentTime.ToString(TimerConfig.timeFormat));
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                StopTimers();
                string inputValue = "";
                DialogResult inputResult = HelperForms.InputBox("Input name", "Enter the name of the game.", ref inputValue);
                if(inputResult == DialogResult.OK)
                {
                    UpdateTime();
                    Save();

                    config.AddEntry(new TimerEntry(inputValue, new TimeSpan(0).ToString(TimerConfig.timeFormat), false));
                    startTime = DateTime.Now;
                    currentGame = inputValue;
                    markAsCompleteToolStripMenuItem.Checked = false;
                    pauseToolStripMenuItem.Checked = false;

                    Save();
                }
                else
                {
                    Close();
                    Environment.Exit(0);
                }

                StartTimers();
            }
            catch(Exception ex)
            {
                StopTimers();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Environment.Exit(-1);
            }
        }

        private void changeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                StopTimers();

                string inputValue = "";
                List<string> sortedNames = new List<string>(config.Names);
                sortedNames.Sort();
                DialogResult inputResult = HelperForms.ChangeGameBox(sortedNames, ref inputValue);
                if(inputResult == DialogResult.OK)
                {
                    if(!config.ContainsKey(inputValue))
                    {
                        throw new Exception(inputValue + " does not exist.");
                    }

                    UpdateTime();
                    Save();
                    currentGame = inputValue;
                    startTime = DateTime.Now - TimeSpan.Parse(config.GetEntryTime(currentGame));
                    markAsCompleteToolStripMenuItem.Enabled = true;
                    markAsCompleteToolStripMenuItem.Checked = config.IsComplete(currentGame);
                    pauseToolStripMenuItem.Checked = false;
                    Save();
                }
                else
                {
                    //Close();
                    //Environment.Exit(0);
                }

                StartTimers();
            }
            catch(Exception ex)
            {
                StopTimers();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Environment.Exit(-1);
            }
        }
    }
}
using Learnr;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace learnr2drive
{
    public partial class GameWindow : Form
    {
        #region private vars
        private Timer uiTimer;
        private UiSettings uiSettings;
        private Renderer uiRenderer;
        private DateTime uiLastFrameTime;
        private int uiMouseState; // 0=None; 1=BtnDown 
        private Color bgColour;
        private Color bgColour1;
        #endregion

        #region init

        public GameWindow(string[] args)
        {
            InitializeComponent();
            uiSettings = new UiSettings(this.ClientSize.Width, this.ClientSize.Height, 120);
            InitGlobals(args);
            InitUI(uiSettings, args);
        }

        private void InitGlobals(string[] args)
        {
            Global.Init();
            args = args.ToList().ConvertAll(s => s.Trim().ToUpper()).ToArray();

            bool loadSuccess = LoadGlobals(true);
            if (!loadSuccess)
            {
                string configpath = Path.Combine(Environment.CurrentDirectory, Global.ConfigFileName);
                Global.SaveSettingsToFile(configpath, Global.R);
            }

            if (Global.R.RenderFullScreenOnLoad)
            {
                this.TopMost = true;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            if (args.Contains("/N")) Global.R.Learnr.InitSize = 0;
        }

        private void InitUI(UiSettings info, string[] args)
        {
            uiRenderer = new Renderer(uiSettings);

            string[] paths = args.Where(s => File.Exists(s)).ToArray();
            if (paths?.Length > 0)
                uiSettings.SendMessage(UiSettingsEventArgs.EventType.WindowEvent, new object[] { Global.LoadPathsEvent, paths }, UiSettingsEventArgs.FLAGS_UI);

            uiLastFrameTime = DateTime.Now;

            uiLastFrameTime = DateTime.Now;

            uiTimer = new Timer();
            uiTimer.Interval = (1000 / info.FPS);
            uiTimer.Tick += UiScreenRefresh;
            uiTimer.Start();
            bgColour = Color.DimGray;
            bgColour = Color.DarkGray;
            NetworkManagerLSTM.BaseColour = bgColour;
            NetworkManagerLSTM.BaseTextColour = Color.White;
        }

        #endregion

        #region drawing methods

        private void UiScreenRefresh(object sender, EventArgs e)
        {
            this.Invalidate(); // calls GameWindow_Paint on every timer tick
        }

        private void GameWindow_Paint(object sender, PaintEventArgs e)
        {
            DateTime nowTime = DateTime.Now;
            int frameDelta = (nowTime - uiLastFrameTime).Milliseconds;

            e.Graphics.Clear(bgColour);
            Rectangle r = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            e.Graphics.FillRectangle(new LinearGradientBrush(r, bgColour, bgColour1, 90), r);
            e.Graphics.Clear(Color.Black);

            uiRenderer.RenderFrame(e.Graphics, frameDelta);

            e.Graphics.Flush();

            uiLastFrameTime = nowTime;
        }

        public static bool SaveNetwork(NeuralNetworkLSTM network)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = Global.ConfigNetworkFileFilter;
            save.InitialDirectory = Environment.CurrentDirectory;
            if (save.ShowDialog() == DialogResult.OK)
            {
                using (TextWriter reader = new StreamWriter(save.FileName))
                {
                    NetworkManagerLSTM.NetworkSaveFile data = new NetworkManagerLSTM.NetworkSaveFile()
                    {
                        Axons = network.Axons,
                        LSTM = network.LSTM.ToArray(),
                        LSTM_Gates = network.LSTM.Gates
                    };
                    string s = XmlExtension.Serialize(data);
                    reader.WriteLine(s);
                }
                return true;
            }
            return false;
        }

        #endregion

        #region user input event handlers

        private void GameWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape) Close();
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyUpEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyPressEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_KeyDown(object sender, KeyEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyDownEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_Click(object sender, EventArgs e)
        {

        }

        private void GameWindow_MouseDown(object sender, MouseEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI | UiSettingsEventArgs.FLAGS_UI_MOUSEDOWN);
            uiMouseState = 1;
        }

        private void GameWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (uiMouseState != 0)
                uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI | UiSettingsEventArgs.FLAGS_UI_MOUSEWHEEL);
        }

        private void GameWindow_MouseUp(object sender, MouseEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI | UiSettingsEventArgs.FLAGS_UI_MOUSEUP);
            uiMouseState = 0;
        }

        private void GameWindow_Close(object sender, FormClosingEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.WindowEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }


        private void GameWindow_DragDrop(object sender, DragEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.WindowEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        #endregion

        #region io methods

        public static bool LoadGlobals()
        {
            return LoadGlobals(false);
        }
        public static bool LoadGlobals(bool defPath)
        {
            if (defPath)
            {
                string configpath = Path.Combine(Environment.CurrentDirectory, Global.ConfigFileName);
                if (File.Exists(configpath)) Global.LoadSettingsFromFile(configpath, out Global.R);
                else return false;
                return true;
            }
            else
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = Global.ConfigFileFilter;
                open.InitialDirectory = Environment.CurrentDirectory;

                if (open.ShowDialog() == DialogResult.OK)
                {
                    Global.LoadSettingsFromFile(open.FileName, out Global.R);
                    return true;
                }
                return false;
            }
        }
        public static bool SaveGlobals()
        {
            return SaveGlobals(false);
        }
        public static bool SaveGlobals(bool defPath)
        {
            if (defPath)
            {
                string configpath = Path.Combine(Environment.CurrentDirectory, Global.ConfigFileName);
                Global.SaveSettingsToFile(configpath, Global.R);
                return true;
            }
            else
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = Global.ConfigFileFilter;
                save.InitialDirectory = Environment.CurrentDirectory;
                save.FileName = Global.ConfigFileName;
                if (save.ShowDialog() == DialogResult.OK)
                {
                    Global.SaveSettingsToFile(save.FileName, Global.R);
                    return true;
                }
                return false;
            }
        }
        public static string LoadNetworkPath()
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Multiselect = true;
            open.Filter = Global.ConfigNetworkFileFilter;
            open.InitialDirectory = Environment.CurrentDirectory;
            if (open.ShowDialog() == DialogResult.OK)
            {
                return string.Join("\n", open.FileNames);
            }
            return null;
        }

        #endregion
    }
}

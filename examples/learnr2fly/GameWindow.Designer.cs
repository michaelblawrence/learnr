namespace learnr2fly
{
    partial class GameWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GameWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(2827, 1691);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "GameWindow";
            this.Text = "Game";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GameWindow_Close);
            this.Click += new System.EventHandler(this.GameWindow_Click);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.GameWindow_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.GameWindow_DragEnter);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GameWindow_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GameWindow_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.GameWindow_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GameWindow_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GameWindow_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GameWindow_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GameWindow_MouseUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.GameWindow_MouseWheel);
            this.ResumeLayout(false);

        }

        #endregion
    }
}


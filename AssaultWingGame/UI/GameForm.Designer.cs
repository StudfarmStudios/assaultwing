namespace AW2.UI
{
    partial class GameForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameForm));
            this._gameView = new AW2.Core.GraphicsDeviceControl();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this._logView = new System.Windows.Forms.TextBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _gameView
            // 
            this._gameView.CausesValidation = false;
            this._gameView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gameView.GraphicsDeviceService = null;
            this._gameView.Location = new System.Drawing.Point(0, 0);
            this._gameView.Margin = new System.Windows.Forms.Padding(0);
            this._gameView.Name = "_gameView";
            this._gameView.Size = new System.Drawing.Size(984, 762);
            this._gameView.TabIndex = 0;
            this._gameView.TabStop = false;
            this._gameView.Text = "game view";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this._gameView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this._logView);
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Panel2MinSize = 0;
            this.splitContainer1.Size = new System.Drawing.Size(984, 762);
            this.splitContainer1.SplitterDistance = 328;
            this.splitContainer1.TabIndex = 1;
            // 
            // _logView
            // 
            this._logView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._logView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logView.Location = new System.Drawing.Point(0, 0);
            this._logView.Margin = new System.Windows.Forms.Padding(0);
            this._logView.MaxLength = 0;
            this._logView.Multiline = true;
            this._logView.Name = "_logView";
            this._logView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._logView.Size = new System.Drawing.Size(150, 46);
            this._logView.TabIndex = 0;
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 762);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1000, 800);
            this.Name = "GameForm";
            this.Text = "Assault Wing";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AW2.Core.GraphicsDeviceControl _gameView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox _logView;
    }
}
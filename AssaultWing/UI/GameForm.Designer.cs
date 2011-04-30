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
            this._splitContainer = new System.Windows.Forms.SplitContainer();
            this._gameView = new AW2.Core.GraphicsDeviceControl();
            this._logView = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
            this._splitContainer.Panel1.SuspendLayout();
            this._splitContainer.Panel2.SuspendLayout();
            this._splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // _splitContainer
            // 
            this._splitContainer.BackColor = System.Drawing.SystemColors.WindowFrame;
            this._splitContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this._splitContainer.ForeColor = System.Drawing.SystemColors.Control;
            this._splitContainer.Location = new System.Drawing.Point(0, 0);
            this._splitContainer.Margin = new System.Windows.Forms.Padding(0);
            this._splitContainer.Name = "_splitContainer";
            this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            this._splitContainer.Panel1.Controls.Add(this._gameView);
            this._splitContainer.Panel1MinSize = 50;
            // 
            // _splitContainer.Panel2
            // 
            this._splitContainer.Panel2.Controls.Add(this._logView);
            this._splitContainer.Panel2Collapsed = true;
            this._splitContainer.Panel2MinSize = 0;
            this._splitContainer.Size = new System.Drawing.Size(984, 762);
            this._splitContainer.SplitterDistance = 544;
            this._splitContainer.SplitterWidth = 2;
            this._splitContainer.TabIndex = 1;
            this._splitContainer.TabStop = false;
            // 
            // _gameView
            // 
            this._gameView.BackColor = System.Drawing.Color.Maroon;
            this._gameView.CausesValidation = false;
            this._gameView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gameView.ForeColor = System.Drawing.SystemColors.Control;
            this._gameView.GraphicsDeviceService = null;
            this._gameView.Location = new System.Drawing.Point(0, 0);
            this._gameView.Margin = new System.Windows.Forms.Padding(0);
            this._gameView.Name = "_gameView";
            this._gameView.Size = new System.Drawing.Size(982, 760);
            this._gameView.TabIndex = 0;
            this._gameView.TabStop = false;
            this._gameView.Text = "game view";
            // 
            // _logView
            // 
            this._logView.BackColor = System.Drawing.Color.Black;
            this._logView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._logView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logView.ForeColor = System.Drawing.Color.Chartreuse;
            this._logView.HideSelection = false;
            this._logView.Location = new System.Drawing.Point(0, 0);
            this._logView.Margin = new System.Windows.Forms.Padding(0);
            this._logView.MaxLength = 0;
            this._logView.Multiline = true;
            this._logView.Name = "_logView";
            this._logView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._logView.Size = new System.Drawing.Size(982, 214);
            this._logView.TabIndex = 0;
            this._logView.TabStop = false;
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(984, 762);
            this.Controls.Add(this._splitContainer);
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(2048, 2048);
            this.MinimumSize = new System.Drawing.Size(1000, 800);
            this.Name = "GameForm";
            this.Text = "Assault Wing";
            this._splitContainer.Panel1.ResumeLayout(false);
            this._splitContainer.Panel2.ResumeLayout(false);
            this._splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
            this._splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AW2.Core.GraphicsDeviceControl _gameView;
        private System.Windows.Forms.SplitContainer _splitContainer;
        private System.Windows.Forms.TextBox _logView;
    }
}
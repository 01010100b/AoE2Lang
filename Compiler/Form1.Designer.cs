namespace Compiler
{
    partial class Form1
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
            this.ButtonCompile = new System.Windows.Forms.Button();
            this.ButtonTest = new System.Windows.Forms.Button();
            this.ButtonCompilerTest = new System.Windows.Forms.Button();
            this.CheckFull = new System.Windows.Forms.CheckBox();
            this.ButtonLoad = new System.Windows.Forms.Button();
            this.ButtonUnitInfo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ButtonCompile
            // 
            this.ButtonCompile.Enabled = false;
            this.ButtonCompile.Location = new System.Drawing.Point(476, 61);
            this.ButtonCompile.Name = "ButtonCompile";
            this.ButtonCompile.Size = new System.Drawing.Size(254, 58);
            this.ButtonCompile.TabIndex = 0;
            this.ButtonCompile.Text = "Compile";
            this.ButtonCompile.UseVisualStyleBackColor = true;
            this.ButtonCompile.Click += new System.EventHandler(this.ButtonCompile_Click);
            // 
            // ButtonTest
            // 
            this.ButtonTest.Enabled = false;
            this.ButtonTest.Location = new System.Drawing.Point(476, 177);
            this.ButtonTest.Name = "ButtonTest";
            this.ButtonTest.Size = new System.Drawing.Size(254, 58);
            this.ButtonTest.TabIndex = 1;
            this.ButtonTest.Text = "Test";
            this.ButtonTest.UseVisualStyleBackColor = true;
            this.ButtonTest.Click += new System.EventHandler(this.ButtonTest_Click);
            // 
            // ButtonCompilerTest
            // 
            this.ButtonCompilerTest.Enabled = false;
            this.ButtonCompilerTest.Location = new System.Drawing.Point(476, 300);
            this.ButtonCompilerTest.Name = "ButtonCompilerTest";
            this.ButtonCompilerTest.Size = new System.Drawing.Size(254, 58);
            this.ButtonCompilerTest.TabIndex = 2;
            this.ButtonCompilerTest.Text = "Test Compiler";
            this.ButtonCompilerTest.UseVisualStyleBackColor = true;
            this.ButtonCompilerTest.Click += new System.EventHandler(this.ButtonCompilerTest_Click);
            // 
            // CheckFull
            // 
            this.CheckFull.AutoSize = true;
            this.CheckFull.Checked = true;
            this.CheckFull.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckFull.Location = new System.Drawing.Point(499, 131);
            this.CheckFull.Name = "CheckFull";
            this.CheckFull.Size = new System.Drawing.Size(81, 17);
            this.CheckFull.TabIndex = 3;
            this.CheckFull.Text = "Full (slower)";
            this.CheckFull.UseVisualStyleBackColor = true;
            // 
            // ButtonLoad
            // 
            this.ButtonLoad.Location = new System.Drawing.Point(46, 61);
            this.ButtonLoad.Name = "ButtonLoad";
            this.ButtonLoad.Size = new System.Drawing.Size(254, 58);
            this.ButtonLoad.TabIndex = 4;
            this.ButtonLoad.Text = "Load mod";
            this.ButtonLoad.UseVisualStyleBackColor = true;
            this.ButtonLoad.Click += new System.EventHandler(this.ButtonLoad_Click);
            // 
            // ButtonUnitInfo
            // 
            this.ButtonUnitInfo.Enabled = false;
            this.ButtonUnitInfo.Location = new System.Drawing.Point(46, 152);
            this.ButtonUnitInfo.Name = "ButtonUnitInfo";
            this.ButtonUnitInfo.Size = new System.Drawing.Size(254, 58);
            this.ButtonUnitInfo.TabIndex = 5;
            this.ButtonUnitInfo.Text = "Generate Unit Info";
            this.ButtonUnitInfo.UseVisualStyleBackColor = true;
            this.ButtonUnitInfo.Click += new System.EventHandler(this.ButtonUnitInfo_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.ButtonUnitInfo);
            this.Controls.Add(this.ButtonLoad);
            this.Controls.Add(this.CheckFull);
            this.Controls.Add(this.ButtonCompilerTest);
            this.Controls.Add(this.ButtonTest);
            this.Controls.Add(this.ButtonCompile);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonCompile;
        private System.Windows.Forms.Button ButtonTest;
        private System.Windows.Forms.Button ButtonCompilerTest;
        private System.Windows.Forms.CheckBox CheckFull;
        private System.Windows.Forms.Button ButtonLoad;
        private System.Windows.Forms.Button ButtonUnitInfo;
    }
}


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
            this.SuspendLayout();
            // 
            // ButtonCompile
            // 
            this.ButtonCompile.Location = new System.Drawing.Point(70, 50);
            this.ButtonCompile.Name = "ButtonCompile";
            this.ButtonCompile.Size = new System.Drawing.Size(254, 58);
            this.ButtonCompile.TabIndex = 0;
            this.ButtonCompile.Text = "Compile";
            this.ButtonCompile.UseVisualStyleBackColor = true;
            this.ButtonCompile.Click += new System.EventHandler(this.ButtonCompile_Click);
            // 
            // ButtonTest
            // 
            this.ButtonTest.Location = new System.Drawing.Point(70, 166);
            this.ButtonTest.Name = "ButtonTest";
            this.ButtonTest.Size = new System.Drawing.Size(254, 58);
            this.ButtonTest.TabIndex = 1;
            this.ButtonTest.Text = "Test";
            this.ButtonTest.UseVisualStyleBackColor = true;
            this.ButtonTest.Click += new System.EventHandler(this.ButtonTest_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.ButtonTest);
            this.Controls.Add(this.ButtonCompile);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonCompile;
        private System.Windows.Forms.Button ButtonTest;
    }
}


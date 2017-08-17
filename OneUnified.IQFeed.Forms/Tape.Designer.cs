namespace OneUnified.IQFeed.Forms {
  partial class frmTape {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing ) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.lvTape = new System.Windows.Forms.ListView();
      this.colTime = new System.Windows.Forms.ColumnHeader();
      this.colBATE = new System.Windows.Forms.ColumnHeader();
      this.colSize = new System.Windows.Forms.ColumnHeader();
      this.colPrice = new System.Windows.Forms.ColumnHeader();
      this.colExchange = new System.Windows.Forms.ColumnHeader();
      this.SuspendLayout();
      // 
      // lvTape
      // 
      this.lvTape.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.lvTape.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTime,
            this.colBATE,
            this.colSize,
            this.colPrice,
            this.colExchange});
      this.lvTape.FullRowSelect = true;
      this.lvTape.GridLines = true;
      this.lvTape.Location = new System.Drawing.Point(12, 12);
      this.lvTape.MultiSelect = false;
      this.lvTape.Name = "lvTape";
      this.lvTape.Size = new System.Drawing.Size(281, 388);
      this.lvTape.TabIndex = 0;
      this.lvTape.UseCompatibleStateImageBehavior = false;
      this.lvTape.View = System.Windows.Forms.View.Details;
      // 
      // colTime
      // 
      this.colTime.Text = "Time";
      this.colTime.Width = 53;
      // 
      // colBATE
      // 
      this.colBATE.Text = "BATE";
      // 
      // colSize
      // 
      this.colSize.Text = "Size";
      this.colSize.Width = 42;
      // 
      // colPrice
      // 
      this.colPrice.Text = "Price";
      // 
      // colExchange
      // 
      this.colExchange.Text = "Exch";
      // 
      // frmTape
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(305, 412);
      this.Controls.Add(this.lvTape);
      this.Name = "frmTape";
      this.Text = "Tape";
      this.Load += new System.EventHandler(this.frmTape_Load);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ColumnHeader colTime;
    private System.Windows.Forms.ColumnHeader colBATE;
    private System.Windows.Forms.ColumnHeader colSize;
    private System.Windows.Forms.ColumnHeader colPrice;
    private System.Windows.Forms.ColumnHeader colExchange;
    public System.Windows.Forms.ListView lvTape;

  }
}
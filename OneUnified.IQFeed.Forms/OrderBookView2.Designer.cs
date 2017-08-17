namespace OneUnified.IQFeed.Forms {
  partial class frmOrderBookView2 {
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
      this.lvOrderBookView2 = new System.Windows.Forms.ListView();
      this.chBidSize = new System.Windows.Forms.ColumnHeader();
      this.chPrice = new System.Windows.Forms.ColumnHeader();
      this.chAskSize = new System.Windows.Forms.ColumnHeader();
      this.chNull = new System.Windows.Forms.ColumnHeader();
      this.SuspendLayout();
      // 
      // lvOrderBookView2
      // 
      this.lvOrderBookView2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.lvOrderBookView2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chNull,
            this.chBidSize,
            this.chPrice,
            this.chAskSize});
      this.lvOrderBookView2.FullRowSelect = true;
      this.lvOrderBookView2.GridLines = true;
      this.lvOrderBookView2.Location = new System.Drawing.Point(12, 12);
      this.lvOrderBookView2.MultiSelect = false;
      this.lvOrderBookView2.Name = "lvOrderBookView2";
      this.lvOrderBookView2.Size = new System.Drawing.Size(171, 776);
      this.lvOrderBookView2.TabIndex = 0;
      this.lvOrderBookView2.UseCompatibleStateImageBehavior = false;
      this.lvOrderBookView2.View = System.Windows.Forms.View.Details;
      // 
      // chBidSize
      // 
      this.chBidSize.Text = "Size";
      this.chBidSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.chBidSize.Width = 45;
      // 
      // chPrice
      // 
      this.chPrice.Text = "Price";
      this.chPrice.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      // 
      // chAskSize
      // 
      this.chAskSize.Text = "Size";
      this.chAskSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.chAskSize.Width = 45;
      // 
      // chNull
      // 
      this.chNull.Text = "";
      this.chNull.Width = 2;
      // 
      // frmOrderBookView2
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(197, 800);
      this.Controls.Add(this.lvOrderBookView2);
      this.Name = "frmOrderBookView2";
      this.Text = "Order Book";
      this.Load += new System.EventHandler(this.frmOrderBook_Load);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ColumnHeader chBidSize;
    private System.Windows.Forms.ColumnHeader chPrice;
    private System.Windows.Forms.ColumnHeader chAskSize;
    public System.Windows.Forms.ListView lvOrderBookView2;
    private System.Windows.Forms.ColumnHeader chNull;

  }
}
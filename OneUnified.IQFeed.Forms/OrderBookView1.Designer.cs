namespace OneUnified.IQFeed.Forms {
  partial class frmOrderBookView1 {
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
      this.lvBid = new System.Windows.Forms.ListView();
      this.chBidMMID = new System.Windows.Forms.ColumnHeader();
      this.chBidCount = new System.Windows.Forms.ColumnHeader();
      this.chBid = new System.Windows.Forms.ColumnHeader();
      this.chBidSize = new System.Windows.Forms.ColumnHeader();
      this.chBidCumCnt = new System.Windows.Forms.ColumnHeader();
      this.chBidTime = new System.Windows.Forms.ColumnHeader();
      this.lvAsk = new System.Windows.Forms.ListView();
      this.chAskMMID = new System.Windows.Forms.ColumnHeader();
      this.chAskCount = new System.Windows.Forms.ColumnHeader();
      this.chAsk = new System.Windows.Forms.ColumnHeader();
      this.chAskSize = new System.Windows.Forms.ColumnHeader();
      this.chAskCumCnt = new System.Windows.Forms.ColumnHeader();
      this.chAskTime = new System.Windows.Forms.ColumnHeader();
      this.SuspendLayout();
      // 
      // lvBid
      // 
      this.lvBid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.lvBid.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chBidMMID,
            this.chBid,
            this.chBidSize,
            this.chBidCumCnt,
            this.chBidTime,
            this.chBidCount});
      this.lvBid.FullRowSelect = true;
      this.lvBid.GridLines = true;
      this.lvBid.Location = new System.Drawing.Point(12, 12);
      this.lvBid.Name = "lvBid";
      this.lvBid.Size = new System.Drawing.Size(315, 413);
      this.lvBid.TabIndex = 0;
      this.lvBid.UseCompatibleStateImageBehavior = false;
      this.lvBid.View = System.Windows.Forms.View.Details;
      // 
      // chBidMMID
      // 
      this.chBidMMID.Text = "MMID";
      this.chBidMMID.Width = 50;
      // 
      // chBidCount
      // 
      this.chBidCount.Text = "Cnt";
      this.chBidCount.Width = 55;
      // 
      // chBid
      // 
      this.chBid.Text = "Bid";
      // 
      // chBidSize
      // 
      this.chBidSize.Text = "Size";
      this.chBidSize.Width = 40;
      // 
      // chBidCumCnt
      // 
      this.chBidCumCnt.Text = "Cum";
      this.chBidCumCnt.Width = 40;
      // 
      // chBidTime
      // 
      this.chBidTime.Text = "Time";
      this.chBidTime.Width = 55;
      // 
      // lvAsk
      // 
      this.lvAsk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.lvAsk.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chAskMMID,
            this.chAsk,
            this.chAskSize,
            this.chAskCumCnt,
            this.chAskTime,
            this.chAskCount});
      this.lvAsk.FullRowSelect = true;
      this.lvAsk.GridLines = true;
      this.lvAsk.Location = new System.Drawing.Point(333, 12);
      this.lvAsk.Name = "lvAsk";
      this.lvAsk.Size = new System.Drawing.Size(317, 413);
      this.lvAsk.TabIndex = 1;
      this.lvAsk.UseCompatibleStateImageBehavior = false;
      this.lvAsk.View = System.Windows.Forms.View.Details;
      // 
      // chAskMMID
      // 
      this.chAskMMID.Text = "MMID";
      this.chAskMMID.Width = 50;
      // 
      // chAskCount
      // 
      this.chAskCount.Text = "Cnt";
      this.chAskCount.Width = 55;
      // 
      // chAsk
      // 
      this.chAsk.Text = "Ask";
      // 
      // chAskSize
      // 
      this.chAskSize.Text = "Size";
      this.chAskSize.Width = 40;
      // 
      // chAskCumCnt
      // 
      this.chAskCumCnt.Text = "Cum";
      this.chAskCumCnt.Width = 40;
      // 
      // chAskTime
      // 
      this.chAskTime.Text = "Time";
      this.chAskTime.Width = 55;
      // 
      // frmOrderBookView1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(663, 437);
      this.Controls.Add(this.lvAsk);
      this.Controls.Add(this.lvBid);
      this.Name = "frmOrderBookView1";
      this.Text = "Order Book";
      this.Load += new System.EventHandler(this.frmOrderBook_Load);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ColumnHeader chBidMMID;
    private System.Windows.Forms.ColumnHeader chBidCount;
    private System.Windows.Forms.ColumnHeader chBid;
    private System.Windows.Forms.ColumnHeader chBidSize;
    private System.Windows.Forms.ColumnHeader chBidTime;
    private System.Windows.Forms.ColumnHeader chAskMMID;
    private System.Windows.Forms.ColumnHeader chAskCount;
    private System.Windows.Forms.ColumnHeader chAsk;
    private System.Windows.Forms.ColumnHeader chAskSize;
    private System.Windows.Forms.ColumnHeader chAskTime;
    public System.Windows.Forms.ListView lvBid;
    public System.Windows.Forms.ListView lvAsk;
    private System.Windows.Forms.ColumnHeader chBidCumCnt;
    private System.Windows.Forms.ColumnHeader chAskCumCnt;
  }
}
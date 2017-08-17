//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using OneUnified.IQFeed;

namespace OneUnified.IQFeed.Forms {
  public partial class frmTape : Form {

    private string Symbol;
    private IQFeed iqf;
    private SymbolEvent se;
    private int NumRows = 25;

    double LastBid;
    double LastBidSize;
    double LastAsk;
    double LastAskSize;
    double SessionHi;
    double SessionLo;
    bool InitQuoteVars;

    Queue qUpdateMessages;

    public frmTape() {

      InitializeComponent();

      qUpdateMessages = new Queue(10);

      LastBid = 0;
      LastBidSize = 0;
      LastAsk = 0;
      LastAskSize = 0;
      SessionHi = 0;
      SessionLo = 0;
      InitQuoteVars = true;
    }

    private void frmTape_Load( object sender, EventArgs e ) {
      this.ClientSizeChanged += new EventHandler(frmTape_ClientSizeChanged);
      this.FormClosing += new FormClosingEventHandler(frmTape_FormClosing);
      this.FormClosed += new FormClosedEventHandler(frmTape_FormClosed);
      this.RegionChanged += new EventHandler(frmTape_RegionChanged);
      this.Resize += new EventHandler(frmTape_Resize);
      this.ResizeBegin += new EventHandler(frmTape_ResizeBegin);
      this.ResizeEnd += new EventHandler(frmTape_ResizeEnd);
      this.Shown += new EventHandler(frmTape_Shown);

      for (int i = 1; i <= NumRows; i++) {
        string[] items = { "-", "-", "-", "-", "-" };
        ListViewItem lvi = new ListViewItem(items );
        lvTape.Items.Add(lvi);
      }


    }

    public void StartWatch( string Symbol, IQFeed iqf ) {
      this.Symbol = Symbol;
      this.iqf = iqf;
      this.Text = "T&S - " + Symbol;
      se = iqf.startWatch(Symbol);
      se.HandleUpdateMessage += new UpdateMessageHandler(se_HandleUpdateMessage);
    }

    public void StopWatch() {
      se = iqf.stopWatch(Symbol);
      se.HandleUpdateMessage -= new UpdateMessageHandler(se_HandleUpdateMessage);
    }


    void se_HandleUpdateMessage( object sender, UpdateMessageEventArgs args ) {

      lock (qUpdateMessages.SyncRoot) {
        qUpdateMessages.Enqueue(args);
      }
      UpdateTape();

    }

    delegate void UpdateTapeHandler();
    private void UpdateTape() {

      if (InvokeRequired) {
        BeginInvoke(new UpdateTapeHandler(UpdateTape));
        //Invoke(new UpdateTapeHandler(UpdateTape));
      }
      else {

        UpdateMessageEventArgs args;
        lock (qUpdateMessages.SyncRoot) {
          args = qUpdateMessages.Dequeue() as UpdateMessageEventArgs;
        }

        /*
        Console.WriteLine(
          "symbol {0} price {1} sz {2} bid {3} sz {4} ask {5} sz {6} tm {7}",
          args.Message.Symbol, args.Message.Last, args.Message.LastSize,
          args.Message.Bid, args.Message.BidSize,
          args.Message.Ask, args.Message.AskSize,
          args.Message.Time + " " + args.Message.Type
        );
         */

        string Type = args.Message.Type;
        double price = args.Message.Last;
        double size = args.Message.LastSize;
        double bid = args.Message.Bid;
        double bidsize = args.Message.BidSize;
        double ask = args.Message.Ask;
        double asksize = args.Message.AskSize;
        int precision = args.Message.DecimalPrecision;
        Color color = Color.White;
        string time = DateTime.Now.ToString("HH:mm:ss");
        string exch = args.Message.ExchangeID;

        //Console.WriteLine("{0} p {1},{2} b {3},{4} a {5},{6}",
          //Type, price, size, bid, bidsize, ask, asksize);

        if ( InitQuoteVars ) {
          InitQuoteVars = false;
          LastBid = bid;
          LastAsk = ask;
        }

        if (0 != bidsize) {
          if ((LastBid != bid) || (LastBidSize != bidsize)) {
            DisplayLine(time, exch, Color.LightYellow,
              bid > LastBid ? "Best Bid" : "Bid", bid, bidsize, precision);
            LastBid = bid;
            LastBidSize = bidsize;
          }
        }

        if (0 != asksize) {
          if ((LastAsk != ask) || (LastAskSize != asksize)) {
            DisplayLine(time, exch, Color.White,
              ask < LastAsk ? "Best Ask" : "Ask", ask, asksize, precision);
            LastAsk = ask;
            LastAskSize = asksize;
          }
        }

        if ("t" == Type || "T" == Type) {
          double avg = (bid + ask) / 2;
          color = price >= avg ? Color.LightGreen : Color.LightPink;
          if (0 == SessionHi) SessionHi = price;
          else {
            if (price > SessionHi) {
              SessionHi = price;
              color = Color.LightBlue;
            }
          }
          if (0 == SessionLo) SessionLo = price;
          else {
            if (price < SessionLo) {
              SessionLo = price;
              color = Color.LightSalmon;
            }
          }
          DisplayLine(time, exch, color, "Trade", price, args.Message.LastSize, precision);
        }

      }
    }

    void DisplayLine( string time, string exch, Color color, string type, double price, double size, int precision ) {

      for (int ix = NumRows - 1; ix > 0; ix--) {
        for (int jx = 0; jx <= 4; jx++) {
          lvTape.Items[ix].SubItems[jx].Text = lvTape.Items[ix - 1].SubItems[jx].Text;
        }
        lvTape.Items[ix].BackColor = lvTape.Items[ix - 1].BackColor;
      }

      //Console.WriteLine("fraction display {0} precision {1}", args.Message.FractionDisplayCode, args.Message.DecimalPrecision);
      lvTape.Items[0].SubItems[0].Text = time;
      lvTape.Items[0].SubItems[4].Text = exch;

      lvTape.Items[0].BackColor = color;
      lvTape.Items[0].SubItems[1].Text = type;
      lvTape.Items[0].SubItems[2].Text = size.ToString();
      string zero = "";
      for (int i = 1; i <= precision; i++) zero += "0";
      lvTape.Items[0].SubItems[3].Text = price.ToString("#0." + zero);
    }

    void frmTape_Shown( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_Shown");
    }

    void frmTape_ResizeEnd( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_ResizeEnd");
    }

    void frmTape_ResizeBegin( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_ResizeBegin");
    }

    void frmTape_Resize( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_Resize");
    }

    void frmTape_RegionChanged( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_RegionChanged");
    }

    void frmTape_FormClosed( object sender, FormClosedEventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_FormClosed");
    }

    void frmTape_FormClosing( object sender, FormClosingEventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_FormClosing");
    }

    void frmTape_ClientSizeChanged( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine("Tape frmTape_ClientSizeChanged {0}, {1}", lvTape.Height, lvTape.TileSize);
    }


  }
}
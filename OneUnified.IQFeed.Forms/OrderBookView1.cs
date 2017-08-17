//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using OneUnified.IQFeed;
//using OneUnified.SmartQuant;

using SmartQuant.Data;

namespace OneUnified.IQFeed.Forms {


  // need to share the Level2Port feed with other windows so as to not open too many redundant ports

  /*
   * The Ensign Windows Level II screen can be sorted by Total Bid/Ask Volume.  
   * This shows who is active and leading the market.  
   * It also counts how many times each Market Maker presented the LEAD bid or ask.  
   * It also displays ratios on bid and ask LEAD counts when a Market Maker is selected.  
   * By the way, there are some very interesting patterns formed while CHARTING the two top Market Makers 
   * (on the same Chart) which may tip-off the direction of the market for the next several minutes.  
   * Great for Scalpers or the Market Makers themselves.  
   * http://www.ensignsoftware.com/help/nasdaq.htm
   * 
   * http://www.cmastation.com/images/station_tela11.gif
   */

  public partial class frmOrderBookView1 : Form {

    private IQFeed iqf;
    private IQFeedLevelII l2port;

    private int MaxRowsToShow = 25;

    private DateTime LastDateTime = new DateTime(0);
    private bool bUpdateDisplay = true;

    private int DecimalPrecision = 2;
    private string PriceFormat = "#0.00";

    private bool HasSymbol = false;

    System.Timers.Timer OrderBookRefreshTimer;

    OneUnified.IQFeed.OrderBook ob;

    public frmOrderBookView1() {
      InitializeComponent();

    }

    public frmOrderBookView1( string Symbol ) {
      InitializeComponent();
      this.Text = Symbol;

    }

    private void frmOrderBook_Load( object sender, EventArgs e ) {

      ob = new OneUnified.IQFeed.OrderBook();

      lvBid.Columns[0].TextAlign = HorizontalAlignment.Center;
      lvBid.Columns[1].TextAlign = HorizontalAlignment.Right;
      lvBid.Columns[2].TextAlign = HorizontalAlignment.Right;
      lvBid.Columns[3].TextAlign = HorizontalAlignment.Right;
      lvBid.Columns[4].TextAlign = HorizontalAlignment.Center;
      lvBid.Columns[5].TextAlign = HorizontalAlignment.Right;

      for (int i = 1; i <= MaxRowsToShow; i++) {
        string[] items = { "-", "-", "-", "-", "-", "-" };
        ListViewItem lvi = new ListViewItem(items);
        lvBid.Items.Add(lvi);
      }

      lvAsk.Columns[0].TextAlign = HorizontalAlignment.Center;
      lvAsk.Columns[1].TextAlign = HorizontalAlignment.Right;
      lvAsk.Columns[2].TextAlign = HorizontalAlignment.Right;
      lvAsk.Columns[3].TextAlign = HorizontalAlignment.Right;
      lvAsk.Columns[4].TextAlign = HorizontalAlignment.Center;
      lvAsk.Columns[5].TextAlign = HorizontalAlignment.Right;

      for (int i = 1; i <= MaxRowsToShow; i++) {
        string[] items = { "-", "-", "-", "-", "-", "-" };
        ListViewItem lvi = new ListViewItem(items);
        lvAsk.Items.Add(lvi);
      }

    }

    public void StartWatch( string Symbol, IQFeed iqf, IQFeedLevelII l2port ) {
      //Console.WriteLine("StartWatch");

      if (!HasSymbol) {
        
        HasSymbol = true;

        this.Text = "Level II - " + Symbol;

        SymbolEvent se;

        this.iqf = iqf;
        this.l2port = l2port;

        se = iqf.startWatch(Symbol);
        se.HandleSummaryMessage += new SummaryMessageHandler(se_HandleLISummaryMessage);
        iqf.requestSummary(Symbol);

        l2port.StartWatch(Symbol, new LevelIIUpdateMessageHandler(se_HandleLIIUpdateMessage));

        OrderBookRefreshTimer = new System.Timers.Timer(750);
        OrderBookRefreshTimer.Elapsed += new ElapsedEventHandler(OrderBookRefreshTimer_Elapsed);
        OrderBookRefreshTimer.Enabled = true;
      }
    }

    public void StopWatch( string Symbol ) {
      //Console.WriteLine("StopWatch");
      l2port.StopWatch(Symbol, new LevelIIUpdateMessageHandler(se_HandleLIIUpdateMessage));

      OrderBookRefreshTimer.Stop();
      OrderBookRefreshTimer.Enabled = false;
      OrderBookRefreshTimer.Elapsed -= new ElapsedEventHandler(OrderBookRefreshTimer_Elapsed);
    }

    void se_HandleLISummaryMessage( object sender, SummaryMessageEventArgs args ) {

      SymbolEvent se;

      DecimalPrecision = args.Message.DecimalPrecision;
      se = iqf.stopWatch(args.Message.Symbol);
      se.HandleSummaryMessage -= new SummaryMessageHandler(se_HandleLISummaryMessage);

      string zero = "";
      for (int i = 1; i <= DecimalPrecision; i++) zero += "0";
      PriceFormat = "#0." + zero;
    }

    void se_HandleLIIUpdateMessage( object sender, LevelIIUpdateMessageEventArgs args ) {

      //Console.WriteLine("se_HandleUpdateMessage");
      lock (ob) {
      //lock (htMMInfo.SyncRoot) {
        UpdateDataStructures(args.Message);
      }
    }

    private void UpdateDataStructures( LevelIIUpdateMessage Message ) {

      MarketDepth md;
      md = new MarketDepth(Message.TimeStamp, Message.MMID, 0, MDOperation.Undefined, MDSide.Bid, Message.BidPrice, Message.BidSize);
      ob.Update(md);
      md = new MarketDepth(Message.TimeStamp, Message.MMID, 0, MDOperation.Undefined, MDSide.Ask, Message.AskPrice, Message.AskSize);
      ob.Update(md);

    }

    void OrderBookRefreshTimer_Elapsed( object sender, ElapsedEventArgs e ) {
      CallRedrawDisplay();
    }

    delegate void CallRedrawDisplayHandler();
    void CallRedrawDisplay() {
      if (bUpdateDisplay) {
        if (InvokeRequired) {
          BeginInvoke(new CallRedrawDisplayHandler(CallRedrawDisplay));
        }
        else {
          RedrawDisplay( ob );
        }
      }
    }

    public void Add( MarketDepth md ) {
      ob.Update(md);
    }

    public void Refresh() {
      RedrawDisplay(ob);
    }

    public void RedrawDisplay( OneUnified.IQFeed.OrderBook ob ) {
      
      lock (ob) {
      //lock (htMMInfo.SyncRoot) {
        int i;
        int cumcnt;
        int colorix;
        Color[] colors = { Color.LightYellow, Color.PaleGoldenrod, Color.Wheat, Color.Goldenrod, Color.DarkGoldenrod,
          Color.DarkKhaki, Color.DarkOrange, 
          Color.Orange, Color.OrangeRed, Color.Red, Color.Crimson };
        // Color.LightGoldenrodYellow, , Color.LemonChiffon, Color.Cornsilk, Color.PeachPuff, 
        //Color[] colors = { Color.Yellow, Color.Green, Color.Blue, Color.Purple, Color.Red };
        int cntColors = colors.GetLength(0);
        double LastValue = 0;

        if (bUpdateDisplay) {
          //Console.WriteLine("OrderBookRefreshTimer_Elapsed");
          //bUpdateDisplay = false;

          try {
            i = 0;
            colorix = 0;
            cumcnt = 0;
            foreach (MarketMakerBidAsk mmba in ob.slAsk.Values) {
              if (0 == i) {
                colorix = 0;
                LastValue = mmba.Ask;
              }
              else {
                if (LastValue != mmba.Ask) {
                  LastValue = mmba.Ask;
                  colorix = ++colorix % cntColors;
                }
              }
              cumcnt += mmba.AskSize;
              DisplayLine(lvAsk, i++, colors[colorix], mmba.MMID, mmba.AskInside, cumcnt, mmba.Ask, mmba.AskSize, 
                new TimeSpan( mmba.LastStamp.Hour, mmba.LastStamp.Minute, mmba.LastStamp.Second));
              if (i >= MaxRowsToShow) break;
            }
            while (i < MaxRowsToShow) {
              DisplayLine(lvAsk, i++, Color.White, "", 0, 0, 0, 0, new TimeSpan(0));
            }
          }
          catch (Exception e) {
            Console.WriteLine("OrderBookRefreshTimer_Elapsed foreach problem ask\n{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
              e.Data, e.HelpLink, e.Message, e.Source, e.StackTrace, e.ToString());
          }

          try {
            i = 0;
            colorix = 0;
            cumcnt = 0;
            foreach (MarketMakerBidAsk mmba in ob.slBid.Values) {
              if (0 == i) {
                colorix = 0;
                LastValue = mmba.Bid;
              }
              else {
                if (LastValue != mmba.Bid) {
                  LastValue = mmba.Bid;
                  colorix = ++colorix % cntColors;
                }
              }
              cumcnt += mmba.BidSize;
              DisplayLine(lvBid, i++, colors[colorix], mmba.MMID, mmba.BidInside, cumcnt, mmba.Bid, mmba.BidSize,
                new TimeSpan(mmba.LastStamp.Hour, mmba.LastStamp.Minute, mmba.LastStamp.Second));
              if (i >= MaxRowsToShow) break;
            }
            while (i < MaxRowsToShow) {
              DisplayLine(lvBid, i++, Color.White, "", 0, 0, 0, 0, new TimeSpan(0));
            }
          }
          catch (Exception e) {

            Console.WriteLine("OrderBookRefreshTimer_Elapsed foreach problem bid\n{0}\n{1}\n{2}\n{3}\n{4}\n{5}", 
              e.Data, e.HelpLink, e.Message,e.Source,e.StackTrace,e.ToString());
          }
        }
        base.Refresh();
        //this.Refresh();
      }

    }

    void DisplayLine( ListView lv, int line, Color color, string mmid, int count, int cum, double bid, int size, TimeSpan time ) {
      lv.Items[line].BackColor = color;
      lv.Items[line].SubItems[0].Text = mmid;
      lv.Items[line].SubItems[1].Text = 0 == bid ? "" : bid.ToString(PriceFormat);
      lv.Items[line].SubItems[2].Text = 0 == size ? "" : size.ToString();
      lv.Items[line].SubItems[3].Text = 0 == cum ? "" : cum.ToString();
      lv.Items[line].SubItems[4].Text = 0 == time.Ticks ? "" : time.ToString();
      lv.Items[line].SubItems[5].Text = 0 == count ? "" : count.ToString();
    }
  }
}
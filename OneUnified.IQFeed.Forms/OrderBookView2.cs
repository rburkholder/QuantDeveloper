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

  public partial class frmOrderBookView2 : Form {

    private IQFeed iqf;
    private IQFeedLevelII l2port;

    private int MaxRowsToShow = 51;  // keep it odd
    private int NumRowsEachSide = 25;
    //private Queue qUpdateMessages;

    private DateTime LastDateTime = new DateTime(0);
    private bool bUpdateDisplay = true;

    private int DecimalPrecision = 2;
    private string PriceFormat = "#0.00";

    private bool HasSymbol = false;

    System.Timers.Timer OrderBookRefreshTimer;

    OneUnified.IQFeed.OrderBook ob;

    int LastMidPrice = 0;
    SortedList slSizesBid;
    SortedList slSizesAsk;
    int HiPrice;
    int LoPrice;

    double LastTradePrice = 0;

    public frmOrderBookView2() {
      //Console.WriteLine("frmOrderBook");
      InitializeComponent();
      slSizesBid = new SortedList(MaxRowsToShow);
      slSizesAsk = new SortedList(MaxRowsToShow);
    }

    public frmOrderBookView2( string Symbol ) {
      //Console.WriteLine("frmOrderBook");
      InitializeComponent();
      slSizesBid = new SortedList(MaxRowsToShow);
      slSizesAsk = new SortedList(MaxRowsToShow);
      this.Text = Symbol;

    }

    private void frmOrderBook_Load( object sender, EventArgs e ) {
      //Console.WriteLine("frmOrderBook_Load");

      ob = new OneUnified.IQFeed.OrderBook();

      lvOrderBookView2.Columns[1].TextAlign = HorizontalAlignment.Center;
      lvOrderBookView2.Columns[2].TextAlign = HorizontalAlignment.Center;
      lvOrderBookView2.Columns[3].TextAlign = HorizontalAlignment.Center;

      for (int i = 1; i <= MaxRowsToShow; i++) {
        string[] items = { "", "-", "-", "-" };
        ListViewItem lvi = new ListViewItem(items);
        lvOrderBookView2.Items.Add(lvi);
      }

    }

    public double LatestTrade {
      get { return Math.Round( 100 * LastTradePrice, 2 ); }
      set { LastTradePrice = value; }
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
          RedrawDisplay(ob);
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

        if (ob.slAsk.Count > 0 && ob.slBid.Count > 0) {

          int ix;

          MarketMakerBidAsk mmbaBid = (MarketMakerBidAsk)ob.slBid.GetByIndex(0);
          MarketMakerBidAsk mmbaAsk = (MarketMakerBidAsk)ob.slAsk.GetByIndex(0);

          //double BidTop = mmbaBid.Bid;
          //double AskTop = mmbaAsk.Ask;
          //int BidTop = (int)Math.Round(100 * mmbaBid.Bid);
          int BidTop = mmbaBid.Bid;
          //int AskTop = (int)Math.Round(100 * mmbaAsk.Ask);
          int AskTop = mmbaAsk.Ask;

          int MidPrice = (BidTop + AskTop) / 2;
          if (MidPrice != LastMidPrice) {
            HiPrice = MidPrice + NumRowsEachSide;
            LoPrice = MidPrice - NumRowsEachSide;

            slSizesBid.Clear();
            slSizesAsk.Clear();
            for (int i = LoPrice; i <= HiPrice; i++) {
              slSizesBid.Add(i, (int)0);
              slSizesAsk.Add(i, (int)0);
            }
            LastMidPrice = MidPrice;
          }
          else {
            for (int i = LoPrice; i <= HiPrice; i++) {
              slSizesBid[i] = (int)0;
              slSizesAsk[i] = (int)0;
            }
          }

          ix = ob.slBidPrice.IndexOfKey(mmbaBid.Bid);  // start at highest bid and work down
          while (ix >= 0) {
            int val = (int)Math.Round(100 * (double)ob.slBidPrice.GetKey(ix));
            if (val < LoPrice) break;
            if (val > HiPrice) break;
            slSizesBid[val] = (int)ob.slBidPrice.GetByIndex(ix);
            ix--;
          }

          ix = ob.slAskPrice.IndexOfKey(mmbaAsk.Ask);  // start at lowest ask and work up
          while (ix < ob.slAskPrice.Count) { 
            int val = (int)Math.Round(100 * (double)ob.slAskPrice.GetKey(ix));
            if (val < LoPrice) break;
            if (val > HiPrice) break;
            slSizesAsk[val] = (int)ob.slAskPrice.GetByIndex(ix);
            ix++;
          }

          /*
          foreach (MarketMakerBidAsk mmba in ob.slBid.Values) {
            int val = (int)Math.Round(100 * mmba.Bid);
            if (val < LoPrice) break;
            if (val > HiPrice) break;
            int size = (int)slSizes[val];
            slSizes[val] = size + mmba.BidSize;
          }

          foreach (MarketMakerBidAsk mmba in ob.slAsk.Values) {
            int val = (int)Math.Round(100 * mmba.Ask);
            if (val > HiPrice) break;
            if (val < LoPrice) break;
            int size = (int)slSizes[val];
            slSizes[val] = size + mmba.AskSize;
          }
           */

          for (int i = 1; i <= MaxRowsToShow; i++) {
            int pr = (int)slSizesBid.GetKey(i - 1);
            double price = (double)pr / 100.0;
            int sizeBid = (int)slSizesBid.GetByIndex(i - 1);
            int sizeAsk = (int)slSizesAsk.GetByIndex(i - 1);
            int j = MaxRowsToShow - i;
            lvOrderBookView2.Items[j].SubItems[1].Text = (0 == sizeBid ? "" : sizeBid.ToString());
            lvOrderBookView2.Items[j].SubItems[3].Text = (0 == sizeAsk ? "" : sizeAsk.ToString());
            lvOrderBookView2.Items[j].SubItems[2].Text = price.ToString(PriceFormat);
            lvOrderBookView2.Items[j].SubItems[2].ForeColor = ( price == LastTradePrice ) ? Color.Green : Color.Black;
            /*
            if (pr == MidPrice) {
              lvOrderBookView2.Items[j].SubItems[1].Text = (0 == sizeBid ? "" : sizeBid.ToString());
              lvOrderBookView2.Items[j].SubItems[3].Text = (0 == sizeAsk ? "" : sizeAsk.ToString());
            }
            if (pr > MidPrice) {
              lvOrderBookView2.Items[j].SubItems[1].Text = "";
              lvOrderBookView2.Items[j].SubItems[3].Text = (0 == sizeBid ? "" : size.ToString());
            }
            if (pr < MidPrice) {
              lvOrderBookView2.Items[j].SubItems[1].Text = (0 == size ? "" : size.ToString());
              lvOrderBookView2.Items[j].SubItems[3].Text = "";
            }
             * */
          }
        }
        base.Refresh();

      }

    }

  }
}
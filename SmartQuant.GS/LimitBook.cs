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
using System.Collections.Generic;
using System.Text;

using OneUnified.SmartQuant;

using SmartQuant.Data;

namespace SmartQuant.GS {

  public class MarketMakerStats {

    private string mmid;
    private int cntInsideAsk = 0;
    private int cntInsideBid = 0;
    private int cntAskChanged = 0;
    private int cntBidChanged = 0;
    private int cntNonZeroAskChanges = 0;
    private int cntNonZeroBidChanges = 0;
    private int cntCoveredAskLevels = 0;  // number of ask price levels it currently covers
    private int cntCoveredBidLevels = 0;  // number of bid price levels it currently covers

    public MarketMakerStats( string MMID ) {
      mmid = MMID;
    }
  }

  public class DepthInfo {

    private MarketMakerStats mms;
    private MarketDepth md;
    private int level;
    private ObjectAtTime oat;

    public DepthInfo( MarketMakerStats mms, MarketDepth md, int level ) {
      this.mms = mms;
      this.md = md;
      this.level = level;
    }

    public ObjectAtTime Object {
      get { return oat; }
      set { oat = value; }
    }

    public int Level {
      get { return level; }
    }

    public MarketDepth Depth {
      get { return md; }
    }

    public MarketMakerStats Stats {
      get { return mms; }
    }
  }

  // need to update list of market maker id's into concatenated string for presentation to price book
  // send limit orders to most popular market maker (based upon quantity, or most times that was inside, or similar)

  public class LimitBookEntry {  // one each per price level

    private int m_Level;  // either double * 100 (equities) or double * 10000 (futures)
    private double m_Price;  // original price
    private int m_quantity; // total quantity offered at price level (either bid or ask side)
    private Dictionary<string, DepthInfo> MarketMakers;

    public LimitBookEntry ( int Level, double Price ) {
      m_Price = Price;
      m_Level = Level;
      m_quantity = 0;
      MarketMakers = new Dictionary<string, DepthInfo>();
    }

    public int Size {
      get { return m_quantity; }
      set { m_quantity = value; }
    }

    public double Price {
      get { return m_Price; }
    }

    public bool Remove( DepthInfo di ) {

      bool removed = false;
      if (MarketMakers.ContainsKey(di.Depth.MarketMaker)) {
        DepthInfo tmp = MarketMakers[di.Depth.MarketMaker];
        if (tmp == di) {
          int size = tmp.Depth.Size;
          m_quantity -= size;  // remove affect of market maker
          //tmp.Object.Node.List.Remove(tmp.Object.Node);  // remove this node from list:  already removed
          MarketMakers.Remove(di.Depth.MarketMaker);  // then remove market maker entry
          removed = true;
        }
      }
      return removed;
    }

    public void Update( DepthInfo di ) {
      MarketDepth md = di.Depth;
      // check for market maker, add if not present
      // what is best?  keep removing and creating lbse's, or leave them there and just set to zero when nothing?
      if ( 0 == md.Size ) {
        // remove market maker entry
        if ( MarketMakers.ContainsKey( md.MarketMaker ) ) {
          DepthInfo tmp = MarketMakers[ md.MarketMaker];
          int size = tmp.Depth.Size;
          m_quantity -= size;  // remove affect of market maker
          tmp.Object.Node.List.Remove(tmp.Object.Node);  // remove this node from list
          MarketMakers.Remove( md.MarketMaker );  // then remove market maker entry

        }
        else {
          //Console.WriteLine( "0 size but market maker {0} does not exist at {1:0.00}", md.MarketMaker, md.Price );
        }
      }
      else {
        // add or update the market maker entry
        if ( MarketMakers.ContainsKey(md.MarketMaker) ) {
          DepthInfo tmp = MarketMakers[md.MarketMaker];
          int size = tmp.Depth.Size;
          m_quantity -= size;
          tmp.Object.Node.List.Remove(tmp.Object.Node);
          MarketMakers[md.MarketMaker] = di;  // replace expired entry with new entry
          m_quantity += md.Size;
        }
        else {
          MarketMakers.Add( md.MarketMaker, di );
          m_quantity += md.Size;
        }
      }
    }
  }

  public class LimitBook: SlidingWindow {

    private SortedList<int, LimitBookEntry> entries;  // indexed with price level to get at values for price level

    public delegate void PriceBookRowChangedHandler( object sender, int Level, double Price, int Quantity );
    public event PriceBookRowChangedHandler PriceBookRowChanged;

    public delegate void PriceBookEdgeChangedHandler( object sender, int index, int level, double price );
    public event PriceBookEdgeChangedHandler PriceBookHiEdgeChanged;
    public event PriceBookEdgeChangedHandler PriceBookLoEdgeChanged;

    public LimitBook(): base( 30) {
      entries = new SortedList<int, LimitBookEntry>();
    }

    public SortedList<int, LimitBookEntry> Entries {
      get { return entries; }
    }

    public void Update( MarketMakerStats mms, MarketDepth md ) {

      int keyindex;
      int level;
      double price = md.Price;
      LimitBookEntry lbe;
      int HiChanged = 0;
      int LoChanged = 0;

      lock (entries) {
        level = OneUnified.SmartQuant.Convert.DoublePriceToIntStock(md.Price);

        DepthInfo di = new DepthInfo(mms, md, level);

        // obtain level at which depth is used
        if (entries.ContainsKey(level)) {
          keyindex = entries.IndexOfKey(level);
          lbe = entries.Values[keyindex];
        }
        else {
          lbe = new LimitBookEntry(level, md.Price);
          entries.Add(level, lbe);
          keyindex = entries.IndexOfKey(level);
        }

        // update the level, and signal change event
        //lbe.Update(mms, md);
        di.Object = base.Add(md.DateTime, di);
        lbe.Update(di);
        if (null != PriceBookRowChanged) PriceBookRowChanged(this, level, lbe.Price, lbe.Size);

        // used for indicating inside quote changed (is this useful due to problems with expiration?)
        if (0 == keyindex) {
          LoChanged++;
        }
        if (entries.Count - 1 == keyindex) {
          HiChanged++;
        }

        // if level has no active depth markers, remove it
        if (0 == lbe.Size) {
          entries.RemoveAt(keyindex);
          if (0 == keyindex) {
            LoChanged++;
          }
          if (entries.Count == keyindex) { // matches without the '-1' with the removal
            HiChanged++;
          }
        }

        // signal event that inside quote has changed (again, not sure if this will actually work properly, 
        //   as we are using quotes to clear this stuff out.
        if (entries.Count > 0) {
          if (0 < HiChanged) {
            // highest changed so indicate as such, other code will determine if it is a new inside or not
            int ix = entries.Count - 1;
            if (null != PriceBookHiEdgeChanged)
              PriceBookHiEdgeChanged(this, ix, entries.Keys[ix], entries.Values[ix].Price);
          }
          if (0 < LoChanged) {
            // lowest changed so indicate as such, other code will determine if it is a new inside or not
            if (null != PriceBookLoEdgeChanged)
              PriceBookLoEdgeChanged(this, 0, entries.Keys[0], entries.Values[0].Price);
          }
        };

      }
    }

    protected override ObjectAtTime Remove() {

      LimitBookEntry lbe;
      int keyindex;

      ObjectAtTime oat = base.Remove();
      DepthInfo di = (DepthInfo) oat.Object;
      if (entries.ContainsKey(di.Level)) {
        keyindex = entries.IndexOfKey(di.Level);
        lbe = entries.Values[keyindex];
        bool removed = lbe.Remove(di);

        if (removed) {
          if (null != PriceBookRowChanged) PriceBookRowChanged(this, di.Level, lbe.Price, lbe.Size);

          // if level has no active depth markers, remove it
          if (0 == lbe.Size) {
            entries.RemoveAt(keyindex);
            if (0 == keyindex) {
              //LoChanged++;
            }
            if (entries.Count == keyindex) { // matches without the '-1' with the removal
              //HiChanged++;
            }
          }
        }

      }

      // handle instance where depth entry has expired
      return oat;
    }

    public void Update( Quote quote ) {
      // remove old items
      base.UpdateWindow();
    }
  }

  public class LimitBooks {

    public LimitBook BidBook;
    public LimitBook AskBook;
    public Dictionary<string, MarketMakerStats> MMStats;  // list of all market makers so as to update appropriate stats

    public delegate void NewInsideQuoteHandler( object sender, double Price );
    public event NewInsideQuoteHandler NewInsideAskQuote;
    public event NewInsideQuoteHandler NewInsideBidQuote;

    public event LimitBook.PriceBookRowChangedHandler AskPriceBookRowChanged;
    public event LimitBook.PriceBookRowChangedHandler BidPriceBookRowChanged;

    public LimitBooks() {

      BidBook = new LimitBook();
      AskBook = new LimitBook();
      MMStats = new Dictionary<string, MarketMakerStats>();

      BidBook.PriceBookRowChanged += new LimitBook.PriceBookRowChangedHandler(BidBook_PriceBookRowChanged);
      AskBook.PriceBookRowChanged += new LimitBook.PriceBookRowChangedHandler(AskBook_PriceBookRowChanged);

      BidBook.PriceBookHiEdgeChanged += new LimitBook.PriceBookEdgeChangedHandler(BidBook_PriceBookHiEdgeChanged);
      AskBook.PriceBookLoEdgeChanged += new LimitBook.PriceBookEdgeChangedHandler(AskBook_PriceBookLoEdgeChanged);
    }

    void AskBook_PriceBookLoEdgeChanged( object sender, int index, int level, double price ) {
      if (null != NewInsideAskQuote) NewInsideAskQuote(sender, price);
    }

    void BidBook_PriceBookHiEdgeChanged( object sender, int index, int level, double price ) {
      if (null != NewInsideBidQuote) NewInsideBidQuote(sender, price);
    }

    void AskBook_PriceBookRowChanged( object sender, int Level, double Price, int Quantity ) {
      if (null != AskPriceBookRowChanged) AskPriceBookRowChanged(sender, Level, Price, Quantity);
    }

    void BidBook_PriceBookRowChanged( object sender, int Level, double Price, int Quantity ) {
      if (null != BidPriceBookRowChanged) BidPriceBookRowChanged(sender, Level, Price, Quantity);
    }

    public void Update( MarketDepth md ) {

      if ( !MMStats.ContainsKey( md.MarketMaker) ) {
        MMStats.Add( md.MarketMaker, new MarketMakerStats( md.MarketMaker));
      }

      switch ( md.Side ) {
        case MDSide.Ask:
          AskBook.Update( MMStats[ md.MarketMaker ], md );
          break;
        case MDSide.Bid:
          BidBook.Update( MMStats[ md.MarketMaker ], md );
          break;
      }
    }

    public void Update( Quote quote ) {
      // need to use this to clean out crap from the order book
      AskBook.Update(quote);
      BidBook.Update(quote);
    }
  }
}

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

namespace OneUnified.IQFeed {

  public class LimitBookKey {

    //public double price;  // primary sort key
    public int price;
    public int size;  // may or may not be used
    public string mmid;  // provides the unique identifier slot
    public DateTime time;  // may or may not be used
    public int count;

    public LimitBookKey( string mmid ) {
      this.mmid = mmid;
    }

    public void Update( int price, int size ) {
      this.price = price;
      this.size = size;
    }
  }

  public class MarketMakerBidAsk {

    public string MMID;
    public int Bid;
    public int BidSize;
    public int BidInside = 0;  // number of times was inside bid
    public int BidActivity = 0;
    public int Ask;
    public int AskSize;
    public int AskInside = 0;  // number of times was inside ask
    public int AskActivity = 0;
    public DateTime LastStamp;

    public LimitBookKey BidKey;
    public LimitBookKey AskKey;

    public MarketMakerBidAsk( string MMID ) {
      this.MMID = MMID;
      this.Bid = 0;
      this.BidSize = 0;
      this.Ask = 0;
      this.AskSize = 0;
      this.LastStamp = new DateTime(0);

      BidKey = new LimitBookKey(MMID);
      AskKey = new LimitBookKey(MMID);

      UpdateLimitBookKeys();
    }

    public MarketMakerBidAsk( 
      string MMID, int Bid, int BidSize, int Ask, int AskSize, DateTime LastStamp ) {
      this.MMID = MMID;
      this.Bid = Bid;
      this.BidSize = BidSize;
      this.Ask = Ask;
      this.AskSize = AskSize;
      this.LastStamp = LastStamp;

      BidKey = new LimitBookKey(MMID);
      AskKey = new LimitBookKey(MMID);

      UpdateLimitBookKeys();
    }

    
    private void UpdateLimitBookKeys() {

      BidKey.Update(Bid, BidSize);
      AskKey.Update(Ask, AskSize);

    }
    
    public void Update( 
      int Bid, int BidSize, int Ask, int AskSize, DateTime LastStamp ) {
      this.Bid = Bid;
      this.BidSize = BidSize;
      this.Ask = Ask;
      this.AskSize = AskSize;
      this.LastStamp = LastStamp;

      UpdateLimitBookKeys();
      
    }

    public void UpdateBid( DateTime LastStamp, int Bid, int BidSize ) {
      this.LastStamp = LastStamp;
      this.Bid = Bid;
      this.BidSize = BidSize;
      BidKey.Update(Bid, BidSize);
    }

    public void UpdateAsk( DateTime LastStamp, int Ask, int AskSize ) {
      this.LastStamp = LastStamp;
      this.Ask = Ask;
      this.AskSize = AskSize;
      AskKey.Update(Ask, AskSize);
    }
  }

  public class OrderBook {

    private int NumRows = 500;

    private int ActiveAskRows = 0;
    private int ActiveBidRows = 0;

    public SortedList slAsk;  // Ask Quantity by MMID
    public SortedList slBid;  // Bid Quantity by MMID
    public SortedList slAskPrice;  // Quantity at each Ask level
    public SortedList slBidPrice;  // Quantity at each Bid level

    public Hashtable htMMInfo;

    public delegate void PriceBookRowChangedHandler( object sender, int Price, int Quantity );
    public event PriceBookRowChangedHandler BidPriceBookRowChanged;
    public event PriceBookRowChangedHandler AskPriceBookRowChanged;

    public event EventHandler InsideQuoteChangedEventHandler;
    private bool bInsideQuoteChanged;

    public OrderBook() {

      htMMInfo = new Hashtable(NumRows);

      slBid = new SortedList(new CompareByPriceSizeMmidDec(), NumRows);
      slAsk = new SortedList(new CompareByPriceSizeMmidAsc(), NumRows);
      slAskPrice = new SortedList( NumRows );  // price book
      slBidPrice = new SortedList( NumRows );  // price book

    }

    public void Update( MarketDepth md ) {
    
      int ix = 0;

      MarketMakerBidAsk mmba = null;
      MarketMakerBidAsk mmbaInside = null;

      LimitBookKey lbk = null;
      int quan;

      int price = OneUnified.SmartQuant.Convert.DoublePriceToIntStock( md.Price );

      try {

        bInsideQuoteChanged = false;

        if (!htMMInfo.ContainsKey(md.MarketMaker)) {
          // simply add a new entry
          mmba = new MarketMakerBidAsk(md.MarketMaker);
          htMMInfo.Add(md.MarketMaker, mmba);
        }
        else {
          // update existing entry after removing from sorted lists
          mmba = (MarketMakerBidAsk)htMMInfo[md.MarketMaker];

          // remove key from sorted arrays

          switch (md.Side) {
            case MDSide.Bid:
              if ( mmba.BidSize > 0 ) {
                lbk = mmba.BidKey;
                ix = slBid.IndexOfKey( lbk );
                slBid.RemoveAt( ix );
                ActiveBidRows--;
                if ( 0 < slBid.Count ) {
                  // get new inside bid if records remaining in order book
                  if ( 0 == ix ) mmbaInside = ( MarketMakerBidAsk )slBid.GetByIndex( 0 );
                }
                // update price table
                ix = slBidPrice.IndexOfKey( mmba.Bid );
                quan = ( int )slBidPrice.GetByIndex( ix );
                if ( quan == mmba.BidSize ) {
                  quan = 0;
                  slBidPrice.RemoveAt( ix );
                }
                else {
                  quan -= mmba.BidSize;
                  slBidPrice.SetByIndex( ix, quan );
                }
                if ( null != BidPriceBookRowChanged ) BidPriceBookRowChanged( this, mmba.Bid, quan );
              }
              else {
                Console.WriteLine( "ob bid remove problem bs:{0}, bid:{1}, price:{2}, MM:{3}", mmba.BidSize, mmba.Bid, price, md.MarketMaker );
              }
              break;
            case MDSide.Ask:
              if ( mmba.AskSize > 0 ) {
                lbk = mmba.AskKey;
                ix = slAsk.IndexOfKey( lbk );
                slAsk.RemoveAt( ix );
                ActiveAskRows--;
                if ( 0 < slAsk.Count ) {
                  // get new inside ask if records remaining in order book
                  if ( 0 == ix ) mmbaInside = ( MarketMakerBidAsk )slAsk.GetByIndex( 0 );
                }
                // update price table
                ix = slAskPrice.IndexOfKey( mmba.Ask );
                quan = ( int )slAskPrice.GetByIndex( ix );
                if ( quan == mmba.AskSize ) {
                  quan = 0;
                  slAskPrice.RemoveAt( ix );
                }
                else {
                  quan -= mmba.AskSize;
                  slAskPrice.SetByIndex( ix, quan );
                }
                if ( null != AskPriceBookRowChanged ) AskPriceBookRowChanged( this, mmba.Ask, quan );
              }
              else {
                Console.WriteLine( "ob ask remove problem as:{0}, ask:{1}, price:{2}, MM:{3}", mmba.AskSize, mmba.Ask, price, md.MarketMaker );
              }
              break;
          }
        }

        switch (md.Side) {
          case MDSide.Bid:
            mmba.UpdateBid( md.DateTime, price, md.Size );
            if (md.Size > 0) {
              lbk = mmba.BidKey;
              slBid.Add(lbk, mmba);
              ActiveBidRows++;
              mmba.BidActivity++;
              MarketMakerBidAsk mmbaTmp = (MarketMakerBidAsk)slBid.GetByIndex(0);
              if (mmba.MMID == mmbaTmp.MMID) mmbaInside = mmbaTmp;
              // update price table
              if ( slBidPrice.ContainsKey( price ) ) {
                ix = slBidPrice.IndexOfKey( price );
                quan = (int)slBidPrice.GetByIndex(ix);
                quan += md.Size;
                slBidPrice.SetByIndex(ix, quan);
              }
              else {
                quan = md.Size;
                slBidPrice.Add( price, quan );
              }
              if ( null != BidPriceBookRowChanged ) BidPriceBookRowChanged( this, price, quan );
            }
            if (null != mmbaInside) {
              mmbaInside.BidInside++;
              bInsideQuoteChanged = true;
            }
            break;
          case MDSide.Ask:
            mmba.UpdateAsk( md.DateTime, price, md.Size );
            if (md.Size > 0) {
              lbk = mmba.AskKey;
              slAsk.Add(lbk, mmba);
              ActiveAskRows++;
              mmba.AskActivity++;
              MarketMakerBidAsk mmbaTmp = (MarketMakerBidAsk)slAsk.GetByIndex(0);
              if (mmba.MMID == mmbaTmp.MMID) mmbaInside = mmbaTmp;
              // update price table
              if ( slAskPrice.ContainsKey( price ) ) {
                ix = slAskPrice.IndexOfKey( price );
                quan = (int)slAskPrice.GetByIndex(ix);
                quan += md.Size; 
                slAskPrice.SetByIndex(ix, quan );
              }
              else {
                quan = md.Size;
                slAskPrice.Add( price, quan );
              }
              if ( null != AskPriceBookRowChanged ) AskPriceBookRowChanged( this, price, quan );
            }
            if (null != mmbaInside) {
              mmbaInside.AskInside++;
              bInsideQuoteChanged = true;
            }
            break;
        }
        if ( bInsideQuoteChanged ) {
          if (null != InsideQuoteChangedEventHandler) 
            InsideQuoteChangedEventHandler( this, EventArgs.Empty );
        }
      }
      catch (Exception e) {
        Console.WriteLine("UpdateDataStructures problem {0}", e);
      }
      finally { 
      }
    }
  }


  public class CompareByPriceSizeMmidAsc : IComparer {
    // Ascending for Ask

    public int Compare( object x, object y ) {

      if (((LimitBookKey)x).price == ((LimitBookKey)y).price) {
        if (((LimitBookKey)x).size == ((LimitBookKey)y).size) {
          return (((LimitBookKey)x).mmid.CompareTo(((LimitBookKey)y).mmid));
        }
        else {
          return (((LimitBookKey)x).size.CompareTo(((LimitBookKey)y).size));
        }
      }
      else {
        return (((LimitBookKey)x).price.CompareTo(((LimitBookKey)y).price));
      }
      return 0;
    }
  }

  public class CompareByPriceSizeMmidDec : IComparer {
    // Descending for Bid

    public int Compare( object y, object x ) { // simply reversed the objects

      if (((LimitBookKey)x).price == ((LimitBookKey)y).price) {
        if (((LimitBookKey)x).size == ((LimitBookKey)y).size) {
          return (((LimitBookKey)x).mmid.CompareTo(((LimitBookKey)y).mmid));
        }
        else {
          return (((LimitBookKey)x).size.CompareTo(((LimitBookKey)y).size));
        }
      }
      else {
        return (((LimitBookKey)x).price.CompareTo(((LimitBookKey)y).price));
      }
      return 0;
    }
  }

}

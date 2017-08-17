//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
// First File  : 2006/01/10
//============================================================================

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

using SmartQuant;
using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Series;
using SmartQuant.FIXData;
using SmartQuant.Providers;
using SmartQuant.Instruments;

namespace OneUnified.IQFeed {

  using OneUnified.Sockets;

  public class IQFeedDataRequestRecord {
    internal FIXMarketDataRequest request;
    internal Instrument instrument;
    internal string sSecurityType;
    internal string sCurrency;
    internal string sSecurityExchange;
    internal int cnt;
    private DateTime latestDateTimeUsed = new DateTime(0);

    TimeSpan ms = new TimeSpan(0, 0, 0, 0, 1);
    
    public IQFeedDataRequestRecord() {
      cnt = 1;
    }

    public DateTime GetUniqueTimeStamp() {
      DateTime now = Clock.Now;
      if (now > latestDateTimeUsed) {
        latestDateTimeUsed = now;
      }
      else {
        latestDateTimeUsed += ms;
      }
      return latestDateTimeUsed;

    }
  }

  public class t : IHistoryProvider {
    #region IHistoryProvider Members

    public bool BarSupported {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool DailySupported {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public Bar[] GetBarHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2, int barSize ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Daily[] GetDailyHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2, bool dividendAndSplitAdjusted ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Quote[] GetQuoteHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Trade[] GetTradeHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public MarketDepth[] GetMarketDepthHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool MarketDepthSupported {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool QuoteSupported {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool TradeSupported {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IProvider Members

    public void Connect( int timeout ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Connect() {
      throw new Exception("The method or operation is not implemented.");
    }

    public event EventHandler Connected;

    public void Disconnect() {
      throw new Exception("The method or operation is not implemented.");
    }

    public event EventHandler Disconnected;

    public event ProviderErrorEventHandler Error;

    public byte Id {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsConnected {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public string Name {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Shutdown() {
      throw new Exception("The method or operation is not implemented.");
    }

    public ProviderStatus Status {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public event EventHandler StatusChanged;

    public string Title {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public string URL {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion
  }

  public class IQFeedProvider : IProvider, IMarketDataProvider, IHistoryProvider, IInstrumentProvider   {

    private const string CATEGORY_INFO = "Information";
    private const string CATEGORY_STATUS = "Status";
    private const string CATEGORY_SETTINGS = "Settings";
    private const string CATEGORY_DATA = "Data";

    private const string PROVIDER_NAME = "IQFeed";
    private const string PROVIDER_TITLE = "DTN IQFeed Provider";
    private const string PROVIDER_URL = "http://www.iqfeed.net/";

    private static byte id = 102;

    private static int cntClients = 0;      // number of client objects connected

    private IQFeed iqf;
    private IQFeedLevelII iqfl2;

    private ProviderStatus psIQFeed = ProviderStatus.Disconnected;
    // Two stages:  1) connect to dll ( loggingin ), 2) make connection ( connecting )
    // 1) managed via static mechanisms, 2) managed via object oriented mechanisms

    private IBarFactory factory;
    private Hashtable htL1WatchedSymbols;
    private Hashtable htL2WatchedSymbols;

    public IQFeedProvider() {

      iqf = new IQFeed();
      //iqf.IQ32DLLPath = "H:\\Program Files\\trading\\DTN\\DTN.IQ\\";
      iqf.Messages += new MessageEventHandler(EmitMessage);

      iqfl2 = new IQFeedLevelII();

      BarFactory = new BarFactory();
      htL1WatchedSymbols = new Hashtable();
      htL2WatchedSymbols = new Hashtable();

      ProviderManager.Add(this);
    }

    #region IProvider Members

    public event EventHandler StatusChanged;
    public event EventHandler Connected;
    public event EventHandler Disconnected;
    public event ProviderErrorEventHandler Error;

    public void Connect( int timeout ) {

      Connect();
      ProviderManager.WaitConnected(this, timeout);
    }

    public void Connect() {
      cntClients++;
      if (ProviderStatus.Disconnected == psIQFeed) {

        psIQFeed = ProviderStatus.LoggingIn;
        if (null != StatusChanged) StatusChanged(this, new EventArgs());

        iqf.LoggedInEventHandler += OnLoggedIn;
        iqf.ConnectingEventHandler += OnConnecting;
        iqf.ConnectedEventHandler += OnConnected;
        iqf.DisconnectedEventHandler += OnDisconnected;

        iqf.Connect();
      }
      else {
      }
      // might persue the bit about adding another port 5009 connection, but one will do for now
    }

    private void EmitMessage( object o, MessageArgs args ) {
      EmitError(1, 1, args.Message);
    }

    internal void EmitError(int id, int code, string message) {
      if (null != Error) {
        Error( new ProviderErrorEventArgs(Clock.Now,this, id, code, message) );
      }
    }

    private void OnLoggedIn( object sender, EventArgs e ) {
      psIQFeed = ProviderStatus.LoggedIn;
      if (null != StatusChanged) StatusChanged(this, new EventArgs());
    }

    private void OnConnecting( object sender, EventArgs e ) {
      psIQFeed = ProviderStatus.Connecting;
      if (null != StatusChanged) StatusChanged(this, new EventArgs());
    }

    private void OnConnected( object sender, EventArgs e ) {
      psIQFeed = ProviderStatus.Connected;
      if (null != StatusChanged) StatusChanged(this, new EventArgs());
      if (null != Connected) Connected(this, new EventArgs());

      iqf.HandleNewsMessage += new NewsMessageHandler(iqf_HandleNewsMessage);
      iqfl2.Open();
    }

    public void Disconnect() {
      cntClients--;
      if (ProviderStatus.Connected == psIQFeed) {
        iqfl2.Close();
        iqf.HandleNewsMessage -= new NewsMessageHandler(iqf_HandleNewsMessage);
        iqf.Disconnect();
        //wait();
      }
      else {
      }
    }

    private void OnDisconnected( object sender, EventArgs e ) {
      psIQFeed = ProviderStatus.Disconnected;
      if (null != StatusChanged) StatusChanged(this, new EventArgs());
      if (null != Disconnected) Disconnected(this, new EventArgs());

      iqf.LoggedInEventHandler -= OnLoggedIn;
      iqf.ConnectingEventHandler -= OnConnecting;
      iqf.ConnectedEventHandler -= OnConnected;
      iqf.DisconnectedEventHandler -= OnDisconnected;

    }

    public void Shutdown() {
      Console.WriteLine("IQFeedProvider.Shutdown() Called");
    }

    [Category(CATEGORY_INFO)]
    public byte Id {
      get { return id; }
    }

    [Category(CATEGORY_INFO)]
    public string Name {
      get { return PROVIDER_NAME; }
    }

    [Category(CATEGORY_INFO)]
    public string Title {
      get { return PROVIDER_TITLE; }
    }

    [Category(CATEGORY_INFO)]
    public string URL {
      get { return PROVIDER_URL; }
    }

    [Category(CATEGORY_STATUS)]
    public bool IsConnected {
      get { return ProviderStatus.Connected == psIQFeed; }
    }

    [Category(CATEGORY_STATUS)]
    public ProviderStatus Status {
      get { return psIQFeed; }
    }

    #endregion

    #region IMarketDataProvider Members

    public IBarFactory BarFactory {
      get { return factory; }
      set {
        if (factory != null) {
          factory.NewBar -= new BarEventHandler(OnNewBar);
          factory.NewBarOpen -= new BarEventHandler(OnNewBarOpen);
          factory.NewBarSlice -= new BarSliceEventHandler(OnNewBarSlice);
        }

        factory = value;

        if (factory != null) {
          factory.NewBar += new BarEventHandler(OnNewBar);
          factory.NewBarOpen += new BarEventHandler(OnNewBarOpen);
          factory.NewBarSlice += new BarSliceEventHandler(OnNewBarSlice);
        }
      }
    }

    public event MarketDataRequestRejectEventHandler MarketDataRequestReject;

    public event MarketDataSnapshotEventHandler MarketDataSnapshot;

    public event BarEventHandler NewBar;

    public event BarEventHandler NewBarOpen;

    public event BarSliceEventHandler NewBarSlice;

    public event CorporateActionEventHandler NewCorporateAction;

    public event FundamentalEventHandler NewFundamental;

    public event BarEventHandler NewMarketBar;

    public event MarketDataEventHandler NewMarketData;

    public event MarketDepthEventHandler NewMarketDepth;

    public event QuoteEventHandler NewQuote;

    public event TradeEventHandler NewTrade;

    public event NewsEventHandler NewNews;

    // not in standard interace, specific to iqf
    public event TradeEventHandler TradeSummary;

    void iqf_HandleNewsMessage( object sender, NewsMessageEventArgs args ) {

      FIXNews news = new FIXNews();
      news.Headline = args.Message.HeadLine;
      news.NoRelatedSym = args.Message.Symbols.Length;
      news.SenderSubID = args.Message.DistributorCode;
      news.SendingTime = args.Message.TimeStamp;
      news.SenderCompID = args.Message.StoryID;
      news.TargetSubID = args.Message.SymbolList;

      if (null != NewNews) NewNews(this, new NewsEventArgs(news));
    }

    private void OnNewBar( object sender, BarEventArgs args ) {
      if (NewBar != null)
        NewBar(this, new BarEventArgs(args.Bar, args.Instrument, this));
    }

    private void OnNewBarOpen( object sender, BarEventArgs args ) {
      if (NewBarOpen != null)
        NewBarOpen(this, new BarEventArgs(args.Bar, args.Instrument, this));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnNewBarSlice( object sender, BarSliceEventArgs args ) {
      if (NewBarSlice != null)
        NewBarSlice(this, new BarSliceEventArgs(args.BarSize, this));
    }

    public void SendMarketDataRequest( FIXMarketDataRequest request ) {
      // need to request as iqfeed's native symbol type, but return as a generic symbol

      FIXRelatedSymGroup symgrp;
      IQFeedDataRequestRecord rr;

      bool tradeRequest = false;
      bool quoteRequest = false;
      bool marketDepthRequest = false;

      if (request.NoMDEntryTypes > 0) {
        FIXMDEntryTypesGroup mdEntryTypesGroup = request.GetMDEntryTypesGroup(0);

        switch (mdEntryTypesGroup.MDEntryType) {
          case FIXMDEntryType.Trade: {
              tradeRequest = true;
            }
            break;
          case FIXMDEntryType.Bid:
          case FIXMDEntryType.Offer: {
              if (request.MarketDepth == 1)
                quoteRequest = true;
              else
                marketDepthRequest = true;
            }
            break;
        }
      }

      switch (request.SubscriptionRequestType) {
        case DataManager.MARKET_DATA_SUBSCRIBE:
          for (int i = 0; i < request.NoRelatedSym; i++) {

            symgrp = request.GetRelatedSymGroup(i);

            string sWatchedSymbol = symgrp.Symbol;
            Instrument instrument = InstrumentManager.Instruments[symgrp.Symbol];

            foreach (FIXSecurityAltIDGroup grp in symgrp.SecurityAltIDGroup) {
              if (Name == grp.SecurityAltIDSource) {
                sWatchedSymbol = grp.SecurityAltID;
              }
            }

            if (tradeRequest || quoteRequest) {
              if (htL1WatchedSymbols.ContainsKey(sWatchedSymbol)) {
                rr = htL1WatchedSymbols[sWatchedSymbol] as IQFeedDataRequestRecord;
                rr.cnt++;
                rr = null;
              }
              else {
                rr = new IQFeedDataRequestRecord();
                rr.request = request;
                rr.instrument = instrument;
                // Symbol		= instrument.GetSymbol(Name); <== hint for other code

                rr.sCurrency = symgrp.Currency;
                rr.sSecurityType = symgrp.SecurityType;
                rr.sSecurityExchange = symgrp.SecurityExchange;

                htL1WatchedSymbols.Add(sWatchedSymbol, rr);
                Console.WriteLine("MARKET_DATA_SUBSCRIBE {0}", sWatchedSymbol);
                if (1 == htL1WatchedSymbols.Count) {
                  iqf.HandleFundamentalMessage += new FundamentalMessageHandler(EmitFundamental);
                  iqf.HandleSummaryMessage += new SummaryMessageHandler(EmitTradeSummary);
                  iqf.HandleUpdateMessage += new UpdateMessageHandler(EmitQuoteTrade);
                }
                iqf.startWatch(sWatchedSymbol);
                rr = null;
              }
            }

            if (marketDepthRequest) {
              if (htL2WatchedSymbols.ContainsKey(sWatchedSymbol)) {
                rr = htL1WatchedSymbols[sWatchedSymbol] as IQFeedDataRequestRecord;
                rr.cnt++;
                rr = null;
              }
              else {
                instrument.OrderBook.Clear();

                rr = new IQFeedDataRequestRecord();
                rr.request = request;
                rr.instrument = instrument;
                // Symbol		= instrument.GetSymbol(Name); <== hint for other code

                rr.sCurrency = symgrp.Currency;
                rr.sSecurityType = symgrp.SecurityType;
                rr.sSecurityExchange = symgrp.SecurityExchange;

                htL2WatchedSymbols.Add(sWatchedSymbol, rr);
                Console.WriteLine("MARKETDEPTH_DATA_SUBSCRIBE {0}", sWatchedSymbol);
                iqfl2.StartWatch(sWatchedSymbol, new LevelIIUpdateMessageHandler(EmitMarketDepth));
                rr = null;
              }
            }
          }
          break;
        case DataManager.MARKET_DATA_UNSUBSCRIBE:
          for (int i = 0; i < request.NoRelatedSym; i++) {

            symgrp = request.GetRelatedSymGroup(i);

            string sWatchedSymbol = symgrp.Symbol;
            Instrument instrument = InstrumentManager.Instruments[symgrp.Symbol];

            foreach (FIXSecurityAltIDGroup grp in symgrp.SecurityAltIDGroup) {
              if (Name == grp.SecurityAltIDSource) {
                sWatchedSymbol = grp.SecurityAltID;
              }
            }

            if (tradeRequest || quoteRequest) {
              if (htL1WatchedSymbols.ContainsKey(sWatchedSymbol)) {
                rr = htL1WatchedSymbols[sWatchedSymbol] as IQFeedDataRequestRecord;
                rr.cnt--;
                if (0 == rr.cnt) {
                  Console.WriteLine("MARKET_DATA_UNSUBSCRIBE {0}", sWatchedSymbol);
                  iqf.stopWatch(sWatchedSymbol);
                  if (0 == htL1WatchedSymbols.Count) {
                    iqf.HandleFundamentalMessage -= new FundamentalMessageHandler(EmitFundamental);
                    iqf.HandleSummaryMessage -= new SummaryMessageHandler(EmitTradeSummary);
                    iqf.HandleUpdateMessage -= new UpdateMessageHandler(EmitQuoteTrade);
                  }

                  rr.request = null;
                  rr.instrument = null;
                }
                rr = null;
              }
              else {
                throw new ArgumentException("No to stop l1 for symbol " + symgrp.Symbol + "/" + sWatchedSymbol);
              }
            }

            if (marketDepthRequest) {
              if (htL2WatchedSymbols.ContainsKey(sWatchedSymbol)) {
                rr = htL2WatchedSymbols[sWatchedSymbol] as IQFeedDataRequestRecord;
                rr.cnt--;
                if (0 == rr.cnt) {
                  Console.WriteLine("MARKETDEPTH_DATA_UNSUBSCRIBE {0}", sWatchedSymbol);
                  iqfl2.StopWatch(sWatchedSymbol,new LevelIIUpdateMessageHandler(EmitMarketDepth));

                  rr.request = null;
                  rr.instrument = null;
                }
                rr = null;
              }
              else {
                throw new ArgumentException("No to stop l2 for symbol " + symgrp.Symbol + "/" + sWatchedSymbol);
              }
            }


          }
          break;
        default:
          throw new ArgumentException("Unknown subscription type: " + request.SubscriptionRequestType.ToString());
      }

    }

    #endregion

    #region DataHandlers

    internal void EmitMarketDepth( object o, LevelIIUpdateMessageEventArgs args ) {
      if (null != NewMarketDepth) {

        /*
        Console.WriteLine("EmitMarketDepth {0} {1} {2} {3} {4} {5}",
          args.Message.Symbol, args.Message.MMID, args.Message.BidPrice, args.Message.BidSize,
          args.Message.AskPrice, args.Message.AskSize);
        */

        MarketDepth md;
        string Symbol = args.Message.Symbol;
        IQFeedDataRequestRecord rr = htL2WatchedSymbols[Symbol] as IQFeedDataRequestRecord;

        md = new MarketDepth(
          rr.GetUniqueTimeStamp(), 
          args.Message.MMID, 0, MDOperation.Undefined, MDSide.Bid, args.Message.BidPrice, args.Message.BidSize);
        NewMarketDepth(this, new MarketDepthEventArgs(md, rr.instrument, this));

        md = new MarketDepth(
          rr.GetUniqueTimeStamp(), 
          args.Message.MMID, 0, MDOperation.Undefined, MDSide.Ask, args.Message.AskPrice, args.Message.AskSize);
        NewMarketDepth(this, new MarketDepthEventArgs(md, rr.instrument, this));
      }
    }

    internal void EmitFundamental( object o, FundamentalMessageEventArgs args ) {
      if (null != NewFundamental) {
        string sSymbol = args.Message.items[1];
        IQFeedDataRequestRecord rr = htL1WatchedSymbols[sSymbol] as IQFeedDataRequestRecord;
        FundamentalEventArgs f = new FundamentalEventArgs(new Fundamental(), rr.instrument, this);
        try {
          if ("" != args.Message.items[10]) {
            f.Fundamental.EarningsPerShare = Convert.ToDouble(args.Message.items[10]);
          }
        }
        catch {
        }
        NewFundamental(this, f);
      }
    }

    internal void EmitTradeSummary( object sender, SummaryMessageEventArgs args ) {

      IQFeedDataRequestRecord rr = htL1WatchedSymbols[args.Message.Symbol] as IQFeedDataRequestRecord;

      if (null != TradeSummary) {
        //if (0 < args.Message.Last) {
          TradeEventArgs t = new TradeEventArgs(new Trade(
            rr.GetUniqueTimeStamp(), 
            args.Message.Last, 
            args.Message.LastSize
            ),
            rr.instrument, this);
          TradeSummary(this, t);
        //}
        //else {
        //  Console.WriteLine("*** IQFeedProvider tradesummary dLast is {0} ***", args.Message.Last);
        //}
      }
    
    }

    internal void EmitQuoteTrade( object sender, UpdateMessageEventArgs args ) {

      IQFeedDataRequestRecord rr = htL1WatchedSymbols[args.Message.Symbol] as IQFeedDataRequestRecord;

      /*
      if (SmartQuant.TraceLevel.Verbose == trace) {
        string s;
        s = string.Format("  sym='{0}',lst='{1}',vol='{2}',bid='{3}',ask='{4}',bsz='{5}',asz='{6}',typ='{7}'",
          args.Message.Symbol, args.Message.Last, args.Message.LastSize,
          args.Message.Bid, args.Message.Ask, args.Message.BidSize, args.Message.Type );
        Console.WriteLine(s);
      }
       */

      switch (args.Message.Type) {
        case "a":
        case "b":
          if (null != NewQuote) {
            QuoteEventArgs q = new QuoteEventArgs(
              new Quote(
              rr.GetUniqueTimeStamp(), 
              args.Message.Bid, 
              args.Message.BidSize,
              args.Message.Ask,
              args.Message.AskSize),
              rr.instrument, this);
            NewQuote(this, q);
          }
          break;
        case "t":
        case "T":
          Trade trade = new Trade(
            rr.GetUniqueTimeStamp(), 
            args.Message.Last,
            args.Message.LastSize);
          if (null != NewTrade) {
            TradeEventArgs t = new TradeEventArgs(trade, rr.instrument, this);
            NewTrade(this, t);
          }
          if (null != factory) {
            factory.OnNewTrade(rr.instrument, trade);
          }
          break;
        case "o":
          break;
        default:
          break;
      }
    }
  
    #endregion DataHandlers
    
    #region IHistoryProvider Members

    [Category(CATEGORY_DATA)]
    public bool BarSupported {
      get { return false; }
    }

    [Category(CATEGORY_DATA)]
    public bool DailySupported {
      get { return false; }
    }

    [Category(CATEGORY_DATA)]
    public bool QuoteSupported {
      get { return false; }
    }

    [Category(CATEGORY_DATA)]
    public bool TradeSupported {
      get { return false; }
    }

    [Category(CATEGORY_DATA)]
    public bool MarketDepthSupported {
      get { return false; }
    }

    public Bar[] GetBarHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2, int barSize ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Daily[] GetDailyHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2, bool dividendAndSplitAdjusted ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Quote[] GetQuoteHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public Trade[] GetTradeHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    public MarketDepth[] GetMarketDepthHistory( IFIXInstrument instrument, DateTime datetime1, DateTime datetime2 ) {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion

    #region IInstrumentProvider Members

    public event SecurityDefinitionEventHandler SecurityDefinition;

    public void SendSecurityDefinitionRequest( FIXSecurityDefinitionRequest request ) {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion


  }
}

/*
 * History data
 * Old formats:
NONE_MESSAGE = "\r\n!NONE!\r\n";
SYNTAX_ERROR_MESSAGE = "\r\n!SYNTAX_ERROR!\r\n";
ERROR_MESSAGE = "\r\n!ERROR! ";

New formats:
NONE_MESSAGE = "!NONE!";
SYNTAX_ERROR_MESSAGE = "!SYNTAX_ERROR!\r\n";
ERROR_MESSAGE = "!ERROR! ";
*/

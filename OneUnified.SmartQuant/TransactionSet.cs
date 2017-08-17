//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Trading;
using SmartQuant.Execution;
using SmartQuant.Instruments;

namespace OneUnified.SmartQuant {
  #region TransactionSet

  public class ExecutionStatistics {

    SingleOrder Order;

    Quote quoteInitiation;
    Quote quoteCompletion;

    Instrument instrument;

    PositionSide side;

    Quote quoteLatest;

    public double MaxProfit = 0;
    public double CurrentProfit = 0;
    public double RealizedProfit = 0;
    public double MaxProfitBeforeDown = 0;

    enum EState { Created, Submitted, Filled, Reported, Done };
    EState state;

    public static int id = 0;
    int Id = 0;

    public ExecutionStatistics( Instrument instrument ) {
      state = EState.Created;
      this.instrument = instrument;
      Id = ++id;
    }

    public void Enter( Quote quote, SingleOrder Order ) {
      quoteLatest = quote;
      Order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      this.Order = Order;
      if (EState.Created != state) {
        throw new InvalidOperationException("ExecutionStatistics already 'Enter'd "
          + instrument.Symbol + ", " + state);
      }
      state = EState.Submitted;
      quoteInitiation = quote;
      if (Order.Side == Side.Buy) side = PositionSide.Long;
      if (Order.Side == Side.Sell || Order.Side == Side.SellShort) side = PositionSide.Short; 
      // need an error condition here
    }

    public void UpdateQuote( Quote quote ) {
      //Console.WriteLine(quote);
      quoteLatest = quote;
      if (EState.Filled == state ) {
        switch (side) {
          case PositionSide.Long:
            CurrentProfit = quote.Bid - Order.AvgPx;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
          case PositionSide.Short:
            CurrentProfit = Order.AvgPx - quote.Ask;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
        }
      }
    }

    void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {

      ExecutionReport er;
      er = args.ExecutionReport;
      SingleOrder order = (SingleOrder)sender;

      /*
      Console.Write( "er '{0}',{1:#.00},{2},{3},{4},{5}",
      er.ClOrdID, er.AvgPx, er.Commission, er.CumQty, er.LastQty, er.OrderID  );
      Console.Write( ",{0},{1:#.00},{2},{3},{4}",
      er.OrdStatus, er.Price, er.Side, er.Tag, er.Text ); 
      Console.WriteLine(".");
      Console.WriteLine( "er State {0}", state );
      */

      switch (state) {
        case EState.Created:
          throw new Exception("es ExecutionReport EState.Create");
          break;
        case EState.Submitted:
          //throw new Exception( "State.EntrySubmitted" );
          switch (er.OrdStatus) {
            case OrdStatus.Filled:
              state = EState.Filled;
              quoteCompletion = quoteLatest;
              break;
            case OrdStatus.Cancelled:
            case OrdStatus.New:
            case OrdStatus.PartiallyFilled:
            case OrdStatus.PendingCancel:
            case OrdStatus.PendingNew:
            case OrdStatus.Rejected:
            case OrdStatus.Stopped:
              break;
          }
          break;
        case EState.Filled:
          break;
        case EState.Done:
          //throw new Exception( "State.Done" );
          break;
      }
    }

    public void Report() {

      if ( EState.Filled != state ) {
        //Console.WriteLine( "{0} {1} {2} {3}", 
        //	quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteExitInitiation.DateTime.ToString("HH:mm:ss.fff")  );
        //	quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        TimeSpan tsDelay = quoteCompletion.DateTime - quoteInitiation.DateTime;
        string sDelay = tsDelay.Seconds.ToString("D2") + "." + tsDelay.Milliseconds.ToString("D3");
        //string sEntryDelay = tsEntryDelay.ToString("HH:mm:ss.fff"); 

        //   slippage on signal quote to order price
        double slippage = 0.0;
        switch (side) {
          case PositionSide.Long:
            slippage = Order.AvgPx - quoteInitiation.Bid;
            break;
          case PositionSide.Short:
            slippage = quoteInitiation.Ask - Order.AvgPx;
            break;
        }

        double limit = 0;
        if ( OrdType.Limit == Order.OrdType ) {
          limit = Order.Price;
        }

        Console.Write("es,{0,2},{1},{2},",
          Id,
          quoteInitiation.DateTime.ToString("HH:mm:ss.fff"),
          quoteCompletion.DateTime.ToString("HH:mm:ss.fff")
          );
        Console.Write(" {0}:{1,-5},{2},{3,6:#0.0},{4,6:#0.00}",
          instrument.Symbol,
          side,
          sDelay, slippage, limit
          );

        Console.WriteLine(Order.Text);

        state = EState.Reported;
      }
      else {
        //Console.WriteLine("*** {0} esReport called more than once.  Longs {1} Shorts {2}", TotalLong, TotalShrt);
      }

    }


  }

  public class RoundTrip {

    SingleOrder EntryOrder;
    SingleOrder ExitOrder;

    DateTime dtEntryInitiation;
    DateTime dtEntryCompletion;
    DateTime dtExitInitiation;
    DateTime dtExitCompletion;

    Quote quoteEntryInitiation;
    Quote quoteEntryCompletion;
    Quote quoteExitInitiation;
    Quote quoteExitCompletion;

    Instrument instrument;

    PositionSide side;

    Quote latestQuote;

    public double MaxProfit = 0;
    public double CurrentProfit = 0;
    public double MaxProfitBeforeDown = 0;

    int TotalLong = 0;
    int TotalShrt = 0;

    bool ReportActive = true;

    public static int id = 0;
    int Id = 0;

    enum State {
      Created, EntrySubmitted, EntryFilled, HardStopSubmitted, SoftStopSubmitted, ExitSubmitted, ExitFilled, Done
    }
    State state = State.Created;

    public RoundTrip( Instrument instrument ) {
      this.instrument = instrument;
      Id = ++id;
    }

    public void Enter( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      EntryOrder = order;
      if (State.Created == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.Enter in {1}", instrument.Symbol, state);
      }
      //Console.WriteLine( "setting entry submitted" );
      state = State.EntrySubmitted;
      dtEntryInitiation = quote.DateTime; 
      quoteEntryInitiation = quote; 
      if (order.Side == Side.Buy) side = PositionSide.Long;
      if (order.Side == Side.Sell || order.Side == Side.SellShort ) side = PositionSide.Short; 
      // need an error condition here
    }

    public void HardStop( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      if (State.EntryFilled == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.HardStop in {1}", instrument.Symbol, state);
      }
      state = State.HardStopSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void SoftStop( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      if (State.HardStopSubmitted == state) {
        // ok
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.SoftStop in {1}", instrument.Symbol, state);
      }
      state = State.SoftStopSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void Exit( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      //Console.WriteLine( "setting exit submitted" );
      if (State.EntryFilled == state || State.HardStopSubmitted == state) {
        // ok
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.Exit in {1}", instrument.Symbol, state);
      }
      state = State.ExitSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void UpdateQuote( Quote quote ) {
      //Console.WriteLine(quote);
      latestQuote = quote;
      if (State.EntryFilled == state || State.HardStopSubmitted == state) {
        switch (side) {
          case PositionSide.Long:
            CurrentProfit = quote.Bid - EntryOrder.AvgPx;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
          case PositionSide.Short:
            CurrentProfit = EntryOrder.AvgPx - quote.Ask;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
        }
      }
    }

    public void Check() {
    }

    public void Report() {

      if (ReportActive) {
        //Console.WriteLine( "{0} {1} {2} {3}", 
        //	quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteExitInitiation.DateTime.ToString("HH:mm:ss.fff")  );
        //	quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        TimeSpan tsEntryDelay = quoteEntryCompletion.DateTime - quoteEntryInitiation.DateTime;
        string sEntryDelay = tsEntryDelay.Seconds.ToString("D2") + "." + tsEntryDelay.Milliseconds.ToString("D3");
        //string sEntryDelay = tsEntryDelay.ToString("HH:mm:ss.fff"); 

        TimeSpan tsExitDelay = quoteExitCompletion.DateTime - quoteExitInitiation.DateTime;
        string sExitDelay = tsExitDelay.Seconds.ToString("D2") + "." + tsExitDelay.Milliseconds.ToString("D3");

        TimeSpan tsTripDuration = quoteExitCompletion.DateTime - quoteEntryCompletion.DateTime;
        string sTripDuration = tsTripDuration.TotalSeconds.ToString("F3");

        double pl = 0.0;
        switch (side) {
          case PositionSide.Long:
            pl = ExitOrder.AvgPx - EntryOrder.AvgPx;
            break;
          case PositionSide.Short:
            pl = EntryOrder.AvgPx - ExitOrder.AvgPx;
            break;
        }
        // need two more values:  
        //   slippage on signal quote to entry price
        //   slippage on signal quote to exit price


        Console.Write("rt,{0,2},{1},{2},",
          Id,
          quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"),
          quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff")
          );
        Console.Write("{0} {1}:{2,-5},{3},{4,3:#0.0},{5},{6,6:#0.00},{7,6:#0.00},",
          quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"),
          instrument.Symbol,
          side,
          sEntryDelay, sTripDuration, sExitDelay, MaxProfit, pl
          );

        Console.WriteLine(ExitOrder.Text);

        ReportActive = false;
      }
      else {
        Console.WriteLine("*** {0} Report called more than once.  Longs {1} Shorts {2}", TotalLong, TotalShrt);
      }

    }

    /*
    public static void FinalReport() {
      Console.WriteLine( "# Round Trips = {0}", alRoundTrips.Count );
    }
    */

    void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {

      ExecutionReport er;
      er = args.ExecutionReport;
      SingleOrder order = (SingleOrder)sender;

      /*
      Console.Write( "er '{0}',{1:#.00},{2},{3},{4},{5}",
      er.ClOrdID, er.AvgPx, er.Commission, er.CumQty, er.LastQty, er.OrderID  );
      Console.Write( ",{0},{1:#.00},{2},{3},{4}",
      er.OrdStatus, er.Price, er.Side, er.Tag, er.Text ); 
      Console.WriteLine(".");
      Console.WriteLine( "er State {0}", state );
      */

      // a validation that our buy side ultimately matches our sell side
      switch (order.Side) {
        case Side.Buy:
          TotalLong += (int)Math.Round(er.LastQty);
          break;
        case Side.Sell:
          TotalShrt += (int)Math.Round(er.LastQty);
          break;
      }

      switch (state) {
        case State.Created:
          throw new Exception("State.Create");
          break;
        case State.EntrySubmitted:
          //throw new Exception( "State.EntrySubmitted" );
          switch (er.OrdStatus) {
            case OrdStatus.Filled:
              state = State.EntryFilled;
              dtEntryCompletion = latestQuote.DateTime;
              quoteEntryCompletion = latestQuote;
              switch (side) {
                case PositionSide.Long:
                  MaxProfit = latestQuote.Bid - EntryOrder.AvgPx;
                  break;
                case PositionSide.Short:
                  MaxProfit = EntryOrder.AvgPx - latestQuote.Ask;
                  break;
              }
              break;
            case OrdStatus.Cancelled:
            case OrdStatus.New:
            case OrdStatus.PartiallyFilled:
            case OrdStatus.PendingCancel:
            case OrdStatus.PendingNew:
            case OrdStatus.Rejected:
            case OrdStatus.Stopped:
              break;
          }
          break;
        case State.EntryFilled:
          break;
        case State.HardStopSubmitted:
        case State.SoftStopSubmitted:
        case State.ExitSubmitted:
          //throw new Exception( "State.ExitSubmitted" );
          switch (er.OrdStatus) {
            case OrdStatus.Filled:
              state = State.ExitFilled;
              dtExitCompletion = latestQuote.DateTime;
              quoteExitCompletion = latestQuote;
              Report();
              break;
            case OrdStatus.Cancelled:
            case OrdStatus.New:
            case OrdStatus.PartiallyFilled:
            case OrdStatus.PendingCancel:
            case OrdStatus.PendingNew:
            case OrdStatus.Rejected:
            case OrdStatus.Stopped:
              break;
          }
          break;
        case State.ExitFilled:
          //throw new Exception( "State.ExitFilled" );
          break;
        case State.Done:
          //throw new Exception( "State.Done" );
          break;
      }
    }
  }

  //
  // need order round tip  as well as the existing transaction round trip statistics
  //

  public class TransactionSetEventHolder {

    public delegate void UpdateQuoteHandler( object source, Quote quote );
    public event UpdateQuoteHandler OnUpdateQuote;

    public delegate void StrategyStopHandler( object source, EventArgs e );
    public event StrategyStopHandler OnStrategyStop;

    public delegate void UpdateSignalStatusHandler( object source, bool Exited );
    public event UpdateSignalStatusHandler UpdateSignalStatus;

    public void UpdateQuote( object source, Quote quote ) {
      if (null != OnUpdateQuote) OnUpdateQuote(source, quote);
    }

    public void StrategyStop( object source, EventArgs e ) {
      if (null != OnStrategyStop) OnStrategyStop(source, e);
    }

    public void SignalStatus( object source, bool Exited ) {
      if (null != UpdateSignalStatus) UpdateSignalStatus(source, Exited);
    }

  }

  public class TransactionSet {

    TransactionSetEventHolder eventholder;

    bool UpdateQuoteActive = false;
    bool OnStrategyStopEventActive = false;

    public enum ESignal { Long, Short, ScaleIn, ScaleOut, Exit };
    ESignal EntrySignal;
    bool bDone = false;

    enum EState { Init, EntrySent, WaitForExit, CleanUp, Done };
    EState State = EState.Init;

    enum EOrderTag { Entry, Exit, HardStop, TrailingStop };
    bool MaintainStopOrder = false;

    Hashtable ordersSubmitted;
    Hashtable ordersPartiallyFilled;
    Hashtable ordersFilled;
    Hashtable ordersCancelled;
    Hashtable ordersRejected;

    Hashtable ordersTag;

    Quote quote;
    double JumpDelta;
    double HardStopDelta;
    double TrailingStopDelta;
    Instrument instrument;
    string Symbol;
    int quanInitial;
    int quanScaling;
    int quanMax;

    //string HardStopClOrdID = "";  // track our existing stop order
    double HardStopPrice;
    double SoftStopPrice;
    double AvgEntryPrice;  // price at which the entry filled

    bool OutStandingOrdersExist = false;

    int PositionRequested = 0;  // positive for long negative for short
    int PositionFilled = 0;		// positive for long negative for short

    RoundTrip trip;

    ATSComponent atsc;

    public TransactionSet(
      ESignal Signal, ATSComponent atsc,
      int InitialQuantity, int ScalingQuantity, int MaxQuantity,
      Quote quote, double JumpDelta,
      double HardStopDelta, double TrailingStopDelta,
      TransactionSetEventHolder eventholder
      ) {

      //Console.WriteLine( "{0} Entered Transaction Set", quote.DateTime.ToString("HH:mm:ss.fff") );

      this.eventholder = eventholder;

      eventholder.OnUpdateQuote += OnUpdateQuote;
      OnStrategyStopEventActive = true;
      eventholder.OnStrategyStop += OnStrategyStop;
      OnStrategyStopEventActive = true;

      this.EntrySignal = Signal;
      this.JumpDelta = JumpDelta;  // extra bit for setting a limit order
      this.HardStopDelta = HardStopDelta;
      this.TrailingStopDelta = TrailingStopDelta;
      this.quote = quote;
      this.atsc = atsc;
      instrument = atsc.Instrument;
      Symbol = instrument.Symbol;
      quanInitial = InitialQuantity;
      quanScaling = ScalingQuantity;
      quanMax = MaxQuantity;

      trip = new RoundTrip(instrument);

      ordersSubmitted = new Hashtable(10);
      ordersPartiallyFilled = new Hashtable(10);
      ordersFilled = new Hashtable(10);
      ordersCancelled = new Hashtable(10);
      ordersRejected = new Hashtable(10);
      ordersTag = new Hashtable(10);

      if (!((ESignal.Long == Signal) || (ESignal.Short == Signal))) {
        Console.WriteLine("Transaction Set Problem 1");
        throw new ArgumentException(instrument.Symbol + " has improper Entry Signal: " + Signal.ToString());
      }

      SingleOrder order;

      switch (Signal) {
        case ESignal.Long:
          order = atsc.MarketOrder(Side.Buy, quanInitial);
          //order = atsc.LimitOrder(Side.Buy, quanInitial);
          //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
          //order = atsc.StopOrder( SmartQuant.FIX.Side.Buy, quanInitial, Math.Round( quote.Ask + JumpDelta, 2 ) );
          order.Text = "Long Lmt Entr";
          ordersTag.Add(order.ClOrdID, EOrderTag.Entry);
          PositionRequested = quanInitial;
          State = EState.EntrySent;
          trip.Enter(quote, order);
          SendOrder(quote, order);
          break;
        case ESignal.Short:
          order = atsc.MarketOrder(Side.Sell, quanInitial);
          //order = atsc.LimitOrder(Side.Sell, quanInitial);
          //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
          //order = atsc.StopOrder( SmartQuant.FIX.Side.Sell, quanInitial, Math.Round( quote.Bid - JumpDelta, 2 ) );
          order.Text = "Shrt Lmt Entr";
          ordersTag.Add(order.ClOrdID, EOrderTag.Entry);
          PositionRequested = -quanInitial;
          State = EState.EntrySent;
          trip.Enter(quote, order);
          SendOrder(quote, order);
          break;
      }
    }

    private void SendOrder( Quote quote, SingleOrder order ) {
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      order.StatusChanged += new EventHandler(order_StatusChanged);
      OutStandingOrdersExist = true;
      ordersSubmitted.Add(order.ClOrdID, order);
      order.Send();
      //Console.WriteLine( "{0} {1} sent", order.ClOrdID, order.Text );
    }

    private void SetHardStop() {
      SingleOrder order;
      switch (EntrySignal) {
        case ESignal.Long:
          order = atsc.StopOrder(Side.Sell, Math.Abs(PositionFilled), HardStopPrice);
          order.Text = "Long Hard Stop";
          ordersTag.Add(order.ClOrdID, EOrderTag.HardStop);
          //PositionRequested = -Math.Abs( PositionFilled );
          State = EState.WaitForExit;
          trip.HardStop(quote, order);
          SendOrder(quote, order);
          break;
        case ESignal.Short:
          order = atsc.StopOrder(Side.Buy, Math.Abs(PositionFilled), HardStopPrice);
          order.Text = "Shrt Hard Stop";
          ordersTag.Add(order.ClOrdID, EOrderTag.HardStop);
          //PositionRequested = Math.Abs( PositionFilled );
          State = EState.WaitForExit;
          trip.HardStop(quote, order);
          SendOrder(quote, order);
          break;
      }
    }

    private void CheckSoftStop() {
      // don't update if we have no position
      SingleOrder order;
      if (EState.WaitForExit == State & 0 != PositionFilled && trip.MaxProfit > 0) {
        double t;
        switch (EntrySignal) {
          case ESignal.Long:
            t = quote.Bid - TrailingStopDelta;
            if (t > SoftStopPrice) {
              SoftStopPrice = t;
            }
            if (quote.Bid < SoftStopPrice) {
              CancelSubmittedOrders();
              order = atsc.MarketOrder(Side.Sell, Math.Abs(PositionFilled));
              order.Text = "Long Mkt Stop";
              ordersTag.Add(order.ClOrdID, EOrderTag.TrailingStop);
              PositionRequested -= Math.Abs(PositionFilled);
              State = EState.CleanUp;
              trip.SoftStop(quote, order);
              SendOrder(quote, order);
              //State = EState.CleanUp;
            }
            break;
          case ESignal.Short:
            t = quote.Ask + TrailingStopDelta;
            if (t < SoftStopPrice) {
              SoftStopPrice = t;
            }
            if (quote.Ask > SoftStopPrice) {
              CancelSubmittedOrders();
              order = atsc.MarketOrder(Side.Buy, Math.Abs(PositionFilled));
              order.Text = "Shrt Mkt Stop";
              ordersTag.Add(order.ClOrdID, EOrderTag.TrailingStop);
              PositionRequested += Math.Abs(PositionFilled);
              State = EState.CleanUp;
              trip.SoftStop(quote, order);
              SendOrder(quote, order);
              //State = EState.CleanUp;
            }
            break;
        }
      }
    }

    void CancelSubmittedOrders() {
      if (0 < ordersSubmitted.Count) {
        //Console.WriteLine( "Cancelling {0} orders", ordersSubmitted.Count );
        Queue q = new Queue(10);
        foreach (SingleOrder order in ordersSubmitted.Values) {
          q.Enqueue(order);
        }
        while (0 != q.Count) {
          SingleOrder order = (SingleOrder)q.Dequeue();
          //Console.WriteLine( "{0} cancelling", order.ClOrdID );
          order.Cancel();
        }
      }
    }

    public void UpdateSignal( ESignal Signal ) {
      //Console.WriteLine( "In UpdateSignal" );
      if (this.EntrySignal == Signal) {
        // don't bother with stuff in the same direction, just keep monitoring the situation
      }
      else {
        switch (Signal) {
          case ESignal.Long:
          case ESignal.Short:
            throw new ArgumentException(
              instrument.Symbol + " has improper Update Signal: "
              + Signal.ToString() + " vs " + EntrySignal.ToString());
            break;
          case ESignal.ScaleIn:
            break;
          case ESignal.ScaleOut:
            break;
          case ESignal.Exit:
            //Console.WriteLine( "UpdateSignal {0} {1} {2} {3}", Signal, State, PositionRequested, PositionFilled );
            if (EState.WaitForExit == State || EState.EntrySent == State) {
              // cancel all outstanding orders
              CancelSubmittedOrders();
              // set flag so that if something gets filled, to unfill it right away 
              // later may want to keep it if things are going in the correct direction
              SingleOrder order;
              if (0 < PositionFilled) {
                order = atsc.MarketOrder(Side.Sell, Math.Abs(PositionFilled));
                //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
                //entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Sell, Quantity, quote.Bid - Jump );
                order.Text = "Long Mkt Exit";
                ordersTag.Add(order.ClOrdID, EOrderTag.Exit);
                PositionRequested -= Math.Abs(PositionFilled);
                State = EState.CleanUp;
                trip.Exit(quote, order);
                SendOrder(quote, order);
              }
              if (0 > PositionFilled) {
                order = atsc.MarketOrder(Side.Buy, Math.Abs(PositionFilled));
                //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
                //entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, quote.Ask + Jump );
                order.Text = "Shrt Mkt Exit";
                ordersTag.Add(order.ClOrdID, EOrderTag.Exit);
                PositionRequested += Math.Abs(PositionFilled);
                State = EState.CleanUp;
                trip.Exit(quote, order);
                SendOrder(quote, order);
              }
            }
            break;
        }
      }
    }

    void order_StatusChanged( object sender, EventArgs e ) {

      SingleOrder order = sender as SingleOrder;
      //Console.WriteLine( "*** {0} Status {1} cum {2} leaves {3} last {4} total {5}",
      //	instrument.Symbol, order.OrdStatus, order.CumQty, order.LeavesQty, order.LastQty, order.OrderQty );

      bool CheckStop = false;

      switch (order.OrdStatus) {
        case OrdStatus.PartiallyFilled:
          if (0 != order.LeavesQty) {
            Console.WriteLine("*** {0} Remaining quantity = {1}", instrument.Symbol, order.LeavesQty);
          }
          if (!ordersPartiallyFilled.ContainsKey(order.ClOrdID)) {
            if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
              ordersSubmitted.Remove(order.ClOrdID);
            }
            ordersPartiallyFilled.Add(order.ClOrdID, order);
          }
          //CheckStop = true;  // need to fix this sometime
          break;
        case OrdStatus.Filled:
          //Console.WriteLine("Average fill price = {0}@{1:#.00} {2} {3}", order.OrderQty, order.AvgPx, order.Side, order.OrdStatus);
          if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
            ordersSubmitted.Remove(order.ClOrdID);
          }
          if (ordersPartiallyFilled.ContainsKey(order.ClOrdID)) {
            ordersPartiallyFilled.Remove(order.ClOrdID);
          }
          ordersFilled.Add(order.ClOrdID, order);
          CheckStop = true;  // HardStopPrice on set on 'Filled'
          break;
        case OrdStatus.Cancelled:
          if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
            ordersSubmitted.Remove(order.ClOrdID);
          }
          ordersCancelled.Add(order.ClOrdID, order);
          break;
        case OrdStatus.PendingCancel:
          // not used during simulation
          // signalled during realtime trading
          break;
        default:
          Console.WriteLine("*** {0} Order status changed to : {1}", instrument.Symbol, order.OrdStatus.ToString());
          break;
      }

      if (CheckStop) {
        if (ordersTag.ContainsKey(order.ClOrdID)) {
          EOrderTag tag = (EOrderTag)ordersTag[order.ClOrdID];
          if (EOrderTag.Entry == tag) {
            SetHardStop();
          }
          if (EOrderTag.HardStop == tag) {
            State = EState.CleanUp;
          }
        }
      }

      OutStandingOrdersExist = ((0 != ordersSubmitted.Count) || (0 != ordersPartiallyFilled.Count));
      //Console.WriteLine( "{0} status {1} {2} {3} {4}", order.ClOrdID, order.OrdStatus, 
      //	OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
    }

    void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {

      SingleOrder order = sender as SingleOrder;
      ExecutionReport report = args.ExecutionReport;

      //Console.WriteLine("Execution report type : " + report.ExecType);

      if (report.ExecType == ExecType.Fill || report.ExecType == ExecType.PartialFill) {
        //Console.WriteLine("Fill report, average fill price = {0}@{1:#.00}", report.OrderQty, report.AvgPx);
        //Console.WriteLine( "*** {0} Report {1} cum {2} leaves {3} last {4} total {5} ",
        //	instrument.Symbol, report.OrdStatus, report.CumQty, report.LeavesQty, report.LastQty, report.OrderQty );
        switch (order.Side) {
          case Side.Buy:
            PositionFilled += (int)Math.Round(report.LastQty);
            break;
          case Side.Sell:
            PositionFilled -= (int)Math.Round(report.LastQty);
            break;
        }
      }
      if (report.ExecType == ExecType.Fill) {
        if (ordersTag.ContainsKey(order.ClOrdID)) {
          EOrderTag tag = (EOrderTag)ordersTag[order.ClOrdID];
          if (EOrderTag.Entry == tag) {
            AvgEntryPrice = report.AvgPx;
            switch (EntrySignal) {
              case ESignal.Long:
                HardStopPrice = AvgEntryPrice - HardStopDelta;  // this may not work if we have multiple partial fills
                SoftStopPrice = AvgEntryPrice - TrailingStopDelta;
                break;
              case ESignal.Short:
                HardStopPrice = AvgEntryPrice + HardStopDelta;  // this may not work if we have multiple partial fills
                SoftStopPrice = AvgEntryPrice + TrailingStopDelta;
                break;
            }
          }
        }
      }
    }

    void OnStrategyStop( object o, EventArgs e ) {
      //Console.WriteLine( "{0} TransactionSet StrategyStop", instrument.Symbol );
      UpdateSignal(ESignal.Exit);
      ClearEvents();
    }

    private void OnUpdateQuote( object source, Quote quote ) {
      //Console.WriteLine( "In UpdateQuote" );
      this.quote = quote;
      trip.UpdateQuote(quote);
      CheckSoftStop();
      //Console.WriteLine( "updatequote {0} {1} {2} {3}", State, OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
      if (EState.CleanUp == State && !OutStandingOrdersExist) {
        //if ( !OutStandingOrdersExist ) {
        //Console.WriteLine( "transaction set final clean up" );
        ClearEvents();
        eventholder.SignalStatus(this, true);
        State = EState.Done;
      }
    }

    void ClearEvents() {
      if (UpdateQuoteActive) {
        eventholder.OnUpdateQuote -= OnUpdateQuote;
        UpdateQuoteActive = false;
      }
      if (OnStrategyStopEventActive) {
        eventholder.OnStrategyStop -= OnStrategyStop;
        OnStrategyStopEventActive = false;
      }
    }
  }

  #endregion TransactionSet
}

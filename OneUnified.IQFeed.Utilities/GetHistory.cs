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
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;

using System.Data;
using System.Data.SqlClient;

using OneUnified.SmartQuant;

using SmartQuant.Instruments;
using SmartQuant.Series;
using SmartQuant.Data;
using SmartQuant.FIX;

namespace OneUnified.IQFeed.Utilities {

  using OneUnified.Sockets;
  using OneUnified;

  public class TradeQuoteInfo {
    public DateTime dt;
    public float Trade;
    public int Size;
    public float Bid;
    public int BidSize;
    public float Ask;
    public int AskSize;

    public TradeQuoteInfo ( 
      DateTime dt, float Trade, int Size,
      float Bid, int BidSize, float Ask, int AskSize ) {

      this.dt = dt;
      this.Trade = Trade;
      this.Size = Size;
      this.Bid = Bid;
      this.BidSize = BidSize;
      this.Ask = Ask;
      this.AskSize = AskSize;
    }
  }

	public class HistoryState{
		// used in the call back to see what we need to
		public string sSymbol;  // contains the symbol we are collecting
		public string sSeries;  // string describing the series
		public GetHistory gh;  // so that static call gets its object back
    public BufferedSocket bufSock;  // for retrieving historical data
		//public IDataSeries ids;  // where the data will be written
		public GetHistory.stateLine smLine;
		public int size;  // number of seconds in the interval
		public int cntValues;
		public Instrument instrument;
		public int intPlaceInQ;  // sequence number of creation
		public int intUseCount;  // how many times pulled from Q and used
    public int cntLines;  // number of lines retrieved 
    public string sSendString;  // string used to start the action
    public bool bActive; // has been dequeued and is acquiring
    public BarSeries bs;
    public int cntRequestedDays;
    public BarSeries bars;
    public TradeArray trades;
    public QuoteArray quotes;
    public DateTime dtstk;  // queue currently contains this dattime series (to the minute)
    public Stack stkTrades;  // stack the trades so oldest at the bottom based on retrieval from iqfeed



		public HistoryState( 
			GetHistory gh
			) {
			this.gh = gh;			
      trades = new TradeArray();
      quotes = new QuoteArray();
      stkTrades = new Stack(2000);
      dtstk = new DateTime(0);
      Init();
		}

    public HistoryState() {
      Init();
    }

    private void Init() {
      bActive = false;
      intUseCount = 0;
      bars = new BarSeries();
      smLine = GetHistory.stateLine.lineN;  // start state machine with first line
    }

    private void Handle9100Line( object o, BufferArgs args ) {
      //HistoryState hs = (HistoryState)e.user;
      //GetHistory gh = hs.gh;
      //Console.WriteLine("hl {0}", e.Line);
      if (null == gh) {
        throw new ApplicationException("gh is null");
      }
      //lock (typeof(GetHistory)) {
      //Monitor.Enter(typeof(GetHistory));  // ensure only a single entrance
      //Monitor.Enter(cs);
      gh.doLine(args, this);
      //Monitor.Exit(typeof(GetHistory));   // ensure only a single exit
      //Monitor.Exit(cs);
      //}
    }

    public void Open() {
      bufSock = new BufferedSocket("localhost", 9100, new SocketLineHandler(Handle9100Line));
      //bs = new BufferedSocket( "localhost", 9100, hs );
      //bs.Add(new Buffer.LineHandler(Handle9100Line));
      //hs.bs9100 = bs;  // this keeps the reference count up so the object doesn't self-disintegrate
      bufSock.Open();

    }

    public void Close() {
      gh = null;
      instrument = null;
      bs = null;
      bars = null;
      trades = null;
      quotes = null;
      stkTrades = null;
      bufSock.Close();
      bufSock = null;
    }

  }

	/// <summary>
	/// Summary description for getHistory.
	/// </summary>
	public class GetHistory {

		//BufferedSocket bs9100;  // for retrieving historical data
		string sDelimComma = ",";
		char[] chDelimComma;
		string sDelimTime = "- :";
		char[] chDelimTime;

		//		public event Buffer.LineHandler HandleLine;
		public enum stateLine { lineN, lineLast, lineResync, lineRecover1, lineRecover2, lineRecover3, lineRecover4 };
    // lineRecover1 = blank line
    /* 
     HM,CSCO,1,1;!ERROR! !NONE!  -- point of error
                                 -- recover1
    !ENDMSG!                     -- recover2
    !ERROR! Unknown Error.       -- recover3
                                 -- recover4
    !ENDMSG
     */

		TradeDB db = new TradeDB();
		SqlDataReader sdr;

    //const int ACTIVESYMBOLS = 25;   replaced by cntActiveSymbols
		//const int DATATYPES = 2;
		//const int QUEUE9100SIZE = DATATYPES * ACTIVESYMBOLS; // x sets of y commands at a time

		Queue q9100Slots;

    Object semaphore;  // used for synching between foreground and background

    bool bTick;
		bool b1min;
		bool b3min;
		bool b10min;
		bool b30min;
		bool b2hr;
		bool bDaily;

    int cntTick;
    int cnt1min;
    int cnt3min;
    int cnt10min;
    int cnt30min;
    int cnt2hr;
    int cntDaily;

		int cntSymbols = 0;
    int cntTypes = 0;
    int intQSize = 0;
    int cntActiveSymbols = 0;

    bool bAbort = false;

    private string st; // securitytype

    private HistoryState[] hsAcquiring;

    public int cntLps;
    public int intSec;

    private bool bSymbolsDone = false;
		
		public GetHistory( int cntActiveSymbols, 
      bool bTick, bool b1min, bool b3min, bool b10min, bool b30min, bool b2hr, bool bDaily,
      int cntTick, int cnt1min, int cnt3min, int cnt10min, int cnt30min, int cnt2hr, int cntDaily
      ) {

      this.bTick = bTick;
			this.b1min = b1min;
			this.b3min = b3min;
			this.b10min = b10min;
      this.b30min = b30min;
			this.b2hr = b2hr;
			this.bDaily = bDaily;

      this.cntTick = cntTick;
      this.cnt1min = cnt1min;
      this.cnt3min = cnt3min;
      this.cnt10min = cnt10min;
      this.cnt30min = cnt30min;
      this.cnt2hr = cnt2hr;
      this.cntDaily = cntDaily;

      this.cntActiveSymbols = cntActiveSymbols;

			chDelimComma = sDelimComma.ToCharArray();  // initialize some delmiter arrays
			chDelimTime = sDelimTime.ToCharArray();

      if (bTick) cntTypes++;
      if (b1min) cntTypes++;
      if (b3min) cntTypes++;
      if (b10min) cntTypes++;
      if (b30min) cntTypes++;
      if (b2hr) cntTypes++;
      if (bDaily) cntTypes++;

      intQSize = cntTypes * cntActiveSymbols;

      q9100Slots = new Queue(intQSize);
      hsAcquiring = new HistoryState[intQSize + 1];
      hsAcquiring[0] = new HistoryState();
      hsAcquiring[0].bActive = false;

      semaphore = new object();

      lock (semaphore) {

        //Monitor.Enter(typeof(GetHistory));
        while (intQSize > q9100Slots.Count) { // preload with a bunch of buffers for acquiring data
          HistoryState hs = new HistoryState(this); // symbol, GetHistory, BufferedSocket, IDataSeries
          hs.Open();
          //hs = hsAcquiring[q9100Slots.Count + 1];
          //hs.gh = this;
          q9100Slots.Enqueue(hs);
          hs.intPlaceInQ = q9100Slots.Count;
          hsAcquiring[q9100Slots.Count] = hs;

          //Monitor.Wait(typeof(GetHistory), 200);
          Thread.Sleep(100);

        }
        //Monitor.Exit(typeof(GetHistory));
      }
		}

    public void Abort() {
      bAbort = true;
    }

		public void start( string sQuery, string st ) {
			db.Open();
			SqlCommand cmd;

			//Console.WriteLine( "starting query" );
			//string sCmd = "select distinct top 7 symbol from iqrootoptionsymbols";
			//string sCmd = "select distinct symbol from iqrootoptionsymbols where symbol<>'DCLK'";
			//string sCmd = "select distinct symbol from iqrootoptionsymbols where symbol>'M'";
			//string sCmd = "select distinct symbol from iqrootoptionsymbols where symbol>='NU' and symbol<'OG'";
			//string sCmd = "select distinct symbol from iqrootoptionsymbols " + 
			//	 "where (symbol>='HRP' and symbol<='NTMD') or symbol='CSC' or symbol='CSCO' or " +
			//	 "symbol='AFCO' or symbol='AFFX' or symbol='AFG' or symbol='AFL' or symbol='AG' order by symbol";
      Console.WriteLine(sQuery);
      this.st = st;
      //this.st = SecurityType.USTreasuryBill
      cmd = new SqlCommand(sQuery, db.Connection);

			sdr = cmd.ExecuteReader();

      cntLps = 0;
      intSec = DateTime.Now.Second;

      lock (semaphore) {
        //Monitor.Enter( typeof(GetHistory) );
        //Monitor.Enter( this );
        for (int i = 1; i <= cntActiveSymbols; i++) {
          startSymbol();
        }

        //Monitor.Exit( typeof(GetHistory) );
      }

      //Console.WriteLine("gh waiting");
      Monitor.Enter(this);
      Monitor.Wait(this);
      Monitor.Exit(this);
      //Console.WriteLine("gh done waiting");

      while (0 != q9100Slots.Count) {
        HistoryState finalHS = (HistoryState)q9100Slots.Dequeue();
        finalHS.Close();
      }

      hsAcquiring = null;
      //Console.WriteLine("gh cleaned up");

		}

		private void startSymbol() {

			string sSymbol;
			Instrument instrument = null;
      if (bSymbolsDone) {
        // do nothing
      }
      else {
        if (!bAbort && sdr.Read()) {
          sSymbol = sdr.GetString(0);  // get next symbol in list
          cntSymbols++;

          try {
            instrument = new Instrument(sSymbol, st);
            instrument.Currency = "USD";
            instrument.SecurityExchange = "SMART";
            //instrument.Exchange = "SMART";

            FIXSecurityAltIDGroup altid;

            altid = new FIXSecurityAltIDGroup();
            altid.SecurityAltID = sSymbol;
            altid.SecurityAltIDSource = "IB";
            instrument.AddGroup(altid);
            altid = null;

            altid = new FIXSecurityAltIDGroup();
            altid.SecurityAltID = sSymbol;
            altid.SecurityAltIDSource = "IQFeed";
            instrument.AddGroup(altid);
            altid = null;

            instrument.Save();
          }
          catch {
            //Console.WriteLine( "Caught:  Symbol {0} already exists", sSymbol );
            instrument = InstrumentManager.Instruments[sSymbol];
          }

          Console.WriteLine("Starting {0}, {1}", sSymbol, cntSymbols);
          //Console.WriteLine("Starting {0}, {1} {2} {3}", sSymbol, cntSymbols, hs.intUseCount, hs.intPlaceInQ);

          if (bTick) {
            Send(sSymbol, "Trade", instrument, "HT," + sSymbol + "," + cntTick + ";", cntTick);
          }

          if (b1min) {
            Send(sSymbol, "Bar", instrument, "HM," + sSymbol + "," + cnt1min + ",1;", 60, cnt1min);
          }

          if (b3min) {
            Send(sSymbol, "Bar", instrument, "HM," + sSymbol + "," + cnt3min + ",3;", 180, cnt3min);
          }

          if (b10min) {
            Send(sSymbol, "Bar", instrument, "HM," + sSymbol + "," + cnt10min + ",10;", 600, cnt10min);
          }

          if (b30min) {
            Send(sSymbol, "Bar", instrument, "HM," + sSymbol + "," + cnt30min + ",30;", 1800, cnt30min);
          }

          if (b2hr) {
            Send(sSymbol, "Bar", instrument, "HM," + sSymbol + "," + cnt2hr + ",120;", 7200, cnt2hr);
          }

          if (bDaily) {
            //Console.WriteLine( "Starting {0}, {1} {2} {3}", sSymbol, cntSymbols, hs.intUseCount, hs.intPlaceInQ );
            Send(sSymbol, "Daily", instrument, "HD," + sSymbol + "," + cntDaily + ";", cntDaily);
          }
        }
        else {
          Console.WriteLine("All {0} symbols requested", cntSymbols);
          bSymbolsDone = true;
          sdr.Close();
          db.Close();
        }
      }
		}

    private void Send( string sSymbol, string sSeries, Instrument instrument, string sSendString, int cntRequestedDays ) {
      Send(sSymbol, sSeries, instrument, sSendString, 0, cntRequestedDays);
    }

    private void Send( 
      string sSymbol, string sSeries, Instrument instrument, string sSendString, int intSize, int cntRequestedDays  ) {

      HistoryState hs = (HistoryState)q9100Slots.Dequeue();
      hs.sSymbol = sSymbol;
      hs.sSeries = sSeries;
      hs.instrument = instrument;
      hs.cntRequestedDays = cntRequestedDays;
      
      if (0 != intSize) { 
        hs.bs = DataManager.GetBarSeries(
          hs.instrument,
          DateTime.Today - new TimeSpan(cntRequestedDays + 1, 0, 0, 0, 0), DateTime.Today,
          BarType.Time, intSize);
      }
      
      hs.sSendString = sSendString;
      hs.intUseCount++;
      hs.size = intSize;
      hs.cntValues = 0;
      hs.cntLines = 0;
      hs.bActive = true;
      hs.bufSock.Send(hs.sSendString);
      //Thread.Sleep(200);
      Thread.Sleep(100);
      //Monitor.Wait(typeof(GetHistory), 200);
    }

    private void DeStackTrades( HistoryState hs ) {

      // we are assuming that all queued records belong to the same 1 minute interval
      int cnt = hs.stkTrades.Count;

      if (0 != cnt) {

        TimeSpan ts = new TimeSpan( TimeSpan.TicksPerMinute / cnt );

        TradeQuoteInfo tqi;
        TimeSpan TickTime = new TimeSpan(0);
        for (int i = 1; i <= cnt; i++) {
          tqi = (TradeQuoteInfo)hs.stkTrades.Pop();
          tqi.dt += TickTime;
          TickTime += ts;
          if ( tqi.Trade > 0 && tqi.Size > 0 ) {
            hs.trades.Add( new Trade( tqi.dt, tqi.Trade, tqi.Size ) );
          }
          if ( tqi.Bid > 0 && tqi.Ask > 0 ) {
            hs.quotes.Add( new Quote( tqi.dt, tqi.Bid, tqi.BidSize, tqi.Ask, tqi.AskSize ) );
          }
        }
        tqi = null;

      }
    }

		internal void doLine( BufferArgs e, HistoryState hs ) {
      lock (semaphore) {
        ParseLine(e, hs);
      }

    }

    private void ParseLine( BufferArgs e, HistoryState hs ) {
			DateTime dt;
			string[] r;
			Bar bar;
			Daily daily;  //daily = new Daily(

			//Console.WriteLine( "9100: {0} {1} {2}", hs.smLine, hs.sSymbol, e.Line );
      /*
      if (DateTime.Now.Second == hs.gh.intSec) {
        hs.gh.cntLps++;
      }
      else {
        Console.WriteLine("  lps = {0}", hs.gh.cntLps);
        hs.gh.cntLps = 0;
        hs.gh.intSec = DateTime.Now.Second;
      }*/

			e.items = e.Line.Split( chDelimComma );      			

			switch ( hs.smLine ) {
				case stateLine.lineN:
					if ( 0 == e.Line.Length ) { 
					   // ignore the blank line and prepare for end game
						hs.smLine = stateLine.lineLast;
					}
					else {
						//process the line content here
            hs.cntLines++;
						r = e.items[ 0 ].Split( chDelimTime );
						try {
							switch ( hs.sSeries ) {
                case "Trade":
                  dt = new DateTime( 
                      System.Convert.ToInt32(r[0]),  //yyyy
                      System.Convert.ToInt32( r[1] ),  //mm
                      System.Convert.ToInt32( r[2] ),  //dd
                      System.Convert.ToInt32( r[3] ),  //hh
                      System.Convert.ToInt32( r[4] ),   //mm
                      0
                      );
                  if (hs.dtstk != dt) {
                    DeStackTrades( hs );
                  }
                  hs.dtstk = dt;
                  hs.stkTrades.Push( new TradeQuoteInfo( 
                    dt,
                    ( float )System.Convert.ToSingle( e.items[1] ), // trade,
                    System.Convert.ToInt32( e.items[2] ),  // size
                    ( float )System.Convert.ToSingle( e.items[4] ), // bid
                    System.Convert.ToInt32( e.items[7] ),          // bid size
                    ( float )System.Convert.ToSingle( e.items[5] ), // ask
                    System.Convert.ToInt32( e.items[8] )        // ask size
                    )
                    );
                  break;
								case "Bar":
									dt = new DateTime(
                    System.Convert.ToInt32( r[0] ),  //yyyy
                    System.Convert.ToInt32( r[1] ),  //mm
                    System.Convert.ToInt32( r[2] ),  //dd
                    System.Convert.ToInt32( r[3] ),  //hh
                    System.Convert.ToInt32( r[4] ),  //mm
										0 );                    //ss
									bar = new Bar( dt,
                    ( float )System.Convert.ToSingle( e.items[3] ), // open
                    ( float )System.Convert.ToSingle( e.items[1] ), // high
                    ( float )System.Convert.ToSingle( e.items[2] ), // low
                    ( float )System.Convert.ToSingle( e.items[4] ), // close
                    System.Convert.ToInt32( e.items[6] ),          // period volume
										hs.size );
                  int i = hs.bs.GetIndex(dt);
                  if (0 >= i) {
                    //hs.instrument.Add(bar);
                    hs.bars.Add(bar);
                  }
                  hs.cntValues++;
                  break;
								case "Daily":
									dt = new DateTime(
                    System.Convert.ToInt32( r[0] ),  //yyyy
                    System.Convert.ToInt32( r[1] ),  //mm
                    System.Convert.ToInt32( r[2] ),  //dd
										23,                      //hh
										59,                      //mm
										59 );                    //ss
									daily = new Daily( dt,
                    System.Convert.ToSingle( e.items[3] ),
                    System.Convert.ToSingle( e.items[1] ),
                    System.Convert.ToSingle( e.items[2] ),
                    System.Convert.ToSingle( e.items[4] ),
                    System.Convert.ToInt32( e.items[5] )
										);
									//hs.instrument.Add( daily );
                  hs.bars.Add(daily);
									hs.cntValues++;
									break;
							}
						}
						catch {
							Console.WriteLine( "***** Error: '{0}' '{1}' '{2}' '{3}'", hs.smLine, hs.sSymbol, hs.sSeries, e.Line );
              if (Regex.IsMatch(e.Line, "!ERROR! !NONE!", RegexOptions.None)) {
                //Console.WriteLine("caught !NONE!");
                hs.smLine = stateLine.lineRecover1;
              }
              else {
                if (Regex.IsMatch(e.Line, "!ERROR! Invalid symbol.", RegexOptions.None)) {
                  //Console.WriteLine("caught !NONE!");
                  hs.smLine = stateLine.lineRecover1;
                }
                else {
                  // need to catch and process this erro sometime
                  //***** Error: 'lineN' 'FCH' 'Bar' '!ERROR! Could not connect to History socket.'
                  throw (new Exception("Don't know what we caught"));
                }
              }
            }
					}
					break;
				case stateLine.lineLast:
					if ( "!ENDMSG!" == e.Line ) {
						// signal routines that no more data is available
						// prepare for next command if there is one
						// //Console.WriteLine( "*** endmsg for {0} {1} {2} {3} {4}", hs.sSymbol, hs.sSeries, hs.size, hs.cntValues, hs.ids.Count );
						//Console.WriteLine( "*** endmsg for {0} {1} {2} {3}", hs.sSymbol, hs.sSeries, hs.size, hs.cntValues );
            //hs.ids.Flush();
            Console.Write("Writing {0}", hs.sSymbol );
            switch ( hs.sSeries ) {
              case "Trade":
                if (0 != hs.stkTrades.Count) {
                  DeStackTrades(hs);
                }
                foreach (Trade atrade in hs.trades) {
                  hs.instrument.Add(atrade as Trade);
                }
                hs.trades.Clear();
                foreach (Quote aquote in hs.quotes) {
                  hs.instrument.Add(aquote as Quote);
                }
                hs.quotes.Clear();
                break;
              case "Bar":
                foreach (Bar abar in hs.bars) {
                  hs.instrument.Add(abar as Bar );
                }
                hs.bars.Clear();
                break;
              case "Daily":
                foreach (Bar abar in hs.bars) {
                  hs.instrument.Add(abar as Daily );
                }
                hs.bars.Clear();
                break;
            }
            Console.Write( "." );
            hs.instrument.Save();
            Console.Write(".");
            Console.WriteLine( "." );
						hs.smLine = stateLine.lineN;
            hs.bActive = false;
						q9100Slots.Enqueue( hs );
						//Monitor.Enter( this );
            if ( q9100Slots.Count >= cntTypes) {
							startSymbol();
						}
						//Monitor.Exit( this );
            if (q9100Slots.Count > cntTypes) {
							Console.WriteLine( "Slots:  {0}", q9100Slots.Count );
              ///*
              foreach (HistoryState ihs in hs.gh.hsAcquiring) {
                if ( ihs.bActive ) {
                  Console.WriteLine("  {0} {1} {2} {3} {4}", 
                    ihs.sSymbol, ihs.size, ihs.cntLines, ihs.intPlaceInQ, ihs.intUseCount );
                }
              }
               //*/
						}
            if (intQSize == q9100Slots.Count) {
              Monitor.Enter(this);
              Monitor.Pulse(this);
              Monitor.Exit(this);
            }
          }
					else {
            if (Regex.IsMatch(e.Line, "!ERROR!", RegexOptions.None)) {
              // skip a blank line and prepare for finish
              Console.WriteLine("errline");
              hs.smLine = stateLine.lineResync;
            }
            else {
              Console.WriteLine("** lineLast = '{0}'", e.Line);
              throw new ApplicationException("lineLast doesn't have some thing quite correct");
            }
					}
					break;
				case stateLine.lineResync:
					// two blank lines on error before endmsg
					Console.WriteLine( "We are in resync" );
					hs.smLine = stateLine.lineN;
					break;
        case stateLine.lineRecover1:
          //Console.WriteLine("recover1");
          if (0 != e.Line.Length) {
            Console.WriteLine("** lineRecover1 = '{0}'", e.Line);
            throw new ApplicationException("lineRecover1 does not have an empty line");
          }
          //hs.smLine = stateLine.lineRecover2;
          hs.smLine = stateLine.lineLast;  // 2006/09/04  ** what else does this break
          break;
        case stateLine.lineRecover2:
          Console.WriteLine("recover2");
          if (!Regex.IsMatch(e.Line, "!ENDMSG!", RegexOptions.None)) {
            Console.WriteLine("** lineRecover2 = '{0}'", e.Line);
            throw new ApplicationException("lineRecover2 does not have !ENDMSG!");
          }
          hs.smLine = stateLine.lineRecover4;  // 2006/09/04 changed from 3 to 4;
          break;
        case stateLine.lineRecover3:
          //Console.WriteLine("recover3");
          if ( Regex.IsMatch(e.Line, "!ERROR! Unknown Error.", RegexOptions.None)
            || Regex.IsMatch(e.Line, "!ERROR! Invalid symbol.", RegexOptions.None)) {
          }
          else {
            Console.WriteLine("** lineRecover3 = '{0}'", e.Line);
            throw new ApplicationException("lineRecover3 not !ERROR! Unknown Error|InvalidSymbol.");
            }
            hs.smLine = stateLine.lineRecover4;
          break;
        case stateLine.lineRecover4:
          Console.WriteLine("recover4");
          if (0 != e.Line.Length) {
            Console.WriteLine("** lineRecover4 = '{0}'", e.Line);
            throw new ApplicationException("lineRecover4 does not have an empty line");
          }
          hs.smLine = stateLine.lineLast;
          break;
      };

		}
	}
}

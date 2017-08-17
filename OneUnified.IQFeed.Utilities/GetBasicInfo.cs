//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Data;
using System.Threading;
using System.Collections;
using System.Data.SqlClient;

namespace OneUnified.IQFeed.Utilities {

  using OneUnified;
  using OneUnified.Sockets;
  using OneUnified.SmartQuant;
  using OneUnified.IQFeed;

  public class GetBasicInfo {
		// goes through the query selected symbols and pulls in Fundamental data

		IQFeed iq5009;
		TradeDB db1;
    TradeDB db2;
		Hashtable htSymbols;
		Hashtable htSymbolsNotFound;

    private object semaphore;

		string sSelect =
			"select symbol from iqSymbols where "  
			+ "(exchange='NASDAQ' or exchange='NYSE' or exchange='AMEX' or exchange='NMS' "
			//+ " or exchange='HOTSPOT' or exchange='DTN' )"  // having a few problems with DTN (try again sometime)
			+ " or exchange='HOTSPOT' )"
			+ "and ismutualfund = 0 and ismoneymarketfund = 0 and symbol not like 'RFC.%'";  // RFC causes abort in iqconnect
		//string sSelect = "select symbol from iqSymbols where symbol = 'CRR*'";
		SqlCommand cmdSelect;
		SqlDataReader drSelect;

		//public static GetBasicInfo gbi;

		private int cnt;
		private int cntQ;
		private bool bMore;

    //-----

    Hashtable htColumns = new Hashtable();
    Hashtable htOrd = new Hashtable();
    ArrayList alOrd = new ArrayList();
    short[] rOrd;
    string sColumnNames = "";
    string sParams = "";

    static string sDeleteOptions =
      "delete from iqRootOptionSymbols where symbol=@symbol";
    SqlCommand cmdDeleteOptions;

    static string sDeleteF =
      "delete from iqF where symbol = @symbol";
    SqlCommand cmdDeleteF;

    string sInsertF;
    SqlCommand cmdInsertF;

    string sInsertRootOption =
      "insert into iqRootOptionSymbols (symbol, optionroot ) values ( @symbol, @optionroot )";
    SqlCommand cmdInsertOptionRoot;

    string sDelimSpace = " ";
    char[] chDelimSpace;

    #region Basic Events

    public GetBasicInfo() {
      GetBasicInfoInit();
    }

    public GetBasicInfo( string s ) {
      sSelect = s;
      GetBasicInfoInit();
    }

    private void GetBasicInfoInit( ) {

      semaphore = new object();

      iq5009 = new IQFeed();
      iq5009.ConnectedEventHandler += new EventHandler(iq5009_ConnectedEventHandler);
      iq5009.Connect();

      Wait();

    }

    public void Wait() {
      Monitor.Enter(semaphore);
      Monitor.Wait(semaphore);
      Monitor.Exit(semaphore);
    }

    private void Pulse() {
      Monitor.Enter(semaphore);
      Monitor.Pulse(semaphore);
      Monitor.Exit(semaphore); 
    }

    private void iq5009_ConnectedEventHandler( object sender, EventArgs e ) {

      htSymbols = new Hashtable();
      htSymbolsNotFound = new Hashtable();

      db1 = new TradeDB();
      db1.Open();

      db2 = new TradeDB();
      db2.Open();

      InitFundamentalRecordHandling();

      cmdSelect = new SqlCommand(sSelect, db2.Connection);
      drSelect = cmdSelect.ExecuteReader();

      iq5009.HandleFundamentalMessage += new FundamentalMessageHandler(iq5009_HandleFundamentalMessage);
      //iq5009.HandleSummaryMessage += new SummaryMessageHandler(iq5009_HandleSummaryMessage);
      //iq5009.HandleUpdateMessage += new UpdateMessageHandler(iq5009_HandleUpdateMessage);
      iq5009.HandleWatchSymbolNotFound += new WatchSymbolNotFoundHandler(iq5009_HandleWatchSymbolNotFound);
      
      //iq5009.HandleFAdd = new Buffer.LineHandler(processF);
      //iq5009.HandleQAdd = new Buffer.LineHandler(processQ);

      cnt = 0;
      cntQ = 0;
      bMore = true;
      while (cnt <= 200) {
        requestNextWatch();
      }
    }

    /*
		public static void processF( object source, BufferArgs e ) {
      IQFeed iq5009 = (IQFeed)e.user;
			//GetBasicInfo G = iq5009.GBI;

			//Console.WriteLine( "F: " + e.Line );

			string sSymbol = e.items[ 1 ];
			Console.WriteLine( "Stopping " + sSymbol );
			gbi.htSymbols.Remove( sSymbol );
			iq5009.stopWatch( e.items[ 1 ] );
			gbi.cnt--;
			gbi.requestNextWatch();
		}
     * */

    //void iq5009_HandleSummaryMessage( object sender, SummaryMessageEventArgs args ) {
    //  string Symbol = args.Message.Symbol;
    //}

    void iq5009_HandleUpdateMessage( object sender, UpdateMessageEventArgs args ) {
      
    }

    void iq5009_HandleWatchSymbolNotFound( object sender, WatchSymbolNotFoundMessageEventArgs args ) {
      string Symbol = args.Symbol;
      Console.WriteLine("Not found: " + Symbol);
      //Console.WriteLine(e.Line);
      htSymbols.Remove(Symbol);
      htSymbolsNotFound.Add(Symbol, 1);
      iq5009.stopWatch(Symbol);
      cnt--;
      cntQ++;
      requestNextWatch();

    }

    /*
    public static void processQ( object source, BufferArgs e ) {
			IQ5009 iq5009 = (IQ5009) e.user;
			//GetBasicInfo G = iq5009.GBI;

			//Console.WriteLine( "Q: " + e.Line );

			string sSymbol = e.items[ 1 ];
			string sMsg = e.items[ 3 ];
			if ( "Not Found" == sMsg ) {
				Console.WriteLine( "Not found: " + sSymbol );
				Console.WriteLine( e.Line );
				gbi.htSymbols.Remove( sSymbol );
				gbi.htSymbolsNotFound.Add( sSymbol, 1 );
				iq5009.stopWatch( sSymbol );
				gbi.cnt--;
				gbi.cntQ++;
				gbi.requestNextWatch();
			}
		}
    */

    void iq5009_HandleFundamentalMessage( object sender, FundamentalMessageEventArgs args ) {
      ProcessFundamentalRecord(args.Message);
      string sSymbol = args.Message.Symbol;
      //Console.WriteLine("Stopping " + sSymbol);
      htSymbols.Remove(sSymbol);
      iq5009.stopWatch(sSymbol);
      cnt--;
      requestNextWatch();
    }

    public void requestNextWatch() {
      
			string sSymbol;
			if ( bMore ) {
				if ( drSelect.Read() ) {
					sSymbol = drSelect.GetString( 0 );
					//Console.WriteLine( "get {0}:{1}", cnt, sSymbol );
					htSymbols.Add( sSymbol, 1 );
					iq5009.startWatch( sSymbol );
					cnt++;
				}
				else {
					// close up and finish up
					bMore = false;
				}
			}
			else {
				Console.WriteLine( "Finishing counts:" + cnt + ", " + cntQ );
				//if ( 1 < cnt ) {
				//	foreach ( string s in htSymbols.Keys ) Console.WriteLine( "  remaining {0}", s ); 
				//}
			}
			if ( 0 == cnt ) {
				//foreach ( string s in htSymbolsNotFound.Keys ) Console.WriteLine( "  Didn't find {0}", s ); 
				Console.WriteLine( "Done " );
        
        iq5009.HandleFundamentalMessage -= new FundamentalMessageHandler(iq5009_HandleFundamentalMessage);
        //iq5009.HandleSummaryMessage -= new SummaryMessageHandler(iq5009_HandleSummaryMessage);
        iq5009.HandleWatchSymbolNotFound -= new WatchSymbolNotFoundHandler(iq5009_HandleWatchSymbolNotFound);
        //iq5009.HandleFDel = new Buffer.LineHandler(processF);
				//iq5009.HandleQDel = new Buffer.LineHandler( processQ );
				drSelect.Close();
				db1.Close();
        db2.Close();
				iq5009.Disconnect();

        Pulse();
			}
    }

    #endregion Basic Events

    #region Handle Fundamental Record

    void InitFundamentalRecordHandling() {

      string sVarName;
      short iOrd;

      sInsertF = "";  // this will be built up in subsequent steps
      cmdInsertF = new SqlCommand(sInsertF, db1.Connection);
      //			for ( int i = 0; i < alOrd.Count; i++ ) {
      //				Console.Write( i );
      //				Console.Write( " " );
      //				Console.Write( htOrd[ i ] );
      //				Console.WriteLine();
      //				cmdInsertF.Parameters.Add( "@" + htOrd[ i ], SqlDbType.VarChar );

      string sColumns =
        "select VarName, Ord from iqMessageFormats where MsgType='F' and viewable = 1";
      SqlCommand cmdColumns = new SqlCommand(sColumns, db1.Connection);
      SqlDataReader drColumns = cmdColumns.ExecuteReader();
      while (drColumns.Read()) {
        sVarName = drColumns.GetString(0);
        iOrd = (short)drColumns.GetSqlInt16(1);
        //				Console.WriteLine( sVarName + " " + iOrd );
        htColumns[sVarName] = iOrd;
        htOrd[iOrd] = sVarName;
        alOrd.Add(iOrd);
        if ("" == sColumnNames) {
          sColumnNames = sVarName;
          sParams = "@" + sVarName;
        }
        else {
          sColumnNames += ", " + sVarName;
          sParams += ",	@" + sVarName;
        }
        cmdInsertF.Parameters.Add("@" + sVarName, SqlDbType.VarChar);
      }
      drColumns.Close();

      sInsertF =
        "insert into iqF ( " + sColumnNames + " ) values ( " + sParams + " )";
      //			Console.WriteLine( sInsertF );
      cmdInsertF.CommandText = sInsertF;

      rOrd = new short[alOrd.Count];
      for (int i = 0; i < alOrd.Count; i++) {
        rOrd[i] = (short)alOrd[i];
      }

      cmdColumns.Dispose();
      //			Console.WriteLine( "columns:  " + sColumnNames );
      //			Console.WriteLine( "params :  " + sParams );

      cmdDeleteOptions = new SqlCommand(sDeleteOptions, db1.Connection);
      cmdDeleteOptions.Parameters.Add("@symbol", SqlDbType.VarChar);

      cmdDeleteF = new SqlCommand(sDeleteF, db1.Connection);
      cmdDeleteF.Parameters.Add("@symbol", SqlDbType.VarChar);

      //			Hashtable htTypes = new Hashtable();
      //			htTypes[ "string"   ] = SqlDbType.VarChar;
      //			htTypes[ "float"    ] = SqlDbType.Float;
      //			htTypes[ "date"     ] = SqlDbType.DateTime;
      //			htTypes[ "integer"  ] = SqlDbType.Int;
      //			htTypes[ "smallint" ] = SqlDbType.SmallInt;

      cmdInsertOptionRoot = new SqlCommand(sInsertRootOption, db1.Connection);
      cmdInsertOptionRoot.Parameters.Add("@symbol", SqlDbType.VarChar);
      cmdInsertOptionRoot.Parameters.Add("@optionroot", SqlDbType.VarChar);

      chDelimSpace = sDelimSpace.ToCharArray();

      //iq5009.HandleFundamentalMessage += new FundamentalMessageHandler(doF);
      //iq5009.HandleF += new SocketLineHandler(doF);
    }

    void ProcessFundamentalRecord( FundamentalMessage Message ) {
      // contains 'F' as first element
      //IQ5009 iq5009 = (IQ5009) e.user;
      //iqF F = iq5009.F;

      int j; bool b; string s;

      string sym = Message.items[1];

      try {
        cmdDeleteOptions.Parameters[0].Value = sym;
        cmdDeleteOptions.ExecuteNonQuery();
      }
      catch (Exception E) {
        Console.WriteLine("*** Exception in GetBasicInfo::ProcessFundamentalRecord::cmdDeleteOptions, " + sym + ", " + E.ToString());
      }

      try {
        cmdDeleteF.Parameters[0].Value = sym;
        cmdDeleteF.ExecuteNonQuery();
      }
      catch (Exception E) {
        Console.WriteLine("*** Exception in GetBasicInfo::ProcessFundamentalRecord::cmdDeleteF, " + sym + ", " + E.ToString());
      }

      //Console.WriteLine("->");
      bool flag = false;
      for (int i = 0; i < alOrd.Count; i++) {
        string l = Message.items[rOrd[i]-1];
        if ((5 == rOrd[i] - 1) || (6 == rOrd[i] - 1)) {
          if (0 < l.Length) {
            if (l.Substring(0, 2) == ".-") flag = true;
          }
        }
        if ((45 == rOrd[i] - 1) || (46 == rOrd[i] - 1)) {
          if ((0 == l.Length) || (10 == l.Length)) {
          }
          else {
            flag = true;
          }
        }
        cmdInsertF.Parameters[i].Value = l;
        //Console.WriteLine(" {0},{1},{2} ", i, F.rOrd[i]-1, e.items[F.rOrd[i] - 1]);
      }

      if (flag) {
        Console.WriteLine("** Ejected {0}", sym);
      }
      else {
        try {
          cmdInsertF.ExecuteNonQuery();

          string sTemp = Message.RootOptionSymbols;
          if ("" != sTemp) {
            string[] sOptionRoots = sTemp.Split(chDelimSpace);
            for (int i = 0; i < sOptionRoots.Length; i++) {
              s = sOptionRoots[i];
              j = 0; b = false;
              while (j < i) {
                b |= (s == sOptionRoots[j++]);
              }
              if (!b) {
                cmdInsertOptionRoot.Parameters[0].Value = sym;
                cmdInsertOptionRoot.Parameters[1].Value = s;
                cmdInsertOptionRoot.ExecuteNonQuery();
              }
            }
          }
        }
        catch (Exception E) {
          Console.WriteLine("*** Exception in GetBasicInfo::ProcessFundamentalRecord::cmdInsertF, " + E.ToString());
        }
      }
    }

    #endregion Handle Fundamental Record
  }
}

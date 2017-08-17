//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;

using System.Data;
using System.Data.SqlClient;

using OneUnified;
using OneUnified.SmartQuant;

using SmartQuant.Instruments;
using SmartQuant.Series;
using SmartQuant.Data;
using SmartQuant.FIX;

namespace OneUnified.IQFeed.Utilities {
	/// <summary>
	/// Summary description for SymbolStats.
	/// </summary>
	public class SymbolStats
	{
		public SymbolStats()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public void Run( DateTime dtEnd, bool bDebug ) {
			TradeDB db1;
			TradeDB db2;

			string sDel = "delete from SymbolStats";
			//string sSelect = "select distinct top 5 symbol from iqRootOptionSymbols";
			string sSelect = "select distinct symbol from iqRootOptionSymbols";
			string sInsert = "insert into SymbolStats " 
				+ "( symbol, hi52wk, hi52wkdate, lo52wk, lo52wkdate, "
				+ " hi26wk, hi26wkdate, lo26wk, lo26wkdate, "
				+ " volatility, volume, calcdate ) values ( "
				+ " @symbol, @hi52wk, @hi52wkdate, @lo52wk, @lo52wkdate, "
				+ " @hi26wk, @hi26wkdate, @lo26wk, @lo26wkdate, "
				+ " @volatility, @volume, @calcdate )";

			SqlCommand cmdDel, cmdSelect, cmdInsert;
			SqlDataReader dr;

			//DateTime dtEnd = new DateTime( 2005, 08, 4 );
			///DateTime dtEnd = DateTime.Today;
			//string s= dtEnd.ToShortDateString(
			TimeSpan ts52wk = new TimeSpan( 52 * 7, 0, 0 ,0 );
			DateTime dtStart52wk = dtEnd.Subtract( ts52wk );
			TimeSpan ts26wk = new TimeSpan( 26 * 7, 0, 0, 0 );
			DateTime dtStart26wk = dtEnd.Subtract( ts26wk );
			TimeSpan ts20day = new TimeSpan( 20, 0, 0, 0 );
			DateTime dtStart20day = dtEnd.Subtract( ts20day );

			//DataManager.Init();

			db1 = new TradeDB();
			db1.Open();

			db2 = new TradeDB();
			db2.Open();

			cmdDel = new SqlCommand( sDel, db1.Connection );
			cmdSelect = new SqlCommand( sSelect, db1.Connection );
			cmdInsert = new SqlCommand( sInsert, db2.Connection );

			cmdInsert.Parameters.Add( "@symbol", SqlDbType.VarChar, 12 );
			cmdInsert.Parameters.Add( "@hi52wk", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@hi52wkdate", SqlDbType.DateTime );
			cmdInsert.Parameters.Add( "@lo52wk", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@lo52wkdate", SqlDbType.DateTime );
			cmdInsert.Parameters.Add( "@hi26wk", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@hi26wkdate", SqlDbType.DateTime );
			cmdInsert.Parameters.Add( "@lo26wk", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@lo26wkdate", SqlDbType.DateTime );
			cmdInsert.Parameters.Add( "@volatility", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@volume", SqlDbType.Float );
			cmdInsert.Parameters.Add( "@calcdate", SqlDbType.DateTime );

			cmdDel.ExecuteNonQuery();

			dr = cmdSelect.ExecuteReader();
 
			string sSymbol;
			Instrument instrument;
			DailySeries ds52;
			DailySeries ds26;

			DateTime dtNewHigh52;
			DateTime dtNewHigh26;
			//DateTime dtNewLow52;
			//DateTime dtNewLow26;

			int cntNewHigh52 = 0;
			int cntNewHigh26 = 0;
			int cntNewLow52 = 0;
			int cntNewLow26 = 0;

			while ( dr.Read() ) {
				sSymbol = dr.GetString( 0 );
				if ( bDebug ) Console.Write( "Starting {0}", sSymbol );

				instrument = InstrumentManager.Instruments[ sSymbol ];
				if ( null != instrument ) {
					try {
						ds52 = DataManager.GetDailySeries( instrument, dtStart52wk, dtEnd );
						ds26 = DataManager.GetDailySeries( instrument, dtStart26wk, dtEnd );
						if ( null != ds26 ) {

							dtNewHigh52 = ds52.HighestHighBar( dtStart52wk, dtEnd ).DateTime;
							dtNewHigh26 = ds26.HighestHighBar( dtStart26wk, dtEnd ).DateTime;
							//dtNewLow52  = ds52.LowestLowBar( dtStart52wk, dtEnd ).DateTime;
							//dtNewLow26  = ds52.LowestLowBar( dtStart26wk, dtEnd ).DateTime;

							if ( dtNewHigh52 == dtEnd ) cntNewHigh52++;
							if ( dtNewHigh26 == dtEnd ) cntNewHigh26++;
							//if ( dtNewLow52  == dtEnd ) cntNewLow52++;
							//if ( dtNewLow26  == dtEnd ) cntNewLow26++;

							cmdInsert.Parameters[ "@symbol" ].Value = sSymbol;
							cmdInsert.Parameters[ "@hi52wk" ].Value = ds52.HighestHigh( dtStart52wk, dtEnd );
							//cmdInsert.Parameters[ "@hi52wk" ].Value = 0.0;
							cmdInsert.Parameters[ "@hi52wkdate" ].Value = dtNewHigh52;
							//cmdInsert.Parameters[ "@hi52wkdate" ].Value = dtEnd;
							//cmdInsert.Parameters[ "@lo52wk" ].Value = ds52.LowestLow( dtStart52, dtEnd );
							cmdInsert.Parameters[ "@lo52wk" ].Value = 0.0;
							//cmdInsert.Parameters[ "@lo52wkdate" ].Value = dtNewLow52;
							cmdInsert.Parameters[ "@lo52wkdate" ].Value = dtEnd;
							cmdInsert.Parameters[ "@hi26wk" ].Value = ds26.HighestHigh( dtStart26wk, dtEnd );
							cmdInsert.Parameters[ "@hi26wkdate" ].Value = dtNewHigh26;
							//cmdInsert.Parameters[ "@lo26wk" ].Value = ds52.LowestLow( dtStart26, dtEnd );
							cmdInsert.Parameters[ "@lo26wk" ].Value = 0.0;
							//cmdInsert.Parameters[ "@lo26wkdate" ].Value = dtNewLow26;
							cmdInsert.Parameters[ "@lo26wkdate" ].Value = dtEnd;
							//try {
							//	cmdInsert.Parameters[ "@volatility" ].Value = ds52.GetVariance( dtStart20, dtEnd );
							//}
							//catch {
								cmdInsert.Parameters[ "@volatility" ].Value = 0.0;
								//cmdInsert.Parameters[ "@volatility" ].Value = instrument.Volatility();
							//}
							cmdInsert.Parameters[ "@volume" ].Value = ds26.GetVolumeSeries( dtStart20day, dtEnd).GetMean();
							cmdInsert.Parameters[ "@calcdate" ].Value = dtEnd;

							cmdInsert.ExecuteNonQuery();
						}
					}
					catch ( Exception e ) {
						if ( bDebug ) {
							Console.WriteLine();
							Console.WriteLine( "Error:  {0} {1}", e.Message, e.StackTrace );
						}
					}
				}
				if ( bDebug ) Console.WriteLine();
			}

			db1.Close();
			db2.Close();
			//DataManager.Close();
			Console.WriteLine( "{0} 52 High={1}, 52 Low={2}, 26 High={3}, 26 Low={4}", 
				dtEnd, cntNewHigh52, cntNewLow52, cntNewHigh26, cntNewLow26 );
			dr.Close();
		}
	}
}

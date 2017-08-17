//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

using OneUnified;
using OneUnified.SmartQuant;

namespace OneUnified.IQFeed.Utilities {
	/// <summary>
	/// Summary description for iqSymbolImport.
	/// </summary>
	public class iqSymbolImport 	{

    public static string sDirMktSymbols = "K:\\Data\\Projects\\QD\\QDCustom\\bin\\Debug\\mktsymbols.txt";
    //public static string sDirIqFeedDll = "C:\\Program Files\\Trading\\DTN\\DTN.IQ";
    string dir;

    public iqSymbolImport() {
      dir = sDirMktSymbols;
    }

    public iqSymbolImport( string s ) {
      dir = s;
    }

    public void Run() {
			//
			// TODO: Add constructor logic here
			//
			// http://www.dtniq.com/product/mktsymbols.zip

			//			string sConn0 = "Integrated Security=SSPI;" + 
			//				"Persist Security Info=False;Initial Catalog=Trade;" + 
			//				"Data Source=localhost;" + 
			//				"Packet Size=4096;" +
			//				"";
			//			string sConn = "Integrated Security=SSPI;" + 
			//				"Persist Security Info=False;Initial Catalog=Trade;" + 
			//				"Data Source=localhost;" + 
			//				"Packet Size=4096;Workstation ID=I9100;" +
			//				"";

			TradeDB db1;
			db1 = new TradeDB();
			db1.Open();

			//string sConn = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Trade;Data Source=ARMOR01;Packet Size=4096;";
			//SqlConnection connTrade = new SqlConnection();
			//connTrade.ConnectionString = sConn;
			//connTrade.ConnectionTimeout= 14400;
			//connTrade.Open();
 
			string sDelete;
			SqlCommand cmdDelete;

			sDelete = "delete from iqSymbols";
			cmdDelete = new SqlCommand( sDelete, db1.Connection );
			cmdDelete.CommandType = System.Data.CommandType.Text;
			cmdDelete.CommandTimeout = 14400;
			cmdDelete.ExecuteNonQuery();

			sDelete = "delete from iqRootOptionSymbols";
			cmdDelete = new SqlCommand( sDelete, db1.Connection );
			cmdDelete.CommandType = System.Data.CommandType.Text;
			cmdDelete.CommandTimeout = 14400;
			cmdDelete.ExecuteNonQuery();

			string sInsert = "insert into iqSymbols ( symbol, descr, exchange, isindex, iscboeindex, isindicator, " 
				+ "ismutualfund, ismoneymarketfund ) "
				+ "values ( @symbol, @descr, @exchange, @isindex, @iscboeindex, @isindicator, "
				+ "@ismutualfund, @ismoneymarketfund )";
			SqlCommand cmdInsert = new SqlCommand( sInsert, db1.Connection );
			cmdInsert.CommandType = System.Data.CommandType.Text;

			cmdInsert.Parameters.Add( "@symbol", SqlDbType.VarChar );
			cmdInsert.Parameters.Add( "@descr", SqlDbType.VarChar );
			cmdInsert.Parameters.Add( "@exchange", SqlDbType.VarChar );
			cmdInsert.Parameters.Add( "@isindex", SqlDbType.Bit );
			cmdInsert.Parameters.Add( "@iscboeindex", SqlDbType.Bit );
			cmdInsert.Parameters.Add( "@isindicator", SqlDbType.Bit );
			cmdInsert.Parameters.Add( "@ismutualfund", SqlDbType.Bit );
			cmdInsert.Parameters.Add( "@ismoneymarketfund", SqlDbType.Bit );

			Stream fsSymbols = new FileStream( dir, FileMode.Open );
			StreamReader srSymbols = new StreamReader( fsSymbols, Encoding.ASCII );

			string l;
			int i = 0;
			string [] items = null;
			string sDelim = "\t";
			char [] chDelim = sDelim.ToCharArray();
			//string t;
			string sSymbol;
			string sExchange;

			bool bMutual;
			bool bMoneyMkt;
			bool bIndex;
			bool bCboe;
			bool bIndicator;

			Hashtable htExchange = new Hashtable();
			//int cntLines = 0;

			srSymbols.ReadLine();
			//&& cntLines++ < 34
			while ( srSymbols.Peek() >= 0  ) {
				l = srSymbols.ReadLine();
				//Console.WriteLine( l );
				items = l.Split( chDelim );
				//foreach ( string t in items ) {
				//Console.Write( "'" + t + "'," );
				//}
				if ( 3 != items.GetLength( 0 ) ) {
					Console.WriteLine( "Missing an element: {0}", l );
					continue;
				}
				sSymbol = items[0];
				sExchange = items[2];
				bMutual = Regex.IsMatch( sSymbol, ".{3}[^\\.]X$", RegexOptions.None ) && 5 == sSymbol.Length;
				bMoneyMkt = Regex.IsMatch( sSymbol, ".{3}XX$", RegexOptions.None ) && 5 == sSymbol.Length;
				bIndex = Regex.IsMatch( sSymbol, ".+\\.X$", RegexOptions.None );
				bCboe = Regex.IsMatch( sSymbol, ".+\\.XO$", RegexOptions.None );
				bIndicator = Regex.IsMatch( sSymbol, ".+\\.Z$", RegexOptions.None );

				cmdInsert.Parameters[ 0 ].Value = sSymbol;
				cmdInsert.Parameters[ 1 ].Value = items[ 1 ];
				cmdInsert.Parameters[ 2 ].Value = items[ 2 ];
				cmdInsert.Parameters[ 3 ].Value = bIndex;
				cmdInsert.Parameters[ 4 ].Value = bCboe;
				cmdInsert.Parameters[ 5 ].Value = bIndicator;
				cmdInsert.Parameters[ 6 ].Value = bMutual;
				cmdInsert.Parameters[ 7 ].Value = bMoneyMkt;

				cmdInsert.ExecuteNonQuery();

				//Console.WriteLine( htExchange[ sExchange ].GetType() );
				if ( htExchange.ContainsKey( sExchange ) ) {
				}
				else {
					htExchange[ sExchange ] = 0;
				}
				//htExchange[ sExchange ]++;
				i = (int) htExchange[ sExchange ];
				i++;
				htExchange[ sExchange ] = i;
				//i = (int) htExchange[ sExchange ];
				//i++;
				//htExchange[ sExchange ] = i;


				//Console.WriteLine();
			}

			Console.WriteLine( "{0}", DateTime.Now );
			ArrayList aKeys = new ArrayList( htExchange.Keys );
			aKeys.Sort();
			foreach ( string s in aKeys ) {
				Console.WriteLine( "{0} {1}", s, htExchange[ s ] );
			}

			//foreach ( string s in args ) {
			//Console.WriteLine( s );
			//}


			srSymbols.Close();
			fsSymbols.Close();

			db1.Close();

		}
	}
}


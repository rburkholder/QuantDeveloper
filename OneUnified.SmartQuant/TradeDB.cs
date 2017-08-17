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
using System.Data.SqlClient;

namespace OneUnified.SmartQuant {
	/// <summary>
	/// Summary description for db.
	/// </summary>
	public class TradeDB {

		SqlConnection connTrade;

		public TradeDB() {
			//
			// TODO: Add constructor logic here
			//
			// 
			//string sConn = "Integrated Security=SSPI;" + 
			//	"Persist Security Info=False;Initial Catalog=Trade;" + 
			//	"Data Source=localhost;" + 
			//	"Packet Size=4096;" +
			//	"";
			//string sConn = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Trade;Data Source=10.10.10.100;Packet Size=4096;";
      //Data Source=ARMOR01;Initial Catalog=Trade;User ID=sa
      string sConn = "Data Source=127.0.0.1;Initial Catalog=Trade;User ID=sa;Password=ThisIsFine";
			//string sConn = "Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Trade;Data Source=ARMOR01;Use Procedure for Prepare=1;Auto Translate=True;Packet Size=4096;Workstation ID=ARMOR01;Use Encryption for Data=False;";

			connTrade = new SqlConnection();
			connTrade.ConnectionString = sConn;
		}
		public void Open() {
			connTrade.Open();
		}
		public SqlConnection Connection {
			get { return connTrade; }
		}
		public void Close() {
			connTrade.Close();
		}
	}
}
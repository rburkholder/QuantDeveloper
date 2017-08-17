//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Collections;

namespace OneUnified.SmartQuant {

  public class ToTrade {

    private TradeDB db;

    #region sqlinit

    private static string sCheckExistenceinTradeSystem =
      "select count(*) from ToTrade where tradesystem=@tradesystem and symbol=@symbol";
    private static string sSelectByTradeSystem =
      "select symbol, stop from ToTrade where tradesystem=@tradesystem";
    private static string sDeleteByTradeSystem =
      "delete from ToTrade where tradesystem=@tradesystem";
    private static string sInsert =
      "insert into ToTrade (tradesystem, symbol) values (@tradesystem, @symbol)";
    private static string sInsertWithStop =
      "insert into ToTrade (tradesystem, symbol, stop) values (@tradesystem, @symbol, @stop)";
    private static string sUpdateDarvas =
      "update ToTrade set tag1=tag1+1 where symbol=@symbol and tradesystem=@tradesystem";

    private SqlCommand cmdCheckExistenceInTradeSystem;
    private SqlCommand cmdSelectByTradeSystem;
    private SqlCommand cmdDeleteByTradeSystem;
    private SqlCommand cmdInsert;
    private SqlCommand cmdInsertWithStop;
    private SqlCommand cmdUpdateDarvas;

    #endregion sqlinit

    public ToTrade( TradeDB db ) {

      this.db = db;

      cmdCheckExistenceInTradeSystem = new SqlCommand(sCheckExistenceinTradeSystem, db.Connection);
      cmdSelectByTradeSystem = new SqlCommand(sSelectByTradeSystem, db.Connection);
      cmdDeleteByTradeSystem = new SqlCommand(sDeleteByTradeSystem, db.Connection);
      cmdInsert = new SqlCommand(sInsert, db.Connection);
      cmdInsertWithStop = new SqlCommand(sInsertWithStop, db.Connection);
      cmdUpdateDarvas = new SqlCommand(sUpdateDarvas, db.Connection);

      cmdCheckExistenceInTradeSystem.Parameters.Add("@tradesystem", SqlDbType.VarChar);
      cmdCheckExistenceInTradeSystem.Parameters.Add("@symbol", SqlDbType.VarChar);

      cmdSelectByTradeSystem.Parameters.Add("@tradesystem", SqlDbType.VarChar);
      
      cmdDeleteByTradeSystem.Parameters.Add("@tradesystem", SqlDbType.VarChar);

      cmdInsert.Parameters.Add("@tradesystem", SqlDbType.VarChar);
      cmdInsert.Parameters.Add("@symbol", SqlDbType.VarChar);

      cmdInsertWithStop.Parameters.Add("@tradesystem", SqlDbType.VarChar);
      cmdInsertWithStop.Parameters.Add("@symbol", SqlDbType.VarChar);
      cmdInsertWithStop.Parameters.Add("@stop", SqlDbType.Float);

      cmdUpdateDarvas.Parameters.Add("@tradesystem", SqlDbType.VarChar);
      cmdUpdateDarvas.Parameters.Add("@symbol", SqlDbType.VarChar);


    }

    public bool Exists( string sTradeSystem, string sSymbol ) {

      cmdCheckExistenceInTradeSystem.Parameters["@tradesystem"].Value = sTradeSystem;
      cmdCheckExistenceInTradeSystem.Parameters["@symbol"].Value = sSymbol;

      int result = (int) cmdCheckExistenceInTradeSystem.ExecuteScalar();
      return (result > 0);
    }

    public void Insert( string sTradeSystem, string sSymbol ) {

      cmdInsert.Parameters["@tradesystem"].Value = sTradeSystem;
      cmdInsert.Parameters["@symbol"].Value = sSymbol;

      cmdInsert.ExecuteNonQuery();
    }

    public void Insert( string sTradeSystem, string sSymbol, double dblStop ) {

      cmdInsertWithStop.Parameters["@stop"].Value = dblStop;

      cmdInsertWithStop.Parameters["@tradesystem"].Value = sTradeSystem;
      cmdInsertWithStop.Parameters["@symbol"].Value = sSymbol;
      cmdInsertWithStop.Parameters["@stop"].Value = dblStop;

      cmdInsertWithStop.ExecuteNonQuery();
    }

    public int DeleteByTradeSystem( string sTradeSystem ) {

      cmdDeleteByTradeSystem.Parameters["@tradesystem"].Value = sTradeSystem;

      int result = cmdDeleteByTradeSystem.ExecuteNonQuery();

      return result;
    }

    public SqlDataReader TradeSystemList( string sTradeSystem ) {

      cmdSelectByTradeSystem.Parameters["@tradesystem"].Value = sTradeSystem;
      return cmdSelectByTradeSystem.ExecuteReader();
    }

    public void UpdateDarvas( string sTradeSystem, string sSymbol ) {

      cmdUpdateDarvas.Parameters["@tradesystem"].Value = sTradeSystem;
      cmdUpdateDarvas.Parameters["@symbol"].Value = sSymbol;

      cmdUpdateDarvas.ExecuteNonQuery();

    }

  }
}

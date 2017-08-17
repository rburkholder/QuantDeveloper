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
using System.Text;

namespace OneUnified.IQFeed {

  class ExchangeCodes {

    Hashtable htCodes;

    public ExchangeCodes() {
      htCodes = new Hashtable(50);
      htCodes.Add("F", new ExchangeCode("NASDAQ", "Nasdaq"));
      htCodes.Add("B", new ExchangeCode("NAS OTC", "Nasdaq OTC"));
      htCodes.Add("E", new ExchangeCode("AMEX", "American Stock Exchange "));
      htCodes.Add("D", new ExchangeCode("NYSE", "New York Stock Exchange "));
      htCodes.Add("C", new ExchangeCode("OPRA", "OPRA System "));
      htCodes.Add("IE", new ExchangeCode("DJ", "Dow Jones-Wilshire Indexes (not currently supported) "));
      htCodes.Add("1B", new ExchangeCode("DTN", "DTN (Indexes/Statistics) "));
      htCodes.Add("13", new ExchangeCode("CBOT", "Chicago Board Of Trade"));
      htCodes.Add("1D", new ExchangeCode("KCBT", "Kansas City Board Of Trade "));
      htCodes.Add("10", new ExchangeCode("CME", "Chicago Mercantile Exchange "));
      htCodes.Add("14", new ExchangeCode("MGE", "Minneapolis Grain Exchange "));
      htCodes.Add("1A", new ExchangeCode("NYMEX", "New York Mercantile Exchange "));
      htCodes.Add("12", new ExchangeCode("COMEX", "Commodities Exchange Center "));
      htCodes.Add("18", new ExchangeCode("NYBOT", "New York Board Of Trade "));
      htCodes.Add("28", new ExchangeCode("ONECH", "One Chicago"));
      htCodes.Add("29", new ExchangeCode("NQLX", "NQLX "));
      htCodes.Add("4", new ExchangeCode("WPG", "Winnipeg Commodities Exchange "));
      htCodes.Add("6", new ExchangeCode("LIFFE", "London International Financial Futures Exchange"));
      htCodes.Add("17", new ExchangeCode("LME", "London Metals Exchange "));
      htCodes.Add("8", new ExchangeCode("IPE", "International Petroleum Exchange "));
      htCodes.Add("7", new ExchangeCode("SGX", "Singapore International Monetary Exchange "));
      htCodes.Add("15", new ExchangeCode("EUREX", "European Exchange "));
      htCodes.Add("2", new ExchangeCode("EID", "EURONEXT Index Derivatives "));
      htCodes.Add("9", new ExchangeCode("EIR", "EURONEXT Interest Rates "));
      htCodes.Add("16", new ExchangeCode("EURONEXT", "EURONEXT Commodities "));
      htCodes.Add("48", new ExchangeCode("Tullet", "Tullett Liberty (Forex) "));
      htCodes.Add("49", new ExchangeCode("Barclays", "Barclays Bank (Forex)"));
      htCodes.Add("4A", new ExchangeCode("HotSpot", "Hotspot Forex "));
      htCodes.Add("4B", new ExchangeCode("WH", "Warenterminborse Hannover (not currently supported) "));
    }

    public string Code( string id ) {
      string code = id;
      if (htCodes.ContainsKey(id)) {
        ExchangeCode ec = htCodes[id] as ExchangeCode;
        code = ec.code;
      }
      return code;
    }
  }

  class ExchangeCode {

    public string id;
    internal string code;
    internal string desc;

    public ExchangeCode( string id, string code, string desc ) {
      this.id = id;
      this.code = code;
      this.desc = desc;
    }

    public ExchangeCode( string code, string desc ) {
      //this.id = id;
      this.code = code;
      this.desc = desc;
    }
  }
}

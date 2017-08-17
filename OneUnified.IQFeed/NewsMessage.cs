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

namespace OneUnified.IQFeed {

  public class NewsMessageEventArgs : EventArgs {
    public NewsMessage Message;

    public NewsMessageEventArgs( NewsMessage Message ) {
      this.Message = Message;
    }
  }

  public delegate void NewsMessageHandler( object sender, NewsMessageEventArgs args );

  public class NewsMessage: IQFeedMessage {

    private static string sNewsDelim = ":";
    private static char[] chNewsDelim = sNewsDelim.ToCharArray();

    public string DistributorCode;
    public string StoryID;
    public string SymbolList;
    public string[] Symbols;
    public DateTime TimeStamp;
    public string HeadLine;

    public NewsMessage( string[] values, string HeadLine )
      : base("N",values) {

      if ("N" != items[0]) {
        throw new Exception("NewsMessage constructor did not receive 'N' message.");
      }

      try {
        DistributorCode = items[1];
        StoryID = items[2];
        SymbolList = items[3];
        this.HeadLine = HeadLine;
        string t = items[4];
        TimeStamp = new DateTime(
          Convert.ToInt32(t.Substring(0, 4)),
          Convert.ToInt32(t.Substring(4, 2)),
          Convert.ToInt32(t.Substring(6, 2)),
          Convert.ToInt32(t.Substring(9, 2)),
          Convert.ToInt32(t.Substring(11, 2)),
          Convert.ToInt32(t.Substring(13, 2))
          );
        Symbols = items[3].Split(chNewsDelim);

      }
      catch {
        Console.WriteLine("NewsMessage Conversion problem");
        throw new Exception("NewsMessage Conversion Error");
      }





    }
  }
}

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

  public class LevelIIUpdateMessageEventArgs : EventArgs {
    public LevelIIUpdateMessage Message;

    public LevelIIUpdateMessageEventArgs( LevelIIUpdateMessage Message ) {
      this.Message = Message;
    }
  }

  public delegate void LevelIIUpdateMessageHandler( object sender, LevelIIUpdateMessageEventArgs args );

  public class LevelIIUpdateMessage : IQFeedMessage {

    public LevelIIUpdateMessage( string[] values )
      : base("U", values) {
    }

    // on-demand conversion of fields
    public string Symbol { get { return ConvertToString(2); } }
    public string MMID { get { return ConvertToString(3); } }
    public double BidPrice { get { return ConvertToDouble(4); } }
    public double AskPrice { get { return ConvertToDouble(5); } }
    public int BidSize { get { return ConvertToInt32(6); } }
    public int AskSize { get { return ConvertToInt32(7); } }
    public DateTime TimeStamp {
      get {
        string dt = ConvertToString(9) + " " + ConvertToString(8);
        return new DateTime( 
          Convert.ToInt32(dt.Substring(0,4)),
          Convert.ToInt32(dt.Substring(5,2)),
          Convert.ToInt32(dt.Substring(8,2)),
          Convert.ToInt32(dt.Substring(11,2)),
          Convert.ToInt32(dt.Substring(14,2)),
          Convert.ToInt32(dt.Substring(17,2)));
      }
    }
    public string ReasonCode { get { return ConvertToString(10); } }
    public string ConditionCode { get { return ConvertToString(11); } }
    public string SourceID { get { return ConvertToString(12); } }

  }
}

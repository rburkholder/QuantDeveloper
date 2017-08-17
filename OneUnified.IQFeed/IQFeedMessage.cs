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

  public class IQFeedMessage {

    public string[] items;

    public IQFeedMessage( string MessageType, string[] items ) {
      this.items = items;
      if (MessageType != items[0])
        throw new Exception("IQFeedMessage:  MessageType " + MessageType + " does not match Field " + items[0]);
    }

    protected string ConvertToString( int ord ) {
      return items[ord - 1];

    }

    protected double ConvertToDouble( int ord ) {
      int ix = ord - 1;
      double d = 0;
      if (!string.IsNullOrEmpty(items[ix])) d = Convert.ToDouble(items[ix]);
      return d;
    }

    protected Int32 ConvertToInt32( int ord ) {
      int ix = ord - 1;
      Int32 i = 0;
      if (!string.IsNullOrEmpty(items[ix])) i = Convert.ToInt32(items[ix]);
      return i;
    }

    protected DateTime ConvertToDate( int ord ) {
      return ConvertToDate(items[ord - 1]);
    }

    protected DateTime ConvertToDate( string date ) {
      if (string.IsNullOrEmpty(date)) return new DateTime(0);
      else {
        switch (date.Length) {
          case 5:
            return new DateTime(DateTime.Now.Year,
              Convert.ToInt32(date.Substring(0, 2)), Convert.ToInt32(date.Substring(3, 2)));
            break;
          case 8:
            return new DateTime(2000 + Convert.ToInt32(date.Substring(6, 2)),
              Convert.ToInt32(date.Substring(0, 2)), Convert.ToInt32(date.Substring(3, 2)));
            break;
          case 10:
            return new DateTime(Convert.ToInt32(date.Substring(6, 4)),
              Convert.ToInt32(date.Substring(0, 2)), Convert.ToInt32(date.Substring(3, 2)));
            break;
          default:
            break;
        }
      }
      return new DateTime(0);
    }
  }

}

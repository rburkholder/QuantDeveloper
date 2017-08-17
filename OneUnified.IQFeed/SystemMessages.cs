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

  public class SystemMessageEventArgs : EventArgs {
    public SystemMessage Message;

    public SystemMessageEventArgs( SystemMessage Message ) {
      this.Message = Message;
    }
  }

  public delegate void SystemMessageHandler( object sender, SystemMessageEventArgs args );

  public class SystemMessage: IQFeedMessage {

    public enum EType {
      disconnected, connected, key, keyok, reconnectfailed, 
      symbollimitreached, ip, cust, stats, fundamentalfieldnames, updatefieldnames, 
      watches };
    public EType Type;

    public SystemMessage( string[] values ): base( "S", values ) {

      switch ( items[1] ) {
        case "SERVER DISCONNECTED":
          Type = EType.disconnected;
          break;
        case "SERVER CONNECTED":
          Type = EType.connected;
          break;
        case "KEY":
          Type = EType.key;
          break;
        case "KEYOK":
          Type = EType.keyok;
          break;
        case "SERVER RECONNECT FAILED":
          Type = EType.reconnectfailed;
          break;
        case "SYMBOL LIMIT REACHED":
          Type = EType.symbollimitreached;
          break;
        case "IP":
          Type = EType.ip;
          break;
        case "CUST":
          Type = EType.cust;
          break;
        case "STATS":
          Type = EType.stats;
          break;
        case "FUNDAMENTAL FIELDNAMES":
          Type = EType.fundamentalfieldnames;
          break;
        case "UPDATE FIELDNAMES":
          Type = EType.updatefieldnames;
          break;
        case "WATCHES":
          Type = EType.watches;
          break;
        default:
          throw new Exception("SystemMessage has new message type: " + items[1]);
          break;
      }
    }
  }
}

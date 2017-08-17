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

  public delegate void WatchSymbolNotFoundHandler( object sender, WatchSymbolNotFoundMessageEventArgs args );

  public class WatchSymbolNotFoundMessageEventArgs : EventArgs {
    public string Symbol;

    public WatchSymbolNotFoundMessageEventArgs( string Symbol ) {
      this.Symbol = Symbol;
    }
  }

  public delegate void UpdateMessageHandler( object sender, UpdateMessageEventArgs args );

  public class UpdateMessageEventArgs : EventArgs {
    public UpdateMessage Message;

    public UpdateMessageEventArgs( UpdateMessage Message ) {
      this.Message = Message;
    }
  }

  public class UpdateMessage : LevelIDataMessage {

    public UpdateMessage( string[] values )
      : base("Q", values) {

    }
  }

}

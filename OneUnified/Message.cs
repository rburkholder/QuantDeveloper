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

namespace OneUnified {

  public delegate void MessageEventHandler( object o, MessageArgs args );

  public class MessageArgs : EventArgs {
    public string Message;

    public MessageArgs( string Message ) {
      this.Message = Message;
    }
  }
}

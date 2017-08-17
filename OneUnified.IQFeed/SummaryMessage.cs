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

  public class SummaryMessageEventArgs : EventArgs {
    public SummaryMessage Message;

    public SummaryMessageEventArgs( SummaryMessage Message ) {
      this.Message = Message;
    }
  }

  public delegate void SummaryMessageHandler( object sender, SummaryMessageEventArgs args );
  
  public class SummaryMessage: LevelIDataMessage  {

    public SummaryMessage( string[] values )
      : base("P", values) {
    }
  }
}

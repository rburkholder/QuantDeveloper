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

namespace OneUnified.SmartQuant {
  public class Convert {

    public static int DoublePriceToIntStock( double Price ) {
      // used for keys in dictRow;
      int t = ( int )Math.Round( ( Price * 100.0 ), 0 );
      //Console.WriteLine( "DPTI {0:0.0000} -> {1}", Price, t);
      return t;
    }

    public static int DoublePriceToIntFutures( double Price ) {
      // used for keys in dictRow;
      int t = ( int )Math.Round( ( Price * 10000.0 ), 0 );
      //Console.WriteLine( "DPTI {0:0.0000} -> {1}", Price, t);
      return t;
    }

  }
}

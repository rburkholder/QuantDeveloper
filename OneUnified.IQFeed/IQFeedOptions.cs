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

  using OneUnified.Sockets;

  public class OptionArgs : EventArgs {
    public string[] Calls;
    public string[] Puts;

    public OptionArgs( string[] Calls, string[] Puts ) {
      this.Calls = Calls;
      this.Puts  = Puts;
    }
  }

  public class IQFeedOptions {

    public delegate void ChainEventHandler( object o, OptionArgs args );

    private ChainEventHandler NewChain;

    private static string sDelim = ",";
    private static char[] chDelim = sDelim.ToCharArray();

    private string[] rCalls;
    private string[] rPuts;

    private IQFeed9100 port;

    public IQFeedOptions() {
    }

    public void GetChain( string Symbol, ChainEventHandler handler ) {
      NewChain += handler;
      port = new IQFeed9100();
      port.BeginCmd("OEA," + Symbol + ",0,0;", new SocketLineHandler(OnChain));
    }

    private void OnChain( object o, BufferArgs args ) {

      //Console.WriteLine(args.Line);
      int colon = args.Line.IndexOf(":");
      string sCalls = args.Line.Substring(0, colon);
      string sPuts = args.Line.Substring(colon + 1);

      port.IgnoreRemainingLines();

      if (sCalls.EndsWith(" ")) sCalls = sCalls.Substring(0, sCalls.Length - 1);
      if (sCalls.EndsWith(",")) sCalls = sCalls.Substring(0, sCalls.Length - 1);
      if ("," == sCalls.Substring(0, 1)) sCalls = sCalls.Substring(1);
      if (sPuts.EndsWith(" ")) sPuts = sPuts.Substring(0, sPuts.Length - 1);
      if (sPuts.EndsWith(",")) sPuts = sPuts.Substring(0, sPuts.Length - 1);
      if ("," == sPuts.Substring(0, 1)) sPuts = sPuts.Substring(1);

      rCalls = sCalls.Split(chDelim);
      rPuts = sPuts.Split(chDelim);

      if (null != NewChain) {
        NewChain(this, new OptionArgs(rCalls, rPuts));
      }

      NewChain = null;
      port = null;

    }
  }
}

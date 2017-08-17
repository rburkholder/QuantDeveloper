//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;

using OneUnified.Sockets;

using SmartQuant;
using SmartQuant.Data;

namespace OneUnified.IQFeed {

  public delegate void IQFeedMarketDepthHandler( string Symbol, MarketDepth md );

  public class IQFeedLevelII {

    System.Diagnostics.TraceLevel trace = System.Diagnostics.TraceLevel.Off;

    //public event LevelIIUpdateMessageHandler HandleLevelIIUpdateMessage;
    //public event IQFeedMarketDepthHandler IQFeedMarketDepth;

    private BufferedSocket bs;
    private Hashtable htWatchEvents;  // saved by symbol

    private static string sDelim = ",";
    private static char[] chDelim = sDelim.ToCharArray();

    public IQFeedLevelII() {

      bs = new BufferedSocket("127.0.0.1", Constants.Level2Port, new SocketLineHandler(OnResult));
      htWatchEvents = new Hashtable(Constants.MaxSymbols); 

    }

    public void Open() {

      bs.Open();
    }

    public void Close() {

      bs.Close();
    }

    public System.Diagnostics.TraceLevel Trace {
      get { return trace; }
      set { trace = value; }
    }

    public void StartWatch( string Symbol, LevelIIUpdateMessageHandler handler ) {
      LevelIIUpdateMessageHandler mh = null;
      if (htWatchEvents.ContainsKey(Symbol)) {
        mh = htWatchEvents[Symbol] as LevelIIUpdateMessageHandler;
        mh += handler;
      }
      else {
        mh += handler;
        htWatchEvents.Add(Symbol, mh);
        bs.Send('w' + Symbol + "\n");
      }
    }

    public void StopWatch( string Symbol, LevelIIUpdateMessageHandler handler ) {
      LevelIIUpdateMessageHandler mh = null;
      if (htWatchEvents.ContainsKey(Symbol)) {
        mh = htWatchEvents[Symbol] as LevelIIUpdateMessageHandler;
        mh -= handler;
        if (null == mh) {
          Console.WriteLine("Stopping watch on {0}", Symbol);
          bs.Send('r' + Symbol + "\n");
          htWatchEvents.Remove(Symbol);
        }
      }
    }

    private void OnResult( object o, BufferArgs args ) {

      //Console.WriteLine(args.Line);
      args.items = args.Line.Split(chDelim);

      switch (args.items[0]) {
        case "U": // data
          UMessage(args);
          break;
        case "T": // time stamp
          TMessage(args);
          break;
        case "M": // mmid,name
          break;
        case "E": // error text
          Console.WriteLine("{0} 9200 Error: {1}", Clock.Now, args.items[1]);
          break;
        case "O=":
        case "O": // market is open
          Console.WriteLine("{0} 9200 Market is open", Clock.Now);
          break;
        case "C=":
        case "C": // market is closed
          Console.WriteLine("{0} 9200 Market is closed", Clock.Now);
          break;
        case "n": // symbol not found 
          Console.WriteLine("9200 Symbol not found: {0}", args.items[1]);
          break;
        default:
          Console.WriteLine("{0} 9200 unknown: {1}", Clock.Now, args.Line);
          break;
      }
    }

    private void TMessage( BufferArgs args ) {
    }

    private void UMessage( BufferArgs args ) {

      LevelIIUpdateMessage Message;

      try {
        Message = new LevelIIUpdateMessage(args.items);
        if (htWatchEvents.ContainsKey(Message.Symbol)) {
          LevelIIUpdateMessageHandler mh = htWatchEvents[Message.Symbol] as LevelIIUpdateMessageHandler;
          if (null != mh) mh(this, new LevelIIUpdateMessageEventArgs(Message));
        }
      }
      catch ( Exception e ) {
        Console.WriteLine("IQFeedLevelII.UMessage error: " + args.Line);
        Console.WriteLine("  ** Exception {0}", e.ToString());
      }

    }

    private void Ignore( object o, BufferArgs args ) {
    }

  }

}

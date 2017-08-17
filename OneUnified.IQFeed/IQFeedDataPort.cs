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
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OneUnified.IQFeed {

  using OneUnified.Sockets;

  internal class IQFeed9100: IQFeedPort {
    public IQFeed9100( )
      : base(9100) {
    }
  }

  internal class IQFeed9200 : IQFeedPort {

    public IQFeed9200()
      : base(9200) {
    }
  }

  internal class IQFeedPort {

    // see IQFeedOptions.cs::GetChain as example of usage

    private static Queue qPortCmds;  // holds a queue of BufferedSockets for use by misc commands, created on an as needed basis

    private BufferedSocket bs;
    private event SocketLineHandler lh;
    private bool bInUse = false;
    private string sCmd;
    private IQFeedPort iqfport;  // keep a reference to ourself for killing purposes
    protected int portnumber;

    //static IQFeedPort() {
    //  qPortCmds = new Queue(100, 10);
    //}

    public IQFeedPort( int portnumber ) {
      this.portnumber = portnumber;
      qPortCmds = new Queue(100, 10);
    }

    public void BeginCmd( string sCmd, SocketLineHandler lh ) {

      if (bInUse) {
        new Exception("IQFeedPort " + portnumber.ToString() + " is in use with '" + sCmd + "'");
      }
      else {
        this.lh = lh;
        bInUse = true;
        iqfport = this;
        this.sCmd = sCmd;
        //Monitor.Enter(qPortCmds);
        lock (qPortCmds.SyncRoot) {
          if (0 == qPortCmds.Count) {
            //Console.WriteLine("BeginCmd creating new socket");
            bs = new BufferedSocket("127.0.0.1", portnumber, lh);
            bs.Open();
          }
          else {
            //Console.WriteLine("BeginCmd regurgitating socket");
            bs = qPortCmds.Dequeue() as BufferedSocket;
            bs.Add(lh);
          }
        }
        //Monitor.Exit(qPortCmds);
        bs.Send(sCmd);
      }

    }

    public void IgnoreRemainingLines() {
      bs.Remove(lh);
      bs.Add(new SocketLineHandler(IgnoreRemainingLines));
    }

    private void IgnoreRemainingLines( object o, BufferArgs e ) {
      if ("!ENDMSG!" == e.Line) {
        //Console.WriteLine("EndCmd");
        EndCmd();
        //bs = null;
      }
      else {
        if (0 == e.Line.Length) {
          // ignore it
        }
        else {
          if (Regex.IsMatch(e.Line, "!ERROR!", RegexOptions.None)) {
            // skip a blank line and prepare for finish
            Console.WriteLine("errline: {0}", e.Line);
          }
        }
      }
    }

    public void EndCmd() {
      // called externally only if !ENDMSG! is handled externally
      bs.Remove(new SocketLineHandler(IgnoreRemainingLines));
      sCmd = null;
      bInUse = false;
      lock (qPortCmds.SyncRoot) {
        //Monitor.Enter(qPortCmds);
        //Console.WriteLine("enqueing old 9100cmd");
        qPortCmds.Enqueue(bs);
      }
      //Monitor.Exit(qPortCmds);
      iqfport = null;  //let our selves be released
    }

  }
}

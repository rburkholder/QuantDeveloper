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

    private static Queue qPortCmds;  // holds a queue of BufferedSockets for use by misc commands, created on an as needed basis

    private BufferedSocket bs;
    private event Buffer.LineHandler lh;
    private bool bInUse = false;
    private string sCmd;
    private IQFeedPort port;  // keep a reference to ourself for killing purposes
    protected int portnumber;

    static IQFeedPort() {
      qPortCmds = new Queue(100, 10);
    }

    public IQFeedPort( int portnumber ) {
      this.portnumber = portnumber;
    }

    public void BeginCmd( string sCmd, Buffer.LineHandler lh ) {

      if (bInUse) {
        new Exception("IQFeedPort " + portnumber.ToString() + " is in use with '" + sCmd + "'");
      }
      else {
        this.lh = lh;
        bInUse = true;
        port = this;
        this.sCmd = sCmd;
        Monitor.Enter(qPortCmds);
        if (0 == qPortCmds.Count) {
          //Console.WriteLine("BeginCmd creating new socket");
          bs = new BufferedSocket("127.0.0.1", portnumber, this);
          bs.Add(lh);
        }
        else {
          //Console.WriteLine("BeginCmd regurgitating socket");
          bs = qPortCmds.Dequeue() as BufferedSocket;
          bs.Add(lh);
        }
        Monitor.Exit(qPortCmds);
        bs.Send(sCmd);
      }

    }

    public void IgnoreRemainingLines() {
      bs.Remove(lh);
      bs.Add(new Buffer.LineHandler(IgnoreRemainingLines));
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
      bs.Remove(new Buffer.LineHandler(IgnoreRemainingLines));
      sCmd = null;
      bInUse = false;
      Monitor.Enter(qPortCmds);
      //Console.WriteLine("enqueing old 9100cmd");
      qPortCmds.Enqueue(bs);
      Monitor.Exit(qPortCmds);
      port = null;  //let our selves be released
    }



  }
}

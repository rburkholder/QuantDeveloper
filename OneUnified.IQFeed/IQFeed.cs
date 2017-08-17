//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
// First File  : 2006/01/10
//============================================================================

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace OneUnified.IQFeed {

  using OneUnified;
  using OneUnified.Sockets;

  public class IQFeed {

    private static string sProductName = "ONE_UNIFIED";
    private static string sProductKey = "0.11111111";
    private static string sProductVersion = "1.0";

    private string sIQ32DLLPath;

    private delegate void iqfLoggedInEventHandler( int x, int y );
    // look in the registry at some point in time for this string
    [DllImport("IQ32.dll")]
    private static extern void SetCallbackFunction( iqfLoggedInEventHandler e );
    [DllImport("IQ32.dll")]
    private static extern int RegisterClientApp( int hClient, string sProductName, string sProductKey, string sProductVersion );
    [DllImport("IQ32.dll")]
    private static extern void RemoveClientApp( int hClient );

    private static string sDelim = ",";
    private static char[] chDelim = sDelim.ToCharArray();

    private BufferedSocket bs5009;

    private bool bLoadedDLL = false;

    private int hIqFeed = 10;  // a number for lack of anything else better to put here

    private static IQFeed IQFeedCBObjRef;  // used by return from login.  Will, unfortunately, be reused by other instances.
    
    public event EventHandler LoggedInEventHandler;
    public event EventHandler ConnectingEventHandler;
    public event EventHandler ConnectedEventHandler;
    public event EventHandler DisconnectedEventHandler;

    public event MessageEventHandler Messages;

    public event WatchSymbolNotFoundHandler HandleWatchSymbolNotFound;
    public event UpdateMessageHandler HandleUpdateMessage;
    public event SummaryMessageHandler HandleSummaryMessage;
    public event FundamentalMessageHandler HandleFundamentalMessage;
    public event NewsMessageHandler HandleNewsMessage;
    public event SystemMessageHandler HandleSystemMessage;

    private bool connected = false;

    private Hashtable htWatchSymbols;

    private TimeSpan tsIQFeedDif = new TimeSpan(0);

    public IQFeed() {

      Random rand = new Random();
      hIqFeed = rand.Next(1, 99);

      IQFeedCBObjRef = this;  // used for static return by OnIQFeedLoggedIn

      htWatchSymbols = new Hashtable(2 * 1300);  // 2 * max symbols from iqfeed

      HandleSystemMessage += new SystemMessageHandler(ProcessSystemMessage);
      HandleUpdateMessage += new UpdateMessageHandler(SymbolSpecificUpdateMessage);
      HandleSummaryMessage += new SummaryMessageHandler(SymbolSpecificSummaryMessage);
      HandleFundamentalMessage += new FundamentalMessageHandler(SymbolSpecificFundamentalMessage);
      HandleNewsMessage += new NewsMessageHandler(SymbolSpecificNewsMessage);

      RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\DTN\\IQFeed");
      sIQ32DLLPath = (string)rk.GetValue("EXEDIR", "");
      rk.Close();
      string newPath = Environment.GetEnvironmentVariable("Path");
      newPath += ";" + sIQ32DLLPath;
      Environment.SetEnvironmentVariable("Path", newPath);
    }

    #region Connection Initiation

    public bool Connected {
      get { return connected; }
    }

    public void Connect() {

      SetCallbackFunction(new iqfLoggedInEventHandler(OnIQFeedLoggedIn));
      RegisterClientApp(hIqFeed, sProductName, sProductKey, sProductVersion);
    }

    private static void OnIQFeedLoggedIn( int x, int y ) {
      // this is called once iqfeed has initialized itself
      //Console.WriteLine("iqfeed login callback {0} {1}", x, y);
      switch (x) {
        case 0:
          switch (y) {
            case 0:
              if ( !IQFeedCBObjRef.bLoadedDLL ) IQFeedCBObjRef.ConnectionOk();
              IQFeedCBObjRef.bLoadedDLL = true;
              break;
            case 1:
              IQFeedCBObjRef.LoginFailed();
              break;
          }
          break;
        case 1:
          switch (y) {
            case 0:
              IQFeedCBObjRef.OffLineNotification();
              break;
            case 1:
              IQFeedCBObjRef.IQFeedIsTerminating();
              break;
          }
          break;
      }
    }

    private void ConnectionOk() {

      // what happens on disconnect and reconnect?
      if (null != LoggedInEventHandler) LoggedInEventHandler(this, EventArgs.Empty);
      ConnectToPort5009();
    }

    private void LoginFailed() {
      Console.WriteLine("IQFeed Login Failed");
    }

    private void OffLineNotification() {
      Console.WriteLine("IQFeed Offline Notification");
    }

    private void IQFeedIsTerminating() {
      Console.WriteLine("IQFeed Is Terminating");
    }

    public void Disconnect() {
      bs5009.Close();
      RemoveClientApp(hIqFeed);
      if (null != DisconnectedEventHandler) DisconnectedEventHandler(this, EventArgs.Empty);
    }

    private void ConnectToPort5009() {

      if (null != ConnectingEventHandler) ConnectingEventHandler(this, EventArgs.Empty);

      //smS = stateS.getkey;  // may need to use init if we do what we did over in perl

      bs5009 = new BufferedSocket("localhost", 5009, new SocketLineHandler(ProcessPort5009));
      bs5009.Open( );
    }

    private void ProcessPort5009( object o, BufferArgs args ) {

      //Console.WriteLine( "*" + args.Line + "*" );

      args.items = args.Line.Split(chDelim);

      switch (args.items[0]) {
        case "Q":
          if ("Not Found" == args.items[3]) {
            Console.WriteLine("Symbol not found, number of items: {0} {1}", args.items[1], args.items.Length);
            if (null != HandleWatchSymbolNotFound) HandleWatchSymbolNotFound(this, new WatchSymbolNotFoundMessageEventArgs(args.items[1]));
          }
          else {
            UpdateMessage update = new UpdateMessage(args.items);
            if (null != HandleUpdateMessage) HandleUpdateMessage(this, new UpdateMessageEventArgs(update));
            //if (null != HandleQ) HandleQ(this, args);
          }
          break;
        case "P":
          SummaryMessage summary = new SummaryMessage(args.items);
          if (null != HandleSummaryMessage) HandleSummaryMessage(this, new SummaryMessageEventArgs(summary));
          //if (null != HandleP) HandleP(this, args);
          break;
        case "F":
          FundamentalMessage fundamental = new FundamentalMessage(args.items);
          if (null != HandleFundamentalMessage) HandleFundamentalMessage(this, new FundamentalMessageEventArgs(fundamental));
          //if (null != HandleF) HandleF(this, args);
          break;
        case "N":

          int pos;  // extract headline just in case it has commas in it
          pos = args.Line.IndexOf(",", 2); // just prior story id
          pos = args.Line.IndexOf(",", pos + 1);  // just prior symbol list
          pos = args.Line.IndexOf(",", pos + 1);  // just prior datetime
          pos = args.Line.IndexOf(",", pos + 1);  // just prior headline
          args.headline = args.Line.Substring(pos + 1);  // get rest of line

          NewsMessage news = new NewsMessage(args.items, args.headline);
          if (null != HandleNewsMessage) HandleNewsMessage(this, new NewsMessageEventArgs(news));
          //if (null != HandleN) HandleN(this, args);
          break;
        case "T":
          CheckTime(args);
          //if (null != HandleT) HandleT(this, args);
          break;
        case "S":
          Console.WriteLine( "*" + args.Line + "*" );
          SystemMessage system = new SystemMessage(args.items);
          if (null != HandleSystemMessage) HandleSystemMessage(this, new SystemMessageEventArgs(system));
          //HandleS(e);
          break;
      }
    }

    private void CheckTime( BufferArgs args ) {
      DateTime now = DateTime.Now;
      string dt = args.items[1];
      DateTime dtIq = new DateTime(
        Convert.ToInt32(dt.Substring(0, 4)),
        Convert.ToInt32(dt.Substring(4, 2)),
        Convert.ToInt32(dt.Substring(6, 2)),
        Convert.ToInt32(dt.Substring(9, 2)),
        Convert.ToInt32(dt.Substring(12, 2)),
        0);
      TimeSpan ts = now - dtIq;
      if (TimeSpan.Zero == tsIQFeedDif) {
        tsIQFeedDif = ts;
        Console.WriteLine("Computer {0} IQFeed {1} Difference {2}", now, dtIq, ts);
      }
      else {
        if (ts > (tsIQFeedDif + TimeSpan.FromSeconds(5)) 
          || ts < (tsIQFeedDif - TimeSpan.FromSeconds(5)) 
          ) {
          //Console.WriteLine("Computer {0} IQFeed {1} Difference {2}", now, dtIq, ts);
        }
      }
    }

    private void ProcessSystemMessage( object sender, SystemMessageEventArgs args ) {

      switch (args.Message.Type) {
        case SystemMessage.EType.connected:
          connected = true;
          EmitMessage("IQFeed is Connected");
          break;
        case SystemMessage.EType.cust:
          break;
        case SystemMessage.EType.disconnected:
          // Does Auto Reconnect most of the time, so stay with it
          //Disconnect();
          connected = false;
          EmitMessage("IQFeed is Temporarily Disconnected");
          break;
        case SystemMessage.EType.fundamentalfieldnames:
          break;
        case SystemMessage.EType.ip:
          break;
        case SystemMessage.EType.key:
          bs5009.Send("S,KEY," + args.Message.items[2] + "\n");
          bs5009.Send("S,NEWSON\n");
          if (null != ConnectedEventHandler) ConnectedEventHandler(this, EventArgs.Empty);
          break;
        case SystemMessage.EType.keyok:
          break;
        case SystemMessage.EType.reconnectfailed:
          break;
        case SystemMessage.EType.stats:
          break;
        case SystemMessage.EType.symbollimitreached:
          break;
        case SystemMessage.EType.updatefieldnames:
          break;
        case SystemMessage.EType.watches:
          break;
      }
    }

    #endregion Connection Initiation

    #region IQFeed Misc

    private void EmitMessage( string Message ) {
      if (null != Messages) {
        Messages(this, new MessageArgs(Message));
      }
    }

    public SymbolEvent startWatch( string Symbol ) {

      // this is a bit tenuous in that the caller has to get attached to the event before
      //   asynchronous data is returned -- how often will/does this happen?  
      //   particularily if we are watching for a 'Not Found' message.

      SymbolEvent se = null;
      if (!htWatchSymbols.ContainsKey(Symbol)) {
        se = new SymbolEvent();
        htWatchSymbols.Add(Symbol, se);
      }
      else {
        se = htWatchSymbols[Symbol] as SymbolEvent;
      }

      se.Count++;

      if (1 == se.Count) {
//        Console.WriteLine("StartWatch {0}", sSymbol);
        bs5009.Send("w" + Symbol + "\n");
      }
      return se;
    }

    public SymbolEvent stopWatch( string Symbol ) {

      SymbolEvent se = null;

      if (htWatchSymbols.ContainsKey(Symbol)) {

        se = htWatchSymbols[Symbol] as SymbolEvent;
        if ( 0 < se.Count ) se.Count--;
        if (0 == se.Count) {
//          Console.WriteLine("StopWatch {0}", sSymbol);
          bs5009.Send("r" + Symbol + "\n");
        }
      }
      return se;
    }

    public void requestSummary( string Symbol ) {
      // assume caller has already requested watch which is necessary for a summary message
      bs5009.Send("f" + Symbol + "\n");
    }


    private void SymbolSpecificUpdateMessage( object o, UpdateMessageEventArgs args ) {

      string Symbol = args.Message.Symbol;
      if (htWatchSymbols.ContainsKey(Symbol)) {
        SymbolEvent se = htWatchSymbols[Symbol] as SymbolEvent;
        se.EmitUpdateMessage(args);
      }
    }

    private void SymbolSpecificSummaryMessage( object o, SummaryMessageEventArgs args ) {

      string Symbol = args.Message.Symbol;
      if (htWatchSymbols.ContainsKey(Symbol)) {
        SymbolEvent se = htWatchSymbols[Symbol] as SymbolEvent;
        se.EmitSummaryMessage(args);
      }
    }

    private void SymbolSpecificFundamentalMessage( object o, FundamentalMessageEventArgs args ) {

      string Symbol = args.Message.Symbol;
      if (htWatchSymbols.ContainsKey(Symbol)) {
        SymbolEvent se = htWatchSymbols[Symbol] as SymbolEvent;
        se.EmitFundamentalMessage(args);
      }
    }

    private void SymbolSpecificNewsMessage( object o, NewsMessageEventArgs args ) {

      foreach (string Symbol in args.Message.Symbols) {
        if (htWatchSymbols.ContainsKey(Symbol)) {
          SymbolEvent se = htWatchSymbols[Symbol] as SymbolEvent;
          se.EmitNewsMessage(args);
        }
      }
    }

    #endregion

  }

  public class SymbolEvent {

    // one of these objects per symbol stored in hashtable keyed by symbol

    public int Count=0;  // number of events attached

    public event UpdateMessageHandler HandleUpdateMessage;
    public event SummaryMessageHandler HandleSummaryMessage;
    public event FundamentalMessageHandler HandleFundamentalMessage;
    public event NewsMessageHandler HandleNewsMessage;

    public SymbolEvent() {
    }

    public void EmitUpdateMessage( UpdateMessageEventArgs args ) {
      if (null != HandleUpdateMessage) HandleUpdateMessage(this, args);
    }

    public void EmitSummaryMessage( SummaryMessageEventArgs args ) {
      if (null != HandleSummaryMessage) HandleSummaryMessage(this, args);
    }

    public void EmitFundamentalMessage( FundamentalMessageEventArgs args ) {
      if (null != HandleFundamentalMessage) HandleFundamentalMessage(this, args);
    }

    public void EmitNewsMessage( NewsMessageEventArgs args ) {
      if (null != HandleNewsMessage) HandleNewsMessage(this, args);
    }
  }

}


/*
    protected void wait() {
      Monitor.Enter(this);
      bWaitForPulse = true;
      Monitor.Wait(this);
      bWaitForPulse = false;
      Monitor.Exit(this);
    }

    protected void pulse() {
      Monitor.Enter(this);
      if ( bWaitForPulse ) Monitor.Pulse(this);
      Monitor.Exit(this);
    }
 * 
 * 
 * 		public void wait() {
      Monitor.Enter( this );
      Monitor.Wait( this );
      Monitor.Exit( this );
    }

    public void pulse() {
      Monitor.Enter( this );
      Monitor.Pulse( this );
      Monitor.Exit( this );
    }

    public void stop() {

      bs5009.Close();
      RemoveClientApp( hIqFeed );

      Monitor.Enter( this );
      Monitor.Pulse( this ); // activate main thread and shutdown
      Monitor.Exit( this );
    }


    */

/************************************************************************
* Copyright(c) 1997-2005, SmartQuant Ltd. All rights reserved.         *
*                                                                      *
* This file is provided as is with no warranty of any kind, including  *
* the warranty of design, merchantibility and fitness for a particular *
* purpose.                                                             *
*                                                                      *
* This software may not be used nor distributed without proper license *
* agreement.                                                           *
************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

using SmartQuant;
using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Execution;
using SmartQuant.Instruments;
using SmartQuant.Providers;

using GTAPINet;

namespace SmartQuant.GS {

  # region SessionState
  internal class SessionState {

    private GTSession session;
    private IProvider Provider;
    private bool connected;
    private ProviderStatus status;
    private string name;
    private string password;

    private bool isOpened;

    public event EventHandler Connected;
    public event EventHandler Disconnected;
    public event EventHandler StatusChanged;

    public Dictionary<string, GTStock> stocks;

    public Dictionary<int, OrderRecord> ordersUserData;
    public Dictionary<int, OrderRecord> ordersTraderSeqNo;
    public Dictionary<string, int> ticketsNo;

    public SessionState( IProvider Provider, string AccountName, string Password ) {
      this.Provider = Provider;
      session = null;
      connected = false;
      status = ProviderStatus.Unknown;
      name = AccountName;
      password = Password;
      isOpened = false;

    }

    public GTSession Session {
      get { return session; }
    }

    public override string ToString() {
      return "ss:" + name;
      //return base.ToString();
    }

    public void Connect(
      string executorIP, ushort executorPort,
      string level1IP, ushort level1Port,
      string level2IP, ushort level2Port
      ) {

      if (isOpened) {
      }
      else {
        isOpened = true;
        session = new GTSession();
        session.CreateSessionWindow(IntPtr.Zero, 0);
        session.HookExecConnected(new GTSession.PFNOnExecConnected(OnExecConnected));
        session.HookExecDisconnected(new GTSession.PFNOnExecDisconnected(OnExecDisconnected));
        session.HookExecMsgChat(new GTSession.PFNOnExecMsgChat(OnExecMsgChat));
        session.HookExecMsgErrMsg(new GTSession.PFNOnExecMsgErrMsg(OnSessionExecMsgErrMsg));
        session.HookExecMsgLoggedin(new GTSession.PFNOnExecMsgLoggedin(OnExecMsgLoggedin));
        session.HookExecMsgOpenPosition(new GTSession.PFNOnExecMsgOpenPosition(OnExecMsgOpenPosition));
        session.HookExecMsgState(new GTSession.PFNOnExecMsgState(OnExecMsgState));

        GTSession.gtSetExecAddress(session, executorIP, executorPort);
        GTSession.gtSetQuoteAddress(session, level1IP, level1Port);
        GTSession.gtSetLevel2Address(session, level2IP, level2Port);

        status = ProviderStatus.LoggingIn;
        int i = GTSession.gtLogin(session, name.ToUpper(), password);
        Console.WriteLine("session 1 = {0}", i);

        stocks = new Dictionary<string, GTStock>();
        
        ordersUserData = new Dictionary<int, OrderRecord>();
        ordersTraderSeqNo = new Dictionary<int, OrderRecord>();
        ticketsNo = new Dictionary<string, int>();


      }
    }

    public bool DoTradeSeqRemove {
      get { return false; }  // there may be a problem with sync on remove, so disable for now
    }

    public void Disconnect() {
      if (isConnected) {
        GTSession.gtLogout(session);
        connected = false;
        status = ProviderStatus.Disconnected;
      }
    }

    public bool isConnected {
      get { return connected; }
      set { connected = true; }
    }

    public ProviderStatus Status {
      get { return status; }
      set { status = value; }
    }

    public string Password {
      get { return password; }
      set { password = value; }
    }

    public string AccountName {
      get { return name; }
    }

    private int OnExecConnected( uint hSession ) {

      status = ProviderStatus.Connecting;
      if (StatusChanged != null)
        StatusChanged(Provider, EventArgs.Empty);
      return 0;
    }

    private int OnExecDisconnected( uint hSession ) {

      if (connected) {
        connected = false;

        status = ProviderStatus.Disconnected;

        if (Disconnected != null)
          Disconnected(Provider, EventArgs.Empty);

        if (StatusChanged != null)
          StatusChanged(Provider, EventArgs.Empty);
      }

      return 0;
    }

    private int OnExecMsgLoggedin( uint hSession ) {

      connected = true;

      status = ProviderStatus.LoggedIn;

      if (Connected != null)
        Connected(Provider, EventArgs.Empty);

      if (StatusChanged != null)
        StatusChanged(Provider, EventArgs.Empty);

      return 0;
    }

    private int OnExecMsgChat( uint hSession, GTSession.GTChat32 chat ) {
      Console.WriteLine("in OnExecMsgChat -- Lvl:{0} Txt:{1} UsrFm:{2} UsrTo:{3}",
        chat.nLevel, chat.szText, chat.szUserFm, chat.szUserTo);
      return 0;
    }

    private int OnExecMsgOpenPosition( uint hSession, GTSession.GTOpenPosition32 OpenPosition ) {
      Console.WriteLine("in OnExecMsgOpenPosition: {0} {1} {2} {3} {4} {5} {6}",
        OpenPosition.szAccountID, OpenPosition.szAccountCode, OpenPosition.szReconcileID,
        OpenPosition.szStock, OpenPosition.chOpenSide, OpenPosition.dblOpenPrice,
        OpenPosition.nOpenShares);
      return 0;
    }

    private int OnExecMsgState( uint hSession, GTSession.GTServerState32 chat ) {
      Console.WriteLine("in OnExecMsgState -- Srvr:{0} SrvrID:{1} Cnct:{2} RprtSrv:{3}",
        chat.szServer, chat.nSvrID, chat.nConnect, chat.nReportSvrID);
      return 0;
    }

    private int OnSessionExecMsgErrMsg( uint hSession, GTSession.GTErrMsg32 errmsg32 ) {
      // emit provider error
      //EmitError(0, errmsg32.nErrCode, GTSession.GetErrorMessage(errmsg32));
      Console.WriteLine("in OnSessionExecMsgErr Stk:{0} Code:{1} Txt:{2} Seq:{3}",
        errmsg32.szStock, errmsg32.nErrCode, errmsg32.szText, errmsg32.dwOrderSeqNo);
      return 0;
    }



  }
  #endregion SessionState

  /// <summary>
	/// Genesis Securities.
	/// </summary>
	public class Genesis : IMarketDataProvider, IExecutionProvider, IProvider
	{
		// Constants
		private const string CATEGORY_DESCRIPTION	= "Description";
		private const string CATEGORY_LOGIN			= "Login";
		private const string CATEGORY_IP			= "IP";
    private const string CATEGORY_ORDER = "Order";

    public enum EDirect { // is actually bit mapped
      UKN = 0, 
      TICK_LAST_UP = 0x01, TICK_LAST_DOWN = 0x02, 
      TICK_BID_UP=0x10, TICK_BID_DOWN = 0x20,
      TICK_NYSE_UP = 0x40, TICK_NYSE_DOWN = 0x80 };

    public enum ETickColor {
      Bid=0xff0000,
      Ask=0x00ff00,
      Other=0x0000ff,
      Inside=0xffffff
    }

		// Class members

    private string curAccountName;  // current account name
    private string curPassword;
    event EventHandler AccountNameChangeEventHandler; // switch active session

		private string executorIP;
		private string level1IP;
		private string level2IP;

		private ushort executorPort;
		private ushort level1Port;
		private ushort level2Port;

    private uint mmid;

    private int nextOrderId;

    public enum EOrderSubmissionType { BuySell, BidAsk, Direct };
    private EOrderSubmissionType OrderSubmissionType;
    private bool bAutoRoute;

		//private int nextOrderId;

    private bool GenesisInitialized;
    private Dictionary<string, SessionState> sessions;
    private SessionState ss;
    //private GTSession session; // 'current session', ie, one of a number of trading accounts
    //private Dictionary<string, GTSession> sessions;  // maintains list of sessions (ie trading accounts)

    //private Dictionary<string, GTStock> stocks;

    private Dictionary<uint, SessionState> stock2session;

		private Dictionary<string, Instrument> level1Requests;
    private Dictionary<string, Instrument> level2Requests;

		//private Dictionary<int, OrderRecord> ordersUserData;
		//private Dictionary<int, OrderRecord> ordersTraderSeqNo;
		//private Dictionary<string, int>      ticketsNo;

		private IBarFactory factory;

		//private ProviderStatus	status;
		//private bool			isConnected;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Genesis() {
			// login
      curAccountName = "";
      curPassword = "";
      sessions = new Dictionary<string, SessionState>();  // indexed with AccountName
      ss = null;  // assigned upon AccountName assignment
      //AccountNames = new List<string>();
			//password = "";
      AccountNameChangeEventHandler += new EventHandler(Genesis_AccountNameChangeEventHandler);
      
			// ip
			executorIP	= "69.64.202.156";
			level1IP	= "69.64.202.157";
			level2IP	= "69.64.202.155";

			// ports
			executorPort	= 15805;
			level1Port		= 16811;
			level2Port		= 16810;

      // order
      mmid = GTSession.MMID_ISLD;  // this doesn't work here as GTSession hasn't been started yet
      //OrderSubmissionType = EOrderSubmissionType.Direct;
      OrderSubmissionType = EOrderSubmissionType.BidAsk;
      bAutoRoute = true;

      nextOrderId = 0;

      // tables
			//stocks = new Dictionary<string, GTStock>();

			level1Requests = new Dictionary<string, Instrument>();
      level2Requests = new Dictionary<string, Instrument>();

      // order id
      //nextOrderId = 0;

      //ordersUserData    = new Dictionary<int, OrderRecord>();
			//ordersTraderSeqNo = new Dictionary<int, OrderRecord>();
			//ticketsNo         = new Dictionary<string, int>();
      stock2session = new Dictionary<uint, SessionState>();

			// initialize bar factory
			BarFactory = new BarFactory();

			// register provider
      GenesisInitialized = false;
			ProviderManager.Add(this);
    }

    void Genesis_AccountNameChangeEventHandler( object sender, EventArgs e ) {
      // Called whenever the account name changes.  Is used for switching between active sessions
      // Some things, like SendMarketDataRequest require a correct pre-selected session
      if ("" == curAccountName) {
        ss = null;
      }
      else {
        if (!sessions.ContainsKey(curAccountName)) {
          sessions.Add(curAccountName, new SessionState(this, curAccountName, curPassword));
        }
        ss = sessions[curAccountName];
      }
    }

    #region VisibleParameters

    [Category(CATEGORY_LOGIN)]
    [Description("AccountName")]
    public string AccountName {
      get { return curAccountName; }
      set {
        curAccountName = value;
        if (null != AccountNameChangeEventHandler) AccountNameChangeEventHandler(this, EventArgs.Empty);
      }
    }

		[Category(CATEGORY_LOGIN)]
		[Description("Password")]
		public string Password {
			get {
        string password;
        if (null != ss) {
          password = ss.Password;
          curPassword = ss.Password;
        }
        else {
          password = curPassword;
        }
        return password; }
			set { 
        curPassword = value;
        if (null != ss) {
          ss.Password = value;
        }
      }
		}

		[Category(CATEGORY_IP)]
		[Description("Execution Server IP")]
		public string ExecutorIP
		{
			get { return executorIP; }
			set { executorIP = value; }
		}

		[Category(CATEGORY_IP)]
		[Description("Level1 Server IP")]
		public string Level1IP
		{
			get { return level1IP; }
			set { level1IP = value; }
		}

		[Category(CATEGORY_IP)]
		[Description("Level2 Server IP")]
		public string Level2IP
		{
			get { return level2IP; }
			set { level2IP = value; }
		}

		[Category(CATEGORY_IP)]
		[Description("Execution Server IP")]
		public ushort ExecutorPort
		{
			get { return executorPort; }
			set { executorPort = value; }
		}

		[Category(CATEGORY_IP)]
		[Description("Level1 Server Port")]
		public ushort Level1Port
		{
			get { return level1Port; }
			set { level1Port = value; }
		}

		[Category(CATEGORY_IP)]
		[Description("Level2 Server Port")]
		public ushort Level2Port
		{
			get { return level2Port; }
			set { level2Port = value; }
		}

    // need to turn this into enumeration, but GTSession.MMID_ISLD is not a constant and enum doesn't like that
    //[Category(CATEGORY_ORDER)]
    //[Description("MMID")]
    public uint MMID {
      get { return mmid; }
      set { mmid = value; }
    }

    [Category(CATEGORY_ORDER)]
    [Description("Submission Type")]
    public EOrderSubmissionType SubmissionType {
      get { return OrderSubmissionType; }
      set { OrderSubmissionType = value; }
    }

    [Category(CATEGORY_ORDER)]
    [Description("Auto Route")]
    public bool AutoRoute {
      get { return bAutoRoute; }
      set { bAutoRoute = value; }
    }
    
    #endregion VisibleParameters

    #region GenesisSpecial

    public bool GetSessionInfo( string AccountName, Dictionary<string, string> list ) {
      bool OK = true;
      if (sessions.ContainsKey(AccountName)) {
        GTSession session = sessions[AccountName].Session;
        GTSession.GTSession32 Session = session.GetSession32();
        list.Add("LoggedIn", Session.m_bLoggedIn.ToString());
        list.Add("Transferring", Session.m_bTransfering.ToString());
        list.Add("ClosePL", Session.m_dblClosePL.ToString());
        list.Add("GrossNet", Session.m_dblGrossNet.ToString());
        list.Add("OpenPL", Session.m_dblOpenPL.ToString());
        list.Add("PassThr", Session.m_dblPassThr.ToString());
        list.Add("TotalFills", Session.m_nTotalFills.ToString());
        list.Add("TotalShares", Session.m_nTotalShares.ToString());
        list.Add("TotalTickets", Session.m_nTotalTickets.ToString());
        list.Add("Account", Session.m_pAccount32.ToString());
        list.Add("Setting", Session.m_pSetting32.ToString());
        list.Add("Stocks", Session.m_pStocks.ToString());
        list.Add("SysTime", Session.m_pSysTime32.ToString());
        list.Add("User", Session.m_pUser32.ToString());
      }
      else {
        EmitError("Genesis.GetSessionInfo does not have AccountName=" + AccountName);
        OK = false;
      }
      return OK;
    }

    public bool GetAccountInfo( string AccountName, Dictionary<string, string> list ) {
      bool OK = true;
      if (sessions.ContainsKey(AccountName)) {
        GTSession session = sessions[AccountName].Session;
        GTSession.GTAccount32 Account = session.GetAccount32();
        list.Add("BP Scale", Account.dblBPScale.ToString());
        list.Add("Current Amount", Account.dblCurrentAmount.ToString());
        list.Add("Current BP", Account.dblCurrentBP.ToString());
        list.Add("Current Equity", Account.dblCurrentEquity.ToString());
        list.Add("Current Long", Account.dblCurrentLong.ToString());
        list.Add("Current Short", Account.dblCurrentShort.ToString());
        list.Add("Initial BP", Account.dblInitialBP.ToString());
        list.Add("Initial Equity", Account.dblInitialEquity.ToString());
        list.Add("Maint Excess", Account.dblMaintExcess.ToString());
        list.Add("Max Amount Per Day", Account.dblMaxAmountPerDay.ToString());
        list.Add("Max Amount Per Ticket", Account.dblMaxAmountPerTicket.ToString());
        list.Add("Max Loss Per Day", Account.dblMaxLossPerDay.ToString());
        list.Add("PL Realized", Account.dblPLRealized.ToString());
        list.Add("Risk Rate", Account.dblRiskRate.ToString());
        list.Add("Current Cancel", Account.nCurrentCancel.ToString());
        list.Add("Current Partial Fills", Account.nCurrentPartialFills.ToString());
        list.Add("Current Shares", Account.nCurrentShares.ToString());
        list.Add("Current Tickets", Account.nCurrentTickets.ToString());
        list.Add("Discretion", Account.nDiscretion.ToString());
        list.Add("Max Open POs Per Day", Account.nMaxOpenPosPerDay.ToString());
        list.Add("Max Shares Per Day", Account.nMaxSharesPerDay.ToString());
        list.Add("Max Shares Per Pos", Account.nMaxSharesPerPos.ToString());
        list.Add("Max Shares Per Ticket", Account.nMaxSharesPerTicket.ToString());
        list.Add("Max Tickets Per Day", Account.nMaxTicketsPerDay.ToString());
        list.Add("Nort No", Account.nSortNo.ToString());
        list.Add("Trader Type", Account.nTraderType.ToString());
        list.Add("Type", Account.nType.ToString());
        list.Add("Account Code", Account.szAccountCode.ToString());
        list.Add("Account ID", Account.szAccountID.ToString());
        list.Add("Account Name", Account.szAccountName.ToString());
        list.Add("Group ID", Account.szGroupID.ToString());
        list.Add("ReconcileID", Account.szReconcileID.ToString());
      }
      else {
        EmitError("Genesis.GetAccountInfo does not have AccountName=" + AccountName);
        OK = false;
      }
      return OK;
    }

    public bool GetSettingInfo( string AccountName, Dictionary<string, string> list ) {
      bool OK = true;
      if (sessions.ContainsKey(AccountName)) {
        GTSession session = sessions[AccountName].Session;
        GTSession.GTSetting32 Setting = session.GetSetting32();
        list.Add("??", Setting.m_axes32.ToString());
        list.Add("bACPriceThreshold", Setting.m_bACPriceThreshold.ToString());
        list.Add("bACRemoveBook", Setting.m_bACRemoveBook.ToString());
        list.Add("bACRemoveOddShares", Setting.m_bACRemoveOddShares.ToString());
        list.Add("bACRemoveREDI", Setting.m_bACRemoveREDI.ToString());
        list.Add("bACTimeThreshold", Setting.m_bACTimeThreshold.ToString());
        list.Add("bAllowMultipleSell", Setting.m_bAllowMultipleSell.ToString());
        list.Add("bArcaAutoRoute", Setting.m_bArcaAutoRoute.ToString());
        list.Add("bAutoCorrection", Setting.m_bAutoCorrection.ToString());
        list.Add("bAutoRedirect100", Setting.m_bAutoRedirect100.ToString());
        list.Add("bBrutAutoRoute", Setting.m_bBrutAutoRoute.ToString());
        list.Add("bDisplayOrder", Setting.m_bDisplayOrder.ToString());
        list.Add("bIsldAutoRoute", Setting.m_bIsldAutoRoute.ToString());
        list.Add("bReserveARCAShow", Setting.m_bReserveARCAShow.ToString());
        list.Add("bReserveATTNShow", Setting.m_bReserveATTNShow.ToString());
        list.Add("bReserveBRUTShow", Setting.m_bReserveBRUTShow.ToString());
        list.Add("bReserveDATAShow", Setting.m_bReserveDATAShow.ToString());
        list.Add("bReserveINCAShow", Setting.m_bReserveINCAShow.ToString());
        list.Add("bReserveShares", Setting.m_bReserveShares.ToString());
        list.Add("bReserveSIZEShow", Setting.m_bReserveSIZEShow.ToString());
        list.Add("bReserveTRACShow", Setting.m_bReserveTRACShow.ToString());
        list.Add("bRouteARCAtoSOES", Setting.m_bRouteARCAtoSOES.ToString());
        list.Add("bRouteBRUTtoSOES", Setting.m_bRouteBRUTtoSOES.ToString());
        list.Add("bRouteDATAtoSOES", Setting.m_bRouteDATAtoSOES.ToString());
        list.Add("bRouteINCAtoSOES", Setting.m_bRouteINCAtoSOES.ToString());
        list.Add("bRouteISLDtoSOES", Setting.m_bRouteISLDtoSOES.ToString());
        list.Add("bRouteTRACtoSOES", Setting.m_bRouteTRACtoSOES.ToString());
        list.Add("bSameReserveShares", Setting.m_bSameReserveShares.ToString());
        list.Add("bShortsellDirectSend", Setting.m_bShortsellDirectSend.ToString());
        list.Add("bSoesHitECN", Setting.m_bSoesHitECN.ToString());
        list.Add("dblACDiff", Setting.m_dblACDiff.ToString());
        list.Add("dblACPriceThreshold", Setting.m_dblACPriceThreshold.ToString());
        list.Add("dblDirectHitRange", Setting.m_dblDirectHitRange.ToString());
        list.Add("ecns32", Setting.m_ecns32.ToString());
        list.Add("hiddens32", Setting.m_hiddens32.ToString());
        list.Add("mmidAutoRedirect100", Setting.m_mmidAutoRedirect100.ToString());
        list.Add("nACTimeThreshold", Setting.m_nACTimeThreshold.ToString());
        list.Add("nChartDays", Setting.m_nChartDays.ToString());
        list.Add("nChartType", Setting.m_nChartType.ToString());
        list.Add("nLevel2SortNo", Setting.m_nLevel2SortNo.ToString());
        list.Add("nLevelRate", Setting.m_nLevelRate.ToString());
        list.Add("nReserveARCAShares", Setting.m_nReserveARCAShares.ToString());
        list.Add("nReserveATTNShares", Setting.m_nReserveATTNShares.ToString());
        list.Add("nReserveBRUTShares", Setting.m_nReserveBRUTShares.ToString());
        list.Add("nReserveDATAShares", Setting.m_nReserveDATAShares.ToString());
        list.Add("nReserveINCAShares", Setting.m_nReserveINCAShares.ToString());
        list.Add("nReserveSIZEShares", Setting.m_nReserveSIZEShares.ToString());
        list.Add("nReserveTRACShares", Setting.m_nReserveTRACShares.ToString());
        list.Add("nTrainExecDiff0", Setting.m_nTrainExecDiff0.ToString());
        list.Add("nTrainExecDiff1", Setting.m_nTrainExecDiff1.ToString());
        list.Add("nTrainExecDiffECN0", Setting.m_nTrainExecDiffECN0.ToString());
        list.Add("nTrainExecDiffECN1", Setting.m_nTrainExecDiffECN1.ToString());
        list.Add("nChartPort", Setting.nChartPort.ToString());
        list.Add("nExecPort", Setting.nExecPort.ToString());
        list.Add("nLevel2Port", Setting.nLevel2Port.ToString());
        list.Add("nQuotePort", Setting.nQuotePort.ToString());
        list.Add("szChartAddress", Setting.szChartAddress.ToString());
        list.Add("szExecAddress", Setting.szExecAddress.ToString());
        list.Add("szLevel2Address", Setting.szLevel2Address.ToString());
        list.Add("szQuoteAddress", Setting.szQuoteAddress.ToString());
      }
      else {
        EmitError("Genesis.GetSettingInfo does not have AccountName=" + AccountName);
        OK = false;
      }
      return OK;
    }

    #endregion

    #region IProvider Members

		/// <summary>
		/// 
		/// </summary>
		public event EventHandler Connected;

		/// <summary>
		/// 
		/// </summary>
		public event EventHandler Disconnected;

		/// <summary>
		/// 
		/// </summary>
		public event EventHandler StatusChanged;

		/// <summary>
		/// 
		/// </summary>
		public event ProviderErrorEventHandler Error;

		/// <summary>
		/// 
		/// </summary>
		[Category(CATEGORY_DESCRIPTION)]
		[Description("SmartQuant unique identificator for this provider")]
		public byte Id
		{ 
			//get { return ProviderId.Genesis; }
      get { return 103; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Category(CATEGORY_DESCRIPTION)]
		[Description("Name of this execution provider")]
		public string Name
		{ 
			get { return "OUGenesis"; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Category(CATEGORY_DESCRIPTION)]
		[Description("Description of this execution provider")]
		public string Title
		{ 
			get { return "Genesis Securities"; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Category(CATEGORY_DESCRIPTION)]
		[Description("URL of this execution provider")]
		public string URL 
		{ 
			get { return "www.gndt.com"; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsConnected
		{
      get {
        if (null == ss) {
          return false;
        }
        else {
          return ss.isConnected;
        }
      }
		}

		/// <summary>
		/// 
		/// </summary>
    public ProviderStatus Status {
      get {
        if (null == ss) {
          return ProviderStatus.Unknown;
        }
        else {
          return ss.Status;
        }
      }
    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timeout"></param>
		public void Connect(int timeout) {
			Connect();
		
			ProviderManager.WaitConnected(this, timeout);
		}

		/// <summary>
		/// 
		/// </summary>
    public void Connect() {

      if (null == ss) {
        EmitError("Genesis::Connect:  No AccountName has been provided.");
        return;
      }
      else {
        if (!GenesisInitialized) {
          if (GTSession.gtInitialize(GTSession.GTAPI_VERSION, IntPtr.Zero) != 0) {
            EmitError("Cannot initialize API");
            return;
          }
          else {
            mmid = GTSession.MMID_ISLD;
            GenesisInitialized = true;
          }
        }

        if (ProviderStatus.Unknown == ss.Status) {
          ss.Connect(executorIP, executorPort, level1IP, level1Port, level2IP, level2Port);
          ss.Connected += new EventHandler(ss_Connected);
          ss.Disconnected += new EventHandler(ss_Disconnected);
          ss.StatusChanged += new EventHandler(ss_StatusChanged);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Disconnect() {
      if (null != ss) {
        ss.Disconnect();
      }
    }

    void ss_StatusChanged( object sender, EventArgs e ) {
      if (null != StatusChanged) StatusChanged(sender, e);
    }

    void ss_Disconnected( object sender, EventArgs e ) {
      if (null != Disconnected) Disconnected(sender, e);
    }

    void ss_Connected( object sender, EventArgs e ) {
      if (null != Connected) Connected(sender, e);
    }

		public void Shutdown()
		{
			Disconnect();
		}

		#endregion

    #region IMarketDataProvider Members

    /// <summary>
		/// 
		/// </summary>
		public event MarketDataRequestRejectEventHandler MarketDataRequestReject;

		/// <summary>
		/// 
		/// </summary>
		public event BarEventHandler NewBar;

		/// <summary>
		/// 
		/// </summary>
		public event BarEventHandler NewBarOpen;

		/// <summary>
		/// 
		/// </summary>
		public event BarSliceEventHandler NewBarSlice;

		/// <summary>
		/// 
		/// </summary>
		public event QuoteEventHandler NewQuote;

		/// <summary>
		/// 
		/// </summary>
		public event TradeEventHandler NewTrade;

		/// <summary>
		/// 
		/// </summary>
		public event MarketDepthEventHandler NewMarketDepth;

		/// <summary>
		/// 
		/// </summary>
		public event FundamentalEventHandler NewFundamental;

		/// <summary>
		/// 
		/// </summary>
		public event CorporateActionEventHandler NewCorporateAction;

		/// <summary>
		/// 
		/// </summary>
		public event BarEventHandler NewMarketBar;

		/// <summary>
		/// 
		/// </summary>
		public event MarketDataEventHandler NewMarketData;

		/// <summary>
		/// 
		/// </summary>
		public event MarketDataSnapshotEventHandler MarketDataSnapshot;

		/// <summary>
		/// Implements <see cref="IMarketDataProvider.BarFactory"/> property.
		/// </summary>
		public IBarFactory BarFactory
		{
			get { return factory; }
			set
			{
				if (factory != null)
				{
					factory.NewBar      -= new BarEventHandler(OnNewBar);
					factory.NewBarOpen  -= new BarEventHandler(OnNewBarOpen);
					factory.NewBarSlice -= new BarSliceEventHandler(OnNewBarSlice);
				}

				factory = value;

				if (factory != null)
				{
					factory.NewBar      += new BarEventHandler(OnNewBar);
					factory.NewBarOpen  += new BarEventHandler(OnNewBarOpen);
					factory.NewBarSlice += new BarSliceEventHandler(OnNewBarSlice);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
    public void SendMarketDataRequest( FIXMarketDataRequest request ) {
      // check connection
      if (!ss.isConnected) {
        return;
      }

      switch (request.SubscriptionRequestType) {
        // subscribe
        case '1': {
            for (int i = 0; i < request.NoRelatedSym; i++) {
              FIXRelatedSymGroup group = request.GetRelatedSymGroup(i);

              Instrument instrument = InstrumentManager.Instruments[group.Symbol];

              string symbol = instrument.GetSymbol(this.Name);

              if (!level1Requests.ContainsKey(symbol)) {
                GTStock stock = ss.Session.CreateStock(symbol);


                if (stock != null) {

                  Console.WriteLine("subscribe: Created stock in session {0} with handle: {1}", ss.ToString(), (uint)stock);
                  if (stock2session.ContainsKey((uint)stock)) {
                    Console.WriteLine("duplicate stock id {0}", (uint)stock);
                  }
                  else {
                    stock2session.Add((uint)stock, ss);
                  }

                  ss.stocks.Add(symbol, stock);

                  level1Requests.Add(symbol, instrument);

                  stock.HookGotQuoteLevel1( new GTStock.PFNOnGotQuoteLevel1( OnStockGotQuoteLevel1 ) );
                  stock.HookGotQuotePrint( new GTStock.PFNOnGotQuotePrint( OnStockGotQuotePrint ) );
                  stock.HookGotQuoteLevel2( new GTStock.PFNOnGotQuoteLevel2( OnStockGotQuoteLevel2 ) );
                  stock.HookOnBestAskPriceChanged( new GTStock.PFNOnBestAskPriceChanged( OnStockPmBestAskPriceChanged ) );
                  stock.HookOnBestBidPriceChanged( new GTStock.PFNOnBestBidPriceChanged( OnStockPmBestBidPriceChanged ) );
                }
              }
            }
          }
          break;
        // unsubscribe
        case '2': {
            for (int i = 0; i < request.NoRelatedSym; i++) {
              FIXRelatedSymGroup group = request.GetRelatedSymGroup(i);

              Instrument instrument = InstrumentManager.Instruments[group.Symbol];

              string symbol = instrument.GetSymbol(this.Name);

              if (level1Requests.ContainsKey(symbol)) {
                GTStock stock = ss.stocks[symbol];

                if (stock != null) {
                  stock.UnhookGotQuoteLevel1();
                  stock.UnhookGotQuotePrint();
                  stock.UnhookGotQuoteLevel2();
                  stock.UnhookOnBestAskPriceChanged();
                  stock.UnhookOnBestBidPriceChanged();

                  level1Requests.Remove(symbol);
                }
              }
            }
          }
          break;
      }
    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hStock"></param>
		/// <param name="level132"></param>
		/// <returns></returns>
    private int OnStockGotQuoteLevel1( UInt32 hStock, GTSession.GTLevel132 level132 ) {
      lock ( this ) {
        Instrument instrument = level1Requests[level132.szStock];

        Quote quote = new Quote(
          Clock.Now,
          level132.dblBidPrice,
          level132.nBidSize,
          level132.dblAskPrice,
          level132.nAskSize );

        //Console.WriteLine( "level1 aex:{0}, bex:{1}, sc:{2}, hi:{3}, lo:{4}, op:{5}, vol:{6};", 
        //  level132.bboAskExchangeCode, level132.bboBidExchangeCode, level132.chSaleCondition,
        //  level132.dblHigh, level132.dblLow, level132.dblOpen, level132.dwVolume/*, level132.flags */);

        if ( NewQuote != null )
          NewQuote( this, new QuoteEventArgs( quote, instrument, this ) );
      }

      return 0;
    }


    private int OnStockGotQuoteLevel2( UInt32 hStock, GTSession.GTLevel232 level232 ) {
      lock (this) {
        Instrument instrument = level1Requests[level232.szStock];  //being lazy for now

        //string smmid = "1234";
        //uint i = GTSession.copymmid_(out smmid, level232.mmid);
        string smmid = GTSession.ConvertMMID(level232.mmid);
        char c = level232.chSide;
        double price = level232.dblPrice;
        int shares = level232.dwShares;

        MDSide side;
        switch ( level232.chSide ) {
          case 'B':
            side = MDSide.Bid;
            break;
          case 'S':
            side = MDSide.Ask;
            break;
          default:
            throw new Exception( "Unknown side on level 2 quote" );
            break;
        }

        MarketDepth depth
          = new MarketDepth(Clock.Now, smmid, level232.nOwnShares,
          MDOperation.Undefined, side, 
          Math.Round(price,2), shares);
        //          level232.bAxe, level232.bBook, level232.bECN, level232.bOpenView, level232.bTotalView, level232.chSide, 
        //          level232.dblPrice, level232.dwShares, level232.mmid, level232.nOwnShares, level232.szStock 

        //Console.WriteLine( "mmid:{0}, side:{1}, price:{2:0.00}, shr:{3}", smmid, level232.chSide, level232.dblPrice, level232.dwShares );
        //Console.WriteLine("MD={0}, mmid:{1}, tick:{2}, shr:{3}", depth, smmid, level232.dwRecvTick, level232.dwShares );
        //if ( 0 != level232.bBook || 0 != level232.bECN || 0 != level232.bOpenView || 0 != level232.bTotalView ) {
        //  Console.WriteLine( "Level2: bk:{0}, ecn:{1}, ov:{2}, tv:{3}", level232.bBook, level232.bECN, level232.bOpenView, level232.bTotalView );
        //}

        if (NewMarketDepth != null)
          NewMarketDepth(this, new MarketDepthEventArgs(depth, instrument, this));
      }

      return 0;
    }

    private int OnStockPmBestAskPriceChanged( UInt32 hStock ) {
      lock ( this ) {
        SessionState ss = stock2session[hStock];
        
      }
      return 0;
    }

    private int OnStockPmBestBidPriceChanged( UInt32 hStock ) {
      lock ( this ) {
      }
      return 0;
    }

    /// <summary>
		/// 
		/// </summary>
		/// <param name="hStock"></param>
		/// <param name="print32"></param>
		/// <returns></returns>
    private int OnStockGotQuotePrint( UInt32 hStock, GTSession.GTPrint32 print32 ) {
      lock (this) {
        Instrument instrument = level1Requests[print32.szStock];

        Trade trade = new Trade(Clock.Now, print32.dblPrice, print32.dwShares);

        //EDirect direct = (EDirect) print32.nLastDirect;  // these are the only real useful values, use for time and sales.
        //ETickColor color = ( ETickColor )print32.rgbColor;


        //Console.WriteLine( "trade: ex:{0}, sc:{1}, src:{2}, direct:{3}, clr:{4};", 
        //  (char) print32.chExchangeCode, (char) print32.chSaleCondition, print32.chSource, 
        //  direct, color );

        if (NewTrade != null)
          NewTrade(this, new TradeEventArgs(trade, instrument, this));

        if (factory != null)
          factory.OnNewTrade(instrument, trade);
      }

      return 0;
    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void OnNewBar(object sender, BarEventArgs args)
		{
			if (NewBar != null)
				NewBar(this, new BarEventArgs(args.Bar, args.Instrument, this));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void OnNewBarOpen(object sender, BarEventArgs args)
		{
			if (NewBarOpen != null)
				NewBarOpen(this, new BarEventArgs(args.Bar, args.Instrument, this));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void OnNewBarSlice(object sender, BarSliceEventArgs args)
		{
			if (NewBarSlice != null)
				NewBarSlice(this, new BarSliceEventArgs(args.BarSize, this));
		}

		#endregion

		#region IExecutionProvider Members

		public event ExecutionReportEventHandler ExecutionReport;

		public event OrderCancelRejectEventHandler OrderCancelReject;

		public void SendOrderStatusRequest(FIXOrderStatusRequest request)
		{
			throw new NotImplementedException();
		}

		public void SendOrderCancelReplaceRequest(FIXOrderCancelReplaceRequest request)
		{
			throw new NotImplementedException();
		}

		public void SendNewOrderSingle(NewOrderSingle order) {

      SessionState ss = sessions[order.Account];
			Instrument instrument = InstrumentManager.Instruments[order.Symbol];

			string symbol = instrument.GetSymbol(this.Name);

			GTStock stock = null;

      if (!ss.stocks.TryGetValue(symbol, out stock)) {
        stock = ss.Session.CreateStock(symbol);

        if (stock == null)
          return;
        else
          Console.WriteLine("new order: Created stock in session {0} with handle: {1}", ss.ToString(), (uint)stock);
        if (stock2session.ContainsKey((uint)stock)) {
          Console.WriteLine("duplicate stock id {0}", (uint)stock);
        }
        else {
          stock2session.Add((uint)stock, ss);
        }

        ss.stocks.Add(symbol, stock);
      }

			//
			stock.HookSendingOrder  (new GTStock.PFNOnSendingOrder  (OnStockSendingOrder  ));
			stock.HookExecMsgTrade  (new GTStock.PFNOnExecMsgTrade  (OnStockExecMsgTrade  ));
			stock.HookExecMsgPending(new GTStock.PFNOnExecMsgPending(OnStockExecMsgPending));
			stock.HookExecMsgSending(new GTStock.PFNOnExecMsgSending(OnStockExecMsgSending));
			stock.HookExecMsgReject (new GTStock.PFNOnExecMsgReject (OnStockExecMsgReject ));
			stock.HookExecMsgCancel (new GTStock.PFNOnExecMsgCancel (OnStockExecMsgCancel ));
			stock.HookExecMsgErrMsg (new GTStock.PFNOnExecMsgErrMsg (OnStockExecMsgError  ));

			//
			GTSession.GTOrder32 order32 = new GTSession.GTOrder32();

			stock.InitOrder(order32);
      order32.szAccountID = order.Account;


      // is this line needed any longer?  was a temporary fix I think, until I got multiple sessions running
      //order32.szAccountID = order.Account;

      mmid = GTSession.MMID_ISLD;  // this is an override because not assigned correctly before

      if (EOrderSubmissionType.Direct != OrderSubmissionType) {
        if (mmid == GTSession.MMID_ISLD) { order32.m_bIsldAutoRoute = bAutoRoute ? 1 : 0; }
        if (mmid == GTSession.MMID_BRUT) { order32.m_bBrutAutoRoute = bAutoRoute ? 1 : 0; }
        if (mmid == GTSession.MMID_ARCA) { order32.m_bArcaAutoRoute = bAutoRoute ? 1 : 0; }
      }

			// order price
			double price = (order.OrdType == OrdType.Limit) ? order.Price : 0;

			// order side
			switch (order.Side)
			{
				case Side.Buy:
          switch (OrderSubmissionType) {
            case EOrderSubmissionType.BidAsk:
              stock.Bid(order32, (int)order.OrderQty, price, mmid);
              break;
            case EOrderSubmissionType.BuySell:
              //stock.Buy(order32, (int)order.OrderQty, price, GTSession.MMID_ISLD);
              stock.Buy(order32, (int)order.OrderQty, price, mmid);
              break;
            case EOrderSubmissionType.Direct:
              stock.BuyDirect(order32, (int)order.OrderQty, price);
              break;
          }
          break;
				case Side.Sell:
          switch (OrderSubmissionType) {
            case EOrderSubmissionType.BidAsk:
              stock.Ask(order32, (int)order.OrderQty, price, mmid);
              break;
            case EOrderSubmissionType.BuySell:
              //stock.Sell(order32, (int)order.OrderQty, price, GTSession.MMID_ISLD);
              stock.Sell(order32, (int)order.OrderQty, price, mmid);
              break;
            case EOrderSubmissionType.Direct:
              stock.SellDirect(order32, (int)order.OrderQty, price);
              break;
          }
					break;
				default:
					break;
			}

			// order type
			switch (order.OrdType)
			{
				case OrdType.Market:
					order32.chPriceIndicator = GTOrderType.MARKET;
					break;
				case OrdType.Limit:
					order32.chPriceIndicator = GTOrderType.LIMIT;
					break;
				default:
					break;
			}

			// time in force
			switch (order.TimeInForce)
			{
				case TimeInForce.Day:
					order32.dwTimeInForce = GTTimeInForce.Day;
					break;
				case TimeInForce.IOC:
					order32.dwTimeInForce = GTTimeInForce.IOC;
					break;
				default:
					break;
			}

			// user data
			nextOrderId++;
			order32.dwUserData = (uint)nextOrderId;

			ss.ordersUserData.Add(nextOrderId, new OrderRecord(order as SingleOrder));

			// send order
			stock.PlaceOrder(order32);
		}

    public void SendOrderCancelRequest( FIXOrderCancelRequest request ) {

      SessionState ss = sessions[request.Account];
      GTSession session = ss.Session;

      int ticketNo = ss.ticketsNo[request.OrigClOrdID];

      GTSession.gtCancelTicket(session, ticketNo);
    }

		#region Execution callbacks

    private int OnStockExecMsgTrade( uint hStock, GTSession.GTTrade32 trade32 ) {

      string s1 = trade32.szAccountCode;
      string s2 = trade32.szAccountID;
      string s3 = trade32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersTraderSeqNo[trade32.dwTraderSeqNo];

      ExecutionReport report = new ExecutionReport();

      report.TransactTime = Clock.Now;
      report.ClOrdID = record.Order.ClOrdID;
      report.OrderID = trade32.dwTraderSeqNo.ToString();
      report.Currency = record.Order.Currency;
      report.Symbol = record.Order.Symbol;
      report.SecurityType = record.Order.SecurityType;
      report.SecurityExchange = record.Order.SecurityExchange;
      report.HandlInst = record.Order.HandlInst;
      report.Side = record.Order.Side;
      report.OrdType = record.Order.OrdType;
      report.OrderQty = record.Order.OrderQty;
      report.LeavesQty = trade32.nExecRemainShares;
      report.CumQty = record.Order.OrderQty - trade32.nExecRemainShares;
      report.Price = record.Order.Price;
      report.LastPx = trade32.dblExecPrice;
      report.LastQty = trade32.nExecShares;

      // average price
      record.AddFill(trade32.dblExecPrice, trade32.nExecShares);

      report.AvgPx = record.AvgPx;

      // exec type & order status
      if (trade32.nExecRemainShares == 0) {
        report.ExecType = ExecType.Fill;
        report.OrdStatus = OrdStatus.Filled;
      }
      else {
        report.ExecType = ExecType.PartialFill;
        report.OrdStatus = OrdStatus.PartiallyFilled;
      }

      if (ExecutionReport != null)
        ExecutionReport(this, new ExecutionReportEventArgs(report));

      return 0;
    }

    private int OnStockExecMsgPending( uint hStock, GTSession.GTPending32 pending32 ) {

      string s1 = pending32.szAccountCode;
      string s2 = pending32.szAccountID;
      string s3 = pending32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersTraderSeqNo[pending32.dwTraderSeqNo];

      if (ss.ticketsNo.ContainsKey(record.Order.ClOrdID))
        return 0;

      ss.ticketsNo.Add(record.Order.ClOrdID, pending32.dwTicketNo);

      ExecutionReport report = new ExecutionReport();

      report.TransactTime = Clock.Now;
      report.ClOrdID = record.Order.ClOrdID;
      report.OrderID = pending32.dwTraderSeqNo.ToString();
      report.Currency = record.Order.Currency;
      report.Symbol = record.Order.Symbol;
      report.SecurityType = record.Order.SecurityType;
      report.SecurityExchange = record.Order.SecurityExchange;
      report.HandlInst = record.Order.HandlInst;
      report.Side = record.Order.Side;
      report.OrdType = record.Order.OrdType;
      report.OrderQty = record.Order.OrderQty;
      report.LeavesQty = record.Order.OrderQty;
      report.CumQty = 0;
      report.ExecType = ExecType.New;
      report.OrdStatus = OrdStatus.New;

      if (ExecutionReport != null)
        ExecutionReport(this, new ExecutionReportEventArgs(report));

      return 0;
    }

    private int OnStockExecMsgSending( uint hStock, GTSession.GTSending32 sending32 ) {

      string s1 = sending32.szAccountCode;
      string s2 = sending32.szAccountID;
      string s3 = sending32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersTraderSeqNo[sending32.dwTraderSeqNo];

      ExecutionReport report = new ExecutionReport();

      report.TransactTime = Clock.Now;
      report.ClOrdID = record.Order.ClOrdID;
      report.OrderID = sending32.dwTraderSeqNo.ToString();
      report.Currency = record.Order.Currency;
      report.Symbol = record.Order.Symbol;
      report.SecurityType = record.Order.SecurityType;
      report.SecurityExchange = record.Order.SecurityExchange;
      report.HandlInst = record.Order.HandlInst;
      report.Side = record.Order.Side;
      report.OrdType = record.Order.OrdType;
      report.OrderQty = record.Order.OrderQty;
      report.LeavesQty = record.Order.OrderQty;
      report.CumQty = 0;
      report.ExecType = ExecType.PendingNew;
      report.OrdStatus = OrdStatus.PendingNew;

      if (ExecutionReport != null)
        ExecutionReport(this, new ExecutionReportEventArgs(report));

      return 0;
    }

    private int OnStockExecMsgReject( uint hStock, GTSession.GTReject32 reject32 ) {

      string s1 = reject32.szAccountCode;
      string s2 = reject32.szAccountID;
      string s3 = reject32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersTraderSeqNo[reject32.dwTraderSeqNo];

      if ( ss.DoTradeSeqRemove ) {
        ss.ordersTraderSeqNo.Remove( reject32.dwTraderSeqNo );
      }

      ExecutionReport report = new ExecutionReport();

      report.TransactTime = Clock.Now;
      report.ClOrdID = record.Order.ClOrdID;
      report.OrigClOrdID = record.Order.ClOrdID;
      report.OrderID = reject32.dwTraderSeqNo.ToString();
      report.Currency = record.Order.Currency;
      report.Symbol = record.Order.Symbol;
      report.SecurityType = record.Order.SecurityType;
      report.SecurityExchange = record.Order.SecurityExchange;
      report.HandlInst = record.Order.HandlInst;
      report.Side = record.Order.Side;
      report.OrdType = record.Order.OrdType;
      report.OrderQty = record.Order.OrderQty;
      report.LeavesQty = record.Order.OrderQty;
      report.CumQty = record.Order.CumQty;
      report.Text = reject32.szRejectReason;
      report.ExecType = ExecType.Rejected;
      report.OrdStatus = OrdStatus.Rejected;

      if (ExecutionReport != null)
        ExecutionReport(this, new ExecutionReportEventArgs(report));

      return 0;
    }

    private int OnStockExecMsgCancel( uint hStock, GTSession.GTCancel32 cancel32 ) {

      string s1 = cancel32.szAccountCode;
      string s2 = cancel32.szAccountID;
      string s3 = cancel32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersTraderSeqNo[cancel32.dwTraderSeqNo];

      if ( ss.DoTradeSeqRemove ) {
        ss.ordersTraderSeqNo.Remove( cancel32.dwTraderSeqNo );
      }

      ExecutionReport report = new ExecutionReport();

      report.TransactTime = Clock.Now;
      report.ClOrdID = record.Order.ClOrdID;
      report.OrigClOrdID = record.Order.ClOrdID;
      report.OrderID = cancel32.dwTraderSeqNo.ToString();
      report.Currency = record.Order.Currency;
      report.Symbol = record.Order.Symbol;
      report.SecurityType = record.Order.SecurityType;
      report.SecurityExchange = record.Order.SecurityExchange;
      report.HandlInst = record.Order.HandlInst;
      report.Side = record.Order.Side;
      report.OrdType = record.Order.OrdType;
      report.OrderQty = record.Order.OrderQty;
      report.LeavesQty = record.LeavesQty;
      report.CumQty = record.CumQty;
      report.AvgPx = record.AvgPx;
      report.ExecType = ExecType.Cancelled;
      report.OrdStatus = OrdStatus.Cancelled;

      if (ExecutionReport != null)
        ExecutionReport(this, new ExecutionReportEventArgs(report));

      return 0;
    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hStock"></param>
		/// <param name="errmsg32"></param>
		/// <returns></returns>
    private int OnStockExecMsgError( uint hStock, GTSession.GTErrMsg32 errmsg32 ) {

      Console.WriteLine("*** Genesis::OnStockExecMsgError hStk:{0}, orderseq:{1}, cde:{2}, stock:{3}, txt:{4}, msg:{5}",
        hStock, errmsg32.dwOrderSeqNo, errmsg32.nErrCode, errmsg32.szStock, errmsg32.szText,
        GTSession.GetErrorMessage(errmsg32)
      );

      SuperStock stock = new SuperStock(hStock);
      Console.WriteLine("Superstock in errmsg is {0}", stock.Handle);
      if (stock2session.ContainsKey(hStock)) {
        SessionState ss = stock2session[hStock];
        OrderRecord record = ss.ordersTraderSeqNo[errmsg32.dwOrderSeqNo];

        if ( ss.DoTradeSeqRemove ) {
          ss.ordersTraderSeqNo.Remove( errmsg32.dwOrderSeqNo );
        }

        ExecutionReport report = new ExecutionReport();

        report.TransactTime = Clock.Now;
        report.ClOrdID = record.Order.ClOrdID;
        report.OrigClOrdID = record.Order.ClOrdID;
        report.OrderID = errmsg32.dwOrderSeqNo.ToString();
        report.Currency = record.Order.Currency;
        report.Symbol = record.Order.Symbol;
        report.SecurityType = record.Order.SecurityType;
        report.SecurityExchange = record.Order.SecurityExchange;
        report.HandlInst = record.Order.HandlInst;
        report.Side = record.Order.Side;
        report.OrdType = record.Order.OrdType;
        report.OrderQty = record.Order.OrderQty;
        report.LeavesQty = record.Order.OrderQty;
        report.CumQty = record.CumQty;
        report.AvgPx = record.AvgPx;
        report.Text = GTSession.GetErrorMessage(errmsg32);
        report.ExecType = ExecType.Rejected;
        report.OrdStatus = OrdStatus.Rejected;

        if (ExecutionReport != null)
          ExecutionReport(this, new ExecutionReportEventArgs(report));

      }
      else {
        Console.WriteLine("*** Genesis::OnStockExecMsgError hStk:{0} isn't in s2s", hStock);
      }
      //GTStock stock = new GTStock(hStock);
      //GTSession.GTStock32 stk = stock.GetStock32();
      //GTSession.GTLevel232 l2 = stock.GetAskLevel2Item(1);


      /*
      string s1 = errmsg32.szAccountCode;
      string s2 = errmsg32.szAccountID;
      string s3 = errmsg32.szUserID;

      SessionState ss = sessions[s3];
       * */

      return 0;
    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hStock"></param>
		/// <param name="sending32"></param>
		/// <returns></returns>
    private int OnStockSendingOrder( uint hStock, GTSession.GTSending32 sending32 ) {

      string s1 = sending32.szAccountCode;
      string s2 = sending32.szAccountID;
      string s3 = sending32.szUserID;

      SessionState ss = sessions[s3];

      OrderRecord record = ss.ordersUserData[sending32.dwUserData];

      ss.ordersUserData.Remove(sending32.dwUserData);

      ss.ordersTraderSeqNo.Add(sending32.dwTraderSeqNo, record);

      return 0;
    }

		#endregion

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="code"></param>
		/// <param name="message"></param>
		private void EmitError(int id, int code, string message)
		{
			if (Error != null)
				Error(new ProviderErrorEventArgs(this, id, code, message));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		private void EmitError(string message)
		{
			EmitError(-1, -1, message);
		}
	}

  public class SuperStock : GTAPINet.GTStock {

    public SuperStock( uint hStock )
      : base(hStock) {
    }

    public uint Handle {
      get { return m_hStock; }
    }
  }
}

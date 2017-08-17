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

using OneUnified.Sockets;

namespace OneUnified.IQFeed {

  public class FundamentalMessageEventArgs : EventArgs {
    public FundamentalMessage Message;

    public FundamentalMessageEventArgs( FundamentalMessage Message ) {
      this.Message = Message;
    }
  }

  public delegate void FundamentalMessageHandler( object sender, FundamentalMessageEventArgs args );

  public class FundamentalMessage : IQFeedMessage {

    private double m_SplitFactor1Ratio;
    private DateTime m_SplitFactor1Date;
    private double m_SplitFactor2Ratio;
    private DateTime m_SplitFactor2Date;

    public FundamentalMessage( string[] values )
      : base("F", values) {

      char[] sep = { ' ' };
      int ix = 0;

      try {

        // fundamental message doesn't happen too often so not too much overhead to 
        //   pre-convert these values from non-standard fields

        ix = 36 - 1;
        if (string.IsNullOrEmpty(items[ix])) {
          m_SplitFactor1Ratio = 0;
          m_SplitFactor1Date = new DateTime(0);
        }
        else {
          string t1 = items[ix];
          string[] t2 = t1.Split(sep);
          m_SplitFactor1Ratio = Convert.ToDouble(t2[0]);
          m_SplitFactor1Date = ConvertToDate(t2[1]);
        }

        ix = 37 - 1;
        if (string.IsNullOrEmpty(items[ix])) {
          m_SplitFactor2Ratio = 0;
          m_SplitFactor2Date = new DateTime();
        }
        else {
          string t1 = items[ix];
          string[] t2 = t1.Split(sep);
          m_SplitFactor2Ratio = Convert.ToDouble(t2[0]);
          m_SplitFactor2Date = ConvertToDate(t2[1]);
        }

      }
      catch {
        Console.WriteLine("FundamentalMessage Conversion problem at item {0}", ix);
        throw new Exception("FundamentalMessage Conversion Error");
      }
    }

    // on-demand conversion of fields
    public string Symbol { get { return ConvertToString(2); } }
    public string ExchangeID { get { return ConvertToString(3); } }
    public double PriceEarningsRatio { get { return ConvertToDouble(4); } }
    public int AverageVolume { get { return ConvertToInt32(5); } }
    public double Hi52Week { get { return ConvertToDouble(6); } }
    public double Lo52Week { get { return ConvertToDouble(7); } }
    public double HiCalendarYear { get { return ConvertToDouble(8); } }
    public double LoCalendarYear { get { return ConvertToDouble(9); } }
    public double DividendYield { get { return ConvertToDouble(10); } }
    public double DividendAmount { get { return ConvertToDouble(11); } }
    public double DividendRate { get { return ConvertToDouble(12); } }
    public DateTime PayDate { get { return ConvertToDate(13); } }
    public DateTime ExDividendDate { get { return ConvertToDate(14); } }
    public double CurrentYearEarningsPerShare { get { return ConvertToDouble(20); } }
    public double NextYearEarningsPerShare { get { return ConvertToDouble(21); } }
    public double FiveYearGrowthPercentage { get { return ConvertToDouble(22); } }
    public int FiscalYearEnd { get { return ConvertToInt32(23); } }
    public string CompanyName { get { return ConvertToString(25); } }
    public string RootOptionSymbols { get { return ConvertToString(26); } }
    public double PercentHeldByInstitutions { get { return ConvertToDouble(27); } }
    public double Beta { get { return ConvertToDouble(28); } }
    public string Leaps { get { return ConvertToString(29); } }
    public double CurrentAssets { get { return ConvertToDouble(30); } }
    public double CurrentLiabilities { get { return ConvertToDouble(31); } }
    public DateTime BalanceSheetDate { get { return ConvertToDate(32); } }
    public double LongTermDebt { get { return ConvertToDouble(33); } }
    public double CommonSharesOutstanding { get { return ConvertToDouble(34); } }
    public double SplitFactor1Ratio { get { return m_SplitFactor1Ratio; } }
    public DateTime SplitFactor1Date { get { return m_SplitFactor1Date; } }
    public double SplitFactor2Ratio { get { return m_SplitFactor2Ratio; } }
    public DateTime SplitFactor2Date { get { return m_SplitFactor2Date; } }
    public string MarketCenter { get { return ConvertToString(39); } }
    public string FormatCode { get { return ConvertToString(40); } }
    public int Precision { get { return ConvertToInt32(41); } }
    public int SIC { get { return ConvertToInt32(42); } }
    public double HistoricalVolatility { get { return ConvertToDouble(43); } }
    public string SecurityType { get { return ConvertToString(44); } }
    public string ListedMarket { get { return ConvertToString(45); } }
    public DateTime Hi52WeekDate { get { return ConvertToDate(46); } }
    public DateTime Lo52WeekDate { get { return ConvertToDate(47); } }
    public DateTime HiCalendarYearDate { get { return ConvertToDate(48); } }
    public DateTime LoCalendarYearDate { get { return ConvertToDate(49); } }


  }
}
/*
                         REQUEST FUNDAMENTAL FIELDNAMES
S,FUNDAMENTAL FIELDNAMES,
 * Symbol,
 * Exchange ID,
 * PE,
 * Average Volume,
 * 52 Week High,
 * 52 Week Low,
 * Calendar Year High,
 * Calendar Year Low,
 * Dividend Yield,
 * Dividend Amount,
 * Dividend Rate,
 * Pay Date,
 * Ex-dividend Date,
 * (Reserved),
 * (Reserved),
 * (Reserved),
 * (Reserved),
 * (Reserved),
 * Current Year EPS,
 * Next Year EPS,
 * Five-year Growth Percentage,
 * Fiscal Year End,
 * (Reserved),
 * Company Name,
 * Root Option Symbol,
 * Percent Held By Institutions,
 * Beta,
 * Leaps,
 * Current Assets,
 * Current Liabilities,
 * Balance Sheet Date,
 * Long-term Debt,
 * Common Shares Outstanding,
 * (Reserved),
 * Split Factor 1,
 * Split Factor 2,
 * (Reserved),
 * Market Center,
 * Format Code,
 * Precision,
 * SIC,
 * Historical Volatility,
 * Security Type,
 * Listed Market,
 * 2 Week High Date,
 * 52 Week Low Date,
 * Calendar Year High Date,
 * Calendar Year Low Date
 * 
      S,REQUEST UPDATE FIELDNAMES
S,UPDATE FIELDNAMES,
 * Symbol,
 * Exchange ID,
 * Last,
 * Change,
 * Percent Change,
 * Total Volume,
 * Last Size/Incremental Volume,
 * High,
 * Low,
 * Bid,
 * Ask,
 * Bid Size,
 * Ask Size,
 * Tick,
 * Bid Tick,
 * Range,
 * Time,
 * Open Interest,
 * Open,
 * Close,
 * Spread,
 * Strike,
 * Settle,
 * Delay,
 * Market Center,
 * Restricted Code,
 * Net Asset Value,
 * Average Maturity,
 * 7 Day Yield,
 * Last Trade Date,
 * (Reserved),
 * Extended Trading Last,
 * Expiration Date,
 * Regional Volume,
 * Net Asset Value,
 * Extended Trading Change,
 * Extended Trading Difference,
 * Price-Earnings Ratio,
 * % Off Average Volume,
 * Bid Change,
 * Ask Change,
 * Change From Open,
 * Market Open,
 * Volatility,
 * Market Capitalization,
 * Fraction Display Code,
 * Decimal Precision,
 * Days to Expiration,
 * Previous Days Volume,
 * Regions,
 * Open Range 1,
 * Close Range 1,
 * Open Range 2,
 * Close Range 2,
 * Number of Trades Today,
 * Bid Time,
 * Ask Time,
 * VWAP,
 * TickID,
 * Financial Status Indicator
*/
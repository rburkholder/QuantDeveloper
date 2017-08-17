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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using System.Data;
using System.Reflection;
using System.Data.SqlClient;

using SmartQuant;
using SmartQuant.Series;

namespace OneUnified.Trading.PatternAnalysis {

  public class TurningPoint {

    public delegate void PeakFoundHandler( object source, DateTime dtPatternPt1, double PatternPt1, EPatternState Direction );
    public event PeakFoundHandler OnPeakFound;

    public delegate void UpDecisionPointFoundHandler( object source );
    public event UpDecisionPointFoundHandler OnUpDecisionPointFound;

    public delegate void DownDecisionPointFoundHandler( object source );
    public event DownDecisionPointFoundHandler OnDownDecisionPointFound;

    double dblPatternDelta = 0.30; // pt1 becomes new anchor when abs(pt0-pt1)>delta
    double PatternPt0; // pattern end point, drags pt1 away from anchor, but can retrace at will
    double PatternPt1; // pattern mid point, can only move away from anchor point
    DateTime dtPatternPt1;  // when it was last encountered
    public enum EPatternState { init, start, down, up };
    EPatternState PatternState;
    // pt0, pt1 are set when first delta has been reached
    //int[] rPatternDeltaDistance;
    int cntNewUp;
    int cntNewDown;
    int cntTurns;

    public TurningPoint( double delta ) {
      PatternState = EPatternState.init;
      PatternPt0 = 0;
      PatternPt1 = 0;
      cntNewUp = 0;
      cntNewDown = 0;
      cntTurns = 0;
      dblPatternDelta = delta;

    }

    public int NumTurns {
      get { return cntTurns; }
    }

    public void Calculate( Double val ) {

      double dif;

			//
			// Pattern calculation 
			//
			switch ( PatternState ) {
				case EPatternState.init:
					PatternPt1 = val;
					PatternPt0 = val;
					PatternState = EPatternState.start;
					//dsPattern.Add( quote.DateTime, val );
					//Console.WriteLine( "{0} Pattern init {1}", SmartQuant.Clock.Now, PatternState );  
					break;
				case EPatternState.start:
					if ( Math.Abs( val - PatternPt1 ) >= dblPatternDelta ) {
						dtPatternPt1 = Clock.Now;
						PatternPt0 = val;
						if ( val > PatternPt1 ) {
							PatternState = EPatternState.up;
							//gpstrategy.BuySignal = true;
							//Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
						}
						else {
							PatternState = EPatternState.down;
							//gpstrategy.SellSignal = true;
							//Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
						}
						PatternPt1 = val;
					}
					break;
				case EPatternState.up:
					PatternPt0 = val;
					if ( val > PatternPt1 ) {
						PatternPt1 = val;
						dtPatternPt1 = Clock.Now;
						cntNewUp++;
            if (null != OnUpDecisionPointFound) OnUpDecisionPointFound(this);
					}
					dif = PatternPt1 - PatternPt0;
					//dsPt0Ratio.Add( quote.DateTime, dif / dblPatternDelta  );
					if ( dif >= dblPatternDelta ) {
						//dsPattern.Add( dtPatternPt1, PatternPt1 );
            cntTurns++;
            if (null != OnPeakFound) OnPeakFound(this, dtPatternPt1, PatternPt1, PatternState);
						//mp.ClassifyDoubleSeriesEnd( dsPattern );
						if ( PatternPt1 > PatternPt0 ) {
							//Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
							PatternState = EPatternState.down;
						}
						else {
							//Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
						}
					}
					else {
//						rPatternDeltaDistance[ (int) Math.Round( dif * 100 ) ]++;
					}
				break;
				case EPatternState.down:
					PatternPt0 = val;
					if ( val < PatternPt1 ) {
						PatternPt1 = val;
						dtPatternPt1 = Clock.Now;
						cntNewDown++;
            if (null != OnDownDecisionPointFound) OnDownDecisionPointFound(this);
          }
					dif = PatternPt0 - PatternPt1;
					//dsPt0Ratio.Add( quote.DateTime, dif / dblPatternDelta );
					if ( dif >= dblPatternDelta ) {
						//dsPattern.Add( dtPatternPt1, PatternPt1 );
            cntTurns++;
            if (null != OnPeakFound) OnPeakFound(this, dtPatternPt1, PatternPt1, PatternState);
            //mp.ClassifyDoubleSeriesEnd( dsPattern );
						if ( PatternPt1 < PatternPt0 ) {
							//Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
							PatternState = EPatternState.up;
						}
						else {
							//Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
						}
					}
					else {
						//rPatternDeltaDistance[ (int) Math.Round( dif * 100 ) ]++;
					}
					break;
			}
			
			
    }
		

  }

  #region Pattern Analysis

  public enum EPatternType { Uninteresting, UpTrend, DownTrend, HeadAndShoulders, InvertedHeadAndShoulders, Triangle, Broadening };

  public class PatternInfo {


    public string PatternId;
    public EPatternType PatternType;

    public PatternInfo( string PatternId, EPatternType PatternType ) {
      this.PatternId = PatternId;
      this.PatternType = PatternType;
    }
  }

  public class MerrillPattern {

    // page 94 in Bollinger Bands

    static Hashtable htPatterns;

    static MerrillPattern() {

      htPatterns = new Hashtable(32);

      htPatterns["21435"] = new PatternInfo("M1", EPatternType.DownTrend);
      htPatterns["21534"] = new PatternInfo("M2", EPatternType.InvertedHeadAndShoulders);
      htPatterns["31425"] = new PatternInfo("M3", EPatternType.DownTrend);
      htPatterns["31524"] = new PatternInfo("M4", EPatternType.InvertedHeadAndShoulders);
      htPatterns["32415"] = new PatternInfo("M5", EPatternType.Broadening);
      htPatterns["32514"] = new PatternInfo("M6", EPatternType.InvertedHeadAndShoulders);
      htPatterns["41325"] = new PatternInfo("M6", EPatternType.Uninteresting);
      htPatterns["41523"] = new PatternInfo("M8", EPatternType.InvertedHeadAndShoulders);
      htPatterns["42315"] = new PatternInfo("M9", EPatternType.Uninteresting);
      htPatterns["42513"] = new PatternInfo("M10", EPatternType.InvertedHeadAndShoulders);
      htPatterns["43512"] = new PatternInfo("M11", EPatternType.InvertedHeadAndShoulders);
      htPatterns["51324"] = new PatternInfo("M12", EPatternType.Uninteresting);
      htPatterns["51423"] = new PatternInfo("M13", EPatternType.Triangle);
      htPatterns["52314"] = new PatternInfo("M14", EPatternType.Uninteresting);
      htPatterns["52413"] = new PatternInfo("M15", EPatternType.UpTrend);
      htPatterns["53412"] = new PatternInfo("M16", EPatternType.UpTrend);

      htPatterns["13254"] = new PatternInfo("W1", EPatternType.DownTrend);
      htPatterns["14253"] = new PatternInfo("W2", EPatternType.DownTrend);
      htPatterns["14352"] = new PatternInfo("W3", EPatternType.Uninteresting);
      htPatterns["15243"] = new PatternInfo("W4", EPatternType.Triangle);
      htPatterns["15342"] = new PatternInfo("W5", EPatternType.Uninteresting);
      htPatterns["23154"] = new PatternInfo("W6", EPatternType.HeadAndShoulders);
      htPatterns["24153"] = new PatternInfo("W7", EPatternType.HeadAndShoulders);
      htPatterns["24351"] = new PatternInfo("W8", EPatternType.Uninteresting);
      htPatterns["25143"] = new PatternInfo("W9", EPatternType.HeadAndShoulders);
      htPatterns["25341"] = new PatternInfo("W10", EPatternType.Uninteresting);
      htPatterns["34152"] = new PatternInfo("W11", EPatternType.HeadAndShoulders);
      htPatterns["34251"] = new PatternInfo("W12", EPatternType.Broadening);
      htPatterns["35142"] = new PatternInfo("W13", EPatternType.HeadAndShoulders);
      htPatterns["35241"] = new PatternInfo("W14", EPatternType.UpTrend);
      htPatterns["45132"] = new PatternInfo("W15", EPatternType.HeadAndShoulders);
      htPatterns["45231"] = new PatternInfo("W16", EPatternType.UpTrend);

      foreach (string key in htPatterns.Keys) {
        if (!key.Contains("1")) Console.WriteLine("{0} missing 1", key);
        if (!key.Contains("2")) Console.WriteLine("{0} missing 2", key);
        if (!key.Contains("3")) Console.WriteLine("{0} missing 3", key);
        if (!key.Contains("4")) Console.WriteLine("{0} missing 4", key);
        if (!key.Contains("5")) Console.WriteLine("{0} missing 5", key);
      }
    }

    public MerrillPattern() {
    }

    public string Classify( double p1, double p2, double p3, double p4, double p5 ) {
      SortedList sl = new SortedList(5);

      bool ok = true;
      try {
        sl.Add(p1, "1");
        sl.Add(p2, "2");
        sl.Add(p3, "3");
        sl.Add(p4, "4");
        sl.Add(p5, "5");
      }
      catch {
        ok = false;
      }
      if (ok) {
        string key = (string)sl.GetByIndex(4)
          + (string)sl.GetByIndex(3)
          + (string)sl.GetByIndex(2)
          + (string)sl.GetByIndex(1)
          + (string)sl.GetByIndex(0);
        if (htPatterns.ContainsKey(key)) {
          PatternInfo pi = (PatternInfo)htPatterns[key];
          Console.WriteLine("{0} Pattern {1} {2}", Clock.Now, pi.PatternId, pi.PatternType);
          return pi.PatternId;
        }
        else {
          //Console.WriteLine( "{0} Pattern {1} not found", Clock.Now, key );
          return "";
        }
      }
      else {
        return "";
      }
    }

    //public string ClassifyDoubleSeriesEnd( DoubleSeries ds ) {
    //  if (ds.Count >= 5) {
    //    return Classify(ds.Ago(4), ds.Ago(3), ds.Ago(2), ds.Ago(1), ds.Last);
    //  }
    //  else return "";
    //}
  }

  #endregion Pattern Analysis

}

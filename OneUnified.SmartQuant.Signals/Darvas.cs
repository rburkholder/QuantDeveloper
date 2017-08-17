//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.ComponentModel;
//using System.Drawing;
using System.Collections;

using SmartQuant.Data;
//using SmartQuant.Series;
//using SmartQuant.Indicators;
//using SmartQuant.Instruments;
//using SmartQuant.Trading;
//using SmartQuant.Optimization;
//using SmartQuant.Charting;

namespace OneUnified.SmartQuant.Signals {

	/// <summary>
	/// Summary description for Darvas.
	/// </summary>
	/// 

  /*

public class CustomDS: DoubleSeries {

  public override void Paint(Pad Pad, double MinX, double MaxX, double MinY, double MaxY) {
    //Console.WriteLine( "minx {0} maxx {1} miny {2} maxy {3}", 
      //new DateTime( (long) MinX) , new DateTime( (long) MaxX ), MinY, MaxY );
    for ( int i = FirstIndex; i < LastIndex; i++ ) {
        Pad.DrawLine( new Pen( Color ), 
        (double) DataSeries.DateTimeAt( i ).Ticks, this[ i ],
        (double) DataSeries.DateTimeAt( i + 1 ).Ticks, this[ i + 1 ] );
    }
  }

  public override void Draw() {
    //Console.WriteLine( "Draw" );
    base.Draw ();
  }

  public override void Draw(string option) {
    //Console.WriteLine( "Draw Option {0}", option );
    base.Draw (option);
  }

  public override void Draw(string option, Color color) {
    //Console.WriteLine( "Draw option, Color" );
    //Color.
    base.Draw (option, color);
  }

  public override void Draw(Color color) {
    //Console.WriteLine( "Draw Color" );
    base.Draw (color);
  }
}
   */

  public class Darvas {
		bool bTop = false;
		bool bBottom = false;
		bool bSignalBuy = false;
		bool bSignalSetStop = false;
		bool bSignalExit = false;
		bool bSignalDone = false;
		int cntTop = 0;
		int cntBottom = 0;
		double dblTop = 0.0;
		double dblBottom = 0.0;
		double dblStop = 0.0;
		double dblStopStep;
		double dblGhostTop;

		bool bDebug = false;

		//CustomDS dsTop = null;
		//CustomDS dsBottom = null;
		//CustomDS dsStop = null;

		public bool Debug {
			get { return bDebug; }
			set { bDebug = value; }
		}

		public bool SignalBuy {
			get { if ( bSignalBuy ) {
							bSignalBuy = false;
							return true;
						}
						else {
							return false;
						}
			}
		}

		public bool SignalSetStop {
			get { if ( bSignalSetStop ) {
							bSignalSetStop = false;
							return true;
						}
						else {
							return false;
						}
			}
		}

		public bool SignalExit {
			get { if ( bSignalExit ) {
							bSignalExit = false;
							return true;
						}
						else {
							return false;
						}
			}
		}

		public double StopLevel {
			get { return dblStop; }
		}

		public bool SignalDone {
			get { return bSignalDone; }
		}

    public Darvas() {
      // No DoubleSeries used for DataCenter manipulation
    }

    /*
		public Darvas( CustomDS dsTop, CustomDS dsBottom, CustomDS dsStop ) {
			//
			// TODO: Add constructor logic here
			//
			//dsTop = new CustomDS();
			//dsBottom = new CustomDS();

			this.dsTop = dsTop;
			this.dsBottom = dsBottom;
			this.dsStop = dsStop;

			//dsTop.Draw();
			//dsBottom.Draw();

		}
     */

		public void Calc( Bar bar ) {

			// Calculate Darvis Box
			if ( bar.High <= dblTop ) {
				cntTop++;
				if ( 4 == cntTop ) { // we've reached the four day price pattern
					bTop = true;
				}
			}
			else { // top of box has been exceeded
				// perform trade if box completed
				if ( bTop && bBottom ) {  // we have a completed box
					if ( ( bar.Close > dblTop ) && ( bar.Close > dblStop ) ) {
						bSignalBuy = true;
						// calculate a new ghost box
						dblStop = dblTop;
						dblGhostTop = dblStop + dblStopStep;
						bSignalSetStop = true;
					}
				}
				// restart box calculation on new high
				dblTop = bar.High;
				dblBottom = bar.Low;
				cntTop = 1;
				cntBottom = 0;
				bTop = false;
				bBottom = false;
			}

			// calculate bottom of box
			if ( bBottom ) {
				if ( bar.Close < dblBottom ) {
					bSignalExit = true;
					bSignalDone = true;
				}
				else {
					cntBottom++;
					if ( ( 4 < cntBottom ) && ( bar.Close > dblStop ) && ( bar.Open < bar.Close ) ) {
						bSignalBuy = true;  // **
					}
				}

			}
			else { // see if can set the bottom yet
				if ( bar.Low >= dblBottom ) { // higher low
					cntBottom++;
					if ( 4 == cntBottom ) {
						bBottom = true;
						dblStop = Math.Max( dblBottom, dblStop );  // **
						//dblStop = dblBottom;
						dblStopStep = dblTop - dblBottom;
						dblGhostTop = dblTop;
						bSignalSetStop = true;
					}
				}
				else { // lower low so reset
					dblBottom = bar.Low;
					cntBottom = 1;
				}
			}

			// calculate Ghost box if no active box
			if ( !bTop && !bBottom && ( 0.0 < dblStop ) ) {
				if ( bar.Close > dblGhostTop ) {
					dblStop = dblGhostTop;
					dblGhostTop += dblStopStep;
					bSignalSetStop = true;
				}
			}


			//if ( null != dsTop) dsTop.Add( bar.DateTime, dblTop );
			//if ( null != dsBottom ) dsBottom.Add( bar.DateTime, dblBottom );
			//if ( 0.0 != dblStop ) if ( null != dsStop ) dsStop.Add( bar.DateTime, dblStop );
			if ( bDebug ) Console.WriteLine( "{0} cT {1} cB {2} stop {3} Top {4} Bottom {5} GhostTop {6} StopStep {7}", 
			  bar.DateTime, cntTop, cntBottom, dblStop, dblTop, dblBottom, dblGhostTop, dblStopStep );
		}

	}
}

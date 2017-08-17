//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Data;
using System.Reflection;
using System.Data.SqlClient;

using OneUnified.SmartQuant;

using SmartQuant.Series;
using SmartQuant.Data;

namespace OneUnified.SmartQuant.Signals {

  public class PivotSet {

    public string Name;
    public double R3;
    public double R2;
    public double R1;
    public double PV;
    public double S1;
    public double S2;
    public double S3;

    public PivotSet( string Name, double S3, double S2, double S1, double PV, double R1, double R2, double R3 ) {
      this.Name = Name;
      this.R3 = R3;
      this.R2 = R2;
      this.R1 = R1;
      this.PV = PV;
      this.S1 = S1;
      this.S2 = S2;
      this.S3 = S3;
    }
  }

  public class Pivots {

    // from database
    double dayhi;
    double daylo;
    double daycl;

    double day3hi;
    double day3lo;

    double weekhi;
    double weeklo;
    double weekcl;

    double monhi;
    double monlo;
    double moncl;

    public double sma20day;
    public double sma200day;

    public double sixmonposmean;
    public double sixmonpossd;
    public double sixmonnegmean;
    public double sixmonnegsd;

    // misc
    //double open;

    // calculated
    double dayR3;
    double dayR2;
    double dayR1;
    double dayPv;
    double dayS1;
    double dayS2;
    double dayS3;

    double day3R3;
    double day3R2;
    double day3R1;
    double day3Pv;
    double day3S1;
    double day3S2;
    double day3S3;

    double weekR3;
    double weekR2;
    double weekR1;
    double weekPv;
    double weekS1;
    double weekS2;
    double weekS3;

    double monR3;
    double monR2;
    double monR1;
    double monPv;
    double monS1;
    double monS2;
    double monS3;

    #region TheSeries
    DoubleSeries dsOpen;

    DoubleSeries dsDayCl;
    DoubleSeries dsDayR3;
    DoubleSeries dsDayR2;
    DoubleSeries dsDayR1;
    DoubleSeries dsDayPv;
    DoubleSeries dsDayS1;
    DoubleSeries dsDayS2;
    DoubleSeries dsDayS3;

    DoubleSeries dsWeekCl;
    DoubleSeries dsWeekR3;
    DoubleSeries dsWeekR2;
    DoubleSeries dsWeekR1;
    DoubleSeries dsWeekPv;
    DoubleSeries dsWeekS1;
    DoubleSeries dsWeekS2;
    DoubleSeries dsWeekS3;

    DoubleSeries dsMonCl;
    DoubleSeries dsMonR3;
    DoubleSeries dsMonR2;
    DoubleSeries dsMonR1;
    DoubleSeries dsMonPv;
    DoubleSeries dsMonS1;
    DoubleSeries dsMonS2;
    DoubleSeries dsMonS3;

    DoubleSeries dsSma20Day;
    DoubleSeries dsSma200Day;
    #endregion TheSeries

    double pivotHigh;
    double pivotLow;
    double pivotClose;
    bool pivotDraw = false;
    DoubleSeries dsPivot;
    DoubleSeries dsPivotR1;
    DoubleSeries dsPivotR2;
    DoubleSeries dsPivotS1;
    DoubleSeries dsPivotS2;

    double pivot;
    double pivotR1;
    double pivotR2;
    double pivotS1;
    double pivotS2;

    public PivotSet psDay;
    public PivotSet psDay3;
    public PivotSet psWeek;
    public PivotSet psMonth;
	


    public Pivots( string Symbol, string TradeSystem ) {
      TradeDB db1 = new TradeDB();
      db1.Open();

      string sql = "select dayhi, daylo, daycl, day3hi, day3lo, weekhi, weeklo, weekcl, monhi, monlo, moncl, "
        + " sma20day, sma200day, sixmonposmean, sixmonpossd, sixmonnegmean, sixmonnegsd"
        + " from totrade where tradesystem='" + TradeSystem + "' and symbol = '" + Symbol + "'";
      SqlCommand cmd = new SqlCommand(sql, db1.Connection);

      SqlDataReader dr;
      dr = cmd.ExecuteReader();
      if (dr.HasRows) {
        while (dr.Read()) {
          dayhi = dr.GetDouble(0);
          daylo = dr.GetDouble(1);
          daycl = dr.GetDouble(2);
          day3hi = dr.GetDouble(3);
          day3lo = dr.GetDouble(4);
          weekhi = dr.GetDouble(5);
          weeklo = dr.GetDouble(6);
          weekcl = dr.GetDouble(7);
          monhi = dr.GetDouble(8);
          monlo = dr.GetDouble(9);
          moncl = dr.GetDouble(10);
          sma20day = dr.GetDouble(11);
          sma200day = dr.GetDouble(12);
          sixmonposmean = dr.GetDouble(13);
          sixmonpossd = dr.GetDouble(14);
          sixmonnegmean = dr.GetDouble(15);
          sixmonnegsd = dr.GetDouble(16);
        }
      }

      dr.Close();
      db1.Close();

      dayPv = (dayhi + daylo + daycl) / 3;
      dayR1 = 2 * dayPv - daylo;
      dayR2 = dayPv + (dayhi - daylo);
      dayR3 = dayR1 + (dayhi - daylo);
      dayS1 = 2 * dayPv - dayhi;
      dayS2 = dayPv - (dayhi - daylo);
      dayS3 = dayS1 - (dayhi - daylo);
      psDay = new PivotSet("day", dayS3, dayS2, dayS1, dayPv, dayR1, dayR2, dayR3);

      day3Pv = (day3hi + day3lo + daycl) / 3;
      day3R1 = 2 * day3Pv - day3lo;
      day3R2 = day3Pv + (day3hi - day3lo);
      day3R3 = day3R1 + (day3hi - day3lo);
      day3S1 = 2 * day3Pv - day3hi;
      day3S2 = day3Pv - (day3hi - day3lo);
      day3S3 = day3S1 - (day3hi - day3lo);
      psDay3 = new PivotSet("day3", day3S3, day3S2, day3S1, day3Pv, day3R1, day3R2, day3R3);

      weekPv = (weekhi + weeklo + weekcl) / 3;
      weekR1 = 2 * weekPv - weeklo;
      weekR2 = weekPv + (weekhi - weeklo);
      weekR3 = weekR1 + (weekhi - weeklo);
      weekS1 = 2 * weekPv - weekhi;
      weekS2 = weekPv - (weekhi - weeklo);
      weekS3 = weekS1 - (weekhi - weeklo);
      psWeek = new PivotSet("week", weekS3, weekS2, weekS1, weekPv, weekR1, weekR2, weekR3);

      monPv = (monhi + monlo + moncl) / 3;
      monR1 = 2 * monPv - monlo;
      monR2 = monPv + (monhi - monlo);
      monR3 = monR1 + (monhi - monlo);
      monS1 = 2 * monPv - monhi;
      monS2 = monPv - (monhi - monlo);
      monS3 = monS1 - (monhi - monlo);
      psMonth = new PivotSet("mon", monS3, monS2, monS1, monPv, monR1, monR2, monR3);

      // Pivot Information
      if (pivotDraw) {
        pivot = (pivotHigh + pivotLow + pivotClose) / 3.0;
        //dsPivot = new DoubleSeries( "Pivot" );
        pivotR1 = 2.0 * pivot - pivotLow;
        pivotS1 = 2.0 * pivot - pivotHigh;
        pivotR2 = pivotR1 + pivot - pivotS1;
        pivotS2 = pivot - pivotR1 - pivotS1;
        Console.WriteLine("pivot {0} r1 {1} r2 {2} s1 {3} s2 {4}",
          pivot, pivotR1, pivotR2, pivotS1, pivotS2);
      }
		

    }

    public void PresentSeries() {
      //Draw(dsOpen, 0);

      //Draw(dsDayCl, 0);
      //Draw(dsDayR3, 0);
      //Draw(dsDayR2, 0);
      //Draw(dsDayR1, 0);
      //Draw(dsDayPv, 0);
      //Draw(dsDayS1, 0);
      //Draw(dsDayS2, 0);
      //Draw(dsDayS3, 0);

      //Draw(dsWeekCl, 0);
      //Draw(dsWeekPv, 0);
      //Draw(dsWeekR1, 0);
      //Draw(dsWeekS1, 0);

      //Draw(dsMonCl, 0);
      //Draw(dsMonPv, 0);
      //Draw(dsMonR1, 0);
      //Draw(dsMonS1, 0);

      //Draw(dsSma20Day, 0);
      //Draw(dsSma200Day, 0);

    }

    public void CreateSeries() {
      dsOpen = new DoubleSeries("open ");

      dsDayCl = new DoubleSeries("day cls " + daycl.ToString("#.00"));
      dsDayPv = new DoubleSeries("day pv " + dayPv.ToString("#.00"));
      dsDayR1 = new DoubleSeries("day r1 " + dayR1.ToString("#.00"));
      dsDayR2 = new DoubleSeries("day r2 " + dayR2.ToString("#.00"));
      dsDayR3 = new DoubleSeries("day r3 " + dayR3.ToString("#.00"));
      dsDayS1 = new DoubleSeries("day s1 " + dayS1.ToString("#.00"));
      dsDayS2 = new DoubleSeries("day s2 " + dayS2.ToString("#.00"));
      dsDayS3 = new DoubleSeries("day s3 " + dayS3.ToString("#.00"));

      dsWeekCl = new DoubleSeries("week cls " + weekcl.ToString("#.00"));
      dsWeekPv = new DoubleSeries("week pv " + weekPv.ToString("#.00"));
      dsWeekR1 = new DoubleSeries("week r1" + weekR1.ToString("#.00"));
      dsWeekR2 = new DoubleSeries("week r2 " + weekR2.ToString("#.00"));
      dsWeekR3 = new DoubleSeries("week r3 " + weekR3.ToString("#.00"));
      dsWeekS1 = new DoubleSeries("week s1 " + weekS1.ToString("#.00"));
      dsWeekS2 = new DoubleSeries("week s2 " + weekS2.ToString("#.00"));
      dsWeekS3 = new DoubleSeries("week s3 " + weekS3.ToString("#.00"));

      dsMonCl = new DoubleSeries("mon cls " + moncl.ToString("#.00"));
      dsMonPv = new DoubleSeries("mon pv " + monPv.ToString("#.00"));
      dsMonR1 = new DoubleSeries("mon r1 " + monR1.ToString("#.00"));
      dsMonR2 = new DoubleSeries("mon r2 " + monR2.ToString("#.00"));
      dsMonR3 = new DoubleSeries("mon r3 " + monR3.ToString("#.00"));
      dsMonS1 = new DoubleSeries("mon s1 " + monS1.ToString("#.00"));
      dsMonS2 = new DoubleSeries("mon s2 " + monS2.ToString("#.00"));
      dsMonS3 = new DoubleSeries("mon s3 " + monS3.ToString("#.00"));

      dsSma20Day = new DoubleSeries("sma 20 " + sma20day.ToString("#.00"));
      dsSma200Day = new DoubleSeries("sma 200 " + sma200day.ToString("#.00"));

      dsOpen.Color = Color.Violet;

      dsDayCl.Color = Color.Black;
      dsDayPv.Color = Color.Green;
      dsDayR1.Color = Color.Blue;
      dsDayR2.Color = Color.Red;
      dsDayR3.Color = Color.Orange;
      dsDayS1.Color = Color.Blue;
      dsDayS2.Color = Color.Red;
      dsDayS3.Color = Color.Beige;

      dsWeekCl.Color = Color.Black;
      dsWeekPv.Color = Color.Green;
      dsWeekR1.Color = Color.Blue;
      dsWeekS1.Color = Color.Blue;

      dsMonCl.Color = Color.Black;
      dsMonPv.Color = Color.Green;
      dsMonR1.Color = Color.Blue;
      dsMonS1.Color = Color.Blue;

      dsSma20Day.Color = Color.DarkSlateBlue;
      dsSma200Day.Color = Color.DarkSeaGreen;

    }

    public void Update( Bar bar ) {

      dsDayCl.Add(bar.DateTime, daycl);
      dsDayPv.Add(bar.DateTime, dayPv);
      dsDayR1.Add(bar.DateTime, dayR1);
      dsDayR2.Add(bar.DateTime, dayR2);
      dsDayR3.Add(bar.DateTime, dayR3);
      dsDayS1.Add(bar.DateTime, dayS1);
      dsDayS2.Add(bar.DateTime, dayS2);
      dsDayS3.Add(bar.DateTime, dayS3);

      dsWeekPv.Add(bar.DateTime, weekPv);
      dsWeekR1.Add(bar.DateTime, weekR1);
      dsWeekR2.Add(bar.DateTime, weekR2);
      dsWeekR3.Add(bar.DateTime, weekR3);
      dsWeekS1.Add(bar.DateTime, weekS1);
      dsWeekS2.Add(bar.DateTime, weekS2);
      dsWeekS3.Add(bar.DateTime, weekS3);

      dsMonPv.Add(bar.DateTime, monPv);
      dsMonR1.Add(bar.DateTime, monR1);
      dsMonR2.Add(bar.DateTime, monR2);
      dsMonR3.Add(bar.DateTime, monR3);
      dsMonS1.Add(bar.DateTime, monS1);
      dsMonS2.Add(bar.DateTime, monS2);
      dsMonS3.Add(bar.DateTime, monS3);

      dsSma20Day.Add(bar.DateTime, sma20day);
      dsSma200Day.Add(bar.DateTime, sma200day);

    }

  }
}

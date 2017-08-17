//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using SmartQuant.Series;
using SmartQuant.Charting;
using SmartQuant.Data;

namespace OneUnified.SmartQuant {

  #region VolumeInIntervalTracking

  public class VolumeTracking {

    SortedList slPriceAtVolume;
    SortedList slVolumeAtPrice;

    public VolumeTracking() {
      slPriceAtVolume = new SortedList(100);
      slVolumeAtPrice = new SortedList(100);
    }

  }

  #endregion VolumeInIntervalTracking

  #region RunningMinMax

  public class PointStat {

    public int PriceCount = 0;  // # of objects at this price point
    //public int PriceVolume = 0;  // how much volume at this price point

    public PointStat() {
      PriceCount++;
    }
  }

  public class RunningMinMax {

    // keeps track of Min and Max value over selected duration

    public double Max = 0;
    public double Min = 0;

    protected SortedList slPoints;  // holds array of stats per price point
    //protected int PointCount = 0;

    public RunningMinMax() {
      slPoints = new SortedList(500);
    }

    protected virtual void AddPoint( double val ) {
      //qPoints.Enqueue( val );
      if (slPoints.ContainsKey(val)) {
        PointStat ps = (PointStat)slPoints[val];
        ps.PriceCount++;
      }
      else {
        slPoints.Add(val, new PointStat());
        Max = (double)slPoints.GetKey(slPoints.Count - 1);
        Min = (double)slPoints.GetKey(0);
      }
      //PointCount++;
    }

    protected virtual void RemovePoint( double val ) {
      //double t = (double) qPoints.Dequeue();
      if (slPoints.ContainsKey(val)) {
        PointStat ps = (PointStat)slPoints[val];
        ps.PriceCount--;
        if (0 == ps.PriceCount) {
          slPoints.Remove(val);
          if (0 < slPoints.Count) {
            Min = (double)slPoints.GetKey(0);
            Max = (double)slPoints.GetKey(slPoints.Count - 1);
            //Console.Write( "  Min {0:#.00} Max {1:#.00}", Min, Max );
          }
        }
        //PointCount--;
      }
      else {
        throw new Exception("slPoints doesn't have a point to remove");
      }
    }

  }

  #endregion RunningMinMax

  #region AccumulationGroup

  #region RunningStats

  public class RunningStats {

    public double b2 = 0; // acceleration
    public double b1 = 0; // slope
    public double b0 = 0; // offset

    public double meanY;

    public double RR;
    public double R;

    public double SD;

    protected double SumXX = 0.0;
    protected double SumX = 0.0;
    protected double SumXY = 0.0;
    protected double SumY = 0.0;
    protected double SumYY = 0.0;

    protected double Sxx;
    protected double Sxy;
    protected double Syy;

    protected double SST;
    protected double SSR;
    protected double SSE;

    public double BBUpper;
    public double BBLower;

    protected int Xcnt = 0;

    protected int BBMultiplier = 2;

    private bool CanCalcSlope = false;

    public RunningStats() {
    }

    public void Add( double x, double y ) {
      //Console.WriteLine( "add,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
      SumXX += x * x;
      SumX += x;
      SumXY += x * y;
      SumY += y;
      SumYY += y * y;
      Xcnt++;
    }

    public void Remove( double x, double y ) {
      //Console.WriteLine( "rem,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
      SumXX -= x * x;
      SumX -= x;
      SumXY -= x * y;
      SumY -= y;
      SumYY -= y * y;
      Xcnt--;

      CanCalcSlope = true;
    }

    public int BollingerMultiplier {
      get { return BBMultiplier; }
      set { BBMultiplier = value; }
    }

    public virtual void CalcStats() {

      if (Xcnt > 1) {

        double oldb1 = b1;

        Sxx = SumXX - SumX * SumX / Xcnt;
        Sxy = SumXY - SumX * SumY / Xcnt;
        Syy = SumYY - SumY * SumY / Xcnt;

        SST = Syy;
        SSR = Sxy * Sxy / Sxx;
        SSE = SST - SSR;

        RR = SSR / SST;
        R = Sxy / Math.Sqrt(Sxx * Syy);

        //SD = Math.Sqrt(Syy / (Xcnt - 1));
        SD = Math.Sqrt(Syy / Xcnt);

        meanY = SumY / Xcnt;

        double BBOffset = BBMultiplier * SD;
        BBUpper = meanY + BBOffset;
        BBLower = meanY - BBOffset;

        b1 = CanCalcSlope ? Sxy / Sxx : 0;
        b0 = (1 / Xcnt) * (SumY - b1 * SumX);
        b2 = b1 - oldb1;  // *** do this differently
      }
    }
  }

  #endregion RunningStats

  #region Accumulation

  // turn all this into a template<> based method to maintain strong typing?
  public class ObjectAtTime {

    private DateTime dt;
    private object o;
    private LinkedListNode<ObjectAtTime> node;

    public ObjectAtTime( DateTime DateTime, object o ) {
      this.dt = DateTime;
      this.o = o;
    }

    public DateTime DateTime {
      get { return this.dt; }
    }

    public object Object {
      get { return this.o; }
    }

    public LinkedListNode<ObjectAtTime> Node {
      get { return this.node; }
      set { this.node = value; }
    }
  }

  public class SlidingWindow {

    protected int WindowSizeCount = 0;
    protected int WindowSizeSeconds = 0;
    protected TimeSpan tsWindowSize;

    protected LinkedList<ObjectAtTime> values;
    protected DateTime dtLast;

    // put in lock variable, method
    // member variables are protected because inheritance is required to properly handle the Remove override

    public SlidingWindow( int WindowSizeSeconds, int WindowSizeCount ) {
      Init(WindowSizeSeconds, WindowSizeCount);
    }

    public SlidingWindow( int WindowSizeSeconds ) {
      Init(WindowSizeSeconds, 0);
    }

    private void Init( int WindowSizeSeconds, int WindowSizeCount ) {
      this.WindowSizeCount = WindowSizeCount;
      this.WindowSizeSeconds = WindowSizeSeconds;
      if (0 < WindowSizeSeconds) {
        this.tsWindowSize = new TimeSpan(0, 0, WindowSizeSeconds);
      }
      values = new LinkedList<ObjectAtTime>();
    }

    protected ObjectAtTime Add( DateTime dt, object o ) {
      ObjectAtTime oat = new ObjectAtTime(dt, o);
      values.AddLast(oat);
      LinkedListNode<ObjectAtTime> node = values.Last;
      oat.Node = node;
      dtLast = dt;
      return oat;
    }

    protected virtual ObjectAtTime Remove() {
      ObjectAtTime oat = values.First.Value;
      values.RemoveFirst();
      return oat;
    }

    protected ObjectAtTime Remove( LinkedListNode<ObjectAtTime> node ) {

      values.Remove(node);

      ObjectAtTime oat = node.Value;
      return oat;
    }

    protected void UpdateWindow() {

      if (0 < values.Count) {
        bool bDone;

        // Time Based decimation
        if (0 < WindowSizeSeconds) {

          DateTime dtPurgePrior = dtLast - tsWindowSize;
          bDone = false;

          while (!bDone) {
            DateTime dt = values.First.Value.DateTime;
            if (dt < dtPurgePrior) {
              Remove();
              if (0 == values.Count) bDone = true;
            }
            else {
              bDone = true;
            }
          }
        }

        // Count based decimation
        if (0 < WindowSizeCount) {
          while (WindowSizeCount < values.Count) {
            Remove();
          }
        }
      }
    }
  }

  public class SimpleAccumulationStats : SlidingWindow {

    protected long firstTimeTick = 0;  // use as offset, or bias for time calc in, set in inheriting class
    protected double tps = (double)TimeSpan.TicksPerSecond;

    protected RunningStats rs;
    protected string sName;

    protected bool NeedToCalcStats;

    public SimpleAccumulationStats( string Name, int WindowSizeSeconds, int WindowSizeCount )
      : base(WindowSizeSeconds, WindowSizeCount) {

      rs = new RunningStats();
      NeedToCalcStats = false;
      this.sName = Name;
    }

    public string Name {
      get { return sName; }
    }

    public double MeanY {
      get { 
        if (NeedToCalcStats) CalcStats();
        return rs.meanY; 
      }
    }

    public double Offset {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.b0;
      }
    }

    public double Slope {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.b1;
      }
    }

    public double Accel {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.b2;
      }
    }

    public double RR {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.RR;
      }
    }

    public double R {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.R;
      }
    }

    public double SD {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.SD;
      }
    }

    public double BollingerUpper {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.BBUpper;
      }
    }

    public double BollingerLower {
      get {
        if (NeedToCalcStats) CalcStats();
        return rs.BBLower;
      }
    }

    public virtual void Add( DateTime dt, double val ) {

      if (0 == base.values.Count) {
        firstTimeTick = dt.Ticks;
      }

      base.Add(dt, val);
      //values.AddLast(new ObjectAtTime(dt, o));
      //dtLast = dt;
      double t = (double)(dt.Ticks - firstTimeTick) / tps;
      rs.Add(t, val);
      NeedToCalcStats = true;
    }

    protected override ObjectAtTime Remove() {
      //Console.WriteLine( "sas remove {0}", Name ); 
      ObjectAtTime oat = base.Remove();
      double t = (double)(oat.DateTime.Ticks - firstTimeTick) / tps;
      rs.Remove(t, (double)oat.Object);
      return oat;
      //NeedToCalcStats = true;
    }

    protected void CalcStats() {
      base.UpdateWindow();
      rs.CalcStats();
      NeedToCalcStats = false;
      //dsAccel.Add( dtLast, b2 * 10000.0 );
      //dsSlope.Add(dt, rs.b1);
      //dsRR.Add(dt, rs.RR);
      //dsSD.Add( dtLast, SD );
      //dsAvg.Add(dt, rs.meanY);
    }
  }

  public class SimpleAccumulationStatsOnQuotes : SimpleAccumulationStats {

    public SimpleAccumulationStatsOnQuotes( string Name, int WindowSizeSeconds, int WindowSizeCount )
      : base(Name, WindowSizeSeconds, WindowSizeCount) {
    }

    public void Add( Quote quote ) {

      DateTime dt = quote.DateTime;

      if (0 == base.values.Count) {
        firstTimeTick = dt.Ticks;
      }

      //base.Add(dt, quote);
      values.AddLast(new ObjectAtTime(dt, quote));
      dtLast = dt;
      double t = (double)(dt.Ticks - firstTimeTick) / tps;

      rs.Add(t, quote.Ask);
      rs.Add(t, quote.Bid);
      //if ( Name == "768s" ) {
        //Console.WriteLine("avg {0} {1:0.00} {2:0.00} {3:0.00}", rs.Xcnt, rs.SumY, rs.SumY/rs.Xcnt, rs.meanY);
      //}
      NeedToCalcStats = true;
    }

    protected override ObjectAtTime Remove() {
      //ObjectAtTime oat = base.Remove();
      //Console.WriteLine("proper remove");
      //Console.WriteLine( "sasoq remove {0}", Name ); 
      ObjectAtTime oat = values.First.Value;
      values.RemoveFirst();
      double t = (double)(oat.DateTime.Ticks - firstTimeTick) / tps;
      Quote quote = (Quote)oat.Object;
      rs.Remove(t, quote.Ask);
      rs.Remove(t, quote.Bid);
      return oat;
    }
  }

  public class Accumulation: SlidingWindow {

    Color color;

    protected long firstTimeTick = 0;  // use as offset, or bias for time calc in, set in inheriting class
    private double tps = (double)TimeSpan.TicksPerSecond;

    protected RunningStats rs;
    public DoubleSeries dsSlope;
    public DoubleSeries dsRR;
    public DoubleSeries dsAvg;

    protected Accumulation enclosingAccumulation = null;

    public Accumulation EnclosingAccumulation {
      set { enclosingAccumulation = value; }
    }

    public Accumulation(
      string Name, Color color, int WindowSizeSeconds, int WindowSizeCount )
      : base( WindowSizeSeconds, WindowSizeCount ) {

      this.color = color;

      rs = new RunningStats();

      dsSlope = new DoubleSeries("b1 " + Name);
      dsSlope.Color = color;

      dsRR = new DoubleSeries("rr " + Name);
      dsRR.Color = color;

      dsAvg = new DoubleSeries("avg " + Name);
      dsAvg.SecondColor = Color.Purple;
      dsAvg.Color = color;
    }

    public virtual void Add( DateTime dt, double val ) {
      base.Add(dt, val);
      //values.AddLast(new ValueAtTime(dt, val));
      //dtLast = dt;
      double t = (double)(dt.Ticks - firstTimeTick) / tps;
      rs.Add(t, val);
    }

    protected override ObjectAtTime Remove() {
      ObjectAtTime oat = base.Remove();
      double t = (double)(oat.DateTime.Ticks - firstTimeTick) / tps;
      rs.Remove(t, (double) oat.Object);
      return oat;
    }

    protected void CalcStats( DateTime dt ) { 
      rs.CalcStats();
      //dsAccel.Add( dtLast, b2 * 10000.0 );
      dsSlope.Add(dt, rs.b1);
      dsRR.Add(dt, rs.RR);
      //dsSD.Add( dtLast, SD );
      dsAvg.Add(dt, rs.meanY);
    }

  }

  public class AccumulateValues : Accumulation {

    public AccumulateValues( string Name, Color color, int WindowSizeSeconds )
      : base(Name, color, WindowSizeSeconds, 0) {
    }

    public override void Add( DateTime dt, double val ) {
      if (0 == values.Count) {
        firstTimeTick = dt.Ticks;
      } 
      base.Add(dt, val);
      UpdateWindow();
      CalcStats( dt );
    }
  }

  public class AccumulateQuotes : Accumulation {

    //private QuoteArray quotes;
    private TimeSpan ms;
    private DateTime dtUnique;

    public bool CalcTrade;

    protected double BBMultiplier;
    public DoubleSeries dsBBUpper;
    public DoubleSeries dsBBLower;
    public DoubleSeries dsB;
    public DoubleSeries dsBandwidth;

    private AccumulateValues slopeAvg;
    public DoubleSeries dsSlopeAvg;
    private AccumulateValues accelAvg;
    public DoubleSeries dsAccelAvg;

    private double m_SlopeAvgScaleMin = 0;
    private double m_SlopeAvgScaleMax = 0;

    private double m_bbwMin = 0;
    private double m_bbwMax = 0;

    private double m_AccelAvgScaleMin = 0;
    private double m_AccelAvgScaleMax = 0;

    public AccumulateQuotes( string Name, int WindowSizeTime, int WindowSizeCount,
        double BBMultiplier, bool CalcTrade, Color color )
      :
      base(Name, color, WindowSizeTime, WindowSizeCount) {

      this.CalcTrade = CalcTrade;
      ms = new TimeSpan(0, 0, 0, 0, 1);

      this.BBMultiplier = BBMultiplier;

      // see page 157 in Bollinger on Bollinger Bands

      slopeAvg = new AccumulateValues("slope(avg) " + Name, color, WindowSizeTime / 4);
      //dsSlopeAvg = slopeAvg.dsSlope;
      dsSlopeAvg = new DoubleSeries("slope(avg) " + Name);
      dsSlopeAvg.Color = color;
      slopeAvg.dsSlope.ItemAdded += new ItemAddedEventHandler(slopeItemAddedEventHandler);

      accelAvg = new AccumulateValues("accel(avg) " + Name, color, WindowSizeTime / 16);
      dsAccelAvg = new DoubleSeries("accel(avg) " + Name);
      dsAccelAvg.Color = color;
      accelAvg.dsSlope.ItemAdded += new ItemAddedEventHandler(accelItemAddedEventHandler);

      dsBBUpper = new DoubleSeries("bbu " + Name);
      dsBBUpper.Color = color;

      dsBBLower = new DoubleSeries("bbl " + Name);
      dsBBLower.Color = color;

      dsB = new DoubleSeries("%b " + Name);
      dsB.Color = color;

      dsBandwidth = new DoubleSeries("bbw " + Name);
      dsBandwidth.Color = color;

    }

    void slopeItemAddedEventHandler( object sender, DateTimeEventArgs e ) {
      double val = (slopeAvg.dsSlope.Last - m_SlopeAvgScaleMin) / (m_SlopeAvgScaleMax - m_SlopeAvgScaleMin);
      dsSlopeAvg.Add(e.DateTime, val);
      accelAvg.Add(e.DateTime, val);
    }

    void accelItemAddedEventHandler( object sender, DateTimeEventArgs e ) {
      double val = accelAvg.dsSlope.Last;
      double tmp = (val - m_AccelAvgScaleMin) / (m_AccelAvgScaleMax - m_AccelAvgScaleMin);
      tmp = Math.Min(tmp, 1.25);
      tmp = Math.Max(tmp, -0.25);
      dsAccelAvg.Add(e.DateTime, tmp);
    }

    public double SlopeAvgScaleMin {
      get { return m_SlopeAvgScaleMin; }
      set { m_SlopeAvgScaleMin = value; }
    }

    public double SlopeAvgScaleMax {
      get { return m_SlopeAvgScaleMax; }
      set { m_SlopeAvgScaleMax = value; }
    }

    public double AccelAvgScaleMin {
      get { return m_AccelAvgScaleMin; }
      set { m_AccelAvgScaleMin = value; }
    }

    public double AccelAvgScaleMax {
      get { return m_AccelAvgScaleMax; }
      set { m_AccelAvgScaleMax = value; }
    }

    public double bbwMin {
      get { return m_bbwMin; }
      set { m_bbwMin = value; }
    }

    public double bbwMax {
      get { return m_bbwMax; }
      set { m_bbwMax = value; }
    }

    public void Add( Quote quote ) {
      if (0 == values.Count) {
        dtUnique = quote.DateTime;
        firstTimeTick = quote.DateTime.Ticks;
      }
      else {
        dtUnique = (quote.DateTime > dtUnique) ? quote.DateTime : dtUnique + ms;
      }
      Add(dtUnique, quote.Bid);
      Add(dtUnique, quote.Ask);

      //Console.WriteLine( "{0} Added {1:#.00} {2:#.00} {3:#.00} {4}", dt, val, Min, Max, dsPoints.Count );

      UpdateWindow();
      CalcStats( quote.DateTime );

      if (!double.IsNaN(rs.b1) && !double.IsPositiveInfinity(rs.b1) && !double.IsNegativeInfinity(rs.b1)) {

        slopeAvg.Add(dtUnique, rs.meanY);

        double upper = rs.meanY + BBMultiplier * rs.SD;
        double lower = rs.meanY - BBMultiplier * rs.SD;
        dsBBUpper.Add(dtUnique, upper);
        dsBBLower.Add(dtUnique, lower);

        double tmp = (quote.Bid + quote.Ask) / 2;
        double avgquote = 0;
        if (tmp == rs.meanY) {
          avgquote = tmp;
        }
        else {
          if (tmp > rs.meanY) avgquote = quote.Ask;
          if (tmp < rs.meanY) avgquote = quote.Bid;
        }
        dsB.Add(dtUnique, (avgquote - lower) / (upper - lower));
        double bw = 1000.0 * (upper - lower) / rs.meanY;
        dsBandwidth.Add(dtUnique, bw / m_bbwMax);
      }
      //Console.WriteLine( "{0} Added {1} {2} {3} {4} {5}", dtUnique, Xcnt, b1, b0, R, dsSlope.Name );
    }

  }

  #endregion Accumulation

  #endregion AccumulationGroup

}

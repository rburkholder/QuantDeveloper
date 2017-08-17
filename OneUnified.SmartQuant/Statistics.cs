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

using SmartQuant.Series;
using SmartQuant.Data;

namespace OneUnified.SmartQuant {
  public class Statistics {

    // Formulas from New Trading Systems and Methods, Kaufman, pg 27 - 34
    private DoubleSeries ds;  //
    private BarSeries bs;  // used in Min & Max only: keep it that way

    public Statistics( DoubleSeries ds, BarSeries bs ) {
      this.ds = ds;
      this.bs = bs;
    }

    public Statistics( DoubleSeries ds ) {
      this.ds = ds;
    }

    public double Average( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Average:  not enough values");
      double average = 0;
      for (int i = start; i <= ds.LastIndex; i++) {
        average += ds[i];
      }
      average = average / Count;
      return average;
    }

    public double MeanDeviation( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "MeanDeviation:  not enough values");
      double meandeviation = 0;
      double average = Average(Count);
      for (int i = start; i <= ds.LastIndex; i++) {
        meandeviation += Math.Abs( ds[i] - average );
      }
      meandeviation = meandeviation / Count;
      return meandeviation;
    }

    public double Variance( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Variance:  not enough values");
      double variance = 0;
      double average = Average(Count);
      for (int i = start; i <= ds.LastIndex; i++) {
        double tmp = ds[i] - average;
        variance += tmp * tmp;
      }
      variance = variance / ( Count - 1 );
      return variance;
    }

    public double StandardDeviation( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "StandardDeviation:  not enough values");
      double standarddeviation = 0;
      double variance = 0;
      double average = Average(Count);
      for (int i = start; i <= ds.LastIndex; i++) {
        double tmp = ds[i] - average;
        variance += tmp * tmp;
      }
      standarddeviation = Math.Sqrt(variance / Count);
      return standarddeviation;
    }

    public double Skewness( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Skewness:  not enough values");
      double skewness = 0;
      double average = Average(Count);
      double standarddeviation = StandardDeviation(Count);
      for (int i = start; i <= ds.LastIndex; i++) {
        double tmp1 = ((ds[i] - average) / standarddeviation);
        double tmp2 = Math.Pow(tmp1, 3.0);
        skewness += tmp2;
      }
      skewness = skewness * Count / ((Count - 1) * (Count - 2));
      return skewness;
    }

    public double Kurtosis( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Kurtosis:  not enough values");
      double kurtosis = 0;
      double average = Average(Count);
      double standarddeviation = StandardDeviation(Count);
      for (int i = start; i <= ds.LastIndex; i++) {
        double tmp1 = ((ds[i] - average) / standarddeviation);
        double tmp2 = Math.Pow(tmp1, 4.0);
        kurtosis += tmp2;
      }
      kurtosis = kurtosis * Count * (Count + 1) / ((Count - 1) * (Count - 2) * (Count - 3))
        - 3 * (Count - 1) * (Count - 1) / ((Count - 2) * (Count - 3));
      return kurtosis;
    }

    public double StandardError( int Count ) {
      int start = ds.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "StandardError:  not enough values");
      double variance = Variance(Count);
      double standarderror = Math.Sqrt(variance / Count);
      return standarderror;
    }

    public double Min( int Count ) {
      int start = bs.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Min:  not enough values");
      double min = bs.Last.Low;
      for (int i = start; i <= bs.LastIndex; i++) {
        Bar bar = bs[i];
        min = Math.Min(min, bar.Low);
      }
      return min;
    }

    public double Max( int Count ) {
      int start = bs.LastIndex - Count + 1;
      if (start < 0) throw new ArgumentOutOfRangeException("Statistics", "Max:  not enough values");
      double max = bs.Last.High;
      for (int i = start; i <= bs.LastIndex; i++) {
        Bar bar = bs[i];
        max = Math.Max(max, bar.High);
      }
      return max;
    }

  }
}

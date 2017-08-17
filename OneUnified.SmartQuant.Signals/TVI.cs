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

using SmartQuant.Data;
using SmartQuant.Series;
using SmartQuant.Charting;

namespace OneUnified.SmartQuant.Signals {
  #region TVI
  public class TVI : DoubleSeries {

    private double MTV;  // Minimum Tick Value
    private double LastPrice;
    private enum EState { init, first, go };
    private EState state = EState.init;
    private enum EDirection { unknown, accumulate, distribute };
    private EDirection direction = EDirection.unknown;
    private double accum = 0;

    public TVI( double MinimumTickValue ) {
      MTV = MinimumTickValue;
    }

    public TVI( double MinimumTickValue, string Name )
      : base(Name) {
      MTV = MinimumTickValue;
    }

    public TVI( double MinimumTickValue, string Name, string Title )
      : base(Name, Title) {
      MTV = MinimumTickValue;
    }

    public override void Add( DateTime DateTime, double Data ) {
      throw new Exception("Can not use TVI.Add( dt, data )");
    }

    public void Add( Trade trade, Quote quote ) {
      double midpoint = (quote.Bid + quote.Ask) / 2;
      if (trade.Price == midpoint) {
        switch (direction) {
          case EDirection.accumulate:
            accum += trade.Size;
            break;
          case EDirection.distribute:
            accum -= trade.Size;
            break;
        }
      }
      else {
        if (trade.Price > midpoint) {
          accum += trade.Size;
          direction = EDirection.accumulate;
        }
        if (trade.Price < midpoint) {
          accum -= trade.Size;
          direction = EDirection.distribute;
        }
      }
      base.Add(trade.DateTime, accum);
    }

    public void Add( Trade trade ) {
      // assumes additions in chronological order
      double change = 0;

      switch (state) {
        case EState.go:
          change = trade.Price - LastPrice;
          if (Math.Abs(change) > MTV) {
            direction = change > 0 ? EDirection.accumulate : EDirection.distribute;
          }
          switch (direction) {
            case EDirection.accumulate:
              accum += trade.Size;
              break;
            case EDirection.distribute:
              accum -= trade.Size;
              break;
          }
          base.Add(trade.DateTime, accum);
          break;
        case EState.first:
          change = trade.Price - LastPrice;
          if (Math.Abs(change) > MTV) {
            if (change > 0) {
              accum = trade.Size;
              direction = EDirection.accumulate;
            }
            if (change < 0) {
              accum = -trade.Size;
              direction = EDirection.distribute;
            }
            base.Add(trade.DateTime, accum);
            state = EState.go;
          }
          break;
        case EState.init:
          state = EState.first;
          break;

      }

      LastPrice = trade.Price;
    }
  }

  #endregion TVI
}

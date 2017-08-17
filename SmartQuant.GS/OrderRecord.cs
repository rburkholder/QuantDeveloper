//============================================================================
// Author      : Ray Burkholder, ray@oneunified.net
// Copyright   : (c) 2007 One Unified
// License     : Released under GPL3
// Status      : No warranty, express or implied. Supplied as is.
// Note        : Please contact author for commercial use rights
// Date        : 2007/10/07
//============================================================================

using System;

using SmartQuant.Execution;

namespace SmartQuant.GS
{
	class OrderRecord
	{
		public SingleOrder Order;

		public double LeavesQty;
		public double CumQty;
		public double AvgPx;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="order"></param>
		public OrderRecord(SingleOrder order)
		{
			this.Order = order;

			LeavesQty = order.OrderQty;
			CumQty    = 0;
			AvgPx     = 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lastPx"></param>
		/// <param name="lastQty"></param>
		public void AddFill(double lastPx, double lastQty)
		{
			AvgPx = (AvgPx * CumQty + lastPx * lastQty) / (CumQty + lastQty);

			LeavesQty -= lastQty;

			CumQty += lastQty;
		}
	}
}

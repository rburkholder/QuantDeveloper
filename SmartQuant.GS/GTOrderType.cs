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

namespace SmartQuant.GS
{
	/// <summary>
	/// GTOrderType.
	/// </summary>
	class GTOrderType
	{
		public const char MARKET          = '1';
		public const char LIMIT           = '2';
		public const char STOP            = '3';
		public const char STOP_LIMIT      = '4';
		public const char MARKET_ON_OPEN  = 'A';
		public const char MARKET_ON_CLOSE = 'B';
		public const char LIMIT_ON_OPEN   = 'C';
		public const char LIMIT_ON_CLOSE  = 'D';
	}
}

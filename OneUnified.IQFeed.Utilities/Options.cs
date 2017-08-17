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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using SmartQuant;
using SmartQuant.Instruments;
using SmartQuant.FIX;

namespace OneUnified.IQFeed.Utilities {
  class Options {

    string sCall;
    string sPut;
    public string sUnderlying;

    private static string sDelim = ",";
    private static char[] chDelim = sDelim.ToCharArray();

    private string[] rCalls;
    private string[] rPuts;

    private int cntRequests;
    private Hashtable htOptionChain;

    public double dblUnderlyingPrice;

    private int cntOptionsSummary;
    Option[] options;


    // from synthetic.cs
    //private void ParseOptionChain( object o, BufferArgs e ) {
    private void ParseOptionChain( object o, string Line ) {


      int colon = Line.IndexOf(":");
      string sCalls = Line.Substring(0, colon);
      string sPuts = Line.Substring(colon + 1);

      //bs.SetLineHandler(new Buffer.LineHandler(IgnoreRemainingLines));

      if (sCalls.EndsWith(" ")) sCalls = sCalls.Substring(0, sCalls.Length - 1);
      if (sCalls.EndsWith(",")) sCalls = sCalls.Substring(0, sCalls.Length - 1);
      if ("," == sCalls.Substring(0, 1)) sCalls = sCalls.Substring(1);
      if (sPuts.EndsWith(" ")) sPuts = sPuts.Substring(0, sPuts.Length - 1);
      if (sPuts.EndsWith(",")) sPuts = sPuts.Substring(0, sPuts.Length - 1);
      if ("," == sPuts.Substring(0, 1)) sPuts = sPuts.Substring(1);

      rCalls = sCalls.Split(chDelim);
      rPuts = sPuts.Split(chDelim);
    }
  }
}

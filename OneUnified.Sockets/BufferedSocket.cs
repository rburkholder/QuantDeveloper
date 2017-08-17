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
using System.Threading;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace OneUnified.Sockets {

	public class BufferedSocket {

    private Buffer buffer;
    private Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    private string Host;
    private int Port;
    private SocketLineHandler slh;

    public BufferedSocket(string Host, int Port, SocketLineHandler slh) {
      this.Host = Host;
      this.Port = Port;
      this.slh = slh;
    }

    public void Add( SocketLineHandler slh ) {
      buffer.Add(slh);
    }

    //public void Open( string sHost, int port, SocketLineHandler slh ) {
    public void Open() {

      //IPHostEntry he = Dns.GetHostByName( sHost );
      IPHostEntry he = Dns.GetHostEntry(Host);
      IPAddress ipa = he.AddressList[0];
      IPEndPoint ipep = new IPEndPoint(ipa, Port);
      sock.Connect(ipep);

      buffer = new Buffer(sock);
      buffer.Add(slh);
      buffer.Open();

      ipep = null;
      ipa = null;
      he = null;
    }

    public void Send( string s ) {
			sock.Send( Encoding.ASCII.GetBytes( s ) );
		}

    public void Remove( SocketLineHandler slh ) {
      buffer.Remove(slh);
    }

    public void Close() {

      buffer.Close();
      buffer = null;
      slh = null;

      sock.Shutdown(SocketShutdown.Both);
      sock.Close();
      sock = null;
    }

	}
}

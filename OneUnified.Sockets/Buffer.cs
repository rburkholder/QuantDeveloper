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

  public delegate void SocketLineHandler( object source, BufferArgs args );  // can store static or object references

  public class BufferArgs : EventArgs {

    private string line;  // string to be processed
    public string[] items;  // for split items 
    public string headline;  // news event will update this headline

    public BufferArgs( string line ) {
      this.line = line;
    }

    public string Line {
      get { return this.line; }
    }
  }

  public class Buffer {

    private int blocksize = 1024;				 // max we can get at one time
    private byte[] buf;									 // buffer where the socket puts stuff
    private string sPartialLine = "";		 // we build our strings here
    private Socket sock;

    private event SocketLineHandler HandleSocketLine;

    private bool ProcessingBuffer = false;

    private IAsyncResult asyncResult;

    internal bool bClosed = true;
    private bool bClosing = false;
    private bool bInReceive = false;

    //internal Semaphore semaphore;

    public Buffer( Socket sock ) {
      this.sock = sock;
      //semaphore = new Semaphore(1, 1);
      bClosing = false;
    }

    public void Add( SocketLineHandler slh ) {
      this.HandleSocketLine += slh;
    }

    public void Open() {
      buf = new byte[blocksize];
      bClosed = false;
      asyncResult =
        sock.BeginReceive(buf, 0, blocksize, SocketFlags.None, new AsyncCallback(cbProcessBuffer), this);
    }

    public void Remove( SocketLineHandler slh ) {
      this.HandleSocketLine -= slh;
    }

    public void Close() {

      bClosed = true;
      if (ProcessingBuffer) {
        bClosing = true;
      }
      else {
        //semaphore.WaitOne();
        HandleSocketLine = null;
        buf = null;

        //int cntBytesReceived = buffer.sock.EndReceive(asyncResult);
        //int t = cntBytesReceived;
        //Console.WriteLine("buffer closed {0}", cntBytesReceived);

        //sock = null;
        //asyncResult = null;

        // *** need to accept signal when last EndReceive has been processed, then close socket
        //semaphore.Release();
      }
    }

    public static void cbProcessBuffer( IAsyncResult ar ) {

      try {
        int cntBytesReceived;
        int i;  //process each character of the inbound buffer
        char[] ch;  //each character of the buffer is checked and moved

        Buffer buffer = (Buffer)ar.AsyncState;  // get a reference to this object from the callback
        if (buffer.bClosed) {
          //cntBytesReceived = buffer.sock.EndReceive(ar);
          //int t = cntBytesReceived;
          //Console.WriteLine("buffer closed {0}", cntBytesReceived);
          //Console.WriteLine("buffer closing");
        }
        else {

          if (buffer.ProcessingBuffer) {
            Console.WriteLine("*** buffer.cs: being re-entrant");
            throw new Exception("*** buffer.cs being re-entered");
          }

          buffer.ProcessingBuffer = true;

          if (!buffer.sock.Connected) {
            throw new Exception("cbProcessBuffer socket is closed");
          }

          cntBytesReceived = buffer.sock.EndReceive(ar);
          buffer.bInReceive = false;
          //buffer.semaphore.WaitOne();

          //Console.WriteLine("cbProcessBuffer ar {0}, {1}, {2}, {3}, {4}", 
          //ar.IsCompleted, ar.CompletedSynchronously, ar.ToString(), buffer.sock.IsBound, buffer.sock.Connected);

          if (cntBytesReceived > 0) {
            for (i = 0; i < cntBytesReceived; i++) {
              try {
                ch = Encoding.ASCII.GetChars(buffer.buf, i, 1);
                if (0x0a == ch[0]) {
                  //do the callback with callback with the line
                  //Console.WriteLine( "'" + buffer.sPartialLine + "'" );
                  BufferArgs args = new BufferArgs(buffer.sPartialLine);
                  //if (null != buffer.HandleSocketLine) buffer.HandleSocketLine(buffer, buffer.args);
                  if (null != buffer.HandleSocketLine) buffer.HandleSocketLine(buffer, args);
                  args = null;
                  buffer.sPartialLine = "";
                }
                else {
                  if (0x0d == ch[0]) {
                    // ignore the character
                  }
                  else {
                    // move the character to the parial line and try for another
                    buffer.sPartialLine += ch[0];
                  }
                }
              }
              catch (Exception e) {
                throw new Exception("cbProcessBuffer Exception: cntBytesReceived=" + cntBytesReceived.ToString()
                  + ", i=" + i.ToString()
                  + ", buffer.buf=" + buffer.buf.ToString()
                  + ", e=" + e.ToString()
                  );
              }
            }
          }
          buffer.ProcessingBuffer = false;
        }
        //buffer.semaphore.Release();
        if (buffer.bClosing) {
          buffer.Close();
        }
        else {
          if (!buffer.bClosed) {
            buffer.bInReceive = true;
            buffer.asyncResult =
              buffer.sock.BeginReceive(buffer.buf, 0, buffer.blocksize, SocketFlags.None, new AsyncCallback(cbProcessBuffer), buffer);
          }
        }

      }

      catch (Exception e) {
        Console.WriteLine("Buffer::cbProcessBuffer Exception {0}", e);
      }
    }

  }
}


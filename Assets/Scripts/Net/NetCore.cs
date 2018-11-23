using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;
using System.Net;

public delegate void SocketConnected();
public delegate void FuncHandler(byte[] data);

public class NetCore
{
    private static byte[] MsgID = new byte[1];
    private class ProtoMsg
    {
        public ProtoMsg(int length)
        {
            _MainID = 0;
            _SubID = 0;
            data = new byte[length];
        }
        public byte _MainID;
        public byte _SubID;
        public byte[] data;
    }

    private static Socket socket;

    public static bool logined;
    public static bool enabled = true;

    private static int CONNECT_TIMEOUT = 3000;
    private static ManualResetEvent TimeoutObject;

    private static Queue<ProtoMsg> recvQueue = new Queue<ProtoMsg>();

    private static ProtoStream sendStream = new ProtoStream();
    private static ProtoStream recvStream = new ProtoStream();

    private static AsyncCallback connectCallback = new AsyncCallback(Connected);
    private static AsyncCallback receiveCallback = new AsyncCallback(Receive);

    public static void Init()
    {
        byte[] receiveBuffer = new byte[1 << 16];
        recvStream.Write(receiveBuffer, 0, receiveBuffer.Length);
        recvStream.Seek(0, SeekOrigin.Begin);
    }

    public static void Connect(string host, int port, SocketConnected socketConnected)
    {
        Disconnect();

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.BeginConnect(host, port, connectCallback, socket);

        TimeoutObject = new ManualResetEvent(false);
        TimeoutObject.Reset();

        if (TimeoutObject.WaitOne(CONNECT_TIMEOUT, false))
        {
            Receive();
            socketConnected();
        }
        else
        {
            Debug.Log("Connect Timeout");
        }
    }

    private static void Connected(IAsyncResult ar)
    {
        socket.EndConnect(ar);
        TimeoutObject.Set();
    }

    public static void Disconnect()
    {
        if (connected)
        {
            if (socket.Connected)
                socket.Shutdown(SocketShutdown.Both);

            socket.Close();
            sendStream = new ProtoStream();
            recvStream = new ProtoStream();
            receivePosition = 0;
        }
    }

    public static bool connected
    {
        get
        {
            return socket != null && socket.Connected;
        }
    }

    private static int MAX_PACK_LEN = (1 << 16) - 1;
    public static void Send(IMessage obj, byte _MainID, byte _SubID)
    {
        byte[] result;
        using (MemoryStream ms = new MemoryStream())
        {
            obj.WriteTo(ms);
            result = ms.ToArray();
        }

        // 消息长度 = 长度信息（4字节）+ID信息（1+1字节）+PB序列化后的数据长度
        UInt32 lengh = (UInt32)(result.Length + 4 + 1 + 1);
        if (lengh > MAX_PACK_LEN)
        {
            Debug.Log("data.Length > " + MAX_PACK_LEN + " => " + lengh);
            return;
        }
        
        sendStream.Seek(0, SeekOrigin.Begin);
        sendStream.Write(BitConverter.GetBytes(lengh), 0, 4);
        sendStream.WriteByte(_MainID);
        sendStream.WriteByte(_SubID);
        sendStream.Write(result, 0, result.Length);

        try
        {
            socket.Send(sendStream.Buffer, sendStream.Position, SocketFlags.None);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.ToString());
        }
    }

    private static int receivePosition;
    public static void Receive(IAsyncResult ar = null)
    {
        if (!connected)
        {
            return;
        }

        if (ar != null)
        {
            try {
                receivePosition += socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
            }
        }

        int i = recvStream.Position;
        // 4个字节头
        while (receivePosition >= i + 4)
        {
            // 消息总长度
            int length = (recvStream[i]) | (recvStream[i + 1] << 8) | (recvStream[i + 2] << 16) | (recvStream[i + 3] << 24);
            int sz = length;
            if (receivePosition < i + sz)
            {
                break;
            }

            // 去掉消息头（标识消息长度的）
            recvStream.Seek(4, SeekOrigin.Current);
            
            if (length > 0)
            {
                // 读取前两个用于表示MainID和SubID的，并把后面的数据读到data中
                ProtoMsg msg = new ProtoMsg(length - 4 - 2);
                recvStream.Read(MsgID, 0, 1);
                msg._MainID = MsgID[0];
                recvStream.Read(MsgID, 0, 1);
                msg._SubID = MsgID[0];
                recvStream.Read(msg.data, 0, length - 4 - 2);

                recvQueue.Enqueue(msg);
            }

            i += sz;
        }

        if (receivePosition == recvStream.Buffer.Length)
        {
            recvStream.Seek(0, SeekOrigin.End);
            recvStream.MoveUp(i, i);
            receivePosition = recvStream.Position;
            recvStream.Seek(0, SeekOrigin.Begin);
        }

        try {
            socket.BeginReceive(recvStream.Buffer, receivePosition,
                recvStream.Buffer.Length - receivePosition,
                SocketFlags.None, receiveCallback, socket);
        }
        catch (Exception e) {
            Debug.LogWarning(e.ToString());
        }
    }

    public static void Dispatch()
    {
        if (recvQueue.Count > 100)
        {
            Debug.Log("recvQueue.Count: " + recvQueue.Count);
        }

        while (recvQueue.Count > 0)
        {
            Parsers(recvQueue.Dequeue());
        }
    }

    // 项目中注册用于接受远程服务器消息的函数
    private static List<FuncHandler[]> ProtoFunc = new List<FuncHandler[]>();

    public static void RegisterFunc(byte _MainID, byte _SubID, FuncHandler f)
    {
        if (ProtoFunc.Count <= _MainID)
        {
            for (int i = ProtoFunc.Count; i <= _MainID; i++)
            {
                FuncHandler[] list = new FuncHandler[255];
                ProtoFunc.Add(list);
            }
        }

        if (ProtoFunc[_MainID].Length > _SubID)
        {
            ProtoFunc[_MainID][_SubID] += f;
        }
        else
        {
            Debug.Log("注册函数失败！ : " + _MainID + " - " + _SubID + " " + f.ToString());
        }
    }

    private static void Parsers(ProtoMsg msg)
    {
        if (ProtoFunc.Count > msg._MainID && ProtoFunc[msg._MainID].Length > msg._SubID
            && ProtoFunc[msg._MainID][msg._SubID] != null)
            ProtoFunc[msg._MainID][msg._SubID](msg.data);
        else
            Debug.Log("未定义的消息! : " + msg._MainID + " - " + msg._SubID);
    }
}

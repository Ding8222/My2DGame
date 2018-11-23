using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginManager : Singleton<LoginManager>
{
    LoginManager()
    {
        InitAddFunction();
    }

    string account = "Ding";
    string playername = "Ding";
    string loginIP = "127.0.0.1";
    string gameIP = "127.0.0.1";
    int loginPort = 5001;
    int gamePort = 8547;
    ByteString Secret;
    // 注册返回消息函数
    void InitAddFunction()
    {
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_HANDSHAKE_RET, HandShakeRet);
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_AUTH_RET, AuthRet);
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_PLAYER_LIST_RET, PlayerListRet);
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_CREATE_PLAYER_RET, CreatePlayerRet);
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_SELECT_PLAYER_RET, SelectPlayerRet);
        NetCore.RegisterFunc(EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_LOGIN_RET, LoginRet);
        NetCore.RegisterFunc(3, 4, PlayerMoveRet);
    }
    
    public void LoginConnect()
    {
        Debug.Log("LoginConnect!");
        NetCore.Connect(loginIP, loginPort, LoginConnected);
    }

    void LoginConnected()
    {
        QueryHandShake();
    }

    // 请求握手
    void QueryHandShake()
    {
        Debug.Log("请求握手...");
        NetData.HandShake SendMsg = new NetData.HandShake();
        SendMsg.SClientKey = ByteString.CopyFromUtf8("12345678");
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_HANDSHAKE);
    }

    // 握手返回
    private void HandShakeRet(byte[] data)
    {
        NetData.HandShakeRet msg = NetData.HandShakeRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if(msg.NCode == (int)NetData.HandShakeRet.Types.EC.Succ)
        {
            Secret = msg.SChallenge;
            QueryAuth();
        }
    }

    // 请求认证
    void QueryAuth()
    {
        Debug.Log("请求认证...");
        NetData.Auth SendMsg = new NetData.Auth();
        SendMsg.Account = account;
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_AUTH);
    }

    // 认证返回
    void AuthRet(byte[] data)
    {
        NetData.AuthRet msg = NetData.AuthRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        QueryPlayerList();
    }

    // 请求角色列表
    void QueryPlayerList()
    {
        Debug.Log("请求角色列表...");
        NetData.PlayerList SendMsg = new NetData.PlayerList();
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_PLAYER_LIST);
    }

    // 角色列表返回
    void PlayerListRet(byte[] data)
    {
        NetData.PlayerListRet msg = NetData.PlayerListRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if (msg.List.Count == 0)
        {
            QueryCreatePlayer();
        }
        else
        {
            QuerySelectPlayer(msg.List[0].NGuid);
        }
    }

    // 请求创建角色
    void QueryCreatePlayer()
    {
        Debug.Log("请求创建角色...");
        NetData.CreatePlayer SendMsg = new NetData.CreatePlayer();
        SendMsg.SName = playername;
        SendMsg.NJob = 1;
        SendMsg.NSex = 1;
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_CREATE_PLAYER);
    }

    // 创建角色返回
    void CreatePlayerRet(byte[] data)
    {
        NetData.CreatePlayerRet msg = NetData.CreatePlayerRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if (msg.NCode == 1)
        {
            QuerySelectPlayer(msg.Info.NGuid);
        }
    }

    // 请求选择角色
    void QuerySelectPlayer(long guid)
    {
        Debug.Log("请求选择角色...");
        NetData.SelectPlayer SendMsg = new NetData.SelectPlayer();
        SendMsg.NGuid = guid;
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_SELECT_PLAYER);
    }

    // 选角返回
    void SelectPlayerRet(byte[] data)
    {
        NetData.SelectPlayerRet msg = NetData.SelectPlayerRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if (msg.NCode == 1)
        {
            gameIP = msg.SIP;
            gamePort = msg.NPort;
            GameConnect();
        }
    }

    // 连接至GameGateway
    void GameConnect()
    {
        Debug.Log("连接至GameGateway：" + gameIP + ":" + gamePort);
        NetCore.Connect(gameIP, gamePort, GameConnected);
    }

    void GameConnected()
    {
        Debug.Log("连接GameGateway成功...");
        QueryLogin();
    }

    // 请求登陆
    void QueryLogin()
    {
        Debug.Log("请求登陆...");
        NetData.Login SendMsg = new NetData.Login();
        SendMsg.Account = account;
        SendMsg.Secret = Secret;
        NetCore.Send(SendMsg, EnumMain.LOGIN_TYPE_MAIN, LoginEnum.LOGIN_SUB_LOGIN);
    }

    // 登陆返回
    void LoginRet(byte[] data)
    {
        NetData.LoginRet msg = NetData.LoginRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if (msg.NCode == 1)
        {
            QueryPlayerMove();
        }
    }

    // 请求移动
    void QueryPlayerMove()
    {
        Debug.Log("请求移动...");
        NetData.PlayerMove SendMsg = new NetData.PlayerMove();
        SendMsg.X = 1;
        SendMsg.Y = 1;
        SendMsg.Z = 1;
        NetCore.Send(SendMsg, 3, 3);
    }

    // 移动返回
    void PlayerMoveRet(byte[] data)
    {
        NetData.PlayerMoveRet msg = NetData.PlayerMoveRet.Parser.ParseFrom(data);
        Debug.Log(msg);
        if(msg.NCode == 1)
        {
            Debug.Log(msg.NTempID);
        }
    }
}

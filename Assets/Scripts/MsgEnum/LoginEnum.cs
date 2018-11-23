using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LoginEnum
{
    // public const int LOGIN_TYPE_MAIN = 2;

    // 握手
    public const int LOGIN_SUB_HANDSHAKE = 1;

    // 握手返回
    public const int LOGIN_SUB_HANDSHAKE_RET = 2;

    // 挑战握手
    public const int LOGIN_SUB_CHALLENGE = 3;

    // 挑战握手返回
    public const int LOGIN_SUB_CHALLENGE_RET = 4;

    // 认证
    public const int LOGIN_SUB_AUTH = 5;

    // 认证返回
    public const int LOGIN_SUB_AUTH_RET = 6;

    // 请求角色列表
    public const int LOGIN_SUB_PLAYER_LIST = 7;

    // 返回角色列表
    public const int LOGIN_SUB_PLAYER_LIST_RET = 8;

    // 请求创建角色
    public const int LOGIN_SUB_CREATE_PLAYER = 9;

    // 请求创建角色返回
    public const int LOGIN_SUB_CREATE_PLAYER_RET = 10;

    // 请求选择角色
    public const int LOGIN_SUB_SELECT_PLAYER = 11;

    // 请求选择角色返回
    public const int LOGIN_SUB_SELECT_PLAYER_RET = 12;

    // 登陆
    public const int LOGIN_SUB_LOGIN = 13;

    // 登陆返回
    public const int LOGIN_SUB_LOGIN_RET = 14;
}


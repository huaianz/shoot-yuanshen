/// <summary>
/// 登录结果
/// </summary>
public class LoginResult
{
    public bool success;
    public string msg;
    public string username;
    public string token;
}

/// <summary>
/// 注册结果
/// </summary>
public class RegisterResult
{
    public bool success;
    public string msg;
}

/// <summary>
/// 玩家数据结果
/// </summary>
public class PlayerDataResult
{
    public bool success;
    public string msg;
    public int coin;
    public string inventoryJson;
}
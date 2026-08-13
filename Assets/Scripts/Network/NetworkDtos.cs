using System;

/// <summary>
/// 协议 DTO
/// </summary>
[Serializable]
public class LoginRequestDto { public string username; public string password; }
[Serializable]
public class LoginResponseDto { public int code; public string msg; public string username; public string token; }

[Serializable]
public class RegisterRequestDto { public string username; public string password; }
[Serializable]
public class RegisterResponseDto { public int code; public string msg; public string username; public string token; }

[Serializable]
public class GetPlayerDataRequestDto { public string token; }
[Serializable]
public class PlayerDataResponseDto { public int code; public string msg; public string username; public int coin; public string inventoryJson; }
[Serializable]
public class SavePlayerDataRequestDto { public string token; public int coin; public string inventoryJson; }

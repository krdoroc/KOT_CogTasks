using System.Runtime.InteropServices;
using UnityEngine;

public class OperatingSystem
{
    [DllImport("__Internal")]
    private static extern bool isAndroid();

    [DllImport("__Internal")]
    private static extern bool isIos();
    
    [DllImport("__Internal")]
    private static extern string getOperationSystemFamilyName();

    [DllImport("__Internal")]
    private static extern void freeBuffer(string ptr);

    public bool IsAndroid()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        return isAndroid();
#else
        return false;
#endif
    }
    
    public bool IsIos()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        return isIos();
#else
        return false;
#endif
    }
    
    public string GetOperationSystemFamilyName()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        var os = getOperationSystemFamilyName();
        var copy = string.Copy(os);
        freeBuffer(os);
        return copy;
        
#else
        return SystemInfo.operatingSystem;
#endif
    }
}

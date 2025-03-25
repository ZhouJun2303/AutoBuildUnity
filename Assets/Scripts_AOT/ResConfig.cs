using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/ResConfig", menuName = "CreateAsset/ResConfig")]
public class ResConfig : ScriptableObject
{
    [Header("版本")]
    [SerializeField]
    public int ResVersion = 0;

    public const string ResConfigName = "ResConfig";
    public const string ResConfigPath = "Assets/Resources/" + ResConfigName + ".asset";
    private static ResConfig _ins;
    public static ResConfig Instance
    {
        get
        {
            if (_ins == null)
            {
                _ins = Resources.Load<ResConfig>(ResConfigName);
            }
            return _ins;
        }
    }
}

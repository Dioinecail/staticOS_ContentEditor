using UnityEngine;

public static class JSONUtils
{
    public static float[] ToArray(this Vector3 vec3)
    {
        return new float[3]
        {
            vec3.x,
            vec3.y,
            vec3.z
        };
    }

    public static float[] ToFloatArray(this JSONObject arr)
    {
        if (!arr.IsArray)
            throw new System.Exception("Target object is not of type Array");

        var result = new float[arr.Count];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = arr[i].f;
        }

        return result;
    }

    public static Vector3 ToVector3(this float[] arr)
    {
        var result = Vector3.zero;

        if (arr.Length > 0)
            result.x = arr[0];

        if (arr.Length > 1)
            result.y = arr[1];

        if (arr.Length > 2)
            result.z = arr[2];

        return result;
    }
}

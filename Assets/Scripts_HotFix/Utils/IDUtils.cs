using System;
public static class IdUtils
{
    public static Func<int> GetGenerator()
    {
        int id = 0;
        return () => ++id;
    }

    public static Func<string> GetGenerator(string format)
    {
        int id = 0;
        return () => string.Format(format, ++id);
    }
}


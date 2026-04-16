namespace MDictUtils.Extensions;

internal static class ArrayExtensions
{
    /// <summary>
    /// A more ergonomical alternative to inline assignment.
    /// </summary>
    public static T[] AlsoAssignTo<T>(this T[] array, ref T[]? nullableArray)
    {
        nullableArray = array;
        return array;
    }
}

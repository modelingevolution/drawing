using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides extension methods for collections, particularly vector collections with statistical operations.
/// </summary>
public static class CollectionExtensions
{
    
    
    /// <summary>
    /// Creates a List{T} from an IEnumerable{T} with the specified initial capacity.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="capacity">The initial capacity of the list.</param>
    /// <returns>A new List{T} containing the elements from the source.</returns>
    public static List<T> ToList<T>(this IEnumerable<T> source, int capacity)
    {
        List<T> list = new List<T>(capacity);
        list.AddRange(source);
        
        return list;
    }
    /// <summary>
    /// Computes the sum of a collection of double vectors.
    /// </summary>
    /// <param name="items">The collection of vectors to sum.</param>
    /// <returns>The sum of all vectors in the collection.</returns>
    public static Vector<double> Sum(this IEnumerable<Vector<double>> items)
    {

        Vector<double> sum = Vector<double>.Zero;

        foreach (var value in items)
            sum += value;

        return sum;
    }
    /// <summary>
    /// Computes the sum of a collection of float vectors, converting to double precision.
    /// </summary>
    /// <param name="items">The collection of vectors to sum.</param>
    /// <returns>The sum of all vectors in the collection as a double vector.</returns>
    public static Vector<double> Sum(this IEnumerable<Vector<float>> items)
    {

        Vector<double> sum = Vector<double>.Zero;
        
        foreach (var value in items) 
            sum += value.Truncating<double>();

        return sum;
    }
    /// <summary>
    /// Computes the average of a collection of float vectors.
    /// </summary>
    /// <param name="items">The collection of vectors to average.</param>
    /// <returns>The average of all vectors in the collection as a double vector.</returns>
    public static Vector<double> Avg(this IEnumerable<Vector<float>> items)
    {
      
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        foreach (var value in items)
        {
            sum += value.Truncating<double>();
            c += 1.0d;
        }

        return sum / c;
    }

    public static Vector<double> AvgSkipTwoMax(this IEnumerable<Vector<float>> items)
    {
        int maxFirstVecToSkip = GetIndexOfMaxVector(items);
        int maxSecondVecToSkip = GetIndexOfMaxVector(items,maxFirstVecToSkip);
        int counter = 0;

        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        foreach (var value in items)
        {
            if (counter == maxFirstVecToSkip || counter==maxSecondVecToSkip)
            {
                counter++;
                continue;
            }
            sum += value.Truncating<double>();
            c += 1.0d;
            counter++;

        }
        if(c!=0)
            return sum / c;
        else
        {
            return Vector<double>.Zero;
        }
    }
    public static Vector<double> AvgSkipMax(this IEnumerable<Vector<float>> items)
    {
        int maxVecToSkip = GetIndexOfMaxVector(items);
        int counter = 0;

        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        foreach (var value in items)
        {
            if (counter == maxVecToSkip)
            {
                counter++;
                continue;
            }
            sum += value.Truncating<double>();
            c += 1.0d;
            counter++;

        }

        return sum / c;
    }
    public static int GetIndexOfMaxVector(this IEnumerable<Vector<double>> items, int indexToSkip)
    {
        int indexOfMaxVector = 0;
        int counter = 0;
        Vector<double> MaxVector = new Vector<double>();

        foreach (var value in items)
        {
           
            if ((MaxVector.LengthSquared < value.LengthSquared) && indexToSkip!= counter)
            {
                MaxVector = value;
                indexOfMaxVector = counter;
            }

            counter++;
        }
        return indexOfMaxVector;
    }
    public static int GetIndexOfMaxVector(this IEnumerable<Vector<double>> items)
    {
        int indexOfMaxVector = 0;
        int counter = 0;
        Vector<double> MaxVector = new Vector<double>();

        foreach (var value in items)
        {
            if (MaxVector.LengthSquared < value.LengthSquared)
            {
                MaxVector = value;
                indexOfMaxVector = counter;
            }

            counter++;
        }
        return indexOfMaxVector;
    }
    public static int GetIndexOfMaxVector(this IEnumerable<Vector<float>> items, int indexToSkip)
    {
        int indexOfMaxVector = 0;
        int counter = 0;
        Vector<float> MaxVector = new Vector<float>();

        foreach (var value in items)
        {
            if ((MaxVector.LengthSquared < value.LengthSquared)&& indexToSkip!=counter)
            {
                MaxVector = value;
                indexOfMaxVector = counter;
            }

            counter++;
        }
        return indexOfMaxVector;
    }
    public static int GetIndexOfMaxVector(this IEnumerable<Vector<float>> items)
    {
        int indexOfMaxVector = 0;
        int counter = 0;
        Vector<float> MaxVector = new Vector<float>();

        foreach (var value in items)
        {
            if (MaxVector.LengthSquared < value.LengthSquared)
            {
                MaxVector = value;
                indexOfMaxVector = counter;
            }

            counter++;
        }
        return indexOfMaxVector;
    }
    public static Vector<double> Avg(this IEnumerable<Vector<double>> items)
    {
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
   
        foreach (var value in items)
        {
        
            sum += value;
            c += 1.0d;
       
        }

        return sum / c;
    }
    public static Vector<double> AvgSkipTwoMax(this IEnumerable<Vector<double>> items)
    {
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        int maxFirstVecToSkip = GetIndexOfMaxVector(items);
        int maxSecondVecToSkip = GetIndexOfMaxVector(items,maxFirstVecToSkip);
        
        int counter = 0;
        foreach (var value in items)
        {
            if (counter == maxFirstVecToSkip || counter ==maxSecondVecToSkip)
            {
                counter++;
                continue;
            }
            sum += value;
            c += 1.0d;
            counter++;
        }

        if (c!=0)
            return sum / c;
        else
        {
            return Vector<double>.Zero;
        }
    }
    public static Vector<double> AvgSkipMax(this IEnumerable<Vector<double>> items)
    {
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        int maxVecToSkip = GetIndexOfMaxVector(items);
        int counter= 0;
        foreach (var value in items)
        {
            if (counter == maxVecToSkip)
            {
                counter++;
                continue;
            }
            sum += value;
            c += 1.0d;
            counter++;
        }

        return sum / c;
    }
}
/// <summary>
/// Provides extension methods for converting numeric values to CSS pixel units.
/// </summary>
internal static class WebUnitsExtensions
{
    /// <summary>
    /// Converts an integer value to a CSS pixel string.
    /// </summary>
    /// <param name="val">The integer value to convert.</param>
    /// <returns>A string representing the value with "px" suffix.</returns>
    public static string ToPx(this int val) => $"{val}px";
    public static string ToPx(this int? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this long val) => $"{val}px";
    public static string ToPx(this long? val) => val != null ? val.Value.ToPx() : string.Empty;
    /// <summary>
    /// Converts a double value to a CSS pixel string with up to 2 decimal places.
    /// </summary>
    /// <param name="val">The double value to convert.</param>
    /// <returns>A string representing the value with "px" suffix.</returns>
    public static string ToPx(this double val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this double? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this float val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    /// <summary>
    /// Converts a generic floating-point value to a CSS pixel string with up to 2 decimal places.
    /// </summary>
    /// <typeparam name="T">The floating-point type.</typeparam>
    /// <param name="val">The value to convert.</param>
    /// <returns>A string representing the value with "px" suffix.</returns>
    public static string ToPx<T>(this T val) where T:IFloatingPointIeee754<T> => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this float? val) => val != null ? val.Value.ToPx() : string.Empty;
}
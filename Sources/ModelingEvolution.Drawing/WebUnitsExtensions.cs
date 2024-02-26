using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public static class CollectionExtensions
{
    public static Vector<double> Sum(this IEnumerable<Vector<double>> items)
    {

        Vector<double> sum = Vector<double>.Zero;

        foreach (var value in items)
            sum += value;

        return sum;
    }
    public static Vector<double> Sum(this IEnumerable<Vector<float>> items)
    {

        Vector<double> sum = Vector<double>.Zero;

        foreach (var value in items) 
            sum += value.Truncating<double>();

        return sum;
    }
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
internal static class WebUnitsExtensions
{
    public static string ToPx(this int val) => $"{val}px";
    public static string ToPx(this int? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this long val) => $"{val}px";
    public static string ToPx(this long? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this double val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this double? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this float val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx<T>(this T val) where T:IFloatingPointIeee754<T> => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this float? val) => val != null ? val.Value.ToPx() : string.Empty;
}
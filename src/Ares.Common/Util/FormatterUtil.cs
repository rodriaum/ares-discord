/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Globalization;

namespace Ares.Common.Util;

public class FormatterUtil
{
    public static string FormatMillis(long ms)
    {
        return TimeUtil.CurrentTimeMillis() - ms + "ms";
    }

    public static string FormatSeconds(long ms)
    {
        long seconds = (TimeUtil.CurrentTimeMillis() - ms) / 1000;

        return seconds > 0 ? seconds + "s" : FormatMillis(ms);
    }

    public static string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower();
    }

    public static string FormatPrice(decimal value)
    {
        if (value == 0)
            return "0";

        string formatted = value.ToString("G", CultureInfo.InvariantCulture);

        // Garante pelo menos 2 dígitos significativos
        if (value < 1)
        {
            int firstNonZeroIndex = formatted.IndexOfAny("123456789".ToCharArray());
            if (firstNonZeroIndex != -1 && formatted.Length > firstNonZeroIndex + 2)
                formatted = formatted.Substring(0, firstNonZeroIndex + 3);
        }
        else
        {
            formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        return formatted;
    }

    public static string FormatRam(float ram)
    {
        return ram >= 1024 ? $"{ram / 1024:F2} GB" : $"{ram:F2} MB";
    }

    public static string FormatNumberWithLeadingZeros(long value, int zeroCount = 1)
    {
        return new string('0', zeroCount) + value.ToString();
    }
}
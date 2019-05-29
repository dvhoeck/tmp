using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ConversionHelper
    {
        public static byte ConvertTrimbleHexToByte(string hexCode)
        {
            return Convert.ToByte(hexCode, 16);
        }

        public static int ConvertTrimbleHexToInt(string hexCode)
        {
            return Convert.ToInt32(hexCode.Replace("0x", ""), 16);
        }

        public static string ConvertTrimbleHexToString(string hexCode)
        {
            var value = Convert.ToInt32(hexCode.Replace("0x", ""), 16);

            // Get the character corresponding to the integral value. 
            return Char.ConvertFromUtf32(value);
        }

        
    }
}

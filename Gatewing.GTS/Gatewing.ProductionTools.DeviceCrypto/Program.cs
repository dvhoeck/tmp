using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DeviceCrypto
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new InvalidOperationException("This exe expects one data parameter. (You should pass some data before we can encrypt it :). ");

            var crypto = new Delair.ProductionTools.Crypto.CryptoHandler();

            var data = args[0];

            var key = crypto.EncryptJSonDataStringAndReturnRandomizedEncryptionKey(ref data);

            Console.WriteLine(key + " " + data);
        }
    }
}
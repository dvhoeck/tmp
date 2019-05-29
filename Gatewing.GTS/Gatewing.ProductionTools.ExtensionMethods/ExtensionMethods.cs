using Newtonsoft.Json;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Gatewing.ProductionTools.ExtensionMethods
{
    public static class Methods
    {
        /// <summary>
        /// Adds a DistinctBy method to IEnumerables that allows us to refer to a property in a collection of entities. All elements in the collection must then have unique values
        /// for that particular property.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="predicate">The expression to test the items against.</param>
        ///<returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return retVal;
                retVal++;
            }
            return -1;
        }

        ///<summary>Finds the index of the first occurence of an item in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="item">The item to find.</param>
        ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item) { return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i)); }

        /// <summary>
        /// Determines whether [bit is set] for the specified byte.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public static bool IsBitSet(this byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        /// <summary>
        /// Removes illegal characters from a string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveIllegalChars(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            text = r.Replace(text, "");

            return text.ToString();
        }

        /// <summary>
        /// Serializes an object to a JSON string
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static string SerializeToJSon(this object _object)
        {
            return JsonConvert.SerializeObject(_object);
        }

        /// <summary>
        /// Sets the progress bar value, without using Windows Aero animation
        /// </summary>
        public static void SetProgressNoAnimation(this ProgressBar pb, int value)
        {
            // To get around this animation, we need to move the progress bar backwards.
            if (value == pb.Maximum)
            {
                // Special case (can't set value > Maximum).
                pb.Value = value;           // Set the value
                pb.Value = value - 1;       // Move it backwards
            }
            else
            {
                pb.Value = value + 1;       // Move past
            }
            pb.Value = value;               // Move to correct value
        }

        /// <summary>
        /// Creates a subarray from an existing array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Converts a string to a bytes array.
        /// </summary>
        /// <param name="myString">My string.</param>
        /// <returns></returns>
        public static byte[] ToBytesArray(this string myString)
        {
            return Encoding.Default.GetBytes(myString);
        }

        /// <summary>
        /// Converts a string to its hex / bytes representation
        /// </summary>
        /// <param name="myString">My string.</param>
        /// <returns></returns>
        public static string ToBytesString(this string myString)
        {
            var ba = Encoding.Default.GetBytes(myString);
            return BitConverter.ToString(ba).Replace("-", " ");
        }

        /// <summary>
        /// Returns an enum's display name annotation
        /// </summary>
        /// <param name="evaluationState">State of the evaluation.</param>
        /// <returns></returns>
        public static string ToFriendlyName(this Enum evaluationState)
        {
            var enumType = evaluationState.GetType();
            string displayName = "";
            MemberInfo info = enumType.GetMember(evaluationState.ToString()).First();

            if (info != null && info.CustomAttributes.Any())
            {
                DisplayAttribute nameAttr = info.GetCustomAttribute<DisplayAttribute>();
                displayName = nameAttr != null ? nameAttr.Name : evaluationState.ToString();
            }
            else
            {
                displayName = evaluationState.ToString();
            }
            return displayName;
        }

        /// <summary>
        /// Formats a string to OK or NOK according a boolean value.
        /// </summary>
        /// <param name="boolVar">if set to <c>true</c> [bool variable].</param>
        /// <returns></returns>
        public static string ToOkOrNok(this bool boolVar)
        {
            return boolVar ? "OK" : "NOK";
        }

        /// <summary>
        /// Formats a string to yes or no according a boolean value.
        /// </summary>
        /// <param name="boolVar">if set to <c>true</c> [bool variable].</param>
        /// <returns></returns>
        public static string ToYesOrNo(this bool boolVar)
        {
            return boolVar ? "Yes" : "No";
        }
    }
}
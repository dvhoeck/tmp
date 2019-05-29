using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ConfigBase<T> where T : new()
    {
        public string _path;

        /*
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);*/

        // static holder for instance, need to use lambda to construct since constructor private
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static T Instance { get { return _instance.Value; } }

        /// <summary>
        /// Sets the path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void SetPath(string path)
        {
            _path = path;
        }

        /// <summary>
        /// Loads the ini file.
        /// </summary>
        /// <param name="INIPath">The ini path.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Could not find application ini file.</exception>
        public bool LoadIniFile(string INIPath)
        {
            _path = INIPath;
            if (!File.Exists(_path))
                throw new FileNotFoundException("Could not find application ini file.");

            try
            {
                var configEntries = File.ReadAllLines(_path).Where(line => line.Contains("="));
                foreach (var line in configEntries)
                {
                    var key = line.Split('=')[0];
                    var value = line.Split('=')[1];
                    var propertyInfo = this.GetType().GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo != null)
                    {
                        SetPropertyValue(propertyInfo, value);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        private void SetPropertyValue(PropertyInfo propertyInfo, object value)
        {
            var type = propertyInfo.PropertyType;

            if (type == typeof(int))
                propertyInfo.SetValue(this, Convert.ToInt32(value.ToString()));

            if (type == typeof(string))
                propertyInfo.SetValue(this, value.ToString());

            if (type == typeof(decimal))
                propertyInfo.SetValue(this, Convert.ToDecimal(value.ToString()));

            if (type == typeof(bool))
                propertyInfo.SetValue(this, Convert.ToBoolean(value.ToString()));

            if (type == typeof(TimeSpan))
                propertyInfo.SetValue(this, TimeSpan.Parse(value.ToString()));

            if (type == typeof(List<string>))
            {
                var valueList = new List<string>();
                valueList.AddRange(((string)value).Split(';'));
                propertyInfo.SetValue(this, valueList);
            }
        }

        /*

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this._path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this._path);
            return temp.ToString();
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public int IniReadValueAsInt32(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this._path);
            return Convert.ToInt32(temp.ToString());
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public bool IniReadValueAsBool(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this._path);
            return Convert.ToBoolean(temp.ToString());
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public decimal IniReadValueAsDecimal(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this._path);
            return Convert.ToDecimal(temp.ToString());
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public TimeSpan IniReadValueAsTimeSpan(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this._path);
            return TimeSpan.Parse(temp.ToString());
        }

        */
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastMember;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Data.Common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;

namespace DatabaseConnector
{
    public static class AccessDataMapper
    {
        /// <summary>
        /// Handles conversion of data contained in a SqlReader into an object of type T
        /// </summary>
        /// <typeparam name="T">
        /// Object type to return
        /// </typeparam>
        /// <param name="dataReader">
        /// SqlDataReader containing data to be parsed into an object
        /// </param>
        /// <returns></returns>
        public static List<T> ConvertToObject<T>(DbDataReader dataReader) where T : class, new()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Formatting = Formatting.Indented
            };

            DataTable dt = new DataTable();
            dt.Load(dataReader);
            dataReader.Close();
            string JSONString = JsonConvert.SerializeObject(dt, settings);
            dt.Clear();
            return getObjList<T>(JSONString);
        }

        /// <summary>
        ///     This function converts a JSON string into and object of type T
        /// </summary>
        /// <param name="inputJSON">
        ///     Input path to the .json file containing IO data for the current project
        /// </param>
        /// <returns></returns>
        public static List<T> getObjList<T>(string inputJSON) where T : class, new()
        {
            List<T> objlist = new List<T>();

            JArray obj = JArray.Parse(inputJSON);
            foreach (JToken json in obj)
            {
                try { objlist.Add(json.ToObject<T>()); }
                catch (Exception e)
                { 
                    continue; 
                }
            }
            return objlist;
        }
    }

    public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
    where T : INotifyPropertyChanged
    {
        public TrulyObservableCollection()
        {
            CollectionChanged += FullObservableCollectionCollectionChanged;
        }

        public TrulyObservableCollection(IEnumerable<T> pItems) : this()
        {
            foreach (var item in pItems)
            {
                this.Add(item);
            }
        }

        private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
                }
            }
        }

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
            OnCollectionChanged(args);
        }
    }
}

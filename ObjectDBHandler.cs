using DatabaseConnector.Attributes;
using FastMember;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;

namespace DatabaseConnector
{
    /// <summary>
    /// Add, Edit, Delete any class type object to its respective database table
    /// </summary>
    public class ObjectDBHandler
    {
        Log log = Logging.GetLogger(typeof(ObjectDBHandler));
        /// <summary>
        /// Save to datebase
        /// </summary>
        public bool Save<T>(object obj, string columnToUpdate = null)
        {         
            try
            {
                T getterObj = (T)obj;
                ObjectAccessor getter = ObjectAccessor.Create(getterObj);
                string table_name = typeof(T).Name;              
                List<string> propertiesFromDB = GetColumnNamesInDatabase<T>(true); //get column names
                PropertyInfo[] propertiesFromCode = typeof(T).GetProperties().Where(p => propertiesFromDB.Contains(p.Name)).ToArray();
                PropertyInfo pkProperty = FindPrimaryKey(propertiesFromCode);
                if(pkProperty == null)
                {
                    log.Debug($"Unable to location Primary Key property for {table_name}");
                    return false;
                }
                string pkColumn = $"{pkProperty.Name}"; //To get Id, must remove the trailing s from table name
                string pkValue = getter[pkColumn]?.ToString();
                
                if(!string.IsNullOrEmpty(columnToUpdate) )
                {
                    propertiesFromCode = propertiesFromCode.Where(p => p.Name == columnToUpdate).ToArray();
                }

                //Set sql command
                string sqlCommand = "";
                bool firstLoop = true;
                //If object exists in database, update it
                if (checkIfItemExists<T>(obj))
                {
                    sqlCommand = $"UPDATE [GeTurbAutoTools_ACME].[dbo].[{table_name}] SET ";
                    foreach (PropertyInfo property in propertiesFromCode)
                    {
                        string property_name = property.Name;
                        if (property_name.Equals(pkColumn))
                            continue;

                        string property_value = getter[property.Name]?.ToString();
                        int? maxValueLength = try_FindMaxValueLength(property);
                        if (property_value != null && maxValueLength != null && maxValueLength > 0)//Check column value length restrictions
                        {
                            int stringLength = property_value.Length > (int)maxValueLength ? (int)maxValueLength : property_value.Length;
                            property_value = property_value.Substring(0, stringLength);
                        }

                        if (firstLoop)
                        {
                            firstLoop = false;
                            goto SetSql;
                        }
                        sqlCommand += ", ";

                    SetSql:
                        if (property_name == "Option_Default_Value_Id" && (string.IsNullOrEmpty(property_value) || "0".Equals(property_value)))
                        {
                            sqlCommand += $"[{property_name}] = NULL";
                            continue;
                        }
                        sqlCommand += $"[{property_name}] = '{property_value}'";
                    }

                    sqlCommand += $" WHERE {pkColumn} = '{pkValue}'";
                }
                else //new object- save to database
                {
                    sqlCommand = $"INSERT INTO [GeTurbAutoTools_ACME].[dbo].[{table_name}]";
                    foreach (PropertyInfo property in propertiesFromCode)
                    {
                        string property_name = property.Name;
                        if (property_name.Equals(pkColumn))
                            continue;

                        if (firstLoop)
                        {
                            firstLoop = false;
                            sqlCommand += " (";
                            goto SetSql;
                        }
                        sqlCommand += ", ";

                    SetSql:
                        sqlCommand += $"[{property_name}]";
                    }

                    firstLoop = true;
                    sqlCommand += $") VALUES ";
                    foreach (PropertyInfo property in propertiesFromCode)
                    {
                        string property_name = property.Name;
                        if (property_name.Equals(pkColumn))
                            continue;
                        

                        string property_value = getter[property.Name]?.ToString();
                        int? maxValueLength = try_FindMaxValueLength(property);
                        if(property_value != null && maxValueLength !=  null && maxValueLength > 0)//Check property length restrictions
                        {
                            int stringLength = property_value.Length > (int)maxValueLength ? (int)maxValueLength : property_value.Length;
                            property_value = property_value.Substring(0, stringLength);
                        }

                        if (firstLoop)
                        {
                            firstLoop = false;
                            sqlCommand += " (";
                            goto SetSql;
                        }
                        sqlCommand += ", ";

                    SetSql:
                        if (property_name == "Option_Default_Value_Id" && (string.IsNullOrEmpty((getter[property_name]?.ToString())) || "0".Equals(getter[property_name]?.ToString())))
                        {
                            sqlCommand += $"NULL";
                            continue;
                        }
                        sqlCommand += $"'{property_value}'";
                    }
                    sqlCommand += $"); ";
                }

                bool itemSaved = ACMEDBConnection.RunQry_Bool(sqlCommand);

                if (itemSaved)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Debug(e);
                return false;
            }
        }

        /// <summary>
        /// Delete from database
        /// </summary>
        public bool Delete<T>(Object obj)
        {
            try
            {
                T getterObj = (T)obj;
                ObjectAccessor getter = ObjectAccessor.Create(getterObj);
                string table_name = typeof(T).Name;
                List<string> propertiesFromDB = GetColumnNamesInDatabase<T>(true);
                PropertyInfo[] propertiesFromCode = typeof(T).GetProperties().Where(p => propertiesFromDB.Contains(p.Name)).ToArray();
                PropertyInfo pkProperty = FindPrimaryKey(propertiesFromCode);
                if (pkProperty == null)
                {
                    log.Debug($"Unable to location Primary Key property for {table_name}");
                    return false;
                }


                string pkColumn = $"{pkProperty.Name}"; //To get Id, must remove the trailing s from table name
                string pkValue = getter[pkColumn]?.ToString();
                string sqlCommand = $"DELETE FROM [GeTurbAutoTools_ACME].[dbo].[{table_name}] WHERE {pkColumn} = '{pkValue}'";

                bool itemDeleted = ACMEDBConnection.RunQry_Bool(sqlCommand);
                if (itemDeleted)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Debug(e);
                return false;
            }
        }

        private bool checkIfItemExists<T>(object obj)
        {
            try
            {
                T getterObj = (T)obj;
                ObjectAccessor getter = ObjectAccessor.Create(getterObj);
                string table_name = typeof(T).Name;
                //get column names
                List<string> propertiesFromDB = GetColumnNamesInDatabase<T>(true);
                PropertyInfo[] propertiesFromCode = typeof(T).GetProperties().Where(p => propertiesFromDB.Contains(p.Name)).ToArray();
                PropertyInfo pkProperty = FindPrimaryKey(propertiesFromCode);
                if (pkProperty == null)
                {
                    log.Debug($"Unable to location Primary Key property for {table_name}");
                    return false;
                }
                string pkColumn = $"{pkProperty.Name}"; //To get Id, must remove the trailing s from table name
                string pkID = getter[pkColumn]?.ToString();
                if(string.IsNullOrEmpty(pkID) || pkID == "0")
                    return false;
                
                string sql = $"SELECT TOP 1 {pkColumn} FROM [GeTurbAutoTools_ACME].[dbo].[{table_name}] WHERE {pkColumn} = '{pkID}';";
                int returned = ACMEDBConnection.RunQry_Int(sql);
                if (returned.ToString() == pkID)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public List<string> GetColumnNamesInDatabase<T>(bool includePrimaryKey = false)
        {
            string table_name = typeof(T).Name;
            string pkColumn = $"{table_name.Remove(table_name.Length - 1)}_Id";

            //get column names
            List<string> properties = new List<string>();
            string getColumns_Command = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{table_name}'";
            using (DbDataReader dataReader = ACMEDBConnection.RunQry_DataReader(getColumns_Command))
            {
                while (dataReader.Read())
                {
                    //Skip the primary key Id
                    if ( (!includePrimaryKey) && dataReader.GetString(0) == pkColumn)
                        continue;

                    properties.Add(dataReader.GetString(0));
                }
            }
            return properties;
        }

        public PropertyInfo FindPrimaryKey(PropertyInfo[] properties)
        {
            PropertyInfo primaryKeyPropery = null;
            foreach (PropertyInfo property in properties)
            {
                System.Attribute primaryKeyAttribute = System.Attribute.GetCustomAttribute(property, typeof(PrimaryKeyAttribute));
                if(primaryKeyAttribute != null)
                    primaryKeyPropery = property;
            }
            return primaryKeyPropery;
        }

        private int? try_FindMaxValueLength(PropertyInfo property)
        {
            int? length = null;
                System.Attribute maxLengthAttribute = System.Attribute.GetCustomAttribute(property, typeof(ValueLengthAttribute));
                if (maxLengthAttribute != null)
                    length = (int)property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ValueLengthAttribute)).ConstructorArguments[0].Value;
            return length;
        }
    }
}

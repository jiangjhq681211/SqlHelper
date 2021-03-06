using Com.EnjoyCodes.Model.DynamicMethod;
using Com.EnjoyCodes.SqlAttribute;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Com.EnjoyCodes.SqlHelper
{
    public sealed class SqlHelper
    {
        #region Constructures & Private Utility Methods
        private SqlHelper() { }

        /// <summary>
        /// This method is used to attach array of SqlParameters to a SqlCommand.
        /// This method will assign a value of DbNull to any parameter with a direction of
        /// InputOutput and a value of null.  
        /// This behavior will prevent default values from being used, but
        /// this will be the less common case than an intended pure output parameter (derived as InputOutput)
        /// where the user provided no input value.
        /// </summary>
        /// <param name="command">The command to which the parameters will be added</param>
        /// <param name="commandParameters">an array of SqlParameters tho be added to command</param>
        private static void attachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            foreach (SqlParameter p in commandParameters)
            {
                //check for derived output value with no value assigned
                if ((p.Direction == ParameterDirection.InputOutput) && (p.Value == null))
                    p.Value = DBNull.Value;

                command.Parameters.Add(p);
            }
        }

        /// <summary>
        /// This method opens (if necessary) and assigns a connection, transaction, command type and parameters 
        /// to the provided command.
        /// </summary>
        /// <param name="command">the SqlCommand to be prepared</param>
        /// <param name="connection">a valid SqlConnection, on which to execute this command</param>
        /// <param name="transaction">a valid SqlTransaction, or 'null'</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParameters to be associated with the command or 'null' if no parameters are required</param>
        private static void prepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters)
        {
            //if the provided connection is not open, we will open it
            if (connection.State != ConnectionState.Open)
                connection.Open();

            //associate the connection with the command
            command.Connection = connection;

            //set the command text (stored procedure name or SQL statement)
            command.CommandText = commandText;

            //if we were provided a transaction, assign it.
            if (transaction != null)
                command.Transaction = transaction;

            //set the command type
            command.CommandType = commandType;

            //attach the command parameters if they are provided
            if (commandParameters != null)
                attachParameters(command, commandParameters);
        }
        #endregion

        #region GetConnectionString
        /// <summary>
        /// 获取读写操作的数据库连接字符串
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetConnectionString_RW(Type type)
        {
            string connectionStr = string.Empty;
            if (type != null)
            {
                string ns = type.Namespace;
                if (!string.IsNullOrEmpty(ns))
                {
                    string key = string.Empty;
                    switch (ns)
                    {
                        case "Com.EnjoyCodes.SqlHelper":
                        default: key = "MSSQLConnectionString"; break;
                    }
                    connectionStr = GetConnectionString(key);
                }
            }
            return connectionStr;
        }

        public static string GetConnectionString(string key)
        { return ConfigurationManager.AppSettings[key]; }
        #endregion

        #region ExecuteNonQuery
        public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText)
        { return ExecuteNonQuery(connectionString, commandType, commandText, (SqlParameter[])null); }

        public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            int result = 0;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                try
                {
                    result = ExecuteNonQuery(cn, commandType, commandText, commandParameters);
                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }

            return result;
        }

        public static int ExecuteNonQuery(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            prepareCommand(cmd, connection, (SqlTransaction)null, commandType, commandText, commandParameters);

            //finally, execute the command.
            int retval = cmd.ExecuteNonQuery();

            // detach the SqlParameters from the command object, so they can be used again.
            cmd.Parameters.Clear();

            return retval;
        }
        #endregion

        #region ExecuteDataSet
        public static DataSet ExecuteDataSet(string connectionString, CommandType commandType, string commandText)
        { return ExecuteDataSet(connectionString, commandType, commandText, null); }

        public static DataSet ExecuteDataSet(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            DataSet result = null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                try
                {
                    result = ExecuteDataSet(cn, commandType, commandText, commandParameters);
                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }

            return result;
        }

        public static DataSet ExecuteDataSet(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            prepareCommand(cmd, connection, (SqlTransaction)null, commandType, commandText, commandParameters);

            //create the DataAdapter & DataSet
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();

            //fill the DataSet using default values for DataTable names, etc.
            da.Fill(ds);

            // detach the SqlParameters from the command object, so they can be used again.			
            cmd.Parameters.Clear();

            //return the dataset
            return ds;
        }
        #endregion

        #region ExecuteScalar
        public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText)
        { return ExecuteScalar(connectionString, commandType, commandText, null); }

        public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            object result = null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                try
                {
                    result = ExecuteScalar(cn, commandType, commandText, commandParameters);
                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }
            return result;
        }

        public static object ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            prepareCommand(cmd, connection, (SqlTransaction)null, commandType, commandText, commandParameters);

            object retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();
            return retval;
        }

        public static bool IsExists(string connectionString, string commandText)
        { return Convert.ToInt32(ExecuteScalar(connectionString, CommandType.Text, commandText, null)) != 0; }

        public static bool IsExists(string connectionString, string commandText, params SqlParameter[] commandParameters)
        { return Convert.ToInt32(ExecuteScalar(connectionString, CommandType.Text, commandText, commandParameters)) != 0; }
        #endregion
    }

    public class SqlHelper<T>
    {
        #region Members & Utility Methods
        /// <summary>
        /// C#类型与SQLServer类型对照字典
        /// </summary>
        public static Dictionary<Type, SqlDbType> SqlDbTypes = new Dictionary<Type, SqlDbType>() {
            {typeof(long),SqlDbType.BigInt},
            {typeof(int),SqlDbType.Int},
            {typeof(short),SqlDbType.SmallInt},
            {typeof(byte),SqlDbType.TinyInt},
            {typeof(decimal),SqlDbType.Decimal},
            {typeof(double),SqlDbType.Float},
            {typeof(float),SqlDbType.Real},
            {typeof(bool),SqlDbType.Bit},
            {typeof(string),SqlDbType.NVarChar},
            {typeof(char),SqlDbType.Char},
            {typeof(DateTime),SqlDbType.DateTime},
            {typeof(TimeSpan),SqlDbType.Timestamp},
            {typeof(Guid),SqlDbType.UniqueIdentifier},
            {typeof(Enum),SqlDbType.Int}
        };

        private static void fill(T obj, DynamicMethod<T> dm, IDataReader dr, string columnPrefix, PropertyInfo[] properties)
        {
            foreach (var item in properties)
                try
                {
                    object v = dr[columnPrefix + item.Name];
                    //if (v != null) PropertyAccessor.Set(obj, item.Name, v);
                    if (v != null) dm.SetValue(obj, item.Name, convertObject(v, item.PropertyType));
                }
                catch { }
        }
        private static void fill(T obj, IDataReader dr, List<PropertyInfo> fkProperties)
        {
            var dm = new DynamicMethod<T>();
            string modelColumnName = dr.GetName(0).ToString();
            string modelName = dr["MODELNAME"].ToString();

            // 关联表赋值
            PropertyInfo property = fkProperties.First(f => f.PropertyType.IsGenericType ? f.PropertyType.GenericTypeArguments[0].Name == modelName : f.PropertyType.Name == modelName);
            Type type = null;
            string fullName = string.Empty;
            if (property.PropertyType.IsGenericType)
            {
                // 泛型
                type = property.PropertyType.GenericTypeArguments[0];
                fullName = property.PropertyType.GenericTypeArguments[0].FullName;
            }
            else
            {
                type = property.PropertyType;
                fullName = property.PropertyType.FullName;
            }

            string columnPrefix = GetTableAttributes(type).Item3; // 获取关联表的字段前缀
            var detail = Assembly.GetAssembly(type).CreateInstance(fullName); // 创建关联表的空对象
            var dmDetail = new DynamicMethod(detail);
            PropertyInfo tProperty = typeof(T).GetProperty(property.Name); // 获取关联表的属性

            if (tProperty.PropertyType.IsGenericType)
            {
                /*
                 * 泛型
                 *  向泛型中添加新元素
                 */
                var details = dm.GetValue(obj, tProperty.Name); // tProperty.GetValue(obj);
                if (details == null)
                {
                    // 初始化泛型对象
                    details = Assembly.GetAssembly(tProperty.PropertyType).CreateInstance(tProperty.PropertyType.FullName);
                    dm.SetValue(obj, tProperty.Name, details); // tProperty.SetValue(obj, details);
                }

                // 添加新元素
                MethodInfo methodInfo = tProperty.PropertyType.GetMethod("Add");
                methodInfo.Invoke(details, new object[] { detail });
            }
            else
            {
                /*
                 * 非泛型
                 *  关联表赋值给主表
                 */

                dm.SetValue(obj, tProperty.Name, detail); // tProperty.SetValue(obj, detail);
            }

            // 反射取值
            PropertyInfo[] properties = type.GetProperties();
            foreach (var item in properties)
                try
                {
                    object v = dr[columnPrefix + item.Name];
                    if (v != null) dmDetail.SetValue(tProperty.Name, convertObject(v, item.PropertyType)); //item.SetValue(detail, convertObject(v, item.PropertyType));
                }
                catch { }
        }
        private static void fill(List<T> objs, IDataReader dr, PropertyInfo pkProperty, List<PropertyInfo> fkProperties)
        {
            string pk = dr["PK"].ToString();

            T obj = objs.FirstOrDefault(f => pkProperty.GetValue(f).ToString() == pk);
            if (obj != null) fill(obj, dr, fkProperties);
        }

        /// <summary>
        /// 将一个对象转换为指定类型
        /// </summary>
        /// <param name="obj">待转换的对象</param>
        /// <param name="type">目标类型</param>
        /// <returns></returns>
        private static object convertObject(object obj, Type type)
        {
            if (type == null) return obj;
            if (obj == null || string.IsNullOrEmpty(obj.ToString())) return type.IsValueType ? Activator.CreateInstance(type) : null;

            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (type.IsAssignableFrom(obj.GetType()))
            {
                // 如果待转换对象的类型与目标类型兼容，则无需转换
                return obj;
            }
            else if ((underlyingType ?? type).IsEnum)
            {
                // 如果待转换的对象的基类型为枚举

                if (underlyingType != null && string.IsNullOrEmpty(obj.ToString()))
                {
                    // 如果目标类型为可空枚举，并且待转换对象为null 则直接返回null值
                    return null;
                }
                else
                    return Enum.Parse(underlyingType ?? type, obj.ToString());
            }
            else if (typeof(IConvertible).IsAssignableFrom(underlyingType ?? type))
            {
                // 如果目标类型的基类型实现了IConvertible，则直接转换
                try
                {
                    return Convert.ChangeType(obj, underlyingType ?? type, null);
                }
                catch
                {
                    return underlyingType == null ? Activator.CreateInstance(type) : null;
                }
            }
            else
            {
                System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(obj.GetType()))
                    return converter.ConvertFrom(obj);

                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    object o = constructor.Invoke(null);
                    var dmDetail = new DynamicMethod(o);
                    PropertyInfo[] propertys = type.GetProperties();
                    Type oldType = obj.GetType();
                    foreach (PropertyInfo property in propertys)
                    {
                        PropertyInfo p = oldType.GetProperty(property.Name);
                        if (property.CanWrite && p != null && p.CanRead)
                            dmDetail.SetValue(property.Name, convertObject(p.GetValue(obj, null), property.PropertyType)); // property.SetValue(o, convertObject(p.GetValue(obj, null), property.PropertyType), null);
                    }
                    return o;
                }
            }
            return obj;
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetDefaultValue(Type type) { return type.IsValueType ? Activator.CreateInstance(type) : null; }

        /// <summary>
        /// 获取模型属性
        /// </summary>
        /// <returns>表名、主键、前缀</returns>
        public static Tuple<string, string, string> GetTableAttributes(Type type)
        {
            string tableName = string.Empty;
            string prefix = string.Empty;
            string key = string.Empty;

            // 表属性
            TableAttribute tableAttribute = (TableAttribute)Attribute.GetCustomAttribute(type, typeof(TableAttribute));
            if (tableAttribute != null)
            {
                tableName = tableAttribute.Name;
                prefix = tableAttribute.Prefix;
            }
            else
            {
                tableName = type.Name;
            }

            // 主键
            PropertyInfo[] properties = type.GetProperties();
            foreach (var item in properties)
            {
                object attr = item.GetCustomAttribute(typeof(Attribute), true);
                if (attr is KeyAttribute)
                    key = item.Name;
            }

            if (string.IsNullOrEmpty(key))
                throw new Exception("未指定主键！", new Exception("请指定主键KeyAttribute属性。"));

            return Tuple.Create<string, string, string>(tableName, key, prefix);
        }

        /// <summary>
        /// 获取模型的外键属性
        /// </summary>
        /// <returns></returns>
        public static List<PropertyInfo> GetForeignKeyProperties(Type type)
        {
            List<PropertyInfo> fKProperties = new List<PropertyInfo>();
            PropertyInfo[] properties = type.GetProperties();
            foreach (var item in properties)
            {
                object attr = item.GetCustomAttribute(typeof(Attribute), true);
                if (attr is ForeignKeyAttribute)
                    fKProperties.Add(item);
            }
            return fKProperties;
        }

        /// <summary>
        /// 获取查询字符串
        ///     主外表两级查询
        /// </summary>
        /// <param name="sqlWhere"></param>
        /// <returns></returns>
        public static string GetReadString(string sqlWhere)
        {
            if (!string.IsNullOrEmpty(sqlWhere))
                sqlWhere = "WHERE " + sqlWhere;
            StringBuilder sqlStr = new StringBuilder();

            // 主表sql
            Tuple<string, string, string> t0 = GetTableAttributes(typeof(T));
            sqlStr.AppendFormat("SELECT '{0}' MODELNAME,{1} PK,* FROM {2} {3};", typeof(T).Name, t0.Item3 + t0.Item2, t0.Item1, sqlWhere);

            // 关联表sql
            List<PropertyInfo> fKProperties = GetForeignKeyProperties(typeof(T));
            PropertyInfo[] tProperties = typeof(T).GetProperties();
            if (fKProperties.Count > 0)
                foreach (var item in fKProperties)
                {
                    ForeignKeyAttribute fk = (ForeignKeyAttribute)item.GetCustomAttribute(typeof(ForeignKeyAttribute), true); // 外键属性
                    Type type = null; // 外表类型
                    if (item.PropertyType.IsGenericType)
                    {
                        /*
                         * 泛型
                         *  一对多查询
                         *  主表主键与外表字段关联
                         */
                        type = item.PropertyType.GenericTypeArguments[0];
                        Tuple<string, string, string> t1 = GetTableAttributes(type);
                        sqlStr.AppendFormat("SELECT '{0}' MODELNAME,{2} PK,* FROM {1} WHERE {2} IN(SELECT {3} FROM {4} {5});", type.Name, t1.Item1, t1.Item3 + fk.Name, t0.Item3 + t0.Item2, t0.Item1, sqlWhere);
                    }
                    else
                    {
                        /*
                         * 非泛型
                         *  一对一查询
                         */
                        type = item.PropertyType;
                        Tuple<string, string, string> t1 = GetTableAttributes(type);
                        if (tProperties.FirstOrDefault(f => f.Name == fk.Name) != null)
                        {
                            // 主表字段与外表主键关联
                            sqlStr.AppendFormat("SELECT '{0}' MODELNAME,{1}.{2} PK,{3}.* FROM {3} RIGHT JOIN {1} ON {3}.{4}={1}.{5} WHERE {3}.{4} IN(SELECT {5} FROM {1} {6});", type.Name, t0.Item1, t0.Item3 + t0.Item2, t1.Item1, t1.Item3 + t1.Item2, t0.Item3 + fk.Name, sqlWhere);
                        }
                        else
                        {
                            // 主表主键与外表字段关联
                            sqlStr.AppendFormat("SELECT '{0}' MODELNAME,{2} PK,* FROM {1} WHERE {2} IN(SELECT {3} FROM {4} {5});", type.Name, t1.Item1, t1.Item3 + fk.Name, t0.Item3 + t0.Item2, t0.Item1, sqlWhere);
                        }
                    }
                }

            return sqlStr.ToString();
        }
        #endregion

        #region Table Handler
        public static int CreateTable(string connectionString)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            var tns = t.Item1.Split('.');
            var tableName = tns.Length > 0 ? tns[tns.Length - 1] : t.Item1;
            return CreateTable(connectionString, tableName, t.Item2, t.Item3);
        }
        public static int CreateTable(string connectionString, string modelTableName, string modelPrimaryKey, string columnPrefix)
        {
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.AppendFormat("CREATE TABLE [{0}](", modelTableName);

            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (var item in properties)
            {
                try
                {
                    if (!item.PropertyType.IsSealed) continue;

                    sqlStr.AppendFormat("[{0}{1}] ", columnPrefix, item.Name);
                    if (item.PropertyType == typeof(string))
                        sqlStr.AppendFormat("{0}(max)", SqlDbTypes[item.PropertyType]);
                    else if (item.PropertyType.IsEnum)
                        sqlStr.AppendFormat("{0}", SqlDbTypes[typeof(Enum)]);
                    else
                        sqlStr.AppendFormat("{0}", SqlDbTypes[item.PropertyType]);

                    if (item.Name.ToLower() == modelPrimaryKey.ToLower())
                    {
                        sqlStr.Append(" PRIMARY KEY ");
                        if (item.PropertyType == typeof(Int64) || item.PropertyType == typeof(Int32) || item.PropertyType == typeof(Int16))
                            sqlStr.Append("IDENTITY");
                    }
                    sqlStr.Append(",");
                }
                catch { }
            }

            sqlStr.Append(")");

            return SqlHelper.ExecuteNonQuery(connectionString, CommandType.Text, sqlStr.ToString());
        }
        #endregion

        #region CRUD,List&Page
        public static object Create(string connectionString, T model)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return Create(connectionString, model, t.Item1, t.Item2, t.Item3);
        }
        /// <summary>
        /// 添加一条表数据
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="model"></param>
        /// <param name="modelTableName">表名称</param>
        /// <param name="modelPrimaryKey">表主键</param>
        /// <param name="columnPrefix">表字段前缀</param>
        /// <returns></returns>
        public static object Create(string connectionString, T model, string modelTableName, string modelPrimaryKey, string columnPrefix)
        {
            object primaryKeyValue = null;
            Type modelPrimaryKeyType = typeof(T).GetProperty(modelPrimaryKey).PropertyType;

            // 获取有值的属性
            PropertyInfo[] properties = typeof(T).GetProperties();
            List<PropertyInfo> propertyInfoes = new List<PropertyInfo>();
            List<object> values = new List<object>();
            foreach (var item in properties)
                try
                {
                    if (item.PropertyType.IsSealed // 密封类
                        && (item.Name != modelPrimaryKey || (item.Name == modelPrimaryKey && (item.PropertyType != typeof(Int16) && item.PropertyType != typeof(Int32) & item.PropertyType != typeof(Int64)))) // 非主键或非自增字段的主键
                        && (item.PropertyType.IsValueType || (!item.PropertyType.IsValueType && item.GetValue(model) != GetDefaultValue(item.PropertyType))) // 值类型或非空的引用类型
                        )
                    {
                        propertyInfoes.Add(item);
                        values.Add(item.PropertyType.IsEnum ? (int)item.GetValue(model) : item.GetValue(model)); // 枚举类型，保存int值
                    }
                }
                catch { }

            // INSERT SQL 字符串
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.AppendFormat("INSERT INTO {0}({1}) VALUES({2});", modelTableName, string.Join(",", propertyInfoes.Select(k => columnPrefix + k.Name)), "@" + string.Join(",@", propertyInfoes.Select(k => k.Name)));
            if (modelPrimaryKeyType != typeof(Guid))
                sqlStr.Append("SET @ID_FYUJMNBVFGHJ=SCOPE_IDENTITY();");

            // 参数设置
            List<SqlParameter> parameters = new List<SqlParameter>();
            for (int i = 0; i < propertyInfoes.Count; i++)
                parameters.Add(new SqlParameter()
                {
                    ParameterName = "@" + propertyInfoes[i].Name,
                    SqlDbType = SqlDbTypes[values[i].GetType()],
                    Value = values[i]
                });

            // 输出主键
            if (modelPrimaryKeyType != typeof(Guid))
                parameters.Add(new SqlParameter() { ParameterName = "@ID_FYUJMNBVFGHJ", SqlDbType = SqlDbTypes[modelPrimaryKeyType], Direction = ParameterDirection.Output });

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                try
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = cn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sqlStr.ToString();
                    cmd.Parameters.AddRange(parameters.ToArray());

                    if (cmd.ExecuteNonQuery() > 0)
                        primaryKeyValue = modelPrimaryKeyType != typeof(Guid) ? parameters[parameters.Count - 1].Value : typeof(T).GetProperty(modelPrimaryKey).GetValue(model);

                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }

            return primaryKeyValue;
        }

        /// <summary>
        /// 级联查询
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sqlWhere"></param>
        /// <returns></returns>
        public static T Read(string connectionString, string sqlWhere)
        { return Read(connectionString, CommandType.Text, GetReadString(sqlWhere)); }

        public static T Read(string connectionString, CommandType commandType, string commandText)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return Read(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, null);
        }
        public static T Read(string connectionString, CommandType commandType, string commandText, string columnPrefix)
        { return Read(connectionString, commandType, commandText, columnPrefix, null); }
        public static T Read(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return Read(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, commandParameters);
        }
        public static T Read(string connectionString, CommandType commandType, string commandText, string columnPrefix, params SqlParameter[] commandParameters)
        {
            T result = default(T);
            List<T> rList = ReadList(connectionString, commandType, commandText, columnPrefix, commandParameters);
            if (rList.Count > 0)
                result = rList[0];

            return result;
        }

        /// <summary>
        /// 级联查询
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns
        public static List<T> ReadList(string connectionString)
        { return ReadList(connectionString, string.Empty); }
        public static List<T> ReadList(string connectionString, string sqlWhere)
        { return ReadList(connectionString, CommandType.Text, GetReadString(sqlWhere)); }

        public static List<T> ReadList(string connectionString, CommandType commandType, string commandText)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return ReadList(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, null);
        }
        public static List<T> ReadList(string connectionString, CommandType commandType, string commandText, string columnPrefix)
        { return ReadList(connectionString, commandType, commandText, columnPrefix, null); }
        public static List<T> ReadList(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return ReadList(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, commandParameters);
        }
        public static List<T> ReadList(string connectionString, CommandType commandType, string commandText, string columnPrefix, params SqlParameter[] commandParameters)
        {
            List<T> result;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                if (commandParameters != null)
                    cmd.Parameters.AddRange(commandParameters);
                cn.Open();
                try
                {
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        result = readList(sdr, columnPrefix);
                        sdr.Close();
                        sdr.Dispose();
                    }
                    cmd.Parameters.Clear();
                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }
            return result;
        }
        private static List<T> readList(IDataReader dr, string columnPrefix)
        {
            // 读主表数据
            var result = new List<T>();
            PropertyInfo[] properties = typeof(T).GetProperties();
            var dm = new DynamicMethod<T>();
            while (dr.Read())
            {
                var obj = Activator.CreateInstance<T>();
                fill(obj, dm, dr, columnPrefix, properties);
                result.Add(obj);
            }

            /*
             * 读关联表数据
             *  主表需指定外键
             */
            List<PropertyInfo> fkProperties = GetForeignKeyProperties(typeof(T));
            if (fkProperties.Count > 0)
            {
                PropertyInfo pkProperty = typeof(T).GetProperty(GetTableAttributes(typeof(T)).Item2);
                while (dr.NextResult())
                    while (dr.Read())
                        fill(result, dr, pkProperty, fkProperties);
            }

            return result;
        }

        /// <summary>
        /// 分页
        ///     级联查询
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static Pager<T> ReadPaging(string connectionString, int pageNumber, int pageSize)
        { return ReadPaging(connectionString, pageNumber, pageSize, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty); }
        /// <summary>
        /// 分页
        ///     级联查询
        /// </summary>
        /// <param name="connectionstring"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="sqlPre">Count之前的DECLARE等Sql</param>
        /// <param name="sqlFields">查询结果字段</param>
        /// <param name="sqlFrom"></param>
        /// <param name="sqlWhere"></param>
        /// <param name="sqlOrderBy"></param>
        /// <returns></returns>
        public static Pager<T> ReadPaging(string connectionstring, int pageNumber, int pageSize, string sqlPre, string sqlFields, string sqlFrom, string sqlWhere, string sqlOrderBy)
        {
            // 查询参数处理
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            if (string.IsNullOrEmpty(sqlFields))
                sqlFields = "*";
            if (string.IsNullOrEmpty(sqlFrom))
                sqlFrom = t.Item1;
            if (!string.IsNullOrEmpty(sqlWhere))
                sqlWhere = "WHERE " + sqlWhere;
            if (string.IsNullOrEmpty(sqlOrderBy))
                sqlOrderBy = t.Item3 + t.Item2;

            // 查询数据行数
            StringBuilder sqlStr = new StringBuilder();
            if (!string.IsNullOrEmpty(sqlPre))
                sqlStr.AppendFormat("{0}", sqlPre);
            sqlStr.AppendFormat("SELECT COUNT(1) FROM {0} {1};", sqlFrom, sqlWhere);

            /*
             * 查询字符串
             *  1.获取分页后的主键ID集合
             *  2.拼接到级联查询字符串中
             *  3.拼接到分页字符串中
             */
            string sqlIDs = string.Format("SELECT F.{7} FROM (SELECT TOP {0} ROW_NUMBER() OVER (ORDER BY {1}) ROWINDEX, {2} FROM {3} {4}) F WHERE F.ROWINDEX BETWEEN {5} AND {6}", pageNumber * pageSize, sqlOrderBy, sqlFields, sqlFrom, sqlWhere, (pageNumber - 1) * pageSize + 1, pageNumber * pageSize, t.Item3 + t.Item2);
            sqlStr.AppendFormat(GetReadString(string.Format("{0} IN({1})", t.Item3 + t.Item2, sqlIDs)));

            // 执行查询
            Pager<T> result = ReadPaging(connectionstring, CommandType.Text, sqlStr.ToString(), t.Item3, null);
            result.PageNumber = pageNumber;
            result.PageSize = pageSize;

            return result;
        }

        public static Pager<T> ReadPaging(string connectionString, CommandType commandType, string commandText)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return ReadPaging(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, null);
        }
        public static Pager<T> ReadPaging(string connectionString, CommandType commandType, string commandText, string columnPrefix)
        { return ReadPaging(connectionString, commandType, commandText, columnPrefix, null); }
        public static Pager<T> ReadPaging(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return ReadPaging(connectionString, commandType, commandText, t == null ? string.Empty : t.Item3, commandParameters);
        }
        public static Pager<T> ReadPaging(string connectionString, CommandType commandType, string commandText, string columnPrefix, params SqlParameter[] commandParameters)
        {
            Pager<T> result = new Pager<T>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                if (commandParameters != null)
                    cmd.Parameters.AddRange(commandParameters);
                try
                {
                    cn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            // 读数据行数
                            result.RecordCount = sdr.GetInt32(0);

                            // 读分页数据
                            if (sdr.NextResult())
                                result.Datas = readList(sdr, columnPrefix);
                        }
                        sdr.Close();
                        sdr.Dispose();
                    }
                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }
            return result;
        }

        public static int Update(string connectionString, T model)
        {
            Tuple<string, string, string> t = GetTableAttributes(typeof(T));
            return Update(connectionString, model, t.Item1, t.Item2, t.Item3);
        }
        /// <summary>
        /// 更新一条表数据
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="model"></param>
        /// <param name="modelTableName">表名称</param>
        /// <param name="modelPrimaryKey">表主键</param>
        /// <param name="columnPrefix">表字段前缀</param>
        /// <returns></returns>
        public static int Update(string connectionString, T model, string modelTableName, string modelPrimaryKey, string columnPrefix)
        {
            int result = 0;

            // 获取有值的属性
            PropertyInfo[] properties = typeof(T).GetProperties();
            List<PropertyInfo> propertyInfoes = new List<PropertyInfo>();
            List<object> values = new List<object>();
            var dm = new DynamicMethod<T>();
            foreach (var item in properties)
            {
                if (!item.PropertyType.IsSealed) continue;

                object obj = dm.GetValue(model, item.Name); // item.GetValue(model);
                if (obj != null)
                {
                    propertyInfoes.Add(item);
                    values.Add(obj);
                }
            }

            // UPDATE SQL 字符串
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.AppendFormat("UPDATE {0} SET ", modelTableName);
            foreach (var item in propertyInfoes)
                if (item.Name.ToLower() != modelPrimaryKey.ToLower())
                    sqlStr.AppendFormat("{0}{1}=@{1},", columnPrefix, item.Name);
            sqlStr.Remove(sqlStr.Length - 1, 1);
            sqlStr.AppendFormat(" WHERE {0}{1}=@{1}", columnPrefix, modelPrimaryKey);

            // 参数设置
            List<SqlParameter> parameters = new List<SqlParameter>();
            for (int i = 0; i < propertyInfoes.Count; i++)
                parameters.Add(new SqlParameter()
                {
                    ParameterName = "@" + propertyInfoes[i].Name,
                    SqlDbType = propertyInfoes[i].PropertyType.IsEnum ? SqlDbTypes[typeof(Enum)] : SqlDbTypes[values[i].GetType()],
                    Value = propertyInfoes[i].PropertyType.IsEnum ? (int)values[i] : values[i]
                });

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                try
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = cn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sqlStr.ToString();
                    cmd.Parameters.AddRange(parameters.ToArray());

                    result = cmd.ExecuteNonQuery();

                    cn.Close();
                    cn.Dispose();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    cn.Dispose();
                    throw ex;
                }
            }

            return result;
        }
        #endregion
    }
}

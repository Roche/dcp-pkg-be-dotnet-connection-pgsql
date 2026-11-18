using Microsoft.Extensions.Configuration;
using Npgsql;
using OpenDCP.Framework.Configuration.Model.Extensions;
using OpenDCP.Framework.Configuration.Model.Interfaces;
using OpenDCP.Framework.Configuration.Model.Requests;
using OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models;
using OpenDCP.Framework.Datasource.Base;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.FormBuilder.Constants;
using OpenDCP.Framework.FormBuilder.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL
{
    public abstract class PostgreSQLConnection : BaseDatasource, IRelationDataSource
    {
        protected PostgreSQLConnection(IConfiguration configuration) : base(configuration)
        {
        }

        protected PostgreSQLConnection(int? id, JsonObject connectionString, IConfiguration configuration) : base(id, connectionString, configuration)
        {
        }

        private string _formattedConnectionString
        {
            get
            {
                var server = _connectionString["Server"].ToString();
                var database = _connectionString["Database"].ToString();
                var port = _connectionString["Port"].ToString();
                var user = _connectionString["User"].ToString();
                var password = _connectionString["Password"].ToString();

                return string.Format("Host={0};Port={1};Database={2};Username={3};Password={4};Application Name=DCP", server, port, database, user, password);
            }
        }
        protected abstract List<InputField> SchemaConnectionStringFields { get; }

        protected override List<string> SecretFields { get; } = new List<string> { "password" };

        protected override List<string> SecretDistributionFields { get; } = new List<string> { "password", "user", "database", "schema", "server" };

        protected override List<InputField> ConnectionStringFields
        {
            get
            {
                var connectionFields = new List<InputField>
                    {
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.String,
                            Name = "Server",
                            Label = "Server",
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            }
                        },
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.Number,
                            Name = "Port",
                            Label = "Port",
                            FieldSettings = new FieldSettings
                            {
                                DefaultValue = 1443,
                                StepSize = 1
                            },
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            }
                        },
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.String,
                            Name = "Schema",
                            Label = "Schema",
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            },
                            FieldSettings = new FieldSettings
                            {
                                DefaultValue = "public"
                            }
                        },
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.String,
                            Name = "Database",
                            Label = "Database",
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            }
                        },
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.String,
                            Name = "User",
                            Label = "User",
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            }
                        },
                        new InputField
                        {
                            FieldType = (byte)InputFieldType.Secret,
                            Name = "Password",
                            Label = "Password",
                            FieldValidations = new List<FieldValidationDTO>
                            {
                                new FieldValidationDTO
                                {
                                    Type = InputFieldValidationType.Required.ToName()
                                }
                            }
                        },
                    };
                connectionFields.AddRange(SchemaConnectionStringFields);
                return connectionFields;
            }

        }
        public override string DisplayConnectionString()
        {
            return $"Database: {_connectionString["Database"]} at {_connectionString["Server"]}";
        }
        protected abstract Task ValidateDatabaseSchema(NpgsqlConnection connection);
        public async Task ValidateConnectionString(JsonObject connectionString)
        {
            try
            {
                var server = connectionString["Server"].ToString();
                var database = connectionString["Database"].ToString();
                var port = connectionString["Port"].ToString();
                var user = connectionString["User"].ToString();
                var password = connectionString["Password"].ToString();

                var connectionStringBuilt = string.Format("Host={0};Port={1};Database={2};Username={3};Password={4};Application Name=DCP", server, port, database, user, password);

                using (var connection = new NpgsqlConnection(connectionStringBuilt))
                {
                    await connection.OpenAsync();

                    await ValidateDatabaseSchema(connection);
                }
            }
            catch (DCPBadRequestException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new DCPBadRequestException("Connection to database failed");
            }
        }
        protected async Task<PostgreSQLGenericResponse<T>> GetDataAsync<T>(
            string query,
            Dictionary<string, object> parameters = null,
            Dictionary<string, string> mappings = null,
            Func<IDataReader, T> mappingFun = null) where T : new()
        {
            var result = new PostgreSQLGenericResponse<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using (var conn = new NpgsqlConnection(_formattedConnectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (mappingFun != null)
                            {
                                result.Data.Add(mappingFun(reader));
                            }
                            else
                            {
                                var instance = new T();

                                foreach (var prop in properties)
                                {
                                    string columnName = mappings != null && mappings.TryGetValue(prop.Name, out var mappedName)
                                        ? mappedName
                                        : prop.Name;

                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                            if (value != null)
                                            {
                                                Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                                prop.SetValue(instance, Convert.ChangeType(value, targetType));
                                            }
                                            break;
                                        }
                                    }
                                }

                                result.Data.Add(instance);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public async Task<Dictionary<string, List<string>>> FindByQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, List<string>>();

            Func<IDataReader, KeyValuePair<string?, string?>> mappingFunction = reader =>
            {
                var key = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                var value = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString();
                return new KeyValuePair<string?, string?>(key, value);
            };

            var genericResponse = await GetDataAsync(
                query: query,
                mappingFun: mappingFunction);

            foreach (var pair in genericResponse.Data)
            {
                if (pair.Key == null) continue;

                if (!result.TryGetValue(pair.Key, out var list))
                {
                    list = new List<string>();
                    result[pair.Key] = list;
                }

                if (pair.Value != null)
                {
                    list.Add(pair.Value);
                }
            }

            return result;
        }

        public async Task<IList<T>> ExecuteSqlAsync<T>(SqlQueryRequest sqlQueryRequest, CancellationToken cancellationToken = default) where T : new()
        {
            var genericResponse = await GetDataAsync<T>(
                query: sqlQueryRequest.Query,
                parameters: sqlQueryRequest.QueryParameters.ToDictionary(x => x.Key, x => (object)x.Value)
            );

            return genericResponse.Data;
        }
    }
}

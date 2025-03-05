using System.Data;
using System.Data.SqlClient;
using Azure;
using Dapper;
using Newtonsoft.Json;

public class UpdateLogService
{
    private readonly IDbConnection _dbConnection;

    public UpdateLogService(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public void LogUpdate(string tableName, int recordId, string userId, object updatedItem)
    {
        string updatedFields = JsonConvert.SerializeObject(updatedItem);

        string query = "INSERT INTO UpdateLog (Timestamp, UserId, Operation, TableName, RecordId, UpdatedFields) " +
                       "VALUES (@Timestamp, @UserId, 'Update', @TableName, @RecordId, @UpdatedFields)";

        using (var connection = (SqlConnection)_dbConnection)
        {
            connection.Execute(query, new
            {
                Timestamp = DateTime.Now,
                UserId = userId,
                TableName = tableName,
                RecordId = recordId,
                UpdatedFields = updatedFields,
              
            });
        }
    }
  
    public void LogCreate(string tableName, int recordId, string userId, object updatedItem)
    {
        string updatedFields = JsonConvert.SerializeObject(updatedItem);

        string query = "INSERT INTO UpdateLog (Timestamp, UserId, Operation, TableName, RecordId, UpdatedFields) " +
                       "VALUES (@Timestamp, @UserId, 'Create', @TableName, @RecordId, @UpdatedFields)";

        using (var connection = (SqlConnection)_dbConnection)
        {
            connection.Execute(query, new
            {
                Timestamp = DateTime.Now,
                UserId = userId,
                TableName = tableName,
                RecordId = recordId,
                UpdatedFields = updatedFields,

            });
        }
    }
    public void LogDelete(string tableName, int recordId, string userId)
    {
        string query = "INSERT INTO UpdateLog (Timestamp, UserId, Operation, TableName, RecordId, UpdatedFields) " +
                       "VALUES (@Timestamp, @UserId, 'Delete', @TableName, @RecordId,@UpdatedFields)";

        using (var connection = (SqlConnection)_dbConnection)
        {
            connection.Execute(query, new
            {
                Timestamp = DateTime.Now,
                UserId = userId,
                TableName = tableName,
                RecordId = recordId,
                UpdatedFields = "status",
            });
        }
    }


}

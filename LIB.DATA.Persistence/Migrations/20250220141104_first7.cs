using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LIB.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class first7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorLog",
                columns: table => new
                {
                    ticketId = table.Column<string>(type: "text", nullable: false),
                    traceId = table.Column<string>(type: "text", nullable: false),
                    returnCode = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feedbacks = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLog", x => x.ticketId);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountId = table.Column<Guid>(type: "uuid", nullable: true),
                    reservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    referenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    requestedExecutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paymentType = table.Column<string>(type: "text", nullable: false),
                    paymentScheme = table.Column<string>(type: "text", nullable: false),
                    ReciverAccountId = table.Column<string>(type: "text", nullable: false),
                    ReciverAccountIdType = table.Column<string>(type: "text", nullable: false),
                    bankId = table.Column<string>(type: "text", nullable: false),
                    bankIdType = table.Column<string>(type: "text", nullable: false),
                    bankName = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    cbsStatusMessage = table.Column<string>(type: "text", nullable: false),
                    bankStatusMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionSimulation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountId = table.Column<Guid>(type: "uuid", nullable: true),
                    reservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    referenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    requestedExecutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paymentType = table.Column<string>(type: "text", nullable: false),
                    paymentScheme = table.Column<string>(type: "text", nullable: false),
                    ReciverAccountId = table.Column<string>(type: "text", nullable: false),
                    ReciverAccountIdType = table.Column<string>(type: "text", nullable: false),
                    bankId = table.Column<string>(type: "text", nullable: false),
                    bankIdType = table.Column<string>(type: "text", nullable: false),
                    bankName = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    cbsStatusMessage = table.Column<string>(type: "text", nullable: false),
                    bankStatusMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSimulation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateLog",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    RecordId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedFields = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateLog", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Branch = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: false),
                    UpdatedDate = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLog");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "TransactionSimulation");

            migrationBuilder.DropTable(
                name: "UpdateLog");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

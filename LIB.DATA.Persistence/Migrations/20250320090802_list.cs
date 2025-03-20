using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LIB.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class list : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.CreateTable(
                name: "BillGetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResTransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillerType = table.Column<string>(type: "text", nullable: false),
                    ReqProviderId = table.Column<string>(type: "text", nullable: false),
                    UniqueCode = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    ReferenceNo = table.Column<string>(type: "text", nullable: false),
                    ReqTransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccountNo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ResponseError = table.Column<string>(type: "text", nullable: false),
                    ResProviderId = table.Column<List<string>>(type: "text[]", nullable: false),
                    InvoiceId = table.Column<List<int>>(type: "integer[]", nullable: false),
                    InvoiceIdentificationValue = table.Column<List<string>>(type: "text[]", nullable: false),
                    InvoiceAmount = table.Column<List<decimal>>(type: "numeric[]", nullable: false),
                    CurrencyAlphaCode = table.Column<List<string>>(type: "text[]", nullable: false),
                    CurrencyDesignation = table.Column<List<string>>(type: "text[]", nullable: false),
                    CustomerName = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProviderName = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillGetRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.DropTable(
                name: "BillGetRequests");

       
        }
    }
}

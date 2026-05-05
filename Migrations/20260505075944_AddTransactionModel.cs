using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_category",
                table: "transactions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_created_at",
                table: "transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_processed_at",
                table: "transactions",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_type",
                table: "transactions",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_category",
                table: "transactions",
                columns: new[] { "user_id", "category" });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_processed",
                table: "transactions",
                columns: new[] { "user_id", "processed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_type",
                table: "transactions",
                columns: new[] { "user_id", "type" });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_type_processed",
                table: "transactions",
                columns: new[] { "user_id", "type", "processed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions");
        }
    }
}

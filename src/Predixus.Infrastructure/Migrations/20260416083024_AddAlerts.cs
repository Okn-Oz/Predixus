using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Predixus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PredictionPoints_Predictions_PredictionId",
                table: "PredictionPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Stocks_StockId",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_users_UserId",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockPrices_Stocks_StockId",
                table: "StockPrices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Stocks",
                table: "Stocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Predictions",
                table: "Predictions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockPrices",
                table: "StockPrices");

            migrationBuilder.DropIndex(
                name: "IX_StockPrices_StockId",
                table: "StockPrices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PredictionPoints",
                table: "PredictionPoints");

            migrationBuilder.RenameTable(
                name: "Stocks",
                newName: "stocks");

            migrationBuilder.RenameTable(
                name: "Predictions",
                newName: "predictions");

            migrationBuilder.RenameTable(
                name: "StockPrices",
                newName: "stock_prices");

            migrationBuilder.RenameTable(
                name: "PredictionPoints",
                newName: "prediction_points");

            migrationBuilder.RenameIndex(
                name: "IX_Predictions_UserId",
                table: "predictions",
                newName: "IX_predictions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Predictions_StockId",
                table: "predictions",
                newName: "IX_predictions_StockId");

            migrationBuilder.RenameIndex(
                name: "IX_PredictionPoints_PredictionId",
                table: "prediction_points",
                newName: "IX_prediction_points_PredictionId");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "stocks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "stocks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "stocks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "stocks",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "predictions",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Open",
                table: "stock_prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Low",
                table: "stock_prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "High",
                table: "stock_prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Close",
                table: "stock_prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PredictedPrice",
                table: "prediction_points",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualPrice",
                table: "prediction_points",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_stocks",
                table: "stocks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_predictions",
                table: "predictions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_stock_prices",
                table: "stock_prices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prediction_points",
                table: "prediction_points",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    IsTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_Symbol",
                table: "stocks",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_prices_StockId_Date",
                table: "stock_prices",
                columns: new[] { "StockId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_StockId",
                table: "Alerts",
                column: "StockId");

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_points_predictions_PredictionId",
                table: "prediction_points",
                column: "PredictionId",
                principalTable: "predictions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_predictions_stocks_StockId",
                table: "predictions",
                column: "StockId",
                principalTable: "stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_predictions_users_UserId",
                table: "predictions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_prices_stocks_StockId",
                table: "stock_prices",
                column: "StockId",
                principalTable: "stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_points_predictions_PredictionId",
                table: "prediction_points");

            migrationBuilder.DropForeignKey(
                name: "FK_predictions_stocks_StockId",
                table: "predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_predictions_users_UserId",
                table: "predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_prices_stocks_StockId",
                table: "stock_prices");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stocks",
                table: "stocks");

            migrationBuilder.DropIndex(
                name: "IX_stocks_Symbol",
                table: "stocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_predictions",
                table: "predictions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stock_prices",
                table: "stock_prices");

            migrationBuilder.DropIndex(
                name: "IX_stock_prices_StockId_Date",
                table: "stock_prices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prediction_points",
                table: "prediction_points");

            migrationBuilder.RenameTable(
                name: "stocks",
                newName: "Stocks");

            migrationBuilder.RenameTable(
                name: "predictions",
                newName: "Predictions");

            migrationBuilder.RenameTable(
                name: "stock_prices",
                newName: "StockPrices");

            migrationBuilder.RenameTable(
                name: "prediction_points",
                newName: "PredictionPoints");

            migrationBuilder.RenameIndex(
                name: "IX_predictions_UserId",
                table: "Predictions",
                newName: "IX_Predictions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_predictions_StockId",
                table: "Predictions",
                newName: "IX_Predictions_StockId");

            migrationBuilder.RenameIndex(
                name: "IX_prediction_points_PredictionId",
                table: "PredictionPoints",
                newName: "IX_PredictionPoints_PredictionId");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Stocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "Stocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Stocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Stocks",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "Predictions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Open",
                table: "StockPrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Low",
                table: "StockPrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "High",
                table: "StockPrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Close",
                table: "StockPrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PredictedPrice",
                table: "PredictionPoints",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualPrice",
                table: "PredictionPoints",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stocks",
                table: "Stocks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Predictions",
                table: "Predictions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockPrices",
                table: "StockPrices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PredictionPoints",
                table: "PredictionPoints",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_StockId",
                table: "StockPrices",
                column: "StockId");

            migrationBuilder.AddForeignKey(
                name: "FK_PredictionPoints_Predictions_PredictionId",
                table: "PredictionPoints",
                column: "PredictionId",
                principalTable: "Predictions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Stocks_StockId",
                table: "Predictions",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_users_UserId",
                table: "Predictions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockPrices_Stocks_StockId",
                table: "StockPrices",
                column: "StockId",
                principalTable: "Stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

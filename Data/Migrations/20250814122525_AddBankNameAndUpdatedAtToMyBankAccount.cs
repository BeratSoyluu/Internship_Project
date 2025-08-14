using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj_Proje_1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankNameAndUpdatedAtToMyBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MyBankAccounts",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "MyBankAccounts",
                keyColumn: "OwnerUserId",
                keyValue: null,
                column: "OwnerUserId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "MyBankAccounts",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "MyBankAccounts",
                keyColumn: "Currency",
                keyValue: null,
                column: "Currency",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "MyBankAccounts",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "MyBankAccounts",
                keyColumn: "BankName",
                keyValue: null,
                column: "BankName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "MyBankAccounts",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "MyBankAccounts",
                keyColumn: "AccountName",
                keyValue: null,
                column: "AccountName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "AccountName",
                table: "MyBankAccounts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MyBankAccounts_OwnerUserId",
                table: "MyBankAccounts",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MyBankAccounts_AspNetUsers_OwnerUserId",
                table: "MyBankAccounts",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MyBankAccounts_AspNetUsers_OwnerUserId",
                table: "MyBankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MyBankAccounts_OwnerUserId",
                table: "MyBankAccounts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MyBankAccounts",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "MyBankAccounts",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "MyBankAccounts",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "MyBankAccounts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AccountName",
                table: "MyBankAccounts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

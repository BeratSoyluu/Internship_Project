using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj_Proje_1.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeOwnerUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "MyBankAccounts",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

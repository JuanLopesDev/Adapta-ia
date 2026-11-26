using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adapta.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoginPorMatricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [TwoFactorEnabled] = 0 WHERE [TwoFactorEnabled] = 1");
            migrationBuilder.DropTable(
                name: "DesafiosLogin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesafiosLogin",
                columns: table => new
                {
                    ProfessorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodigoProtegido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesafiosLogin", x => x.ProfessorId);
                    table.ForeignKey(
                        name: "FK_DesafiosLogin_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesafiosLogin_Id",
                table: "DesafiosLogin",
                column: "Id",
                unique: true);
        }
    }
}

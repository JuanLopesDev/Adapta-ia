using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adapta.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessamentoDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExclusaoSolicitada",
                table: "Documentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExtracaoJson",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InicioProcessamento",
                table: "Documentos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessadoEm",
                table: "Documentos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Processamento",
                table: "Documentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevisadoEm",
                table: "Documentos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revisao",
                table: "Documentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisaoJson",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Tamanho",
                table: "Documentos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "Documentos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExclusaoSolicitada",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ExtracaoJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "InicioProcessamento",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ProcessadoEm",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Processamento",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RevisadoEm",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Revisao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RevisaoJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Tamanho",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Documentos");
        }
    }
}

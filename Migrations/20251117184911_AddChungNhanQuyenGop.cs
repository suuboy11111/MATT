using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiAmTinhThuong.Migrations
{
    /// <inheritdoc />
    public partial class AddChungNhanQuyenGop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChungNhanQuyenGops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VinhDanhId = table.Column<int>(type: "int", nullable: false),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoChungNhan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChungNhanQuyenGops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChungNhanQuyenGops_VinhDanhs_VinhDanhId",
                        column: x => x.VinhDanhId,
                        principalTable: "VinhDanhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChungNhanQuyenGops_VinhDanhId",
                table: "ChungNhanQuyenGops",
                column: "VinhDanhId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChungNhanQuyenGops");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiAmTinhThuong.Migrations
{
    /// <inheritdoc />
    public partial class InitMaiAmAndSupportModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Situation",
                table: "SupportRequests",
                newName: "Reason");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "SupportRequests",
                newName: "IsSupportedReason");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "SupportRequests",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "Supporters",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Supporters",
                newName: "IsApproved");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Supporters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Contact",
                table: "Supporters",
                newName: "Gender");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "SupportRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "SupportRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSupported",
                table: "SupportRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupportedHealth",
                table: "SupportRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaiAmId",
                table: "SupportRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "SupportRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Supporters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Supporters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "Supporters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Supporters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MaiAmId",
                table: "Supporters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Supporters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "MaiAms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fund = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaiAms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupporterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTypes_Supporters_SupporterId",
                        column: x => x.SupporterId,
                        principalTable: "Supporters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupporterMaiAms",
                columns: table => new
                {
                    SupporterId = table.Column<int>(type: "int", nullable: false),
                    MaiAmId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupporterMaiAms", x => new { x.SupporterId, x.MaiAmId });
                    table.ForeignKey(
                        name: "FK_SupporterMaiAms_MaiAms_MaiAmId",
                        column: x => x.MaiAmId,
                        principalTable: "MaiAms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupporterMaiAms_Supporters_SupporterId",
                        column: x => x.SupporterId,
                        principalTable: "Supporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupporterSupportTypes",
                columns: table => new
                {
                    SupporterId = table.Column<int>(type: "int", nullable: false),
                    SupportTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupporterSupportTypes", x => new { x.SupporterId, x.SupportTypeId });
                    table.ForeignKey(
                        name: "FK_SupporterSupportTypes_SupportTypes_SupportTypeId",
                        column: x => x.SupportTypeId,
                        principalTable: "SupportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupporterSupportTypes_Supporters_SupporterId",
                        column: x => x.SupporterId,
                        principalTable: "Supporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_MaiAmId",
                table: "SupportRequests",
                column: "MaiAmId");

            migrationBuilder.CreateIndex(
                name: "IX_Supporters_MaiAmId",
                table: "Supporters",
                column: "MaiAmId");

            migrationBuilder.CreateIndex(
                name: "IX_SupporterMaiAms_MaiAmId",
                table: "SupporterMaiAms",
                column: "MaiAmId");

            migrationBuilder.CreateIndex(
                name: "IX_SupporterSupportTypes_SupportTypeId",
                table: "SupporterSupportTypes",
                column: "SupportTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTypes_SupporterId",
                table: "SupportTypes",
                column: "SupporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Supporters_MaiAms_MaiAmId",
                table: "Supporters",
                column: "MaiAmId",
                principalTable: "MaiAms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_MaiAms_MaiAmId",
                table: "SupportRequests",
                column: "MaiAmId",
                principalTable: "MaiAms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supporters_MaiAms_MaiAmId",
                table: "Supporters");

            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_MaiAms_MaiAmId",
                table: "SupportRequests");

            migrationBuilder.DropTable(
                name: "SupporterMaiAms");

            migrationBuilder.DropTable(
                name: "SupporterSupportTypes");

            migrationBuilder.DropTable(
                name: "MaiAms");

            migrationBuilder.DropTable(
                name: "SupportTypes");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_MaiAmId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_Supporters_MaiAmId",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "IsSupported",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "IsSupportedHealth",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "MaiAmId",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "MaiAmId",
                table: "Supporters");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Supporters");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "SupportRequests",
                newName: "Situation");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "SupportRequests",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "IsSupportedReason",
                table: "SupportRequests",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Supporters",
                newName: "Organization");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Supporters",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "IsApproved",
                table: "Supporters",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Supporters",
                newName: "Contact");
        }
    }
}

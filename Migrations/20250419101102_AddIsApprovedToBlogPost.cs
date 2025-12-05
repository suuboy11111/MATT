using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiAmTinhThuong.Migrations
{
    /// <inheritdoc />
    public partial class AddIsApprovedToBlogPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "BlogPosts");
        }
    }
}

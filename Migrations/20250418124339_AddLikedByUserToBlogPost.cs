using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiAmTinhThuong.Migrations
{
    /// <inheritdoc />
    public partial class AddLikedByUserToBlogPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LikedByUser",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikedByUser",
                table: "BlogPosts");
        }
    }
}

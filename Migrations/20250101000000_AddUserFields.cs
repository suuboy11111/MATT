using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiAmTinhThuong.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QUAN TRỌNG: Migration này có timestamp sớm hơn InitialCreate
            // Nên phải kiểm tra bảng tồn tại trước khi ALTER
            // Sử dụng SQL thủ công để kiểm tra và thêm columns an toàn
            
            // QUAN TRỌNG: Migration này có timestamp sớm hơn InitialCreate
            // Nên phải kiểm tra bảng tồn tại trước khi ALTER
            // Railway dùng PostgreSQL, nên chỉ chạy PostgreSQL SQL
            
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers') THEN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'Gender') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Gender"" text;
                        END IF;
                        
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'DateOfBirth') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""DateOfBirth"" timestamp without time zone;
                        END IF;
                        
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'Address') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Address"" varchar(200);
                        END IF;
                        
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'PhoneNumber2') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""PhoneNumber2"" text;
                        END IF;
                        
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'CreatedAt') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""CreatedAt"" timestamp without time zone;
                        END IF;
                        
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'AspNetUsers' AND column_name = 'UpdatedAt') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""UpdatedAt"" timestamp without time zone;
                        END IF;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber2",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUsers");
        }
    }
}




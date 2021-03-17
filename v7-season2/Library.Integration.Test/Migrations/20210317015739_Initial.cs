using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Library.Integration.Test.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThankYouSaga",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(nullable: false),
                    MemberId = table.Column<Guid>(nullable: false),
                    BookId = table.Column<Guid>(nullable: false),
                    CurrentState = table.Column<string>(nullable: true),
                    ReservationId = table.Column<Guid>(nullable: true),
                    ThankYouStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThankYouSaga", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThankYouSaga_BookId_MemberId",
                table: "ThankYouSaga",
                columns: new[] { "BookId", "MemberId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThankYouSaga");
        }
    }
}

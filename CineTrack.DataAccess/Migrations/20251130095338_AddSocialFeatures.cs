using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTrack.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityComments_ActivityLogs_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "ActivityLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActivityLikes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLikes", x => new { x.UserId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_ActivityLikes_ActivityLogs_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "ActivityLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityComments_ActivityId",
                table: "ActivityComments",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityComments_UserId",
                table: "ActivityComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLikes_ActivityId",
                table: "ActivityLikes",
                column: "ActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityComments");

            migrationBuilder.DropTable(
                name: "ActivityLikes");
        }
    }
}

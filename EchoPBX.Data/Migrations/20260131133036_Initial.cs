using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EchoPBX.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.Id);
                    table.UniqueConstraint("AK_admins_Username", x => x.Username);
                });

            migrationBuilder.CreateTable(
                name: "cdr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Clid = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Destination = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DestinationContext = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DestinationChannel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LastAppExecuted = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LastAppArguments = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    Answer = table.Column<long>(type: "INTEGER", nullable: true),
                    End = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    BillSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Disposition = table.Column<int>(type: "INTEGER", nullable: false),
                    AmaFlags = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cdr", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "queues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Strategy = table.Column<string>(type: "TEXT", nullable: false),
                    Timeout = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLength = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    MusicOnHold = table.Column<string>(type: "TEXT", nullable: true),
                    Announcement = table.Column<string>(type: "TEXT", nullable: true),
                    WrapUpTime = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "access_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    AdminId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_tokens_admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Codecs = table.Column<string>(type: "TEXT", nullable: false),
                    Cid = table.Column<string>(type: "TEXT", nullable: true),
                    QueueId = table.Column<int>(type: "INTEGER", nullable: true),
                    IncomingCallBehaviour = table.Column<int>(type: "INTEGER", nullable: false, defaultValueSql: "2"),
                    DtmfAnnouncement = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trunks_queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "dtmf_menu_entries",
                columns: table => new
                {
                    TrunkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Digit = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dtmf_menu_entries", x => new { x.TrunkId, x.Digit });
                    table.ForeignKey(
                        name: "FK_dtmf_menu_entries_queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dtmf_menu_entries_trunks_TrunkId",
                        column: x => x.TrunkId,
                        principalTable: "trunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "extensions",
                columns: table => new
                {
                    ExtensionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    MaxDevices = table.Column<int>(type: "INTEGER", nullable: false),
                    OutgoingTrunkId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extensions", x => x.ExtensionNumber);
                    table.ForeignKey(
                        name: "FK_extensions_trunks_OutgoingTrunkId",
                        column: x => x.OutgoingTrunkId,
                        principalTable: "trunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "queue_extensions",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtensionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_extensions", x => new { x.QueueId, x.ExtensionNumber });
                    table.ForeignKey(
                        name: "FK_queue_extensions_extensions_ExtensionNumber",
                        column: x => x.ExtensionNumber,
                        principalTable: "extensions",
                        principalColumn: "ExtensionNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_queue_extensions_queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trunk_extensions",
                columns: table => new
                {
                    TrunkId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtensionNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trunk_extensions", x => new { x.TrunkId, x.ExtensionNumber });
                    table.ForeignKey(
                        name: "FK_trunk_extensions_extensions_ExtensionNumber",
                        column: x => x.ExtensionNumber,
                        principalTable: "extensions",
                        principalColumn: "ExtensionNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trunk_extensions_trunks_TrunkId",
                        column: x => x.TrunkId,
                        principalTable: "trunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "system_settings",
                columns: new[] { "Name", "Id", "Value" },
                values: new object[,]
                {
                    { "AsteriskLanguage", 0, "en" },
                    { "DashboardLanguage", 0, "en" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_tokens_AdminId",
                table: "access_tokens",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_dtmf_menu_entries_QueueId",
                table: "dtmf_menu_entries",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_extensions_OutgoingTrunkId",
                table: "extensions",
                column: "OutgoingTrunkId");

            migrationBuilder.CreateIndex(
                name: "IX_queue_extensions_ExtensionNumber",
                table: "queue_extensions",
                column: "ExtensionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_trunk_extensions_ExtensionNumber",
                table: "trunk_extensions",
                column: "ExtensionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_trunks_QueueId",
                table: "trunks",
                column: "QueueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_tokens");

            migrationBuilder.DropTable(
                name: "cdr");

            migrationBuilder.DropTable(
                name: "contacts");

            migrationBuilder.DropTable(
                name: "dtmf_menu_entries");

            migrationBuilder.DropTable(
                name: "queue_extensions");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "trunk_extensions");

            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropTable(
                name: "extensions");

            migrationBuilder.DropTable(
                name: "trunks");

            migrationBuilder.DropTable(
                name: "queues");
        }
    }
}

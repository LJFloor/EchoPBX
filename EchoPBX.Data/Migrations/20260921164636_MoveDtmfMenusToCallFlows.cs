using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoPBX.Data.Migrations
{
    /// <summary>
    /// Replaces the DTMF menu of trunks with a call flow per trunk: a phone menu playing the
    /// trunk's announcement, with a "Go to queue" step for every digit. The trunk is pointed
    /// at the new flow.
    /// </summary>
    /// <remarks>
    /// The flows keep using the announcement file in the trunk's sound folder, since a migration
    /// cannot move files. <c>ICallFlowWriteRepository.MoveStraySounds</c> moves it into the
    /// flow's own folder at startup.
    /// </remarks>
    public partial class MoveDtmfMenusToCallFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CallFlowId",
                table: "trunks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_trunks_CallFlowId",
                table: "trunks",
                column: "CallFlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_trunks_call_flows_CallFlowId",
                table: "trunks",
                column: "CallFlowId",
                principalTable: "call_flows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // The definition has to match what CallFlowDefinition reads: camelCase, "type" as
            // the first property, digits as strings. json() keeps the nested documents from
            // being stored as strings, since subqueries lose their JSON type.
            migrationBuilder.Sql("""
                INSERT INTO call_flows (Slug, Name, InternalNumber, DefinitionJson)
                SELECT
                    'trunk-' || t.Id || '-ivr',
                    t.Name || ' IVR',
                    NULL,
                    json_object(
                        'nodes', json((
                            SELECT json_group_array(json(node)) FROM (
                                SELECT 0 AS sort, json_object('type', 'start', 'id', 'start') AS node
                                UNION ALL
                                SELECT 1, json_object(
                                    'type', 'menu',
                                    'id', 'menu',
                                    'kind', 1,
                                    'sound', t.DtmfAnnouncement,
                                    'options', json((
                                        SELECT json_group_array(CAST(e.Digit AS TEXT)) FROM (
                                            SELECT Digit FROM dtmf_menu_entries
                                            WHERE TrunkId = t.Id
                                            ORDER BY CASE Digit WHEN 0 THEN 10 ELSE Digit END) e)),
                                    'timeout', 5,
                                    'attempts', 3)
                                UNION ALL
                                SELECT 2 + e.Digit, json_object('type', 'queue', 'id', 'queue-' || e.Digit, 'queueId', e.QueueId)
                                FROM dtmf_menu_entries e
                                WHERE e.TrunkId = t.Id
                                ORDER BY sort))),
                        'edges', json((
                            SELECT json_group_array(json(edge)) FROM (
                                SELECT json_object('id', 'start-menu', 'source', 'start', 'target', 'menu', 'sourceHandle', 'next') AS edge
                                UNION ALL
                                SELECT json_object(
                                    'id', 'menu-queue-' || e.Digit,
                                    'source', 'menu',
                                    'target', 'queue-' || e.Digit,
                                    'sourceHandle', CAST(e.Digit AS TEXT))
                                FROM dtmf_menu_entries e
                                WHERE e.TrunkId = t.Id))))
                FROM trunks t
                WHERE t.IncomingCallBehaviour = 5
                  AND EXISTS (SELECT 1 FROM dtmf_menu_entries e WHERE e.TrunkId = t.Id);

                UPDATE trunks
                SET IncomingCallBehaviour = 6,
                    CallFlowId = (SELECT f.Id FROM call_flows f WHERE f.Slug = 'trunk-' || trunks.Id || '-ivr')
                WHERE IncomingCallBehaviour = 5
                  AND EXISTS (SELECT 1 FROM dtmf_menu_entries e WHERE e.TrunkId = trunks.Id);

                -- A menu without digits never got as far as the announcement: the call was hung up.
                UPDATE trunks SET IncomingCallBehaviour = 1 WHERE IncomingCallBehaviour = 5;
                """);

            migrationBuilder.DropTable(
                name: "dtmf_menu_entries");

            migrationBuilder.DropColumn(
                name: "DtmfAnnouncement",
                table: "trunks");
        }

        /// <inheritdoc />
        /// <remarks>Restores the old schema only. The menus stay call flows.</remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trunks_call_flows_CallFlowId",
                table: "trunks");

            migrationBuilder.DropIndex(
                name: "IX_trunks_CallFlowId",
                table: "trunks");

            migrationBuilder.DropColumn(
                name: "CallFlowId",
                table: "trunks");

            migrationBuilder.AddColumn<string>(
                name: "DtmfAnnouncement",
                table: "trunks",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_dtmf_menu_entries_QueueId",
                table: "dtmf_menu_entries",
                column: "QueueId");
        }
    }
}

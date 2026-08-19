using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassGroupId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderNotifiedAt",
                table: "Exam",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedAt",
                table: "AssignmentSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueMissedNotifiedAt",
                table: "Assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClassGroupId",
                table: "Notifications",
                column: "ClassGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ClassGroup_ClassGroupId",
                table: "Notifications",
                column: "ClassGroupId",
                principalTable: "ClassGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed templates for the new event types (values 13..31). Each row is
            // editable via /Admin/System; placeholders render at creation time.
            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Type", "Title", "Body", "IsActive" },
                values: new object[,]
                {
                    { 13, "Assignment graded", "Your submission for \"{assignmentTitle}\" was graded: {score}/100.", true },
                    { 14, "Exam starting soon", "The exam \"{examTitle}\" starts within 30 minutes. Get ready!", true },
                    { 15, "Assignment due soon", "Assignment \"{assignmentTitle}\" is due in {days} day(s). Submit it before the deadline.", true },
                    { 16, "Assignment missed", "You did not submit \"{assignmentTitle}\" by its due date.", true },
                    { 17, "Class starting soon", "Your class \"{className}\" starts within 30 minutes.", true },
                    { 18, "Course access expiring soon", "Your access to {courseTitle} ends in {days} day(s). Please renew in time.", true },
                    { 19, "Course access expired", "Your access to {courseTitle} has ended. Re-enroll to continue learning.", true },
                    { 20, "Order expired", "Your pending order for {courseTitle} was cancelled because it was not paid in time.", true },
                    { 21, "Refund request closed", "Your refund request for {courseTitle} was not approved within the review window.", true },
                    { 22, "Invoice issued", "Your invoice {invoiceNumber} for {amount} is ready.", true },
                    { 23, "Invoice request rejected", "Your invoice request was rejected. Reason: {reason}", true },
                    { 24, "Invoice voided", "Invoice {invoiceNumber} was voided. Reason: {reason}", true },
                    { 25, "Red-letter invoice issued", "A red-letter invoice {invoiceNumber} has been issued for {originalNumber}.", true },
                    { 26, "Import completed", "Your {kind} import finished: {successCount} succeeded, {errorCount} failed.", true },
                    { 27, "Import failed", "Your {kind} import failed: {error}", true },
                    { 28, "Export ready", "Your {kind} export is ready. Download: {downloadUrl} (expires {expiresAt}).", true },
                    { 29, "Export in progress", "Your {kind} export is {percent}% complete.", true },
                    { 30, "Welcome", "Welcome to OpenLearning, {displayName}! You have been enrolled in: {courseList}", true },
                    { 31, "Course access granted", "You have been enrolled in: {courseList}", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ClassGroup_ClassGroupId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ClassGroupId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ClassGroupId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReminderNotifiedAt",
                table: "Exam");

            migrationBuilder.DropColumn(
                name: "NotifiedAt",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "DueMissedNotifiedAt",
                table: "Assignments");
        }
    }
}

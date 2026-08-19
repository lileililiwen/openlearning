using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttemptAnswers_AnswerOptions_AnswerOptionId",
                table: "QuizAttemptAnswers");

            migrationBuilder.AlterColumn<int>(
                name: "AnswerOptionId",
                table: "QuizAttemptAnswers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "FileAnswerUrl",
                table: "QuizAttemptAnswers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradedScore",
                table: "QuizAttemptAnswers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingFeedback",
                table: "QuizAttemptAnswers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGraded",
                table: "QuizAttemptAnswers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionIds",
                table: "QuizAttemptAnswers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextAnswer",
                table: "QuizAttemptAnswers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionType",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttemptAnswers_AnswerOptions_AnswerOptionId",
                table: "QuizAttemptAnswers",
                column: "AnswerOptionId",
                principalTable: "AnswerOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttemptAnswers_AnswerOptions_AnswerOptionId",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "FileAnswerUrl",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "GradedScore",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "GradingFeedback",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "IsGraded",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "SelectedOptionIds",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "AnswerOptionId",
                table: "QuizAttemptAnswers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttemptAnswers_AnswerOptions_AnswerOptionId",
                table: "QuizAttemptAnswers",
                column: "AnswerOptionId",
                principalTable: "AnswerOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

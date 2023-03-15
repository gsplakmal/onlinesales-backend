﻿using Microsoft.EntityFrameworkCore.Migrations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

#nullable disable

namespace OnlineSales.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredPostToContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_comment_post_post_id",
                table: "comment");

            migrationBuilder.RenameColumn(
                name: "post_id",
                table: "comment",
                newName: "content_id");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "comment",
                newName: "body");

            migrationBuilder.RenameIndex(
                name: "ix_comment_post_id",
                table: "comment",
                newName: "ix_comment_content_id");

            migrationBuilder.RenameTable(
                name: "post",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "content",
                newName: "body");

            migrationBuilder.DropPrimaryKey(
                name: "pk_post",
                table: "content");

            migrationBuilder.AddPrimaryKey(
                name: "pk_content",
                table: "content",
                column: "id");

            migrationBuilder.RenameIndex(
                name: "ix_post_slug",
                table: "content",
                newName: "ix_content_slug");

            migrationBuilder.AddForeignKey(
                name: "fk_comment_content_content_id",
                table: "comment",
                column: "content_id",
                principalTable: "content",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("UPDATE change_log SET object_type = 'Content' where object_type = 'Post'");
            migrationBuilder.Sql("UPDATE change_log set data = regexp_replace(data::text, '/api/images/', '/api/media/', 'gin')::jsonb");
            migrationBuilder.Sql("UPDATE change_log set data = regexp_replace(data::text, ', \"content\":', ', \"body\":', 'gin')::jsonb WHERE object_type IN ('Content', 'Comment')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_comment_content_content_id",
                table: "comment");

            migrationBuilder.RenameColumn(
                name: "content_id",
                table: "comment",
                newName: "post_id");

            migrationBuilder.RenameColumn(
                name: "body",
                table: "comment",
                newName: "content");

            migrationBuilder.RenameIndex(
                name: "ix_comment_content_id",
                table: "comment",
                newName: "ix_comment_post_id");

            migrationBuilder.RenameTable(
                name: "content",
                newName: "post");

            migrationBuilder.RenameColumn(
                name: "body",
                table: "post",
                newName: "content");

            migrationBuilder.DropPrimaryKey(
                name: "pk_content",
                table: "post");

            migrationBuilder.AddPrimaryKey(
                name: "pk_post",
                table: "post",
                column: "id");

            migrationBuilder.RenameIndex(
                name: "ix_content_slug",
                table: "post",
                newName: "ix_post_slug");

            migrationBuilder.AddForeignKey(
                name: "fk_comment_post_post_id",
                table: "comment",
                column: "post_id",
                principalTable: "post",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("UPDATE change_log SET object_type = 'Post' where object_type = 'Content'");
            migrationBuilder.Sql("UPDATE change_log set data = regexp_replace(data::text, '/api/media/', '/api/images/', 'gin')::jsonb");
            migrationBuilder.Sql("UPDATE change_log set data = regexp_replace(data::text, ', \"body\":', ', \"content\":', 'gin')::jsonb WHERE object_type IN ('Content', 'Comment')");
        }
    }
}

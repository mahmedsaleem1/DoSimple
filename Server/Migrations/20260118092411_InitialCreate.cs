using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if PasswordResetToken column exists, if not add it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'PasswordResetToken')
                BEGIN
                    ALTER TABLE [Users] ADD [PasswordResetToken] nvarchar(max) NULL;
                END
            ");

            // Check if PasswordResetTokenExpiry column exists, if not add it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'PasswordResetTokenExpiry')
                BEGIN
                    ALTER TABLE [Users] ADD [PasswordResetTokenExpiry] datetime2 NULL;
                END
            ");

            // Check if Email index exists, if not add it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IX_Users_Email')
                BEGIN
                    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the columns and index if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IX_Users_Email')
                BEGIN
                    DROP INDEX [IX_Users_Email] ON [Users];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'PasswordResetToken')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [PasswordResetToken];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'PasswordResetTokenExpiry')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [PasswordResetTokenExpiry];
                END
            ");
        }
    }
}

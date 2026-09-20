using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NULL AND OBJECT_ID(N'[dbo].[Leads]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Leads]', N'CRM_Lead';
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NULL AND OBJECT_ID(N'[dbo].[Opportunities]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Opportunities]', N'CRM_Opportunity';
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Lead]', N'CreatedBy') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Lead] ADD [CreatedBy] nvarchar(50) NULL;
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Lead]', N'UpdatedBy') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Lead] ADD [UpdatedBy] nvarchar(50) NULL;
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Opportunity]', N'CreatedBy') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Opportunity] ADD [CreatedBy] nvarchar(50) NULL;
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Opportunity]', N'UpdatedBy') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Opportunity] ADD [UpdatedBy] nvarchar(50) NULL;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CRM_Customer]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[CRM_Customer] (
                        [Id] int NOT NULL IDENTITY,
                        [CustomerCode] nvarchar(50) NOT NULL,
                        [CustomerName] nvarchar(200) NOT NULL,
                        [Status] nvarchar(30) NOT NULL,
                        [CustomerNature] nvarchar(30) NOT NULL,
                        [CustomerType] nvarchar(50) NULL,
                        [EnterpriseType] nvarchar(50) NULL,
                        [AliasName] nvarchar(100) NULL,
                        [BusinessRegistrationNumber] nvarchar(80) NULL,
                        [BusinessRegistrationDate] date NULL,
                        [GroupCode] nvarchar(50) NULL,
                        [GroupName] nvarchar(200) NULL,
                        [CountryRegion] nvarchar(80) NULL,
                        [City] nvarchar(80) NULL,
                        [District] nvarchar(80) NULL,
                        [Address] nvarchar(500) NULL,
                        [Manager] nvarchar(80) NULL,
                        [Phone] nvarchar(80) NULL,
                        [Fax] nvarchar(80) NULL,
                        [Email] nvarchar(150) NULL,
                        [Website] nvarchar(200) NULL,
                        [Remarks] nvarchar(1000) NULL,
                        [ArchivePath] nvarchar(300) NULL,
                        [PayerCode] nvarchar(50) NULL,
                        [PayerName] nvarchar(200) NULL,
                        [BankAccount] nvarchar(100) NULL,
                        [BankName] nvarchar(120) NULL,
                        [TransferCode] nvarchar(80) NULL,
                        [Currency] nvarchar(20) NULL,
                        [CreditLimit] decimal(18,3) NOT NULL,
                        [CreditTerm] nvarchar(50) NULL,
                        [DefaultPriceCategory] nvarchar(50) NULL,
                        [PaymentMethod] nvarchar(50) NULL,
                        [MinimumOrderAmount] decimal(18,3) NOT NULL,
                        [DefaultTaxRate] nvarchar(50) NULL,
                        [IsAccountFrozen] bit NOT NULL,
                        [AccountRemarks] nvarchar(1000) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [CreatedBy] nvarchar(50) NULL,
                        [UpdatedBy] nvarchar(50) NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_CRM_Customer] PRIMARY KEY ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_CRM_Customer_CustomerCode_IsDeleted'
                      AND [object_id] = OBJECT_ID(N'[dbo].[CRM_Customer]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_CRM_Customer_CustomerCode_IsDeleted]
                    ON [dbo].[CRM_Customer] ([CustomerCode], [IsDeleted]);
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CRM_CustomerContact]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[CRM_CustomerContact] (
                        [Id] int NOT NULL IDENTITY,
                        [CustomerId] int NOT NULL,
                        [Name] nvarchar(80) NOT NULL,
                        [ContactType] nvarchar(50) NULL,
                        [Salutation] nvarchar(30) NULL,
                        [Position] nvarchar(80) NULL,
                        [Mobile] nvarchar(80) NULL,
                        [Phone] nvarchar(80) NULL,
                        [Fax] nvarchar(80) NULL,
                        [Email] nvarchar(150) NULL,
                        [BusinessCardNote] nvarchar(120) NULL,
                        [Status] nvarchar(30) NULL,
                        [Remarks] nvarchar(500) NULL,
                        [IsDefault] bit NOT NULL,
                        [SortOrder] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [CreatedBy] nvarchar(50) NULL,
                        [UpdatedBy] nvarchar(50) NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_CRM_CustomerContact] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CRM_CustomerContact_CRM_Customer_CustomerId]
                            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[CRM_Customer] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_CRM_CustomerContact_CustomerId_SortOrder'
                      AND [object_id] = OBJECT_ID(N'[dbo].[CRM_CustomerContact]')
                )
                BEGIN
                    CREATE INDEX [IX_CRM_CustomerContact_CustomerId_SortOrder]
                    ON [dbo].[CRM_CustomerContact] ([CustomerId], [SortOrder]);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[CRM_CustomerContact]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[CRM_CustomerContact];
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Customer]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[CRM_Customer];
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Lead]', N'CreatedBy') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Lead] DROP COLUMN [CreatedBy];
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Lead]', N'UpdatedBy') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Lead] DROP COLUMN [UpdatedBy];
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Opportunity]', N'CreatedBy') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Opportunity] DROP COLUMN [CreatedBy];
                END;

                IF OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[CRM_Opportunity]', N'UpdatedBy') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[CRM_Opportunity] DROP COLUMN [UpdatedBy];
                END;

                IF OBJECT_ID(N'[dbo].[Leads]', N'U') IS NULL AND OBJECT_ID(N'[dbo].[CRM_Lead]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[CRM_Lead]', N'Leads';
                END;

                IF OBJECT_ID(N'[dbo].[Opportunities]', N'U') IS NULL AND OBJECT_ID(N'[dbo].[CRM_Opportunity]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[CRM_Opportunity]', N'Opportunities';
                END;
                """);
        }
    }
}

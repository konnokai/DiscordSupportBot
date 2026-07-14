BEGIN TRANSACTION;
CREATE TABLE "AutoCreatePrivateThreadConfig" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AutoCreatePrivateThreadConfig" PRIMARY KEY AUTOINCREMENT,
    "GuildId" INTEGER NOT NULL,
    "ChannelId" INTEGER NOT NULL,
    "MessageId" INTEGER NOT NULL,
    "MentionUserIds" TEXT NOT NULL,
    "MentionRoleIds" TEXT NOT NULL,
    "AddedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_AutoCreatePrivateThreadConfig_MessageId" ON "AutoCreatePrivateThreadConfig" ("MessageId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714105323_AddAutoCreatePrivateThreadMentions', '9.0.8');

COMMIT;

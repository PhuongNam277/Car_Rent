-- ========= Conversations =========
IF OBJECT_ID(N'dbo.Conversations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Conversations
    (
        ConversationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Conversations PRIMARY KEY,
        CustomerId     INT NOT NULL,
        StaffId        INT NULL,
        Status         NVARCHAR(20) NOT NULL CONSTRAINT DF_Conversations_Status DEFAULT N'Open', -- Open/Closed
        CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Conversations_CreatedAt DEFAULT SYSUTCDATETIME(),
        LastMessageAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Conversations_LastMessageAt DEFAULT SYSUTCDATETIME(),
        ClosedAt       DATETIME2(0) NULL
    );

    ALTER TABLE dbo.Conversations
      ADD CONSTRAINT FK_Conversations_Customer
      FOREIGN KEY (CustomerId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION;

    ALTER TABLE dbo.Conversations
      ADD CONSTRAINT FK_Conversations_Staff
      FOREIGN KEY (StaffId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION;

    CREATE INDEX IX_Conversations_Customer_Status ON dbo.Conversations(CustomerId, Status);
    CREATE INDEX IX_Conversations_StaffId         ON dbo.Conversations(StaffId);
    CREATE INDEX IX_Conversations_LastMessageAt   ON dbo.Conversations(LastMessageAt);
END
GO

-- ========= ChatMessages =========
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        ChatMessageId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ConversationId INT NOT NULL,
        SenderId       INT NOT NULL,
        Content        NVARCHAR(4000) NOT NULL,
        SentAt         DATETIME2(0) NOT NULL CONSTRAINT DF_ChatMessages_SentAt DEFAULT SYSUTCDATETIME(),
        IsRead         BIT NOT NULL CONSTRAINT DF_ChatMessages_IsRead DEFAULT(0)
    );

    ALTER TABLE dbo.ChatMessages
      ADD CONSTRAINT FK_ChatMessages_Conversation
      FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(ConversationId) ON DELETE CASCADE;

    ALTER TABLE dbo.ChatMessages
      ADD CONSTRAINT FK_ChatMessages_Sender
      FOREIGN KEY (SenderId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION;

    CREATE INDEX IX_ChatMessages_ConversationId ON dbo.ChatMessages(ConversationId);
    CREATE INDEX IX_ChatMessages_SentAt         ON dbo.ChatMessages(SentAt);
END
GO

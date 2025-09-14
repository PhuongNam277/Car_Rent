IF OBJECT_ID(N'dbo.ConversationReadState', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ConversationReadState (
    ConversationId INT NOT NULL,
    UserId         INT NOT NULL,
    LastReadMessageId BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT PK_ConversationReadState PRIMARY KEY (ConversationId, UserId),
    CONSTRAINT FK_CRS_Conv FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(ConversationId) ON DELETE CASCADE,
    CONSTRAINT FK_CRS_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
  );

  -- tối ưu truy vấn
  CREATE INDEX IX_CRS_User ON dbo.ConversationReadState(UserId);
END
GO

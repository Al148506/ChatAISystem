## ChatAISystem
An intuitive and user-friendly system designed to let you roleplay with your favorite characters using artificial intelligence.

### Features

Character Management: Easily record, view, and update your character information with a seamless and efficient interface.
Chat: Enjoy a modern interface that allows you to effortlessly switch between characters, search for any character, and read your chats comfortably.
Bot Configuration: The bot is configured to remember past conversations, maintain its assigned role, and respond appropriately to the situation.
Security: User account management, reCAPTCHA implementation for login protection, and robust form validations on both the frontend and backend.
## Screenshots
![Character](https://github.com/Al148506/ChatAISystem/blob/dbd6afdb98b8c66f6e022304fc152e1641abdcd9/CharacterLuke.png)
![Character](https://github.com/Al148506/ChatAISystem/blob/d9bf46125e9de7cb1297993505d163bca735158a/ConversationLuke.png)


## Setup
1.  Clone the repository:
   bash
   git clone https://github.com/al148506/ChatAISystem
2. Open the project with Visual Studio 2022
3. Update the appsettings.json file with your database connection string and Google Gemini API key.
4. Install the following NuGet packages:
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
5. Excecute in package manager console:
`Add-Migration "First Migration "`

## Database Schema  
This application uses Microsoft SQL Server. Use the following SQL script to create the database and its tables:
USE [master];
GO

CREATE DATABASE [CharAIDB];
GO

USE [CharAIDB];
GO

CREATE TABLE [dbo].[Users](
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL UNIQUE,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE [dbo].[Characters](
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [AvatarUrl] NVARCHAR(255) NULL,
    [CreatedBy] INT NOT NULL,
    [CreatedAt] DATETIME DEFAULT GETDATE(),
    FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Conversations](
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [CharacterId] INT NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [MessageText] NVARCHAR(MAX) NOT NULL,
    [Timestamp] DATETIME DEFAULT GETDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    FOREIGN KEY ([CharacterId]) REFERENCES [dbo].[Characters]([Id])
);
GO

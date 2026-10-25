IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Itineraries] (
    [Id] uniqueidentifier NOT NULL,
    [AccessTokenHash] nvarchar(64) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Itineraries] PRIMARY KEY ([Id])
);

CREATE TABLE [ItineraryStops] (
    [ItineraryId] uniqueidentifier NOT NULL,
    [ExhibitionId] nvarchar(100) NOT NULL,
    [Position] int NOT NULL,
    CONSTRAINT [PK_ItineraryStops] PRIMARY KEY ([ItineraryId], [ExhibitionId]),
    CONSTRAINT [FK_ItineraryStops_Itineraries_ItineraryId] FOREIGN KEY ([ItineraryId]) REFERENCES [Itineraries] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_ItineraryStops_ItineraryId_Position] ON [ItineraryStops] ([ItineraryId], [Position]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261024050313_InitialCreate', N'9.0.10');

COMMIT;
GO

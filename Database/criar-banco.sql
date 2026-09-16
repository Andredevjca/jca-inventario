-- Executar no SQL Server Management Studio antes de estrutura.sql.
USE [master];
GO
IF DB_ID(N'dbActyon_Inventario') IS NULL
    EXEC(N'CREATE DATABASE [dbActyon_Inventario]');
GO
USE [dbActyon_Inventario];
GO

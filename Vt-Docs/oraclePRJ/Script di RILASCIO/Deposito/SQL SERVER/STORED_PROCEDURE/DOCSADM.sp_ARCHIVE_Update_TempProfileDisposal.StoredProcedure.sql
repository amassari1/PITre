USE [PCM_DEPOSITO_1]
GO
/****** Object:  StoredProcedure [DOCSADM].[sp_ARCHIVE_Update_TempProfileDisposal]    Script Date: 08/14/2013 11:50:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ============================================================================
-- Author:		Giordano Iacozzilli
-- Create date: 10/07/2013
-- Description:	Update record tabella  TempProfileDisposal
-- ============================================================================
CREATE PROCEDURE [DOCSADM].[sp_ARCHIVE_Update_TempProfileDisposal]
	@Disposal_ID int,
	@ProfilesList VARCHAR(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @sql_string nvarchar(MAX)

	-- Create temp table
	--
	IF OBJECT_ID('tempdb..#ProfilesListTable') IS NOT NULL DROP TABLE #ProfilesListTable
	CREATE TABLE #ProfilesListTable
	(
		ID int
	)

	SET @sql_string = CAST(N'
		INSERT INTO #ProfilesListTable (ID)
		SELECT Profile_ID FROM ARCHIVE_TempProfileDisposal
		WHERE Profile_ID IN (' AS NVARCHAR(MAX)) + CAST(@ProfilesList AS NVARCHAR(MAX)) + CAST(N')' AS NVARCHAR(MAX))
		
	PRINT @sql_string;

	EXECUTE sp_executesql @sql_string;


		Update ARCHIVE_TempProfileDisposal 
		set ARCHIVE_TempProfileDisposal.DaScartare = 0 
		where ARCHIVE_TempProfileDisposal.Disposal_ID = @Disposal_ID
		AND ARCHIVE_TempProfileDisposal.Profile_ID in (select ID from #ProfilesListTable);
		
		Update ARCHIVE_TempProfileDisposal 
		set ARCHIVE_TempProfileDisposal.DaScartare = 1 
		where ARCHIVE_TempProfileDisposal.Disposal_ID = @Disposal_ID
		AND ARCHIVE_TempProfileDisposal.Profile_ID not in (select ID from #ProfilesListTable);


END
GO

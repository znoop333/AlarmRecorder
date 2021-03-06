USE [OmmcMes]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[AlarmRecorderList] (@MyPC_ID char(2) = '')
AS
BEGIN
	SET NOCOUNT ON;

SELECT top 30 isnull(EQUIPMENT_CODE, '') EQUIPMENT_CODE
      ,isnull(LINE_CODE, '') LINE_CODE
      ,isnull(PROG_CODE, '') PROG_CODE
      ,isnull(PLC_IP, '') PLC_IP
      ,isnull(PLC_PORT, '') PLC_PORT
      ,isnull(TAG_ID, '') TAG_ID
      ,isnull(DATA_TYPE, '') DATA_TYPE
      ,isnull([DESCRIPTION], '') [DESCRIPTION]
      ,isnull(LINE_CODE1, '') LINE_CODE1
      ,isnull(STATION_CODE, '') STATION_CODE
      ,isnull(VIRTUAL_FLAG, '') VIRTUAL_FLAG
      ,isnull(PC_ID, '') PC_ID
      ,isnull(STOP_TYPE, '') STOP_TYPE
      ,isnull(TORQUE_ID, '') TORQUE_ID
	  ,'' [Value]
  FROM [OmmcMes].[dbo].[MST_EQUIPALARM]
  where 1=1
  and TAG_ID like 'TQ2_%'
  and [LINE_CODE1]='22'
  and [STOP_TYPE]='TORQUE'
union all
  select '','','','172.16.2.191   ','',   'M2_Start','','ML2 Full','',    '','','','','',''
union all
  select '','','','172.16.2.191   ','',   'M2_Start_VTL_H','','ML2 Half','',    '','','','','',''
  order by STATION_CODE

END



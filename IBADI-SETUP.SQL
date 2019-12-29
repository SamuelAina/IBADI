/*
--IBADI -- Integrated Backend Database INTERFACE
--(c) Samuel Aina 
This script is all that is required to turn a database into an IBADI-enabled database.
There's not much to it! 
1) create an IBADI database (optional, as you can create the schemas and objects in an existing database)
2) create the ibadi schema
3) create the webpage schema
4) create usp_check_if_proc_exists stored proc that tells the IBADI web service that a page proc exists
5) create the fuction that serves the html to the IBADI service
*/

CREATE SCHEMA webpage
GO
CREATE SCHEMA [ibadi]
GO

create proc [ibadi].[usp_check_if_proc_exists]  
 @procName  varchar(255)  
AS  
BEGIN  
  if exists(
  SELECT * 
  FROM INFORMATION_SCHEMA.ROUTINES 
  where  ROUTINE_SCHEMA = 'webpage' 
  and    specific_name=@procName
  )  
  BEGIN  
    SELECT result='1'  
  END  
  ELSE  
  BEGIN  
    SELECT result='0'  
  END  
END
GO

CREATE FUNCTION [ibadi].[html]
(
	@html varchar(max)
)
RETURNS varchar(max)
AS
BEGIN
	  SELECT  @html='[{"html":"'+replace(@html,'"','\"')+'"}]'

	  SELECT   @html = REPLACE(@html,CHAR(10)+CHAR(13),' ')  
	  SELECT   @html = REPLACE(@html,CHAR(13)+CHAR(10),' ')  
	  SELECT   @html = REPLACE(@html,CHAR(10),' ')  
	  SELECT   @html = REPLACE(@html,CHAR(13),' ')  
	  SELECT   @html = REPLACE(@html,CHAR(9),' ')   	

      RETURN  @html  
END
GO

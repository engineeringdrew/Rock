/* Use this script to help write a migration that includes updates to a FieldType's Name and/or Description */

select CONCAT('UpdateFieldType( "' , [Name] , '", "' , [Description] , '", "' , [Assembly] , '", "' , [Class] , '", "' , [Guid] , '");')
from FieldType
order by Class
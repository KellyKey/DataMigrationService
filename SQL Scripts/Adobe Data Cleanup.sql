/****************************************************
* Adobe Migration Data Cleanup
*****************************************************/

-- Add email domain to all member usernames.
update Members set Username = Username + '@adobe.com';

-- Remove uneccessary custom fields.
delete from CustomFields where FieldValue IS NULL;

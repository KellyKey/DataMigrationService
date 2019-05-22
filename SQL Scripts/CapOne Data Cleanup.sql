/****************************************************
* CapOne Migration Data Cleanup
*****************************************************/

-- **** SET TARGET DATABASE SIZE
ALTER DATABASE CapOneTarget MODIFY FILE 
(NAME = N'VersionOne', SIZE = 10240000KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024000KB );

ALTER DATABASE CapOneTarget MODIFY FILE 
(NAME = N'VersionOne_log', SIZE = 6144000KB , MAXSIZE = 15360000KB, FILEGROWTH = 1024000KB );

-- **** SET STAGING DATABASE SIZE
ALTER DATABASE CapOneStaging SET RECOVERY SIMPLE WITH NO_WAIT;

ALTER DATABASE CapOneStaging MODIFY FILE 
(NAME = N'VerOne_Scrubbed', SIZE = 5120000KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024000KB );

ALTER DATABASE CapOneStaging MODIFY FILE 
(NAME = N'VerOne_Scrubbed_log', SIZE = 2048000KB , MAXSIZE = 10240000KB , FILEGROWTH = 1024000KB );

-- **** RESET SOME ITERATIONS.
UPDATE Iterations
SET EndDate = '6/20/2013 12:00:00 AM'
WHERE AssetOID = 'Timebox:2467716';

UPDATE Iterations
SET BeginDate = '7/8/2013 12:00:00 AM'
WHERE AssetOID = 'Timebox:2467718';

UPDATE Iterations
SET BeginDate = '7/18/2013 12:00:00 AM'
WHERE AssetOID = 'Timebox:2964164';

UPDATE Iterations
SET BeginDate = '8/19/2013 12:00:00 AM'
WHERE AssetOID = 'Timebox:2964204';

UPDATE Iterations
SET BeginDate = '9/4/2013 12:00:00 AM'
WHERE AssetOID = 'Timebox:2964232';

-- **** DELECT CLOSED PROJECTS
delete from Actuals where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Tests where Parent in (select AssetOID from Stories where Scope in (select AssetOID from Projects where AssetState = 'Closed'));
delete from Tasks where Parent in (select AssetOID from Stories where Scope in (select AssetOID from Projects where AssetState = 'Closed'));
delete from Stories where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Epics where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Issues where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Requests where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from FeatureGroups where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Goals where Scope in (select AssetOID from Projects where AssetState = 'Closed');
delete from Projects where AssetState = 'Closed';

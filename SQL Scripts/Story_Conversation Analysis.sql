-- Story queries for non-matching dependencies
select COUNT(*) from stories

select AssetOID, Dependants from Stories 
where Dependants not in (select assetOID from Stories)

select AssetOID, Dependencies from Stories 
where Dependencies not in (select assetOID from Stories)


-- Conversation queries for non-matching mentions
select * from Conversations
where baseassets is null
and mentions is not null

select COUNT(*) as 'Total Conversations' from Conversations

select COUNT(*) as 'Conversations with no matching issues' from Conversations
where BaseAssets not in (select assetOID from Issues)
and BaseAssets like 'Issue%'

select COUNT(*) as 'Conversations with no matching requests' from Conversations
where BaseAssets not in (select assetOID from Requests)
and BaseAssets like 'Request%'

select COUNT(*) as 'Conversations with no matching stories' from Conversations
where BaseAssets not in (select assetOID from Stories)
and BaseAssets like 'Story%'

select COUNT(*) 'Conversations with no matching tasks' from Conversations
where BaseAssets not in (select assetOID from Tasks)
and BaseAssets like 'Task%'

select COUNT(*) 'Conversations with no matching members' from Conversations
where BaseAssets not in (select assetOID from Members)
and BaseAssets like 'Member%'


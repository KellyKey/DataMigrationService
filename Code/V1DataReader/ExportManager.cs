using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V1DataReader
{
    public class ExportManager
    {
        //public void ExportAssets()
        //{
        //    foreach (MigrationConfiguration.AssetInfo asset in _config.AssetsToMigrate)
        //    {
        //        int assetCount = 0;
        //        switch (asset.Name)
        //        {
        //            case "ListTypes":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting list types.");
        //                    ExportListTypes listTypes = new ExportListTypes(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = listTypes.Export();
        //                    _logger.Info("-> Exported {0} list types.", assetCount);
        //                }
        //                break;

        //            case "Members":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting members.");
        //                    ExportMembers members = new ExportMembers(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = members.Export();
        //                    _logger.Info("-> Exported {0} members.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} member custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Schedules":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting schedules.");
        //                    ExportSchedules schedules = new ExportSchedules(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = schedules.Export();
        //                    _logger.Info("-> Exported {0} schedules.", assetCount);
        //                }
        //                break;

        //            case "Projects":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting projects.");
        //                    ExportProjects projects = new ExportProjects(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = projects.Export();
        //                    _logger.Info("-> Exported {0} projects.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} project custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "MemberGroups":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting member groups.");
        //                    ExportMemberGroups memberGroups = new ExportMemberGroups(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = memberGroups.Export();
        //                    _logger.Info("-> Exported {0} member groups.", assetCount);
        //                }
        //                break;

        //            case "Programs":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting programs.");
        //                    ExportPrograms programs = new ExportPrograms(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = programs.Export();
        //                    _logger.Info("-> Exported {0} programs.", assetCount);
        //                }
        //                break;

        //            case "Iterations":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting iterations.");
        //                    ExportIterations iterations = new ExportIterations(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = iterations.Export();
        //                    _logger.Info("-> Exported {0} iterations.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} iteration custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Teams":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting teams.");
        //                    ExportTeams teams = new ExportTeams(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = teams.Export();
        //                    _logger.Info("-> Exported {0} teams.", assetCount);
        //                }
        //                break;

        //            case "FeatureGroups":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting feature groups.");
        //                    ExportFeatureGroups featureGroups = new ExportFeatureGroups(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = featureGroups.Export();
        //                    _logger.Info("-> Exported {0} feature groups.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} feature group custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Requests":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting requests.");
        //                    ExportRequests requests = new ExportRequests(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = requests.Export();
        //                    _logger.Info("-> Exported {0} requests.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} request custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Goals":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting goals.");
        //                    ExportGoals goals = new ExportGoals(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = goals.Export();
        //                    _logger.Info("-> Exported {0} goals.", assetCount);
        //                }
        //                break;

        //            case "Issues":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting issues.");
        //                    ExportIssues issues = new ExportIssues(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = issues.Export();
        //                    _logger.Info("-> Exported {0} issues.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} issue custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Epics":
        //                if (asset.Enabled == true)
        //                {
        //                    //SPECIAL CASE: Since epics used to be stories (11.3 and earlier), we must first export stories before exporting epics.
        //                    _logger.Info("Exporting stories.");
        //                    ExportStories stories = new ExportStories(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = stories.Export();
        //                    _logger.Info("-> Exported {0} stories.", assetCount);

        //                    //TO DO: This is testing the epic asset, not story, need to fix that!
        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Story");
        //                        assetCount = custom.Export();

        //                        //Also get PrimaryWorkitem custom fields.
        //                        custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "PrimaryWorkitem");
        //                        assetCount += custom.Export();

        //                        _logger.Debug("-> Exported {0} story custom fields.", assetCount);
        //                    }
        //                    _storiesAlreadyExported = true;

        //                    //Now we can export the epics.
        //                    _logger.Info("Exporting epics.");
        //                    ExportEpics epics = new ExportEpics(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = epics.Export();
        //                    _logger.Info("-> Exported {0} epics.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} epic custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Stories":
        //                if (asset.Enabled == true && _storiesAlreadyExported == false)
        //                {
        //                    _logger.Info("Exporting stories.");
        //                    ExportStories stories = new ExportStories(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = stories.Export();
        //                    _logger.Info("-> Exported {0} stories.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();

        //                        //Also get PrimaryWorkitem custom fields.
        //                        custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "PrimaryWorkitem");
        //                        assetCount += custom.Export();

        //                        _logger.Debug("-> Exported {0} story custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Defects":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting defects.");
        //                    ExportDefects defects = new ExportDefects(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = defects.Export();
        //                    _logger.Info("-> Exported {0} defects.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();

        //                        //Also get PrimaryWorkitem custom fields.
        //                        custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "PrimaryWorkitem");
        //                        assetCount += custom.Export();

        //                        _logger.Debug("-> Exported {0} defect custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Tasks":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting tasks.");
        //                    ExportTasks tasks = new ExportTasks(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = tasks.Export();
        //                    _logger.Info("-> Exported {0} tasks.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} task custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Tests":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting tests.");
        //                    ExportTests tests = new ExportTests(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = tests.Export();
        //                    _logger.Info("-> Exported {0} tests.", assetCount);

        //                    if (asset.EnableCustomFields == true)
        //                    {
        //                        ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.InternalName);
        //                        assetCount = custom.Export();
        //                        _logger.Debug("-> Exported {0} test custom fields.", assetCount);
        //                    }
        //                }
        //                break;

        //            case "Actuals":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting actuals.");
        //                    ExportActuals actuals = new ExportActuals(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = actuals.Export();
        //                    _logger.Info("-> Exported {0} actuals.", assetCount);
        //                }
        //                break;

        //            case "Links":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting links.");
        //                    ExportLinks links = new ExportLinks(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = links.Export();
        //                    _logger.Info("-> Exported {0} links.", assetCount);
        //                }
        //                break;

        //            case "Conversations":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting conversations.");
        //                    ExportConversations conversations = new ExportConversations(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
        //                    assetCount = conversations.Export();
        //                    _logger.Info("-> Exported {0} conversations.", assetCount);
        //                }
        //                break;

        //            case "Attachments":
        //                if (asset.Enabled == true)
        //                {
        //                    _logger.Info("Exporting attachments.");
        //                    ExportAttachments attachments = new ExportAttachments(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _sourceImageConnector, _config);
        //                    assetCount = attachments.Export();
        //                    _logger.Info("-> Exported {0} attachments.", assetCount);
        //                }
        //                break;

        //            default:
        //                break;
        //        }
        //    }

        //}

    }
}

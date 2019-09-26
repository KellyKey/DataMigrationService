using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using VersionOne.SDK.APIClient;
using NLog;
using V1DataWriter;
using V1DataReader;
using V1DataCore;
using V1DataCleanup;
//using RallyDataReader;
//using JiraReaderService;

namespace V1DataMigrationService
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static MigrationConfiguration _config;
        private static SqlConnection _sqlConn;

        //Source API connectors.
        //private static V1Connector _sourceMetaConnector;
        private static V1Connector _sourceDataConnector;
        private static V1Connector _sourceImageConnector;
        private static IMetaModel _sourceMetaAPI;
        private static Services _sourceDataAPI;

        //Target API connectors.
        //private static V1Connector _targetMetaConnector;
        private static V1Connector _targetDataConnector;
        private static V1Connector _targetImageConnector;
        private static IMetaModel _targetMetaAPI;
        private static Services _targetDataAPI;

        static void Main(string[] args)
        {
            //create a timer
            DateTime timerStart = DateTime.Now;
            DateTime timerEnd = DateTime.Now;

            try
            {

                CreateLogHeader();
                _logger.Info("Initializing configurations.");
                _config = (MigrationConfiguration)ConfigurationManager.GetSection("migration");

                _logger.Info("Verifying connections.");
                VerifyStagingDBConnection(_config.V1StagingDatabase);

                if (_config.V1Configurations.PerformExport == true && _config.V1Configurations.SourceConnectionToUse == "VersionOne") 
                    VerifyV1SourceConnection();

                if (_config.V1Configurations.PerformImport == true || _config.V1Configurations.PerformClose == true || _config.V1Configurations.PerformOpen == true || _config.V1Configurations.PerformCleanup) 
                    VerifyV1TargetConnection();
                _logger.Info("");


                //Used to delete any bad imports 
                //purgeBadImports("Attachment", 12105, 12194);

                //Export from source system to staging database.
                if (_config.V1Configurations.PerformExport == true)
                {
                    _logger.Info("*** EXPORTING:");
                    //PurgeMigrationDatabase();
                    ExportAssets();
                    _logger.Info("");
                }

                //Import into target system from staging database.
                if (_config.V1Configurations.PerformImport == true)
                {
                    _logger.Info("*** IMPORTING:");
                    ImportAssets();
                    _logger.Info("");
                }

                //Close imported items in target system.
                if (_config.V1Configurations.PerformClose == true)
                {
                    _logger.Info("*** CLOSING:");
                    CloseAssets();
                    _logger.Info("");
                }

                //Open imported items in target system for Maintenance ie. EmbeddedImages, etc.
                if (_config.V1Configurations.PerformOpen == true)
                {
                    _logger.Info("*** Opening:");
                    OpenAssets();
                    _logger.Info("");
                }

                //Do cleanup tasks in target system.
                if (_config.V1Configurations.PerformCleanup== true)
                {
                    _logger.Info("*** CLEANUP:");
                    CleanupAssets();
                    _logger.Info("");
                }

                _sqlConn.Close();
                _logger.Info("Data migration complete!");
            }
            catch (Exception ex)
            {
                _logger.Error("ERROR: " + ex.Message);
                //_logger.Error("INNER: " + ex.InnerException.ToString());
                //_logger.Error("TRACE: " + ex.StackTrace.ToString());
                _logger.Info("");
                _logger.Info("Data migration terminated.");
            }
            finally
            {
                timerEnd = DateTime.Now;
                TimeSpan span = timerEnd.Subtract(timerStart);
                Console.WriteLine("Total Time (seconds): " + span.Seconds);
                Console.WriteLine("Total Time (minutes): " + span.Minutes);
                Console.WriteLine("Total Time (hours): " + span.Hours);

                _logger.Info("");
                _logger.Info("");

                //sendMessage("smtp.gmail.com", "8173953888@mobile.att.net", "kellypkey@gmail.com","Data Migration Process Finished","Process Complete");

                Console.WriteLine();
                Console.WriteLine("Press ENTER to close:");
                Console.Read();
                Environment.Exit(0);
            }
        }

        private static void CleanupAssets()
        {
            _logger.Info("Cleaning up RegressionTests...");
            CleanupRegressionTests regressionTests = new CleanupRegressionTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            regressionTests.Cleanup();

            _logger.Info("Cleaning up Epics...");
            CleanupEpics epics = new CleanupEpics(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            epics.Cleanup();

            _logger.Info("Cleaning up Stories...");
            CleanupStories stories = new CleanupStories(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            stories.Cleanup();

            _logger.Info("Cleaning up Defects...");
            CleanupDefects defects = new CleanupDefects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            defects.Cleanup();

            _logger.Info("Cleaning up Tasks...");
            CleanupTasks tasks = new CleanupTasks(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            tasks.Cleanup();

            //_logger.Info("Cleaning up Tests...");
            //CleanupTests tests = new CleanupTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
            //tests.Cleanup();
        }

        private static void ExportAssets()
        {
            foreach (MigrationConfiguration.AssetInfo asset in _config.AssetsToMigrate)
            {
                int assetCount = 0;
                switch (asset.Name)
                {
                    case "ListTypes":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting list types.");
                            //For V1 Migrations
                            ExportListTypes listTypes = new ExportListTypes(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportListTypes listTypes = new ExportListTypes(_sqlConn, _config);
                            assetCount = listTypes.Export();
                            _logger.Info("-> Exported {0} list types.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "List");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} list type custom fields.", assetCount);
                            }

                        }
                        break;

                    case "Members":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting members.");
                            //For V1 Migrations
                            ExportMembers members = new ExportMembers(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportMembers members = new ExportMembers(_sqlConn, _config);
                            assetCount = members.Export();
                            _logger.Info("-> Exported {0} members.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Member");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} member custom fields.", assetCount);
                            }

                        }
                        break;

                    //For V1 Migrations
                    case "MemberGroups":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting member groups.");
                            ExportMemberGroups memberGroups = new ExportMemberGroups(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = memberGroups.Export();
                            _logger.Info("-> Exported {0} member groups.", assetCount);
                        }
                        break;

                    //For V1 Migrations
                    case "Teams":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting teams.");
                            ExportTeams teams = new ExportTeams(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = teams.Export();
                            _logger.Info("-> Exported {0} teams.", assetCount);
                        }
                        break;

                    case "Schedules":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting schedules.");
                            //For V1 Migrations
                            ExportSchedules schedules = new ExportSchedules(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally Migrations
                            //ExportSchedules schedules = new ExportSchedules(_sqlConn, _config);
                            assetCount = schedules.Export();
                            _logger.Info("-> Exported {0} schedules.", assetCount);
                        }
                        break;
                    
                    case "Projects":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting projects.");
                            //For V1 Migrations
                            ExportProjects projects = new ExportProjects(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportProjects projects = new ExportProjects(_sqlConn, _config);
                            _logger.Info("Calling projects.Export");
                            assetCount = projects.Export();
                            _logger.Info("-> Exported {0} projects.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Scope");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} project custom fields.", assetCount);
                            }

                        }
                        break;

                    //For Rally Data Reader                    
                    //case "Releases":
                    //    if (asset.Enabled == true)
                    //    {
                    //        _logger.Info("Exporting releases.");
                    //        For Rally Migrations
                    //        ExportReleases releases = new ExportReleases(_sqlConn, _config);
                    //        assetCount = releases.Export();
                    //        _logger.Info("-> Exported {0} releases.", assetCount);

                    //        Can create schedules only after projects/releases have been exported.
                    //        _logger.Info("Exporting schedules.");
                    //        For V1 Migrations
                    //        ExportSchedules schedules = new ExportSchedules(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                    //        For Rally Migrations
                    //        ExportSchedules schedules = new ExportSchedules(_sqlConn, _config);
                    //        assetCount = schedules.Export();
                    //        _logger.Info("-> Exported {0} schedules.", assetCount);
                    //    }
                    //    break;


                    //For V1 Migrations
                    case "Programs":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting programs.");
                            ExportPrograms programs = new ExportPrograms(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = programs.Export();
                            _logger.Info("-> Exported {0} programs.", assetCount);
                        }
                        break;

                    case "Iterations":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting iterations.");
                            //For V1 Migrations
                            ExportIterations iterations = new ExportIterations(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportIterations iterations = new ExportIterations(_sqlConn, _config);
                            assetCount = iterations.Export();
                            _logger.Info("-> Exported {0} iterations.", assetCount);
                        }
                        break;

                    //For V1 Migrations
                    case "Goals":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting goals.");
                            ExportGoals goals = new ExportGoals(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = goals.Export();
                            _logger.Info("-> Exported {0} goals.", assetCount);
                        }
                        break;

                    //For V1 Migrations
                    case "FeatureGroups":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting feature groups.");
                            ExportFeatureGroups featureGroups = new ExportFeatureGroups(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = featureGroups.Export();
                            _logger.Info("-> Exported {0} feature groups.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Theme");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} feature group custom fields.", assetCount);
                            }
                        }
                        break;

                    //For V1 Migrations
                    case "Requests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting requests.");
                            ExportRequests requests = new ExportRequests(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = requests.Export();
                            _logger.Info("-> Exported {0} requests.", assetCount);


                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Request");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} request custom fields.", assetCount);
                            }
                        }
                        break;

                    //For V1 Migrations
                    case "Issues":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting issues.");
                            ExportIssues issues = new ExportIssues(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = issues.Export();
                            _logger.Info("-> Exported {0} issues.", assetCount);


                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Issue");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} issue custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Epics":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting epics.");
                            //For V1 Migrations
                            ExportEpics epics = new ExportEpics(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportEpics epics = new ExportEpics(_sqlConn, _config);
                            assetCount = epics.Export();
                            _logger.Info("-> Exported {0} epics.", assetCount);


                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Epic");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} epic custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Stories":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting stories.");
                            //For V1 Migrations
                            ExportStories stories = new ExportStories(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportStories stories = new ExportStories(_sqlConn, _config);
                            assetCount = stories.Export();
                            _logger.Info("-> Exported {0} stories.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                //Need to Export Custom Fields
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Story");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} Story custom fields.", assetCount);

                                //Need to Export the PrimaryWorkitems which will get fields configured for Story, Defect, and TestSets all in one run
                                //Do not need to run it again in Defects and TestSets
                                ExportCustomFields custom2 = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "PrimaryWorkitem");
                                assetCount = custom2.Export();
                                _logger.Debug("-> Exported {0} PrimaryWorkitem custom fields.", assetCount);

                            }

                        }
                        break;

                    case "Defects":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting defects.");
                            //For V1 Migrations
                            ExportDefects defects = new ExportDefects(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportDefects defects = new ExportDefects(_sqlConn, _config);
                            assetCount = defects.Export();
                            _logger.Info("-> Exported {0} defects.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                //ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, asset.Name);
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Defect");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} Defects custom fields.", assetCount);
                            }

                        }
                        break;

                    case "Tasks":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting tasks.");
                            //For V1 Migrations
                            ExportTasks tasks = new ExportTasks(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportTasks tasks = new ExportTasks(_sqlConn, _config);
                            assetCount = tasks.Export();
                            _logger.Info("-> Exported {0} tasks.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Task");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} Tasks custom fields.", assetCount);
                            }

                        }
                        break;

                    //For Rally Data Reader                    
                    //case "TestSteps":
                    //    if (asset.Enabled == true)
                    //    {
                    //        _logger.Info("Exporting test steps.");
                    //        ExportTestSteps teststeps = new ExportTestSteps(_sqlConn, _config);
                    //        assetCount = teststeps.Export();
                    //        _logger.Info("-> Exported {0} test steps.", assetCount);
                    //    }
                    //    break;
                     

                    case "Tests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting tests.");
                            //For V1 Migrations
                            ExportTests tests = new ExportTests(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportTests tests = new ExportTests(_sqlConn, _config);
                            assetCount = tests.Export();
                            _logger.Info("-> Exported {0} tests.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ExportCustomFields custom = new ExportCustomFields(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config, "Test");
                                assetCount = custom.Export();
                                _logger.Debug("-> Exported {0} Tests custom fields.", assetCount);
                            }

                        }
                        break;

                        
                        case "RegressionTests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting regression tests.");
                            //For V1 Migrations
                            ExportRegressionTests regressionTests = new ExportRegressionTests(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally and Jira Migrations
                            //ExportRegressionTests regressionTests = new ExportRegressionTests(_sqlConn, _config);
                            assetCount = regressionTests.Export();
                            _logger.Info("-> Exported {0} regression tests.", assetCount);
                        }
                        break;

                        //For V1 Migrations
                        case "Actuals":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting actuals.");
                            ExportActuals actuals = new ExportActuals(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = actuals.Export();
                            _logger.Info("-> Exported {0} actuals.", assetCount);
                        }
                        break;

                        //For V1 Migrations
                        case "Links":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting links.");
                            ExportLinks links = new ExportLinks(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            assetCount = links.Export();
                            _logger.Info("-> Exported {0} links.", assetCount);
                        }
                        break;

                    case "Conversations":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting conversations.");
                            //For V1 Migrations
                            ExportConversations conversations = new ExportConversations(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _config);
                            //For Rally Migrations
                            //ExportConversations conversations = new ExportConversations(_sqlConn, _config);
                            assetCount = conversations.Export();
                            _logger.Info("-> Exported {0} conversations.", assetCount);
                        }
                        break;

                    case "Attachments":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting attachments.");
                            //For V1 Migrations
                            ExportAttachments attachments = new ExportAttachments(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _sourceImageConnector, _config);
                            //For Rally Migrations
                            //ExportAttachments attachments = new ExportAttachments(_sqlConn, _config);
                            assetCount = attachments.Export();
                            _logger.Info("-> Exported {0} attachments.", assetCount);
                        }
                        break;

                    case "EmbeddedImages":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Exporting embeddedimages.");
                            //For V1 Migrations
                            ExportEmbeddedImages embeddedimages = new ExportEmbeddedImages(_sqlConn, _sourceMetaAPI, _sourceDataAPI, _sourceImageConnector, _config);
                            assetCount = embeddedimages.Export();
                            _logger.Info("-> Exported {0} embeddedimages.", assetCount);
                        }
                        break;


                }
            }
        }

        private static void ImportAssets()
        {
            foreach (MigrationConfiguration.AssetInfo asset in _config.AssetsToMigrate)
            {
                int assetCount = 0;
                switch (asset.Name)
                {
                    case "ListTypes":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing list types.");
                            ImportListTypes listTypes = new ImportListTypes(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = listTypes.Import();
                            _logger.Info("-> Imported {0} list types.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "List");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} ListTypes custom fields.", assetCount);
                            }

                        }
                        break;

                    case "Members":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing members.");
                            ImportMembers members = new ImportMembers(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = members.Import();
                            _logger.Info("-> Imported {0} members.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Member");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} member custom fields.", assetCount);
                            }
                        }
                        break;

                    case "MemberGroups":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing member groups.");
                            ImportMemberGroups memberGroups = new ImportMemberGroups(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = memberGroups.Import();
                            _logger.Info("-> Imported {0} member groups.", assetCount);
                        }
                        break;

                    case "Teams":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing teams.");
                            ImportTeams teams = new ImportTeams(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = teams.Import();
                            _logger.Info("-> Imported {0} teams.", assetCount);
                        }
                        break;

                    case "Schedules":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing schedules.");
                            ImportSchedules schedules = new ImportSchedules(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = schedules.Import();
                            _logger.Info("-> Imported {0} schedules.", assetCount);
                        }
                        break;

                    case "Projects":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing projects.");
                            ImportProjects projects = new ImportProjects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = projects.Import();
                            _logger.Info("-> Imported {0} projects.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Project");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} project custom fields.", assetCount);
                            }

                            if (_config.V1Configurations.SetAllMembershipToRoot == true)
                            {
                                assetCount = projects.SetMembershipToRoot();
                                _logger.Info("-> Imported {0} project memberships to target root project.", assetCount);
                            }
                        }
                        break;

                    case "Programs":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing programs.");
                            ImportPrograms programs = new ImportPrograms(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = programs.Import();
                            _logger.Info("-> Imported {0} programs.", assetCount);
                        }
                        break;

                    case "Iterations":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing iterations.");
                            ImportIterations iterations = new ImportIterations(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = iterations.Import();
                            _logger.Info("-> Imported {0} iterations.", assetCount);
                        }
                        break;

                    case "Goals":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing goals.");
                            ImportGoals goals = new ImportGoals(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = goals.Import();
                            _logger.Info("-> Imported {0} goals.", assetCount);
                        }
                        break;

                    case "FeatureGroups":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing feature groups.");
                            ImportFeatureGroups featureGroups = new ImportFeatureGroups(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = featureGroups.Import();
                            _logger.Info("-> Imported {0} feature groups.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Theme");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} feature group custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Requests":
                        if (asset.Enabled == true)
                        {
                            //_logger.Info("Importing requests.");
                            //ImportRequests requests = new ImportRequests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            //assetCount = requests.Import();
                            //_logger.Info("-> Imported {0} requests.", assetCount);


                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Request");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} request custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Issues":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing issues.");
                            ImportIssues issues = new ImportIssues(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = issues.Import();
                            _logger.Info("-> Imported {0} issues.", assetCount);
                        

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Issue");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} issue custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Epics":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing epics.");
                            ImportEpics epics = new ImportEpics(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = epics.Import();
                            _logger.Info("-> Imported {0} epics.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Epic");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} epic custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Stories":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing stories.");
                            ImportStories stories = new ImportStories(_sqlConn, _targetMetaAPI, _targetDataAPI, _targetImageConnector, _config);
                            assetCount = stories.Import();
                            _logger.Info("-> Imported {0} stories.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Story");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} Story custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Defects":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing defects.");
                            ImportDefects defects = new ImportDefects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = defects.Import();
                            _logger.Info("-> Imported {0} defects.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Defect");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} defects custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Tasks":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing tasks.");
                            ImportTasks tasks = new ImportTasks(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = tasks.Import();
                            _logger.Info("-> Imported {0} tasks.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Task");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} task custom fields.", assetCount);
                            }
                        }
                        break;

                    case "Tests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing tests.");
                            ImportTests tests = new ImportTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = tests.Import();
                            _logger.Info("-> Imported {0} tests.", assetCount);

                            if (asset.EnableCustomFields == true)
                            {
                                ImportCustomFields custom = new ImportCustomFields(_sqlConn, _targetMetaAPI, _targetDataAPI, _config, "Test");
                                assetCount = custom.Import();
                                _logger.Debug("-> Imported {0} test custom fields.", assetCount);
                            }
                        }
                        break;

                    case "OrphanedTests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing orphaned tests.");
                            ImportOrphanedTests orphanedTests = new ImportOrphanedTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = orphanedTests.Import();
                            _logger.Info("-> Imported {0} orphaned tests.", assetCount);
                        }
                        break;

                    case "RegressionTests":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing regression tests.");
                            ImportRegressionTests regressionTests = new ImportRegressionTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = regressionTests.Import();
                            _logger.Info("-> Imported {0} regression tests.", assetCount);
                        }
                        break;

                    case "Actuals":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing actuals.");
                            ImportActuals actuals = new ImportActuals(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = actuals.Import();
                            _logger.Info("-> Imported {0} actuals.", assetCount);
                        }
                        break;

                    case "Links":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing links.");
                            ImportLinks links = new ImportLinks(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = links.Import();
                            _logger.Info("-> Imported {0} links.", assetCount);
                        }
                        break;

                    case "Conversations":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing conversations.");
                            ImportConversations conversations = new ImportConversations(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                            assetCount = conversations.Import();
                            _logger.Info("-> Imported {0} conversations.", assetCount);
                        }
                        break;

                    case "Attachments":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing attachments.");
                            ImportAttachments attachments = new ImportAttachments(_sqlConn, _targetMetaAPI, _targetDataAPI, _targetImageConnector, _config);

                            if (String.IsNullOrEmpty(_config.V1Configurations.ImportAttachmentsAsLinksURL) == true)
                            {
                                assetCount = attachments.Import();
                                _logger.Info("-> Imported {0} attachments.", assetCount);
                            }
                            else
                            {
                                assetCount = attachments.ImportAttachmentsAsLinks();
                                _logger.Info("-> Imported {0} attachments as links.", assetCount);
                            }
                        }
                        break;

                    case "EmbeddedImages":
                        if (asset.Enabled == true)
                        {
                            _logger.Info("Importing embeddedimages.");
                            //For V1 Migrations
                            ImportEmbeddedImages embeddedimages = new ImportEmbeddedImages(_sqlConn, _targetMetaAPI, _targetDataAPI, _targetImageConnector, _config);
                            assetCount = embeddedimages.Import();
                            _logger.Info("-> Imported {0} embeddedimages.", assetCount);
                        }
                        break;


                    default:
                        break;
                }
            }
        }

        private static void CloseAssets()
        {
            int assetCount = 0;

            if (_config.AssetsToMigrate.Find(i => i.Name == "ListTypes").Enabled == true)
            {
                _logger.Info("Closing list types.");
                ImportListTypes listTypes = new ImportListTypes(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = listTypes.CloseListTypes();
                _logger.Info("-> Closed {0} list types.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Members").Enabled == true)
            {
                _logger.Info("Closing members.");
                ImportMembers members = new ImportMembers(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = members.CloseMembers();
                _logger.Info("-> Closed {0} members.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Teams").Enabled == true)
            {
                _logger.Info("Closing teams.");
                ImportTeams teams = new ImportTeams(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = teams.CloseTeams();
                _logger.Info("-> Closed {0} teams.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Goals").Enabled == true)
            {
                _logger.Info("Closing goals.");
                ImportGoals goals = new ImportGoals(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = goals.CloseGoals();
                _logger.Info("-> Closed {0} goals.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "FeatureGroups").Enabled == true)
            {
                _logger.Info("Closing feature groups.");
                ImportFeatureGroups featureGroups = new ImportFeatureGroups(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = featureGroups.CloseFeatureGroups();
                _logger.Info("-> Closed {0} feature groups.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Requests").Enabled == true)
            {
                _logger.Info("Closing requests.");
                ImportRequests requests = new ImportRequests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = requests.CloseRequests();
                _logger.Info("-> Closed {0} requests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Issues").Enabled == true)
            {
                _logger.Info("Closing issues.");
                ImportIssues issues = new ImportIssues(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = issues.CloseIssues();
                _logger.Info("-> Closed {0} issues.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Tests").Enabled == true)
            {
                _logger.Info("Closing tests.");
                ImportTests tests = new ImportTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tests.CloseTests();
                _logger.Info("-> Closed {0} tests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "RegressionTests").Enabled == true)
            {
                _logger.Info("Closing regression tests.");
                ImportRegressionTests tests = new ImportRegressionTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tests.CloseRegressionTests();
                _logger.Info("-> Closed {0} regression tests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Tasks").Enabled == true)
            {
                _logger.Info("Closing tasks.");
                ImportTasks tasks = new ImportTasks(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tasks.CloseTasks();
                _logger.Info("-> Closed {0} tasks.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Defects").Enabled == true)
            {
                _logger.Info("Closing defects.");
                ImportDefects defects = new ImportDefects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = defects.CloseDefects();
                _logger.Info("-> Closed {0} defects.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Stories").Enabled == true)
            {
                _logger.Info("Closing stories.");
                ImportStories stories = new ImportStories(_sqlConn, _targetMetaAPI, _targetDataAPI, _targetImageConnector, _config);
                assetCount = stories.CloseStories();
                _logger.Info("-> Closed {0} stories.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Epics").Enabled == true)
            {
                _logger.Info("Closing epics.");
                ImportEpics epics = new ImportEpics(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = epics.CloseEpics();
                _logger.Info("-> Closed {0} epics.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Iterations").Enabled == true)
            {
                _logger.Info("Closing iterations.");
                ImportIterations iterations = new ImportIterations(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = iterations.CloseIterations();
                _logger.Info("-> Closed {0} iterations.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Projects").Enabled == true)
            {
                _logger.Info("Closing projects.");
                ImportProjects projects = new ImportProjects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = projects.CloseProjects();
                _logger.Info("-> Closed {0} projects.", assetCount);
            }
        }


        private static void OpenAssets()
        {
            int assetCount = 0;

            if (_config.AssetsToMigrate.Find(i => i.Name == "Projects").Enabled == true)
            {
                _logger.Info("Opening projects.");
                ImportProjects projects = new ImportProjects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = projects.OpenProjects();
                _logger.Info("-> Opened {0} projects.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Iterations").Enabled == true)
            {
                _logger.Info("Opening iterations.");
                ImportIterations iterations = new ImportIterations(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = iterations.OpenIterations();
                _logger.Info("-> Opened {0} iterations.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Epics").Enabled == true)
            {
                _logger.Info("Opening epics.");
                ImportEpics epics = new ImportEpics(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = epics.OpenEpics();
                _logger.Info("-> Opened {0} epics.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Stories").Enabled == true)
            {
                _logger.Info("Opening stories.");
                ImportStories stories = new ImportStories(_sqlConn, _targetMetaAPI, _targetDataAPI, _targetImageConnector, _config);
                assetCount = stories.OpenStories();
                //assetCount = stories.CloseStories();
                _logger.Info("-> Opened {0} stories.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Defects").Enabled == true)
            {
                _logger.Info("Opening defects.");
                ImportDefects defects = new ImportDefects(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = defects.OpenDefects();
                _logger.Info("-> Opened {0} defects.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Tasks").Enabled == true)
            {
                _logger.Info("Opening tasks.");
                ImportTasks tasks = new ImportTasks(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tasks.OpenTasks();
                _logger.Info("-> Opened {0} tasks.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "RegressionTests").Enabled == true)
            {
                _logger.Info("Opening regression tests.");
                ImportRegressionTests tests = new ImportRegressionTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tests.OpenRegressionTests();
                _logger.Info("-> Opened {0} regression tests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Tests").Enabled == true)
            {
                _logger.Info("Opening tests.");
                ImportTests tests = new ImportTests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = tests.OpenTests();
                _logger.Info("-> Opened {0} tests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Issues").Enabled == true)
            {
                _logger.Info("Opening issues.");
                ImportIssues issues = new ImportIssues(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = issues.OpenIssues();
                _logger.Info("-> Opened {0} issues.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Requests").Enabled == true)
            {
                _logger.Info("Opening requests.");
                ImportRequests requests = new ImportRequests(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = requests.OpenRequests();
                _logger.Info("-> Opened {0} requests.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "FeatureGroups").Enabled == true)
            {
                _logger.Info("Opening feature groups.");
                ImportFeatureGroups featureGroups = new ImportFeatureGroups(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = featureGroups.OpenFeatureGroups();
                _logger.Info("-> Opened {0} feature groups.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Goals").Enabled == true)
            {
                _logger.Info("Opening goals.");
                ImportGoals goals = new ImportGoals(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = goals.OpenGoals();
                _logger.Info("-> Opened {0} goals.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Teams").Enabled == true)
            {
                _logger.Info("Opening teams.");
                ImportTeams teams = new ImportTeams(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = teams.OpenTeams();
                _logger.Info("-> Opened {0} teams.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "Members").Enabled == true)
            {
                _logger.Info("Opening members.");
                ImportMembers members = new ImportMembers(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = members.OpenMembers();
                _logger.Info("-> Opened {0} members.", assetCount);
            }

            if (_config.AssetsToMigrate.Find(i => i.Name == "ListTypes").Enabled == true)
            {
                _logger.Info("Opening list types.");
                ImportListTypes listTypes = new ImportListTypes(_sqlConn, _targetMetaAPI, _targetDataAPI, _config);
                assetCount = listTypes.OpenListTypes();
                _logger.Info("-> Opened {0} list types.", assetCount);
            }
        }

        private static void PurgeMigrationDatabase()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "spPurgeDatabase";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateLogHeader()
        {
            _logger.Info("*********************************************************");
            _logger.Info("* VersionOne Data Migration Log");
            _logger.Info("* {0}", DateTime.Now);
            _logger.Info("*********************************************************");
            _logger.Info("");
        }

        //Verifies the connection to the V1 source instance.
        private static void VerifyV1SourceConnection()
        {
            try
            {
                //_sourceMetaConnector = new V1Connector(_config.V1SourceConnection.Url + "/meta.v1/");
                //Use Services.Meta since 15.00 API

                if (_config.V1SourceConnection.AuthenticationType == "standard")
                {
                    _sourceDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1SourceConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithUsernameAndPassword(_config.V1SourceConnection.Username, _config.V1SourceConnection.Password)
                        .Build();

                    _sourceImageConnector = _sourceDataConnector;

                }
                else if (_config.V1SourceConnection.AuthenticationType == "windows")
                {
                    _sourceDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1SourceConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithWindowsIntegrated(_config.V1SourceConnection.Username, _config.V1SourceConnection.Password)
                        .Build();
                    
                    _sourceImageConnector = _sourceDataConnector;
                
                }
                else if (_config.V1SourceConnection.AuthenticationType == "accessToken")
                {
                    _sourceDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1SourceConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithAccessToken(_config.V1SourceConnection.AccessToken)
                        .Build();

                    _sourceImageConnector = _sourceDataConnector;

                }
                else if (_config.V1SourceConnection.AuthenticationType == "oauth")
                {
                    throw new Exception("OAuth authentication is not supported -- yet.");
                }
                else
                {
                    throw new Exception("Unable to determine the V1SourceConnection authentication type in the config file. Value used must be standard|windows|oauth.");
                }

                _sourceDataAPI = new Services(_sourceDataConnector);
                _sourceMetaAPI = _sourceDataAPI.Meta;
                
                _logger.Info("Test - Testing URI - Pulling Meta");
                _logger.Info("Test - Meta Data is:  " + _sourceMetaAPI.ToString());
                IAssetType scopeType = _sourceMetaAPI.GetAssetType("Scope");
                _logger.Info("Test - Testing URI - SUCCESS");


                _logger.Info("Test - Testing Token - Pulling Data");
                Oid projectId = _sourceDataAPI.GetOid("Scope:0");
                IAssetType defectType = _sourceMetaAPI.GetAssetType("Defect");
                Asset NewScope = _sourceDataAPI.New(defectType, projectId);

                _logger.Info("Test - Testing Token - SUCCESS");


                //Oid loggedInOid = _sourceDataAPI.LoggedIn;

                //IAssetType assetType = _sourceMetaAPI.GetAssetType("Member");
                //Query query = new Query(assetType);
                //IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Username");
                //query.Selection.Add(nameAttribute);
                //FilterTerm idFilter = new FilterTerm(nameAttribute);
                //idFilter.Equal(_config.V1SourceConnection.Username);
                //query.Filter = idFilter;
                //QueryResult result = _sourceDataAPI.Retrieve(query);
                if (NewScope != null)
                {
                    _logger.Info("-> Connection to V1 source instance \"{0}\" verified.", _config.V1SourceConnection.Url);
                    _logger.Debug("-> V1 source instance version: {0}.", _sourceDataAPI.Meta.ToString());
                    MigrationStats.WriteStat(_sqlConn, "Source API Version", _sourceDataAPI.Meta.ToString());
                }
                else
                {
                    throw new Exception(String.Format("Unable to validate connection to {0} with username {1}. You may not have permission to access this instance.", _config.V1SourceConnection.Url, _config.V1SourceConnection.Username));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("-> Unable to connect to V1 source instance \"{0}\".", _config.V1SourceConnection.Url);
                throw ex;
            }
        }

        //Verifies the connection to the V1 target instance.
        private static void VerifyV1TargetConnection()
        {
            try
            {
                //_targetMetaConnector = new V1Connector(_config.V1TargetConnection.Url + "/meta.v1/");
                //Use Services.Meta since 15.00 API

                if (_config.V1TargetConnection.AuthenticationType == "standard")
                {
                    _targetDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1TargetConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithUsernameAndPassword(_config.V1TargetConnection.Username, _config.V1TargetConnection.Password)
                        .Build();

                    //Version 15.1 upgrade...no need for .UseEndpoint
                    _targetImageConnector = _targetDataConnector;

                    //_targetDataConnector = new V1Connector(_config.V1TargetConnection.Url + "/rest-1.v1/", _config.V1TargetConnection.Username, _config.V1TargetConnection.Password, false);
                    //_targetDataConnector = new V1Connector(_config.V1TargetConnection.Url + "/attachment.img/", _config.V1TargetConnection.Username, _config.V1TargetConnection.Password, false);
                }
                else if (_config.V1TargetConnection.AuthenticationType == "windows")
                {
                    //ProxyProvider proxyProvider = new ProxyProvider(new Uri("proxyURL"), "proxyUsername", "proxyPassword");
                    
                    _targetDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1TargetConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithWindowsIntegrated(_config.V1TargetConnection.Username, _config.V1TargetConnection.Password)
                        //.WithProxy(proxyProvider);
                        .Build();

                    //Version 15.1 upgrade...no need for .UseEndpoint
                    _targetImageConnector = _targetDataConnector;


                    //_targetDataConnector = new V1Connector(_config.V1TargetConnection.Url + "/rest-1.v1/", _config.V1TargetConnection.Username, _config.V1TargetConnection.Password, true);
                    //_targetDataConnector = new V1Connector(_config.V1TargetConnection.Url + "/attachment.img/", _config.V1TargetConnection.Username, _config.V1TargetConnection.Password, true);
                }
                else if (_config.V1TargetConnection.AuthenticationType == "accessToken")
                {
                    _targetDataConnector = V1Connector
                        .WithInstanceUrl(_config.V1TargetConnection.Url)
                        .WithUserAgentHeader("V1DataMigration", "1.0")
                        .WithAccessToken(_config.V1TargetConnection.AccessToken.Trim())
                        .Build();

                    //Version 15.1 upgrade...no need for .UseEndpoint
                    _targetImageConnector = _targetDataConnector;

                    //_targetImageConnector = V1Connector
                    //    .WithInstanceUrl(_config.V1SourceConnection.Url)
                    //    .WithUserAgentHeader("V1DataMigration", "1.0")
                    //    .WithAccessToken(_config.V1SourceConnection.AccessToken)
                    //    .UseEndpoint("attachment.img")
                    //    .Build();
                }
                else if (_config.V1TargetConnection.AuthenticationType == "oauth")
                {
                    throw new Exception("OAuth authentication is not supported -- yet.");
                }
                else
                {
                    throw new Exception("Unable to determine the V1TargetConnection authentication type in the config file. Value used must be standard|windows|oauth.");
                }

                _targetDataAPI = new Services(_targetDataConnector);
                _targetMetaAPI = _targetDataAPI.Meta;

                IAssetType assetType = _targetMetaAPI.GetAssetType("Member");
                Query query = new Query(assetType);
                IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Username");
                query.Selection.Add(nameAttribute);
                FilterTerm idFilter = new FilterTerm(nameAttribute);
                idFilter.Equal(_config.V1TargetConnection.Username);
                query.Filter = idFilter;
                QueryResult result = _targetDataAPI.Retrieve(query);
                if (result.TotalAvaliable > 0)
                {
                    _logger.Info("-> Connection to V1 target instance \"{0}\" verified.", _config.V1TargetConnection.Url);
                    _logger.Debug("-> V1 target instance version: {0}.", _targetDataAPI.Meta.ToString());
                    MigrationStats.WriteStat(_sqlConn, "Target API Version", _targetDataAPI.Meta.ToString());
                }
                else
                {
                    throw new Exception(String.Format("Unable to validate connection to {0} with username {1}. You may not have permission to access this instance.", _config.V1TargetConnection.Url, _config.V1TargetConnection.Username));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("-> Unable to connect to V1 target instance \"{0}\".", _config.V1TargetConnection.Url);
                throw ex;
            }
        }

        //Verifies the connection to the SQL Server staging database.
        private static void VerifyStagingDBConnection(MigrationConfiguration.StagingDatabaseInfo v1DatabaseInfo)
        {
            try
            {
                SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
                sb.DataSource = v1DatabaseInfo.Server;
                sb.UserID = v1DatabaseInfo.Username;
                sb.Password = v1DatabaseInfo.Password;
                sb.InitialCatalog = v1DatabaseInfo.Database;
                sb.MultipleActiveResultSets = true;
                if (_config.V1StagingDatabase.TrustedConnection == true)
                    sb.IntegratedSecurity = true;
                else
                    sb.IntegratedSecurity = false;
                _sqlConn = new SqlConnection(sb.ToString());
                _sqlConn.Open();
                _logger.Info("-> Connection to staging database \"{0}\" verified.", v1DatabaseInfo.Database);
            }
            catch (Exception ex)
            {
                _logger.Error("-> Unable to connect to staging database \"{0}\".", v1DatabaseInfo.Database);
                throw ex;
            }
        }

        private static void sendMessage(string inMailServer, string inTo, string inFrom, string inMsg, string inSubject)
        {
            try
            {
                MailMessage message = new MailMessage(inFrom, inTo, inSubject, inMsg);
                SmtpClient mySmtpClient = new SmtpClient(inMailServer);
                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.Send(message);
                Console.Write("The mail message has been sent to " + message);
            }
            catch (FormatException ex)
            {
                Console.Write(ex.Message);
            }
            catch (SmtpException ex)
            {
                Console.Write(ex.Message);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        //Verifies the connection to the SQL Server staging database.
        private static void purgeBadImports(string assetType, int startValue, int endValue)
        {
            
            for(int x = startValue;x <= endValue; x++)
            {
                IAssetType xAssetType = _targetMetaAPI.GetAssetType(assetType);
                IOperation deleteOperation = _targetDataAPI.Meta.GetOperation(assetType + ".Delete");
                Oid xOid = new Oid(xAssetType,x, 0);
                Oid deletedID = _targetDataAPI.ExecuteOperation(deleteOperation, xOid);

                try
                {
                    Query query = new Query(deletedID.Momentless);
                    _targetDataAPI.Retrieve(query);
                    _logger.Info("{0} has been deleted: {1}", assetType, deletedID.Momentless);
                }
                catch (Exception ex)
                {
                    _logger.Error("{0} has been deleted: {1}", assetType, deletedID.Momentless);
                } 
            }
        }

    }
}

using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace V1DataCore
{
    public class MigrationConfiguration
    {
        public struct ConnectionInfo
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Project { get; set; }
            public string AuthenticationType { get; set; }
        }

        public struct RallyConnectionInfo
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string ExportFileDirectory { get; set; }
            public string UserExportFilePrefix { get; set; }
            public string ProjectExportFilePrefix { get; set; }
            public string ReleaseExportFilePrefix { get; set; }
            public string IterationExportFilePrefix { get; set; }
            public string EpicExportFilePrefix { get; set; }
            public string StoryExportFilePrefix { get; set; }
            public string DefectExportFilePrefix { get; set; }
            public string TaskExportFilePrefix { get; set; }
            public string TestExportFilePrefix { get; set; }
            public string RegressionTestExportFilePrefix { get; set; }
            public string TestStepExportFilePrefix { get; set; }
            public string ConversationExportFilePrefix { get; set; }
            public string OrphanedTestProject { get; set; }
        }

        public struct JiraConfigurationInfo
        {
            public string XmlFileName { get; set; }
            public string ProjectName { get; set; }
            public string ProjectDescription { get; set; }
            public string DefaultSchedule { get; set; }
            public string JiraUrl { get; set; }
        }

        public struct StagingDatabaseInfo
        {
            public string Server { get; set; }
            public string Database { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool TrustedConnection { get; set; }
        }

        public struct ConfigurationInfo
        {
            public string SourceConnectionToUse { get; set; }
            public bool PerformExport { get; set; }
            public bool PerformImport { get; set; }
            public bool PerformClose { get; set; }
            public bool PerformCleanup { get; set; }
            public bool MigrateTemplates { get; set; }
            public bool MigrateAttachmentBinaries { get; set; }
            public bool MigrateProjectMembership { get; set; }
            public bool MigrateDuplicateSchedules { get; set; }
            public bool UseNPIMasking { get; set; }
            public bool MergeRootProjects { get; set; }
            public bool AddV1IDToTitles { get; set; }
            public int PageSize { get; set; }
            public string CustomV1IDField { get; set; }
            public string ImportAttachmentsAsLinksURL { get; set; }
            public bool SetAllMembershipToRoot { get; set; }
            public string SourceListTypeValue { get; set; }
            public bool MigrateUnauthoredConversationsAsAdmin { get; set; }
            public bool LogExceptions { get; set; }
        }

        public struct AssetInfo
        {
            public string Name { get; set; }
            public string InternalName { get; set; }
            public bool Enabled { get; set; }
            public string DuplicateCheckField { get; set; }
            public bool EnableCustomFields { get; set; }
        }

        public struct ListTypeInfo
        {
            public string Name { get; set; }
            public bool Enabled { get; set; }
        }

        public struct CustomFieldInfo
        {
            public string SourceName { get; set; }
            public string TargetName { get; set; }
            public string AssetType { get; set; }
            public string DataType { get; set; }
            public string RelationName { get; set; }
        }

        public ConnectionInfo V1SourceConnection = new ConnectionInfo();
        public ConnectionInfo V1TargetConnection = new ConnectionInfo();
        public StagingDatabaseInfo V1StagingDatabase = new StagingDatabaseInfo();
        public ConfigurationInfo V1Configurations = new ConfigurationInfo();
        public RallyConnectionInfo RallySourceConnection = new RallyConnectionInfo();
        public JiraConfigurationInfo JiraConfiguration = new JiraConfigurationInfo();
        public List<AssetInfo> AssetsToMigrate = new List<AssetInfo>();
        public List<ListTypeInfo> ListTypesToMigrate = new List<ListTypeInfo>();
        public List<CustomFieldInfo> CustomFieldsToMigrate = new List<CustomFieldInfo>();

        public MigrationConfiguration(XmlNode section)
        {
            //Convert the XmlNode to an XDocument (for LINQ).
            XDocument xmlDoc = XDocument.Parse(section.OuterXml);

            // **********************************
            // * V1 source connection.
            // **********************************
            var v1Source = from item in xmlDoc.Descendants("V1SourceConnection")
                           select new ConnectionInfo
                           {
                               Url = item.Element("Url").Value,
                               Username = string.IsNullOrEmpty(item.Element("Username").Value) ? string.Empty : item.Element("Username").Value,
                               Password = string.IsNullOrEmpty(item.Element("Password").Value) ? string.Empty : item.Element("Password").Value,
                               Project = string.IsNullOrEmpty(item.Element("Project").Value) ? string.Empty : item.Element("Project").Value,
                               AuthenticationType = string.IsNullOrEmpty(item.Attribute("authenticationType").Value) ? string.Empty : item.Attribute("authenticationType").Value
                           };
            if (v1Source.Count() == 0)
                throw new ConfigurationErrorsException("Missing V1SourceConnection information in application config file.");
            else
                V1SourceConnection = v1Source.First();


            // **********************************
            // * V1 target connection.
            // **********************************
            var v1Target = from item in xmlDoc.Descendants("V1TargetConnection")
                           select new ConnectionInfo
                           {
                               Url = item.Element("Url").Value,
                               Username = string.IsNullOrEmpty(item.Element("Username").Value) ? string.Empty : item.Element("Username").Value,
                               Password = string.IsNullOrEmpty(item.Element("Password").Value) ? string.Empty : item.Element("Password").Value,
                               Project = string.IsNullOrEmpty(item.Element("Project").Value) ? string.Empty : item.Element("Project").Value,
                               AuthenticationType = string.IsNullOrEmpty(item.Attribute("authenticationType").Value) ? string.Empty : item.Attribute("authenticationType").Value
                           };
            if (v1Target.Count() == 0)
                throw new ConfigurationErrorsException("Missing V1TargetConnection information in application config file.");
            else
                V1TargetConnection = v1Target.First();

            // **********************************
            // * V1 staging database.
            // **********************************
            var v1Database = from item in xmlDoc.Descendants("V1StagingDatabase")
                             select new StagingDatabaseInfo
                             {
                               Server = item.Element("Server").Value,
                               Database = item.Element("Database").Value,
                               Username = string.IsNullOrEmpty(item.Element("Username").Value) ? string.Empty : item.Element("Username").Value,
                               Password = string.IsNullOrEmpty(item.Element("Password").Value) ? string.Empty : item.Element("Password").Value,
                               TrustedConnection = System.Convert.ToBoolean(item.Attribute("trustedConnection").Value)
                             };
            if (v1Database.Count() == 0)
                throw new ConfigurationErrorsException("Missing V1StagingDatabase information in application config file.");
            else
                V1StagingDatabase = v1Database.First();

            // **********************************
            // * Rally source connection.
            // **********************************
            var rallySource = from item in xmlDoc.Descendants("RallySourceConnection")
                              select new RallyConnectionInfo
                              {
                                Url = item.Element("url").Value,
                                Username = string.IsNullOrEmpty(item.Element("username").Value) ? string.Empty : item.Element("username").Value,
                                Password = string.IsNullOrEmpty(item.Element("password").Value) ? string.Empty : item.Element("password").Value,
                                ExportFileDirectory = item.Element("exportFileDirectory").Value,
                                UserExportFilePrefix = item.Element("userExportFilePrefix").Value,
                                ProjectExportFilePrefix = item.Element("projectExportFilePrefix").Value,
                                ReleaseExportFilePrefix = item.Element("releaseExportFilePrefix").Value,
                                IterationExportFilePrefix = item.Element("iterationExportFilePrefix").Value,
                                EpicExportFilePrefix = item.Element("epicExportFilePrefix").Value,
                                StoryExportFilePrefix = item.Element("storyExportFilePrefix").Value,
                                DefectExportFilePrefix = item.Element("defectExportFilePrefix").Value,
                                TaskExportFilePrefix = item.Element("taskExportFilePrefix").Value,
                                TestExportFilePrefix = item.Element("testExportFilePrefix").Value,
                                RegressionTestExportFilePrefix = item.Element("regressiontestExportFilePrefix").Value,
                                TestStepExportFilePrefix = item.Element("teststepExportFilePrefix").Value,
                                ConversationExportFilePrefix = item.Element("conversationExportFilePrefix").Value,
                                OrphanedTestProject = item.Element("orphanedTestProject").Value
                              };
            RallySourceConnection = rallySource.First();

            // **********************************
            // * Jira Configuration.
            // **********************************
            var jiraConfig = from item in xmlDoc.Descendants("JiraConfiguration")
                           select new JiraConfigurationInfo
                           {
                               XmlFileName = item.Element("xmlFileName").Value,
                               ProjectName = item.Element("projectName").Value,
                               ProjectDescription = item.Element("projectDescription").Value,
                               DefaultSchedule = item.Element("defaultSchedule").Value,
                               JiraUrl = item.Element("jiraUrl").Value
                           };
            if (jiraConfig.Count() == 0)
                throw new ConfigurationErrorsException("Missing JiraConfiguration information in application config file.");
            else
                JiraConfiguration = jiraConfig.First();


            // **********************************
            // * General configurations.
            // **********************************
            var v1Config = from item in xmlDoc.Descendants("configurations")
                           select new ConfigurationInfo
                           {
                               SourceConnectionToUse = item.Element("sourceConnectionToUse").Value,
                               PerformExport = System.Convert.ToBoolean(item.Element("performExport").Value),
                               PerformImport = System.Convert.ToBoolean(item.Element("performImport").Value),
                               PerformClose = System.Convert.ToBoolean(item.Element("performClose").Value),
                               PerformCleanup = System.Convert.ToBoolean(item.Element("performCleanup").Value),
                               MigrateTemplates = System.Convert.ToBoolean(item.Element("migrateTemplates").Value),
                               MigrateAttachmentBinaries = System.Convert.ToBoolean(item.Element("migrateAttachmentBinaries").Value),
                               MigrateProjectMembership = System.Convert.ToBoolean(item.Element("migrateProjectMembership").Value),
                               MigrateDuplicateSchedules = System.Convert.ToBoolean(item.Element("migrateDuplicateSchedules").Value),
                               UseNPIMasking = System.Convert.ToBoolean(item.Element("useNPIMasking").Value),
                               MergeRootProjects = System.Convert.ToBoolean(item.Element("mergeRootProjects").Value),
                               AddV1IDToTitles = System.Convert.ToBoolean(item.Element("addV1IDToTitles").Value),
                               PageSize = System.Convert.ToInt32(item.Element("pageSize").Value),
                               CustomV1IDField = string.IsNullOrEmpty(item.Element("customV1IDField").Value) ? string.Empty : item.Element("customV1IDField").Value,
                               ImportAttachmentsAsLinksURL = string.IsNullOrEmpty(item.Element("importAttachmentsAsLinksURL").Value) ? string.Empty : item.Element("importAttachmentsAsLinksURL").Value,
                               SetAllMembershipToRoot = System.Convert.ToBoolean(item.Element("setAllMembershipToRoot").Value),
                               SourceListTypeValue = string.IsNullOrEmpty(item.Element("sourceListTypeValue").Value) ? string.Empty : item.Element("sourceListTypeValue").Value,
                               MigrateUnauthoredConversationsAsAdmin = System.Convert.ToBoolean(item.Element("migrateUnauthoredConversationsAsAdmin").Value),
                               LogExceptions = System.Convert.ToBoolean(item.Element("logExceptions").Value)
                           };
            if (v1Config.Count() == 0)
                throw new ConfigurationErrorsException("Missing general configuration information in application config file.");
            else
                V1Configurations = v1Config.First();

            // **********************************
            // * Assets to migrate.
            // **********************************
            var assetData = from item in xmlDoc.Descendants("asset")
                            select item;
            if (assetData.Count() == 0)
                throw new ConfigurationErrorsException("Missing assets to migrate information in application config file.");

            foreach (var asset in assetData)
            {
                AssetInfo assetInfo = new AssetInfo();
                assetInfo.Name = asset.Attribute("name").Value;
                assetInfo.InternalName = asset.Attribute("internalName").Value;
                assetInfo.Enabled = System.Convert.ToBoolean(asset.Attribute("enabled").Value);
                assetInfo.DuplicateCheckField = asset.Attribute("duplicateCheckField").Value;
                assetInfo.EnableCustomFields = System.Convert.ToBoolean(asset.Attribute("enableCustomFields").Value);
                AssetsToMigrate.Add(assetInfo);
            }

            // **********************************
            // * List types to migrate.
            // **********************************
            var listTypeData = from item in xmlDoc.Descendants("listType")
                               select item;
            if (listTypeData.Count() == 0)
                throw new ConfigurationErrorsException("Missing list types to migrate information in application config file.");

            foreach (var listType in listTypeData)
            {
                ListTypeInfo listTypeInfo = new ListTypeInfo();
                listTypeInfo.Name = listType.Attribute("name").Value;
                listTypeInfo.Enabled = System.Convert.ToBoolean(listType.Attribute("enabled").Value);
                ListTypesToMigrate.Add(listTypeInfo);
            }

            // **********************************
            // * Custom fields to migrate.
            // **********************************
            var customFieldData = from item in xmlDoc.Descendants("customField")
                                  select item;
            if (customFieldData.Count() == 0)
                throw new ConfigurationErrorsException("Missing custom fields to migrate information in application config file.");

            foreach (var customField in customFieldData)
            {
                CustomFieldInfo customFieldInfo = new CustomFieldInfo();
                customFieldInfo.SourceName = customField.Attribute("sourceName").Value;
                customFieldInfo.TargetName = customField.Attribute("targetName").Value;
                customFieldInfo.AssetType = customField.Attribute("assetType").Value;
                customFieldInfo.DataType = customField.Attribute("dataType").Value;
                customFieldInfo.RelationName = customField.Attribute("relationName").Value;
                CustomFieldsToMigrate.Add(customFieldInfo);
            }

        }
    }
}

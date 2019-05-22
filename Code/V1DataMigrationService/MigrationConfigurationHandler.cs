using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using V1DataCore;

namespace V1DataMigrationService
{
    public class ConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            return new MigrationConfiguration(section);
        }
    }
}

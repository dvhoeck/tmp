using FluentNHibernate.Automapping;
using Gatewing.ProductionTools.BLL;
using System;

namespace Gatewing.ProductionTools.DAL
{
    public class AutomappingConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(Type type)
        {
            return type.GetInterface(typeof(DomainObject).FullName) != null;
        }
    }
}
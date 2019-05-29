using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class OrderLineMap : ClassMap<OrderLine>
    {
        public OrderLineMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.EditedBy);
            Map(x => x.PickingBOM);
            Map(x => x.SalesOrder);
            Map(x => x.ClientCode);
            Map(x => x.ClientRef);
            Map(x => x.ClientName);
            Map(x => x.OrderCreationDate);
            Map(x => x.OrderLineNr);
            Map(x => x.PartNr);
            Map(x => x.PartName);
            Map(x => x.Quantity);
            Map(x => x.StockLevel);
            Map(x => x.StockLocation);
            Map(x => x.SalesOrderType);
            Map(x => x.ExpectedDeliveryDate);
            Map(x => x.CommercialBOM);
            Map(x => x.ShipToAddressHeader);
            Map(x => x.ShipToAddressStreet);
            Map(x => x.ShipToAddressStreetExtended);
            Map(x => x.ShipToAddressPostalCode);
            Map(x => x.ShipToAddressCity);
            Map(x => x.ShipToAddressRegion);
            Map(x => x.ShipToAddressCountry);
            Map(x => x.UoM);
            Map(x => x.InputRequirement);
            Map(x => x.FieldInput);

            Table("SageOrderLine");
        }
    }

    public class PickingMap : ClassMap<Picking>
    {
        public PickingMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.EditedBy);
            Map(x => x.PartName);
            Map(x => x.PartNr);
            Map(x => x.Quantity);
            Map(x => x.StockLevel);
            Map(x => x.StockLocation);
            Map(x => x.UoM);

            Table("SagePicking");
        }
    }

    public class ShippingPreparationMap : ClassMap<ShippingPreparation>
    {
        public ShippingPreparationMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.CustomerCode);
            Map(x => x.CustomerName);
            Map(x => x.CustomerRef);
            Map(x => x.IsActive);
            Map(x => x.IsComplete);
            Map(x => x.BOMNr);
            Map(x => x.StartDateTime).Nullable();
            Map(x => x.EndDateTime).Nullable();
            HasMany(x => x.ShippingPreparationOrderLines).Not.OptimisticLock().Inverse().AsBag().Not.LazyLoad();

            Table("SageShippingPreparation");
        }
    }
}
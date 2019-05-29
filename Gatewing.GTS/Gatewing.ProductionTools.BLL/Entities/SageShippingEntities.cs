using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class OrderLine : DomainObject
    {
        public OrderLine()
        {
        }

        public OrderLine(SqlDataReader oReader, string userName)
        {
            PickingBOM = oReader["PickingBOM"].ToString();
            SalesOrder = oReader["SalesOrder"].ToString();
            ClientCode = oReader["ClientCode"].ToString();
            ClientRef = oReader["ClientRef"].ToString();
            ClientName = oReader["ClientName"].ToString();
            OrderCreationDate = DateTime.Parse(oReader["OrderCreationDate"].ToString(), null);
            OrderLineNr = int.Parse(oReader["OrderLineNr"].ToString());
            PartNr = oReader["PartNr"].ToString();
            PartName = oReader["PartName"].ToString();
            Quantity = float.Parse(oReader["Quantity"].ToString());
            var strStockLevel = oReader["StockLevel"].ToString();
            StockLevel = string.IsNullOrEmpty(strStockLevel) ? -999 : float.Parse(oReader["StockLevel"].ToString());
            StockLocation = oReader["StockLocation"].ToString();
            SalesOrderType = oReader["SalesOrderType"].ToString();
            ExpectedDeliveryDate = oReader["ExpectedDeliveryDate"].ToString();
            CommercialBOM = oReader["CommercialBOM"].ToString();
            ShipToAddressHeader = oReader["ShipToAddressHeader"].ToString();
            ShipToAddressStreet = oReader["ShipToAddressStreet"].ToString();
            ShipToAddressStreetExtended = oReader["ShipToAddressStreetExtended"].ToString();
            ShipToAddressPostalCode = oReader["ShipToAddressPostalCode"].ToString();
            ShipToAddressCity = oReader["ShipToAddressCity"].ToString();
            ShipToAddressRegion = oReader["ShipToAddressRegion"].ToString();
            ShipToAddressCountry = oReader["ShipToAddressCountry"].ToString();
            UoM = oReader["UoM"].ToString();
            InputRequirement = oReader["InputRequirement"].ToString();
            FieldInput = "";
            EditedBy = userName;
        }

        public virtual string ClientCode { get; set; }
        public virtual string ClientName { get; set; }
        public virtual string ClientRef { get; set; }
        public virtual string ColorTag { get; set; }
        public virtual string CommercialBOM { get; set; }
        public virtual bool DoIndent { get; set; }
        public virtual string ExpectedDeliveryDate { get; set; }
        public virtual string FieldInput { get; set; }
        public virtual string IdMask { get; set; }
        public virtual string InputRequirement { get; set; }
        public virtual DateTime OrderCreationDate { get; set; }
        public virtual int OrderLineNr { get; set; }
        public virtual string PartName { get; set; }
        public virtual string PartNr { get; set; }
        public virtual string PickingBOM { get; set; }
        public virtual float Quantity { get; set; }
        public virtual string SalesOrder { get; set; }
        public virtual string SalesOrderType { get; set; }
        public virtual string ShipToAddressCity { get; set; }
        public virtual string ShipToAddressCountry { get; set; }
        public virtual string ShipToAddressHeader { get; set; }
        public virtual string ShipToAddressPostalCode { get; set; }
        public virtual string ShipToAddressRegion { get; set; }
        public virtual string ShipToAddressStreet { get; set; }
        public virtual string ShipToAddressStreetExtended { get; set; }
        public virtual float StockLevel { get; set; }
        public virtual string StockLocation { get; set; }
        public virtual string UoM { get; set; }
    }

    public class Picking : DomainObject
    {
        public Picking()
        {
        }

        public Picking(OrderLine packagingDTO)
        {
            Quantity = packagingDTO.Quantity;
            StockLocation = packagingDTO.StockLocation;
            PartNr = packagingDTO.PartNr;
            PartName = packagingDTO.PartName;
            StockLevel = packagingDTO.StockLevel;
            UoM = packagingDTO.UoM;
            EditedBy = packagingDTO.EditedBy;
        }

        public virtual bool IsPicked { get; set; }
        public virtual string PartName { get; set; }
        public virtual string PartNr { get; set; }
        public virtual float Quantity { get; set; }
        public virtual float StockLevel { get; set; }
        public virtual string StockLocation { get; set; }
        public virtual string UoM { get; set; }
    }

    public class ShippingBoxJnstance : ShippingBoxTemplate
    {
        public virtual float WeightInKg { get; set; }
    }

    public class ShippingBoxTemplate : DomainObject
    {
        public virtual float Depth { get; set; }
        public virtual float Height { get; set; }
        public virtual string Name { get; set; }
        public virtual float Width { get; set; }
    }

    public class ShippingPreparation : DomainObject
    {
        public virtual string BOMNr { get; set; }
        public virtual string CustomerCode { get; set; }
        public virtual string CustomerName { get; set; }
        public virtual string CustomerRef { get; set; }
        public virtual DateTime? EndDateTime { get; set; }
        public virtual bool IsActive { get; set; }
        public virtual bool IsComplete { get; set; }
        public virtual string SalesOrder { get; set; }
        public virtual IList<OrderLine> ShippingPreparationOrderLines { get; set; }
        public virtual DateTime? StartDateTime { get; set; }
    }
}
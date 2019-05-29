using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.ExtensionMethods;
using NHibernate.Criterion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Gatewing.ProductionTools.GTS.Controllers
{
    /// <summary>
    /// Controller to prepare shipments
    /// </summary>
    /// <seealso cref="Gatewing.ProductionTools.GTS.Controllers.BaseController" />
    [Authorize]
    public class ShippingController : BaseController
    {
        private const string SAGE_CONN_STR = "Data Source=BEG-EU-APP-03\\SAGE100;Initial Catalog=GATEWING;integrated security=false;persist security info=True;User ID=gtsUser;Password=1gWRmFwCI3Wk;";
        private const string SAGE_SQL_STR = "Select* from GW_ShippingPrep sp ORDER BY sp.PickingBOM, sp.OrderLineNr";

        private Dictionary<int, string> _colorTable = new Dictionary<int, string> {
            { 0, "#cc00cc" },
            { 1, "#ff0099" },
            { 2, "#ff3333" },
            { 3, "#ff9900" },
            { 4, "#cccc00" },
            { 5, "#99ff00" }
        };

        [AuthorizeUsersInRole("Administrators")]
        public ActionResult InitShippingManagement()
        {
            _logger.LogInfo("User {0} attempts to initialize data for the shipping management interface", User.Identity.Name);

            try
            {
                // 1. RETRIEVE SHIPPING PREPARATIONS
                var bomListFromSageDB = new List<string>();
                // do an old-school Sql Cmd approach
                var connection = new SqlConnection(SAGE_CONN_STR);
                using (connection)
                {
                    var SqlCmd = new SqlCommand(SAGE_SQL_STR, connection);

                    connection.Open();

                    using (SqlDataReader oReader = SqlCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            var currentPickingBom = oReader["PickingBOM"].ToString();
                            bomListFromSageDB.Add(currentPickingBom);
                        }
                    }

                    connection.Close();
                }

                throw new InvalidOperationException("Rule below this always closes shipments => not good");
                bomListFromSageDB.ForEach(bom => PersistAndStartStopShippingPreparation(false, bom));

                // get all shipping preps found in our database
                var orderCriteria = UnitOfWorkSession.CreateCriteria<ShippingPreparation>();
                var savedPreparations = orderCriteria.List<ShippingPreparation>().ToList();

                // 2. RETRIEVE SHIPPING BOXES
                var boxCriteria = UnitOfWorkSession.CreateCriteria<ShippingBoxTemplate>();
                var savedBoxes = boxCriteria.List<ShippingBoxTemplate>().ToList();

                // 3. RETRIEVE A LIST OF PARTNRS AND ASSOCIATED MODEL NAMES
                var partNrCritera = UnitOfWorkSession.CreateCriteria<PartNumber>();
                var partNrs = partNrCritera.List<PartNumber>().ToList().Select(partNr => new { Number = partNr.Number, ProductModelId = partNr.ProductModel.Id }).ToList();

                var result = new
                {
                    ShippingPreparations = savedPreparations,
                    ShippingBoxes = savedBoxes,
                    PartNumbers = partNrs
                };

                return SerializeForWeb(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Starts the stop shipping preparation.
        /// </summary>
        /// <param name="doStart">if set to <c>true</c> [do start].</param>
        /// <param name="bomNr">The bom nr.</param>
        /// <returns></returns>
        public ActionResult PersistAndStartStopShippingPreparation(bool doStart, string bomNr)
        {
            _logger.LogDebug("User {0} attempts to set the Shipping Preparation with Bom Nr {2}'s IsActive property to {1}.", User.Identity.Name, doStart, bomNr);
            try
            {
                var newGuid = Guid.NewGuid();
                var shippingPreparation = UnitOfWorkSession.CreateCriteria<ShippingPreparation>()
                    .Add(Expression.Eq("BOMNr", bomNr)).UniqueResult<ShippingPreparation>()
                    ?? new ShippingPreparation // if retrieval fails, create a new shippingPreparation
                    {
                        Id = newGuid,
                        BOMNr = bomNr,
                        EditedBy = User.Identity.Name,
                        StartDateTime = DateTime.Now,
                    };

                shippingPreparation.IsActive = doStart;

                if (newGuid == shippingPreparation.Id)
                {
                    _logger.LogInfo("Shipping preparation did not exist yet, creating new ShippingPreparation entity with id {0}", newGuid);
                    GTSDataRepository.Create<ShippingPreparation>(shippingPreparation);
                }
                else
                {
                    GTSDataRepository.Update<ShippingPreparation>(shippingPreparation);
                }

                _logger.LogInfo("User {0} has successfully set the Shipping Preparation with Bom Nr {2}'s IsActive property to {1}.", User.Identity.Name, doStart, bomNr);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        // GET: Shipping
        /// <summary>
        /// Prepares the shipping.
        /// </summary>
        /// <returns></returns>
        public ActionResult PrepareShipping()
        {
            return View();
        }

        /// <summary>
        /// Prepares the shipping json.
        /// </summary>
        /// <returns></returns>
        public ActionResult PrepareShippingJSON()
        {
            _logger.LogDebug("User {0} chose to display the Shipping Preparation Interface.", User.Identity.Name);

            try
            {
                // create lists for picking, packaging and validations
                var packagingList = new List<OrderLine>();
                var pickingList = new List<Picking>();
                var bomList = new List<dynamic>();
                var validationMessages = new List<ShippingPreparationValidationMessage>();

                // retrieve shipping GTS meta data from the database
                var activeShippingPreparations = getActiveShippingPreparations();

                // do an old-school Sql Cmd approach
                var connection = new SqlConnection(SAGE_CONN_STR);
                using (connection)
                {
                    var SqlCmd = new SqlCommand(SAGE_SQL_STR, connection);

                    connection.Open();

                    using (SqlDataReader oReader = SqlCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            Console.WriteLine(oReader.ToString());

                            // create a list of BOM nrs
                            bomList.Add(
                                new
                                {
                                    BomNr = oReader["PickingBOM"].ToString(),
                                    IsActive = activeShippingPreparations.Where(x => x.BOMNr == oReader["PickingBOM"].ToString()).Count() > 0
                                });

                            // filter out data for non-activated Shippings (activation starts / stops are done in this page as well)
                            if (activeShippingPreparations.Where(x => x.BOMNr == oReader["PickingBOM"].ToString()).Count() == 0)
                                continue;

                            // Use query result row to cast to internal DTO classes
                            var packagingDTO = new OrderLine(oReader, User.Identity.Name);
                            packagingList.Add(packagingDTO);
                        }

                        connection.Close();
                    }
                }

                if (packagingList.Count > 0)
                {
                    // ensure ClientRef and SalesOrder are found on every line
                    var clientRef = packagingList.Where(order => !string.IsNullOrEmpty(order.SalesOrder)).First().ClientRef;
                    var salesOrder = packagingList.Where(order => !string.IsNullOrEmpty(order.SalesOrder)).First().SalesOrder;
                    var colorTagIndex = 0;
                    var isCommercialBom = false;
                    packagingList.ForEach(order =>
                    {
                        if (order.CommercialBOM == "O")
                            isCommercialBom = true;

                        if (string.IsNullOrEmpty(order.CommercialBOM))
                            isCommercialBom = false;

                        if (!isCommercialBom)
                            colorTagIndex = (colorTagIndex + 1) % 6;
                        else if (order.CommercialBOM != "O")
                            order.DoIndent = true;

                        if (!string.IsNullOrEmpty(order.PartNr))
                            order.ColorTag = _colorTable[colorTagIndex];

                        order.ClientRef = clientRef;
                        order.SalesOrder = salesOrder;
                    });
                }

                // when we're done creating an picking list, regroup the orderlist according to BOM nr
                var tempOrderList = new List<OrderLine>();
                packagingList.OrderBy(x => x.PickingBOM).ToList().ForEach(order =>
                {
                    var currentorderQuantity = order.Quantity;
                    if (currentorderQuantity > 1 && order.UoM == "PCE")
                        for (var c = 0; c < currentorderQuantity; c++)
                        {
                            //order.Quantity = 1;
                            tempOrderList.Add(order);
                        }
                    else
                        tempOrderList.Add(order);
                });

                var groupedOrderList = tempOrderList.GroupBy(order => order.PickingBOM);

                // create a picking list from the list of orders made in the previous step
                packagingList.ToList().ForEach(packagingDTO =>
                {
                    var existingItemInPickingList = pickingList.Where(item => item.PartNr == packagingDTO.PartNr).SingleOrDefault();

                    if (existingItemInPickingList == null)
                    {
                        existingItemInPickingList = new Picking(packagingDTO);
                        existingItemInPickingList.Quantity = 0; // set quantity to null, we will recalculate this below

                        // create a fake stock location if needed
                        if (string.IsNullOrEmpty(packagingDTO.StockLocation) && packagingDTO.StockLevel > 0 && !string.IsNullOrEmpty(packagingDTO.UoM))
                            existingItemInPickingList.StockLocation = "";

                        pickingList.Add(existingItemInPickingList);
                    }

                    if (/*packagingDTO.UoM == "PCE" && */packagingDTO.Quantity > 0)
                    {
                        existingItemInPickingList.Quantity += packagingDTO.Quantity;
                    }
                });

                // check stock levels to create a picking issue summary
                ShippingPreparationValidationMessage shippingPrepValidationMessage = null;
                pickingList.ForEach(shippingDTO =>
                {
                    if (ShippingPreparationValidationMessage.GetShippingPreparationValidationMessage(shippingDTO.Quantity, shippingDTO.StockLevel, shippingDTO.PartNr, out shippingPrepValidationMessage)
                            == false)
                        validationMessages.Add(shippingPrepValidationMessage);
                });

                _logger.LogInfo("User {0} successfully requested Shipping Preparation data.", User.Identity.Name);

                assignIdMasks(tempOrderList);

                // try to retrieve existing orderLines with the BOM from the database
                var searchCriteria = UnitOfWorkSession.CreateCriteria<OrderLine>()
                    .Add(Expression.Eq("PickingBOM", packagingList.Count > 0 ? packagingList[0]?.PickingBOM : ""));

                // put the results in a list
                var existingPackagingEntities = searchCriteria.List<OrderLine>().ToList();

                // for every collection of order lines with the same BOM
                foreach (var item in packagingList)
                {
                    // iterate over all current oderlines and use the previously created list to check if the current line
                    // is already in the database and create it, if not
                    var potentiallyExistingOrderLines = existingPackagingEntities.Where(entity => entity.OrderLineNr == item.OrderLineNr);
                    if (potentiallyExistingOrderLines.Count() > 1)
                        throw new InvalidOperationException(string.Format("More then 1 row was found in the database for the current Picking BOM {0} and Order Line Nr {1}", item.PickingBOM, item.OrderLineNr));

                    if (potentiallyExistingOrderLines.Count() == 0)
                    {
                        item.Id = Guid.NewGuid();
                        item.EditedBy = User.Identity.Name;
                        GTSDataRepository.Create<OrderLine>(item);
                    }
                    else // at this point we can assume there's only orderline found in the database, retrieve any input value if possible
                    {
                        item.FieldInput = potentiallyExistingOrderLines.First().FieldInput;
                    }
                }

                return SerializeForWeb(new
                {
                    Picking = pickingList,
                    Packing = groupedOrderList,
                    BomList = bomList.Distinct().ToList(),
                    ValidationMessages = validationMessages,
                    TotalToPick = pickingList.Where(picking => !string.IsNullOrEmpty(picking.StockLocation)).DistinctBy(picking => picking.PartNr).Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult ShippingManagement()
        {
            return View();
        }

        /// <summary>
        /// Updates an order line.
        /// </summary>
        /// <param name="orderLineJSON">The order line json.</param>
        /// <returns></returns>
        public ActionResult UpdateOrderLine(string orderLineJSON)
        {
            _logger.LogInfo("User {0} attempts to update an order line in the shipping management interface.", User.Identity.Name);

            try
            {
                var orderLine = new JavaScriptSerializer().Deserialize<OrderLine>(orderLineJSON);

                var existingOrderLine = UnitOfWorkSession.CreateCriteria<OrderLine>()
                    .Add(Expression.Eq("PickingBOM", orderLine.PickingBOM))
                    .Add(Expression.Eq("OrderLineNr", orderLine.OrderLineNr)).List<OrderLine>().ToList().Single();

                // check if input was required but not given
                //if (string.IsNullOrEmpty(orderLine.FieldInput) && orderLine.InputRequirement == "SERIE")
                //    throw new InvalidOperationException(string.Format("The orderline {0} for BOM {1} expects input which wasn't provided.", orderLine.OrderLineNr, orderLine.PickingBOM));

                // check if part nr is associated with a product model
                var productModelBaseId = GetProductModelBaseIdByPartNumber(orderLine.PartNr);

                // check if an actual physical product exists
                var assembly = GTSDataRepository.GetByProductSerial<ProductAssembly>(orderLine.FieldInput, productModelBaseId);
                if (assembly == null)
                    throw new InvalidOperationException("Could not find assembly with serial: " + orderLine.FieldInput);

                // check if associated assembly has any remarks
                if (assembly.RemarkSymptoms.Where(remark => remark.Resolved == false).Count() > 0)
                    throw new InvalidOperationException(string.Format("Assembly with serial {0} has open remarks and cannot be shipped", assembly.ProductSerial));

                // check if associated assembly is "Delivered"
                if (assembly.ProductModelState.Name.ToLower() != "delivered")
                    throw new InvalidOperationException(string.Format("Assembly with serial {0} does not have the state 'Delivered'", assembly.ProductSerial));

                // update id and EditedBy
                existingOrderLine.FieldInput = orderLine.FieldInput;
                existingOrderLine.EditedBy = User.Identity.Name;

                // update existing order line in the database
                GTSDataRepository.Update<OrderLine>(existingOrderLine);

                return SerializeForWeb(orderLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Assigns the identifier masks to an order line.
        /// </summary>
        /// <param name="packaging">The packaging.</param>
        private void assignIdMasks(List<OrderLine> packaging)
        {
            var partNumbers = packaging.Select(pack => pack.PartNr).Distinct().ToList();
            var partNumberDictionary = getPartNumberData(partNumbers);

            packaging.ForEach(item =>
            {
                var partNumber = partNumberDictionary.Where(number => number.Key == item.PartNr).SingleOrDefault();
                if (partNumber.Key != null && partNumber.Value != null)
                    item.IdMask = partNumber.Value;
            });
        }

        /// <summary>
        /// Retrieves the active shipping preparations.
        /// </summary>
        /// <returns></returns>
        private List<ShippingPreparation> getActiveShippingPreparations()
        {
            return UnitOfWorkSession.CreateCriteria<ShippingPreparation>()
                    .Add(Expression.Eq("IsActive", true))
                    .List<ShippingPreparation>().ToList();
        }

        /// <summary>
        /// Retrieves part number data.
        /// </summary>
        /// <param name="partNumbersForShipping">The part numbers for shipping.</param>
        /// <returns></returns>
        private Dictionary<string, string> getPartNumberData(List<string> partNumbersForShipping)
        {
            var searchCriteria = UnitOfWorkSession.CreateCriteria<PartNumber>()
                .Add(Expression.In("Number", partNumbersForShipping))
                .CreateCriteria("ProductModel")
                    .Add(Expression.Like("IsReleased", true))
                    .Add(Expression.Like("IsArchived", false));

            ///var hits = searchCriteria.List<PartNumber>().Select(item => new { Number = item.Number, IdMask = item.ProductModel.IdMask }).ToList().ToDictionary<dynamic, string>(item => item.Number);
            var result = new Dictionary<string, string>();

            searchCriteria.List<PartNumber>().ToList().ForEach(partNumber =>
            {
                result.Add(partNumber.Number, partNumber.ProductModel.IdMask);
            });

            return result;
        }

        /// <summary>
        /// Retrieves a product model by its part number.
        /// </summary>
        /// <param name="partNr">The part nr.</param>
        /// <returns></returns>
        private Guid GetProductModelBaseIdByPartNumber(string partNr)
        {
            var searchCriteria = UnitOfWorkSession.CreateCriteria<PartNumber>()
                .Add(Expression.Eq("Number", partNr));

            var results = searchCriteria.List<PartNumber>();

            if (results == null || results.Count == 0)
                throw new InvalidOperationException("Could not find product model for part number :" + partNr);

            if (results.ToList<PartNumber>().DistinctBy(currentPartNumber => currentPartNumber.ProductModel.BaseModelId).Count() > 1)
                throw new InvalidOperationException("Found multiple distinct models for part number :" + partNr);

            return results[0].ProductModel.BaseModelId;
        }
    }

    /// <summary>
    ///
    /// </summary>

    /// <summary>
    ///
    /// </summary>
    internal class ShippingPreparationValidationMessage
    {
        public string Message { get; set; }

        public string PartNr { get; set; }

        internal static bool GetShippingPreparationValidationMessage(float quantity, float stockLevel, string partNr, out ShippingPreparationValidationMessage shippingPreparationValidationMessage)
        {
            var newInstance = new ShippingPreparationValidationMessage();
            newInstance.PartNr = partNr;

            var valid = true;
            if ((quantity >= 0 && stockLevel >= 0) && (quantity > stockLevel))
            {
                newInstance.Message = string.Format("The stock level for part with nr {0} is too low. Ordered quantity: {1}, actual stock level: {2}", partNr, quantity, stockLevel);
                valid = false;
            }

            shippingPreparationValidationMessage = newInstance;

            return valid;
        }
    }

    /*

    SELECT TOP (100) PERCENT dbo.F_DOCLIGNE.DO_Piece AS PickingBOM, dbo.F_DOCLIGNE.DL_PieceBC AS SalesOrder, dbo.F_DOCLIGNE.CT_Num AS ClientCode,
                  dbo.F_DOCLIGNE.DO_Ref AS ClientRef, SAGE100GP_GATEWING.dbo.T_CLIENT.LIBELLE_CLIENT AS ClientName, dbo.F_DOCLIGNE.DO_Date AS OrderCreationDate,
                  dbo.F_DOCLIGNE.DL_Ligne AS OrderLineNr, dbo.F_DOCLIGNE.AR_Ref AS PartNr, dbo.F_DOCLIGNE.DL_Design AS PartName, dbo.F_DOCLIGNE.DL_Qte AS Quantity,
                  SAGE100GP_GATEWING.dbo.T_UNITE.LIBELLE_UNITE AS UoM, dbo.DP_STOCKS.STO_QTE AS StockLevel,
                  SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.EMPLACEMENT AS StockLocation, dbo.F_DOCLIGNE.CA_Num AS SalesOrderType,
                  dbo.F_DOCLIGNE.DO_DateLivr AS ExpectedDeliveryDate, SAGE100GP_GATEWING.dbo.T_ARTICLE.NOMENCLATURE_COMMERCIALE AS CommercialBOM,
                  dbo.F_LIVRAISON.LI_Intitule AS ShipToAddressHeader, dbo.F_LIVRAISON.LI_Adresse AS ShipToAddressStreet,
                  dbo.F_LIVRAISON.LI_Complement AS ShipToAddressStreetExtended, dbo.F_LIVRAISON.LI_CodePostal AS ShipToAddressPostalCode,
                  dbo.F_LIVRAISON.LI_Ville AS ShipToAddressCity, dbo.F_LIVRAISON.LI_CodeRegion AS ShipToAddressRegion, dbo.F_LIVRAISON.LI_Pays AS ShipToAddressCountry,
                  SAGE100GP_GATEWING.dbo.T_ARTICLE.CODE_GESTION_LS AS InputRequirement
    FROM     dbo.F_DOCLIGNE LEFT OUTER JOIN
                  dbo.F_LOTSERIE ON dbo.F_DOCLIGNE.DL_No = dbo.F_LOTSERIE.DL_NoOut LEFT OUTER JOIN
                  SAGE100GP_GATEWING.dbo.T_CLIENT ON SAGE100GP_GATEWING.dbo.T_CLIENT.CODE_CLIENT = dbo.F_DOCLIGNE.CT_Num LEFT OUTER JOIN
                  SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT ON SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.REF_ARTICLE = dbo.F_DOCLIGNE.AR_Ref LEFT OUTER JOIN
                  SAGE100GP_GATEWING.dbo.T_ARTICLE ON SAGE100GP_GATEWING.dbo.T_ARTICLE.REF_ARTICLE = dbo.F_DOCLIGNE.AR_Ref LEFT OUTER JOIN
                  dbo.DP_STOCKS ON dbo.F_DOCLIGNE.AR_Ref = dbo.DP_STOCKS.STO_ART_NUM INNER JOIN
                  dbo.F_DOCENTETE ON dbo.F_DOCENTETE.DO_Piece = dbo.F_DOCLIGNE.DO_Piece INNER JOIN
                  dbo.F_LIVRAISON ON dbo.F_LIVRAISON.LI_No = dbo.F_DOCENTETE.LI_No LEFT OUTER JOIN
                  SAGE100GP_GATEWING.dbo.T_UNITE ON SAGE100GP_GATEWING.dbo.T_ARTICLE.UNITE = SAGE100GP_GATEWING.dbo.T_UNITE.CODE_UNITE
    WHERE  (dbo.F_DOCLIGNE.DO_Domaine = 0) AND (dbo.F_DOCLIGNE.DO_Type = 2) AND (SAGE100GP_GATEWING.dbo.T_CLIENT.CODE_SOCIETE = 1) AND
                  (SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.CODE_DEPOT = 1 OR
                  SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.CODE_DEPOT IS NULL) AND (SAGE100GP_GATEWING.dbo.T_ARTICLE.CODE_SOCIETE = 1 OR
                  SAGE100GP_GATEWING.dbo.T_ARTICLE.CODE_SOCIETE IS NULL) AND (SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.CODE_SOCIETE = 1 OR
                  SAGE100GP_GATEWING.dbo.T_ARTICLE_DEPOT.CODE_SOCIETE IS NULL) AND (dbo.DP_STOCKS.STO_DEP_NUM = 1 OR
                  dbo.DP_STOCKS.STO_DEP_NUM IS NULL) AND (dbo.F_DOCLIGNE.DO_Type = 2) AND (SAGE100GP_GATEWING.dbo.T_UNITE.CODE_SOCIETE = 1 OR
                  SAGE100GP_GATEWING.dbo.T_UNITE.CODE_SOCIETE IS NULL)
    ORDER BY 'PickingBOM', 'OrderLineNr'

    */
}